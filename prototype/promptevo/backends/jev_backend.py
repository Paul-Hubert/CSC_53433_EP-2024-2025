"""JEV-9B decision backend (rev. 2026-10-08): a distilled decision model instead of a chat LLM.

autotrust/JEV-9B = a frozen Qwen3.5-9B backbone + "System 1": a LoRA adapter and a 24-slot
decision head that answer typed questions in ONE forward pass, no text generated. The
prithivMLmods/JEV-9B-GGUF files are the backbone only (no adapter, no head): plain Qwen3.5-9B.

Served by vLLM (docker/compose.yaml, address `jev.host`): the LoRA module `jev-decision`
(adapter_vllm/: backbone LoRA + the head as an lm_head LoRA). One /v1/completions request per
decision with max_tokens 1, allowed_token_ids = the option letters, logprobs returned; then
+ head bias (decision_head.json), / T_choice (calibration.json), softmax over the options.

Request (model card, template "bare-v1"), kind `choice`:
    [kind] choice
    [state] <the species prompt: world rules, genes, situation; without the {ask} line>
    [question] Which action does this animal take now?
    [options]
    A) eat
    B) flee ...
    [decision]:
Options always in the species' action order (the model changes 11.5 % of 16-option answers
when only the order changes, model card).
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np

from ..cache import KVCache, make_key
from ..obs_text import render
from ..species import SPECIES
from .base import Query, batch_actions
from .ollama_policy import _genes_block

LETTERS = "ABCDEFGHIJKLMNOP"
QUESTION = {"prey": "Which action does this animal take now?",
            "predator": "Which action does this predator take now?"}


def jev_text(kind: str, state: str, question: str, options) -> str:
    """The System 1 input string (bare-v1), exactly as in the model card's `decide`."""
    lines = list(options) if kind != "choice" else [f"{LETTERS[i]}) {o}" for i, o in enumerate(options)]
    return f"[kind] {kind}\n[state] {state}\n[question] {question}\n[options]\n" + "\n".join(lines) + "\n[decision]:"


def jev_state(template: str, genes: dict, situation: str, actions) -> str:
    """The species prompt with genes and situation filled in and the answer instruction left out."""
    return template.format(genes=_genes_block(genes, actions), situation=situation, ask="").strip()


def option_probs(z, temperature: float) -> np.ndarray:
    """Softmax of the options' decision logits (log-prob + head bias) at the calibrated temperature."""
    x = np.asarray(z, dtype=float) / temperature
    e = np.exp(x - x.max())
    return e / e.sum()


class JevModel:
    """JEV System 1 behind a vLLM server. `choice_logits(texts, n)` -> [len(texts), n] decision
    logits before the temperature (log-probs + head bias; vLLM's normaliser cancels in the softmax)."""

    def __init__(self, client, repo: str = "autotrust/JEV-9B", revision: str | None = None,
                 served: str = "jev-decision", max_tokens: int = 1024,
                 head: dict | None = None, temps: dict | None = None):
        if head is None or temps is None:
            from huggingface_hub import hf_hub_download
            get = lambda f: json.loads(Path(hf_hub_download(repo, f, revision=revision)).read_text(encoding="utf-8"))
            head = head or get("adapter_vllm/decision_head.json")
            temps = temps or get("calibration.json")["per_kind"]
        self.temps = temps
        self.ranges = {k: tuple(v) for k, v in head["slots"]["ranges"].items()}
        self.bias = [float(x) for x in head["bias"]]
        self.ids = [int(x) for x in head["verbalizer_ids"]]
        self.client, self.served, self.max_tokens = client, served, int(max_tokens)
        self.id = f"{repo}@{(revision or 'main')[:12]}:{client.digest(served)}"
        self.too_long = 0                       # inputs above the model's 1 024-token training length

    def choice_logits(self, texts: list[str], n: int) -> np.ndarray:
        s = self.ranges["choice"][0]
        ids, bias = self.ids[s:s + n], self.bias[s:s + n]
        out = np.empty((len(texts), n))
        for r, text in enumerate(texts):        # one request each; batching later
            lp, n_prompt = self.client.token_logprobs(self.served, text, ids)
            if n_prompt and n_prompt > self.max_tokens:
                self.too_long += 1
            out[r] = [lp.get(t, -1e9) + b for t, b in zip(ids, bias)]
        return out


class JevBackend:
    name = "jev"

    def __init__(self, model, prompt_path: str | Path, predator_prompt_path: str | Path | None = None,
                 style: str = "V1", cache: KVCache | None = None, strict: bool = True):
        self.model, self.style, self.cache, self.strict = model, style, cache, strict
        self.templates: dict[str, str] = {}
        self.prompt_ids: dict[str, str] = {}
        for species, path in (("prey", prompt_path), ("predator", predator_prompt_path)):
            if path is not None:
                text = Path(path).read_text(encoding="utf-8")
                self.templates[species] = text
                self.prompt_ids[species] = f"{Path(path).name}:{hashlib.sha256(text.encode()).hexdigest()[:8]}"
        self.temperature = float(model.temps["choice"])
        self._mem: dict[str, list[float]] = {}
        self.calls = 0              # forward passes (situations the model actually read)

    def text(self, q: Query) -> str:
        if q.species not in self.templates:
            raise ValueError(f"no prompt for {q.species} queries (set policy.predator_prompt)")
        actions = SPECIES[q.species].actions
        state = jev_state(self.templates[q.species], q.genes, render(q.obs, self.style), actions)
        return jev_text("choice", state, QUESTION[q.species], actions)

    def _key(self, q: Query) -> str:
        return make_key("jev", self.model.id, self.prompt_ids.get(q.species), "bare-v1", self.style,
                        q.genome_key, render(q.obs, self.style))

    def _lookup(self, key: str):
        hit = self._mem.get(key)
        if hit is None and self.cache is not None:
            hit = self.cache.get(key)
            if hit is not None:
                self._mem[key] = hit
        return hit

    def decide(self, queries: list[Query]) -> np.ndarray:
        n = len(batch_actions(queries))
        out = np.zeros((len(queries), n))
        todo: dict[str, list[int]] = {}                  # key -> rows (one forward pass per distinct key)
        for i, q in enumerate(queries):
            k = self._key(q)
            hit = self._lookup(k)
            if hit is not None:
                out[i] = hit
            else:
                todo.setdefault(k, []).append(i)
        if not todo:
            return out
        keys = list(todo)
        texts = [self.text(queries[todo[k][0]]) for k in keys]
        z = self.model.choice_logits(texts, n)
        self.calls += len(texts)
        items = []
        for k, row in zip(keys, z):
            p = [float(x) for x in option_probs(row, self.temperature)]
            self._mem[k] = p
            items.append((k, p))
            for i in todo[k]:
                out[i] = p
        if self.cache is not None:
            self.cache.put_many(items)
        return out

    @property
    def too_long(self) -> int:
        return getattr(self.model, "too_long", 0)

    def info(self) -> dict:
        return {"model": self.model.id, "host": getattr(getattr(self.model, "client", None), "host", None), "prompts": self.prompt_ids, "template": "bare-v1",
                "temperature_choice": self.temperature}

    @classmethod
    def from_config(cls, cfg, model=None, **over) -> "JevBackend":
        from ..config import resolve
        jc, pc = cfg.jev, cfg.policy
        if model is None:
            from ..llm.openai_client import openai_client
            model = JevModel(openai_client(jc), jc.repo, jc.get("revision"), served=jc.served_model,
                             max_tokens=int(jc.max_tokens))
        pred = pc.get("predator_prompt")
        return cls(model, resolve(pc.prompt), resolve(pred) if pred else None,
                   style=over.get("style") or cfg.backend.obs_style,
                   cache=KVCache(resolve(cfg.paths.cache_dir) / "jev.sqlite"))
