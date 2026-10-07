"""LLM decision backend (local Ollama or Ollama Cloud) — the default "brain" (doc 05, rev. 2026-09-30).

Both species (rev. 2026-10-07): prey queries use policy.prompt, predator queries
policy.predator_prompt; the answer schema lists the actions of the query's species.

Modes
  points  — one call per (genome, situation): integer points per action → distribution.
  ksample — k calls with different seeds; empirical distribution (costly; for checks).
  logprobs— ONE generated token per decision: the model answers with an action word and
            Ollama's top_logprobs for that token give the distribution. Cheapest mode if the
            installed Ollama returns logprobs (UNVERIFIED — probe S1.3); otherwise it falls
            back to points for that query and counts `logprob_fallbacks`.
  table   — one call per genome for up to K situations at once (default for simulations).
            In a simulation each agent usually misses on ONE situation per step, so the call
            is topped up ("prefetch") with the situations seen most often so far in the run;
            all K answers are cached, so that genome's later decisions mostly hit the cache.
Every (genome, situation) result is also stored in a per-query cache (if given), so a
situation seen in any earlier run or chunk is never asked again.
A failed call (after the client's retries) raises with strict=True (simulations stop
cleanly); otherwise that decision gets a uniform answer, which is never cached.
`TeacherBackend` is kept as an alias: the same class labels data for distillation.
"""
from __future__ import annotations

import hashlib
from collections import Counter
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np

from ..cache import KVCache, make_key
from ..genome import ACTIONS
from ..obs_text import render
from ..species import SPECIES
from .base import Query, batch_actions, normalise


def points_schema(actions=ACTIONS) -> dict:
    return {"type": "object",
            "properties": {a: {"type": "integer", "minimum": 0, "maximum": 100} for a in actions},
            "required": list(actions)}


def choice_schema(actions=ACTIONS) -> dict:
    return {"type": "object",
            "properties": {"action": {"type": "string", "enum": list(actions)}},
            "required": ["action"]}


POINTS_SCHEMA = points_schema()          # prey (names kept for older scripts)
CHOICE_SCHEMA = choice_schema()
ASK_POINTS = "Distribute 100 points across the actions according to how likely this animal is to choose each."
ASK_CHOICE = "Answer with the single action this animal takes now."


def ask_word(actions=ACTIONS) -> str:
    return "Answer with exactly one word, the action this animal takes now: " + ", ".join(actions) + "."


ASK_WORD = ask_word()


def _genes_block(genes: dict, actions=ACTIONS) -> str:
    return "\n".join(f'- {a}: "{genes[a]}"' for a in actions)


def teacher_prompt(template: str, genes: dict, situation: str, mode: str, actions=ACTIONS) -> str:
    ask = {"points": ASK_POINTS, "logprobs": ask_word(actions)}.get(mode, ASK_CHOICE)
    return template.format(genes=_genes_block(genes, actions), situation=situation, ask=ask)


def table_prompt(template: str, genes: dict, situations: list[str], actions=ACTIONS) -> str:
    listing = "\n".join(f"s{i + 1}: {s}" for i, s in enumerate(situations))
    ask = (f"Consider each situation separately. For EACH one ({', '.join(f's{i + 1}' for i in range(len(situations)))}), "
           f"distribute 100 points across the actions according to how likely this animal is to choose each.\n"
           f"{listing}")
    return template.format(genes=_genes_block(genes, actions),
                           situation="several possible situations, listed below.", ask=ask)


def table_schema(n: int, actions=ACTIONS) -> dict:
    keys = [f"s{i + 1}" for i in range(n)]
    return {"type": "object", "properties": {k: points_schema(actions) for k in keys}, "required": keys}


def points_to_probs(r: dict, actions=ACTIONS) -> np.ndarray:
    return normalise([max(0, int(r.get(a, 0))) for a in actions], eps=0.01)


def action_for_token(tok: str, actions=ACTIONS) -> str | None:
    t = tok.strip().lower().strip('"\'.,:;*')
    if not t:
        return None
    hits = [a for a in actions if a.startswith(t) or t.startswith(a)]
    return hits[0] if len(hits) == 1 else None


def logprobs_to_probs(res: dict, min_coverage: float = 0.5, actions=ACTIONS) -> np.ndarray | None:
    """Distribution over actions from the first generated token's top_logprobs, or None."""
    lp = res.get("logprobs") or (res.get("message") or {}).get("logprobs")
    if not isinstance(lp, list) or not lp:
        return None
    first = lp[0]
    tops = first.get("top_logprobs") or [first]
    mass = np.zeros(len(actions))
    for t in tops:
        a = action_for_token(str(t.get("token", "")), actions)
        if a is not None:
            mass[actions.index(a)] += float(np.exp(t.get("logprob", -np.inf)))
    if mass.sum() < min_coverage:
        return None
    return normalise(mass, eps=1e-4)


class LLMPolicyBackend:
    name = "llm"

    def __init__(self, client, model: str, prompt_path: str | Path, mode: str = "points",
                 style: str = "V1", k: int = 8, seed: int = 0, workers: int = 1,
                 keep_alive: str = "30m", strict: bool = False, table_k: int = 8,
                 cache: KVCache | None = None, prefetch: bool = True,
                 predator_prompt_path: str | Path | None = None):
        if mode not in ("points", "ksample", "table", "logprobs"):
            raise ValueError(f"unknown mode {mode}")
        self.client, self.model, self.mode, self.style = client, model, mode, style
        self.templates: dict[str, str] = {}
        self.prompt_ids: dict[str, str] = {}
        for species, path in (("prey", prompt_path), ("predator", predator_prompt_path)):
            if path is not None:
                text = Path(path).read_text(encoding="utf-8")
                self.templates[species] = text
                self.prompt_ids[species] = f"{Path(path).name}:{hashlib.sha256(text.encode()).hexdigest()[:8]}"
        self.template, self.prompt_id = self.templates["prey"], self.prompt_ids["prey"]
        self.k, self.seed, self.workers, self.keep_alive = k, seed, workers, keep_alive
        self.table_k, self.cache, self.prefetch = table_k, cache, prefetch
        self._mem: dict[str, list[float]] = {}   # per-query results of this process
        self._seen: Counter = Counter()          # how often each situation was asked (prefetch order)
        self.prefetched = 0
        self.strict = strict            # True: raise instead of returning a uniform fallback
        self.failures = 0
        self.calls = 0                  # LLM requests actually sent (after caches)
        self.answered = 0               # situations the LLM generated an answer for (≈ output cost)
        self.logprob_fallbacks = 0

    def _template(self, species: str) -> str:
        if species not in self.templates:
            raise ValueError(f"no prompt for {species} queries (set policy.predator_prompt)")
        return self.templates[species]

    # --- single query ----------------------------------------------------------
    def _one(self, q: Query) -> np.ndarray | None:
        """The model's distribution for one situation; None if the call failed (not strict)."""
        actions = SPECIES[q.species].actions
        mode = "points" if self.mode == "table" else self.mode
        if mode == "logprobs":
            p = self._one_logprobs(q)
            if p is not None:
                return p
            self.logprob_fallbacks += 1
            mode = "points"
        prompt = teacher_prompt(self._template(q.species), q.genes, render(q.obs, self.style), mode, actions)
        msgs = [{"role": "user", "content": prompt}]
        try:
            if mode == "points":
                self.calls += 1
                r = self.client.chat_json(self.model, msgs, points_schema(actions), keep_alive=self.keep_alive,
                                          options={"seed": self.seed, "temperature": 0})
                self.answered += 1
                return points_to_probs(r, actions)
            counts = np.zeros(len(actions))
            for i in range(self.k):
                self.calls += 1
                r = self.client.chat_json(self.model, msgs, choice_schema(actions), keep_alive=self.keep_alive,
                                          options={"seed": self.seed + i, "temperature": 0.8})
                if r.get("action") in actions:
                    counts[actions.index(r["action"])] += 1
            return normalise(counts + 1.0)          # add-one smoothing
        except Exception:
            self.failures += 1
            if self.strict:
                raise
            return None

    def _one_logprobs(self, q: Query) -> np.ndarray | None:
        actions = SPECIES[q.species].actions
        prompt = teacher_prompt(self._template(q.species), q.genes, render(q.obs, self.style), "logprobs", actions)
        try:
            self.calls += 1
            res = self.client.chat_raw(self.model, [{"role": "user", "content": prompt}],
                                       options={"seed": self.seed, "temperature": 0, "num_predict": 1},
                                       extra={"logprobs": True, "top_logprobs": 10},
                                       keep_alive=self.keep_alive)
        except Exception:
            return None
        p = logprobs_to_probs(res, actions=actions)
        if p is not None:
            self.answered += 1
        return p

    # --- several situations for one genome (table mode) ----------------------------
    def _table(self, qs: list[Query]) -> list[np.ndarray]:
        actions = SPECIES[qs[0].species].actions
        situations = [render(q.obs, self.style) for q in qs]
        prompt = table_prompt(self._template(qs[0].species), qs[0].genes, situations, actions)
        try:
            self.calls += 1
            r = self.client.chat_json(self.model, [{"role": "user", "content": prompt}],
                                      table_schema(len(qs), actions), keep_alive=self.keep_alive,
                                      options={"seed": self.seed, "temperature": 0})
            self.answered += len(qs)
        except Exception:
            r = {}
        out = []
        for i, q in enumerate(qs):
            entry = r.get(f"s{i + 1}") if isinstance(r, dict) else None
            if isinstance(entry, dict) and sum(max(0, int(entry.get(a, 0) or 0)) for a in actions) > 0:
                out.append(points_to_probs(entry, actions))
            else:
                out.append(self._one(q))            # malformed row: ask for this one alone
        return out

    # --- backend protocol --------------------------------------------------------
    def _key(self, q: Query) -> str:
        if q.species not in self.prompt_ids:
            self._template(q.species)                    # raises: no prompt for this species
        return make_key("policy", self.model, self.client.digest(self.model), self.prompt_ids[q.species],
                        {"table": "points"}.get(self.mode, self.mode), self.style,
                        q.genome_key, render(q.obs, self.style))

    def _lookup(self, key: str):
        hit = self._mem.get(key)
        if hit is None and self.cache is not None:
            hit = self.cache.get(key)
            if hit is not None:
                self._mem[key] = hit
        return hit

    def _store(self, key: str, p) -> None:
        v = [float(x) for x in p]
        self._mem[key] = v
        if self.cache is not None:
            self.cache.put(key, v)

    def _top_up(self, chunk: list[Query]) -> list[Query]:
        """Add up to K-len(chunk) frequent, not-yet-answered situations for this genome."""
        if not self.prefetch or len(chunk) >= self.table_k:
            return chunk
        have = {q.obs for q in chunk}
        extra = []
        for obs, _ in self._seen.most_common():
            if len(chunk) + len(extra) >= self.table_k:
                break
            if obs in have or obs.species != chunk[0].species:
                continue
            q = Query(chunk[0].genome_key, chunk[0].genes, obs, chunk[0].species)
            if self._lookup(self._key(q)) is None:
                extra.append(q)
        self.prefetched += len(extra)
        return chunk + extra

    def decide(self, queries: list[Query]) -> np.ndarray:
        n_actions = len(batch_actions(queries))
        out = np.zeros((len(queries), n_actions))
        todo = []
        for i, q in enumerate(queries):
            self._seen[q.obs] += 1
            hit = self._lookup(self._key(q))
            if hit is not None:
                out[i] = hit
            else:
                todo.append(i)
        if self.mode == "table":
            by_genome: dict[str, list[int]] = {}
            for i in todo:
                by_genome.setdefault(queries[i].genome_key, []).append(i)
            chunks = [idx[s:s + self.table_k] for idx in by_genome.values()
                      for s in range(0, len(idx), self.table_k)]
            jobs = [(idx, self._top_up([queries[i] for i in idx])) for idx in chunks]
            run = lambda job: (job[0], job[1], self._table(job[1]))
        else:
            jobs = [([i], [queries[i]]) for i in todo]
            run = lambda job: (job[0], job[1], [self._one(job[1][0])])
        with ThreadPoolExecutor(max(1, self.workers)) as ex:
            for idx, qs, probs in ex.map(run, jobs):
                for q, p in zip(qs, probs):              # includes prefetched situations
                    if p is not None:                    # a failed call is never remembered
                        self._store(self._key(q), p)
                for i, p in zip(idx, probs):
                    out[i] = np.full(n_actions, 1.0 / n_actions) if p is None else p
        return out

    @classmethod
    def from_config(cls, cfg, client=None, **over) -> "LLMPolicyBackend":
        from ..config import resolve
        from ..llm.ollama_client import client_from_config
        pc = cfg.policy
        model = over.get("model") or pc.model or cfg.ollama.teacher_model
        if not model:
            raise SystemExit("set policy.model in configs/base.yaml (a local or cloud Ollama model)")
        pred = pc.get("predator_prompt")
        return cls(client or client_from_config(cfg), model, resolve(pc.prompt),
                   mode=over.get("mode") or pc.mode, style=over.get("style") or cfg.backend.obs_style,
                   k=int(cfg.ollama.ksample_k), workers=int(over.get("workers") or pc.workers),
                   table_k=int(pc.table_k), strict=bool(over.get("strict", False)),
                   cache=KVCache(resolve(cfg.paths.cache_dir) / "policy.sqlite"),
                   predator_prompt_path=resolve(pred) if pred else None)


class TeacherBackend(LLMPolicyBackend):
    """Same backend, used as the labelling teacher (distillation pipeline, parked)."""
    name = "ollama_policy"
