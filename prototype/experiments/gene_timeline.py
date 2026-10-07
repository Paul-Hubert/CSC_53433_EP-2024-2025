"""How the genes develop during one run.

    python -m experiments.gene_timeline results/runs/llm_long [--every 500] [--window 2000]
                                        [--tag llm_long] [--drops 500] [--judge] [--species predator]

Reads the run files (also a run still going, or stopped hard) and answers six questions:
1. The gene pool, per checkpoint and slot: distinct genes, effective number (1 / sum of share²),
   the most common gene, mutant share, mutation depth (mutations since a founder text), words per
   gene, share using a word of the animal's world, word overlap between the different texts.
2. Every gene that reached 25 % of its slot: origin, speed, peak, fate, lineage; leader changes.
3. Mutants produced, those that had at least 5 living carriers at once, and those alive at the
   end: instructions, words changed, length, world words; next to the mutation test (no selection).
4. Selection or drift. Gene dropping: genes handed down the real family tree at random (each child
   takes each slot from a random parent, real mutations kept), --drops times, give the share each
   gene would have by inheritance alone. Also fitness relative to contemporaries (gene_report), and
   the families (founders and newcomers) with living descendants over time.
5. Behaviour per window of ticks: action shares, births, deaths by cause, predator kills, lifespan.
6. Meaning: world words, length and overlap over time; with --judge, gemma4:12b says whether each
   gene that had at least 3 carriers at once still gives a usable rule for its slot (cached,
   temperature 0, at most --judge-max new calls).
Writes results/<tag>_timeline.md, .json, .csv (one row per checkpoint and slot) and .html.
--species predator follows the predators' genes instead (predators have genes since 2026-10-07).
"""
from __future__ import annotations

import argparse
import csv
import html
import json
import random
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

from experiments.gene_report import Run
from experiments.gene_report import words as content_words
from experiments.mutation_test import compare
from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import load_instructions
from promptevo.species import SPECIES, species_of_locus

SWEEP = 0.25        # a gene "swept" when it reached this share of its slot at a checkpoint
SPREAD = 5          # a mutant "spread" when it had this many living carriers at once
RARE = 0.05         # charts: genes that never reached this share of their slot share one grey band
JUDGE_MIN = 3       # --judge asks only about texts that had this many living carriers at once
JUDGE_PROMPT = "prompts/judge_sense_v1.txt"
TOPIC = {"eat": "when and how to eat", "flee": "when to run away", "follow": "when to follow other animals",
         "rest": "when to rest", "mate": "when to look for a partner",
         "predator.hunt": "when and how to hunt prey", "predator.rest": "when to rest",
         "predator.mate": "when to look for a partner"}


def mean(xs) -> float | None:
    xs = list(xs)
    return float(np.mean(xs)) if xs else None


# --- genes ---------------------------------------------------------------------------------
class Genes:
    """Per gene: mutant or not, depth (mutations since a founder text), family (the founder text
    it descends from), instruction, edit from its parent, words, world words."""

    def __init__(self, run: Run, instructions: list[str], per_prompt: dict | None):
        self.run, self.al = run, run.alleles
        self.depth, self.root, self._edit = {}, {}, {}
        for aid in self.al:
            self._walk(aid)
        self.kind = {}                          # instruction index -> small | medium | big
        for k, ins in enumerate(instructions):
            w = (per_prompt or {}).get(ins, {}).get("edit_words")
            self.kind[k] = None if w is None else "small" if w <= 2 else "big" if w >= 4 else "medium"

    def _walk(self, aid: str) -> None:
        chain, cur = [], aid
        while cur not in self.depth:
            a = self.al.get(cur)
            if a is None or a["origin"] != "mutant" or not a.get("parent_id"):
                self.depth[cur], self.root[cur] = 0, cur
                break
            chain.append(cur)
            cur = a["parent_id"]
        d, r = self.depth[cur], self.root[cur]
        for x in reversed(chain):
            d += 1
            self.depth[x], self.root[x] = d, r

    def text(self, aid: str) -> str:
        return self.al[aid]["text"]

    def mutant(self, aid: str) -> bool:
        return self.al[aid]["origin"] == "mutant"

    def instruction(self, aid: str) -> int | None:
        op = self.al[aid].get("operator") or ""
        return int(op[4:]) if op.startswith("llm#") else None

    def edit(self, aid: str) -> dict | None:
        """Words changed and similarity to the parent text (mutation_test.compare)."""
        if aid not in self._edit:
            p = self.al[aid].get("parent_id")
            self._edit[aid] = compare(self.text(p), self.text(aid)) if self.mutant(aid) and p in self.al else None
        return self._edit[aid]

    def nwords(self, aid: str) -> int:
        return len(self.text(aid).split())

    def world(self, aid: str) -> bool:
        return bool(content_words(self.text(aid)) & self.run.world)


# --- one pass over the events --------------------------------------------------------------
def checkpoints(run: Run, every: int):
    """Yield (t, event) for every event and (t, None) at ticks 0, every, 2·every, ... and the end.
    A checkpoint at t includes the events of tick t (same as gene_report.frequencies)."""
    nxt, end = 0, run.summary["ticks"]
    for e in run.events:
        while e["t"] > nxt:
            yield nxt, None
            nxt += every
        yield e["t"], e
    while nxt < end:
        yield nxt, None
        nxt += every
    yield end, None


def scan(run: Run, every: int) -> dict:
    """Living animals at each checkpoint; each gene's first tick and most living carriers at once;
    generations; families (founders and newcomers) with living descendants, the families that are
    ancestors of every living animal, and each family's expected share of the genes at the end."""
    alive, gen, count, peak, first = {}, {}, Counter(), Counter(), {}
    root_of, roots = {}, []                 # family index per founder/newcomer; (id, kind, tick)
    desc, contrib = {}, {}                  # living animal -> bitset of families; {family: share}
    snaps = []
    for t, e in checkpoints(run, every):
        if e is None:
            if snaps and snaps[-1]["t"] == t:
                continue
            union, inter = 0, None
            for i in alive:
                union |= desc[i]
                inter = desc[i] if inter is None else inter & desc[i]
            snaps.append({"t": t, "ids": list(alive), "families": bin(union).count("1"),
                          "common": [r for r in range(len(roots)) if inter and inter >> r & 1]})
            continue
        k = e["kind"]
        if k in ("founder", "immigrant", "birth"):
            i = e["id"]
            alive[i], gen[i] = e["genome"], e.get("gen", 0)
            for aid in e["genome"]:
                count[aid] += 1
                peak[aid] = max(peak[aid], count[aid])
                first.setdefault(aid, e["t"])
            if k == "birth":
                ps = [p for p in e["parents"] if p in desc]
                desc[i] = 0
                for p in ps:
                    desc[i] |= desc[p]
                share = defaultdict(float)
                for p in ps:
                    for r, v in contrib[p].items():
                        share[r] += v / len(ps)
                contrib[i] = dict(share)
            else:
                root_of[i] = len(roots)
                roots.append({"id": i, "kind": "founder" if k == "founder" else "newcomer", "t": e["t"]})
                desc[i], contrib[i] = 1 << root_of[i], {root_of[i]: 1.0}
        elif k == "death" and e["id"] in alive:
            for aid in alive.pop(e["id"]):
                count[aid] -= 1
            desc.pop(e["id"], None)
            contrib.pop(e["id"], None)
    total = defaultdict(float)
    for i in alive:
        for r, v in contrib[i].items():
            total[r] += v / max(1, len(alive))
    return {"snaps": snaps, "gen": gen, "peak": peak, "first": first, "roots": roots, "contrib": dict(total)}


def gene_drop(run: Run, every: int, targets: list[str], drops: int, seed: int = 0, start: int = 0,
              maxima: dict | None = None) -> dict:
    """{gene: [{t, share, expected, p_hi, p_lo}]}: its real share of the living animals, the mean share
    over `drops` worlds where genes went down the same family tree at random, and the chance of a
    share at least (p_hi) or at most (p_lo) as high by inheritance alone.
    start: genes go down at random only from that tick on (before it, every animal has its real
    genome), to test a hypothesis made at that tick on what came after.
    maxima: if given, receives {"code": {gene: column}, "max": (drops, genes) highest share of each
    gene in each world}, to count the sweeps that inheritance alone gives on this family tree."""
    rng = np.random.default_rng(seed)
    code: dict[str, int] = {}
    c = lambda aid: code.setdefault(aid, len(code))
    tg = [(run.loci.index(run.alleles[a]["locus"]), c(a), a) for a in targets]
    arr, out, last_t = {}, {a: [] for a in targets}, None
    M = np.zeros((drops, 0))
    as_is = lambda g: np.tile(np.array([c(a) for a in g], np.int32), (drops, 1))    # the real genome
    for t, e in checkpoints(run, every):
        if e is None:
            if not arr or t == last_t:
                continue
            last_t = t
            ids = list(arr)
            stack = np.stack([arr[i] for i in ids])                     # (animals, drops, slots)
            if maxima is not None:
                K = len(code)
                M = np.pad(M, ((0, 0), (0, K - M.shape[1])))
                for li in range(len(run.loci)):
                    idx = (stack[:, :, li] + K * np.arange(drops)[None, :]).ravel()
                    share = np.bincount(idx, minlength=drops * K).reshape(drops, K) / len(ids)
                    np.maximum(M, share, out=M)
            for li, cc, a in tg:
                sims = (stack[:, :, li] == cc).mean(axis=0)
                real = float(np.mean([run.genome[i][li] == a for i in ids]))
                out[a].append({"t": t, "share": real, "expected": float(sims.mean()),
                               "p_hi": float((sims >= real - 1e-9).mean()), "p_lo": float((sims <= real + 1e-9).mean())})
            continue
        k = e["kind"]
        if k in ("founder", "immigrant") or (k == "birth" and e["t"] < start):
            arr[e["id"]] = as_is(e["genome"])
        elif k == "birth":
            ps = [arr[p] if p in arr else as_is(run.genome[p]) for p in e["parents"]]
            child = np.where(rng.random(ps[0].shape) < 0.5, ps[0], ps[1]) if len(ps) == 2 else ps[0].copy()
            for m in e.get("mutations", []):
                child[:, run.loci.index(m["locus"])] = c(m["child"])
            arr[e["id"]] = child
        elif k == "death":
            arr.pop(e["id"], None)
    if maxima is not None:
        maxima.update(code=dict(code), max=np.pad(M, ((0, 0), (0, len(code) - M.shape[1]))))
    return out


def null_sweeps(run: Run, g: Genes, sh: dict, mx: dict) -> dict:
    """Sweeps in reality against the random-inheritance worlds of gene_drop(maxima=...): how many
    mutants reached 50 % and 90 % of their slot, and how many founder texts reached 90 %. Unlike a
    P-value of a gene picked because it swept, these counts are not biased by the picking."""
    code, M = mx["code"], mx["max"]
    top = {a: max(v.values()) for a, v in sh.items()}
    out = {}
    for name, pick, q in (("mutants that reached 50 %", g.mutant, 0.5),
                          ("mutants that reached 90 %", g.mutant, 0.9),
                          ("founder texts that reached 90 %", lambda a: not g.mutant(a), 0.9)):
        cols = [j for a, j in code.items() if a in g.al and pick(a)]
        null = (M[:, cols] >= q - 1e-9).sum(axis=1)
        real = sum(1 for a, m in top.items() if pick(a) and m >= q - 1e-9)
        out[name] = {"real": real, "median": float(np.median(null)), "lo": float(np.percentile(null, 5)),
                     "hi": float(np.percentile(null, 95)), "p_hi": float((null >= real).mean())}
    return out


# --- the six questions -----------------------------------------------------------------------
def pool(run: Run, g: Genes, sc: dict, sense: dict) -> list[dict]:
    """Question 1 (and 6): one row per checkpoint and slot."""
    rows, jac = [], {}

    def overlap(a: str, b: str) -> float | None:
        if (a, b) not in jac:
            wa, wb = content_words(g.text(a)), content_words(g.text(b))
            jac[(a, b)] = len(wa & wb) / len(wa | wb) if wa | wb else None
        return jac[(a, b)]
    for s in sc["snaps"]:
        ids, pop = s["ids"], len(s["ids"])
        gens = [sc["gen"][i] for i in ids]
        for li, locus in enumerate(run.loci):
            n = Counter(run.genome[i][li] for i in ids)
            row = {"t": s["t"], "slot": locus, "pop": pop, "mean_gen": mean(gens), "max_gen": max(gens, default=0),
                   "families": s["families"], "distinct": len(n)}
            if pop:
                lead, k = sorted(n.items(), key=lambda kv: (-kv[1], kv[0]))[0]
                pairs = [(n[a] * n[b], overlap(a, b)) for a in n for b in n if a < b]
                pairs = [(w, j) for w, j in pairs if j is not None]
                judged = [(m, sense[a]) for a, m in n.items() if a in sense]
                row.update({
                    "effective": 1 / sum((m / pop) ** 2 for m in n.values()), "leader": lead,
                    "leader_text": g.text(lead), "leader_share": k / pop,
                    "mutant_share": sum(m for a, m in n.items() if g.mutant(a)) / pop,
                    "depth": sum(m * g.depth[a] for a, m in n.items()) / pop,
                    "words": sum(m * g.nwords(a) for a, m in n.items()) / pop,
                    "world": sum(m for a, m in n.items() if g.world(a)) / pop,
                    "overlap": sum(w * j for w, j in pairs) / sum(w for w, _ in pairs) if pairs else None,
                    "sense": sum(m for m, ok in judged if ok) / sum(m for m, _ in judged) if judged else None})
            rows.append(row)
    return rows


def shares(run: Run, sc: dict) -> dict:
    """{gene: {t: share of its slot}} at every checkpoint (absent = 0)."""
    out = defaultdict(dict)
    for s in sc["snaps"]:
        pop = len(s["ids"]) or 1
        for li in range(len(run.loci)):
            for aid, m in Counter(run.genome[i][li] for i in s["ids"]).items():
                out[aid][s["t"]] = m / pop
    return out


def sweeps(g: Genes, sc: dict, sh: dict, instructions: list[str]) -> list[dict]:
    """Question 2: every gene that reached SWEEP of its slot at a checkpoint."""
    ts = [s["t"] for s in sc["snaps"]]
    out = []
    for aid, by_t in sh.items():
        if max(by_t.values()) < SWEEP:
            continue
        traj = [by_t.get(t, 0.0) for t in ts]
        first = sc["first"][aid]
        reach = {q: next((t for t, v in zip(ts, traj) if v >= q), None) for q in (0.25, 0.5, 0.9)}
        peak = max(traj)
        last_pos = max(i for i, v in enumerate(traj) if v > 0)
        end = traj[-1]
        fate = ("fixed" if end >= 1 else f"present, {end:.0%}" if end > 0
                else f"lost by t={ts[last_pos + 1]}")
        out.append({"gene": aid, "slot": g.al[aid]["locus"], "text": g.text(aid),
                    "origin": "mutant" if g.mutant(aid) else "founder text", "depth": g.depth[aid],
                    "instruction": (instructions[g.instruction(aid)] if g.instruction(aid) is not None
                                    and g.instruction(aid) < len(instructions) else None),
                    "first": first, "to_25": None if reach[0.25] is None else reach[0.25] - first,
                    "to_50": None if reach[0.5] is None else reach[0.5] - first,
                    "to_90": None if reach[0.9] is None else reach[0.9] - first,
                    "peak": peak, "peak_t": ts[traj.index(peak)],
                    "fixed_t": next((t for t, v in zip(ts, traj) if v >= 1), None), "end": end, "fate": fate,
                    "lineage": g.run.lineage(aid, instructions)})
    return sorted(out, key=lambda x: (g.run.loci.index(x["slot"]), x["first"], x["gene"]))


def leaders(rows: list[dict]) -> dict:
    """{slot: [(t, gene, text, share)]}: the most common gene of each slot, a row when it changes."""
    out = defaultdict(list)
    for r in rows:
        if r.get("leader") and (not out[r["slot"]] or out[r["slot"]][-1][1] != r["leader"]):
            out[r["slot"]].append((r["t"], r["leader"], r["leader_text"], r["leader_share"]))
    return dict(out)


def describe(aids, g: Genes, edit=None, words_of=None, world_of=None, kind_of=None, depth_of=None) -> dict:
    """Question 3: size, instructions, edit, length, world words of a set of mutants."""
    edit = edit or g.edit
    words_of, world_of = words_of or g.nwords, world_of or g.world
    kind_of = kind_of or (lambda a: g.kind.get(g.instruction(a)))
    depth_of = depth_of or (lambda a: g.depth[a])
    aids = [a for a in aids if edit(a)]
    if not aids:
        return {"n": 0}
    return {"n": len(aids), "small": mean(kind_of(a) == "small" for a in aids),
            "big": mean(kind_of(a) == "big" for a in aids),
            "words_changed": mean(edit(a)["size"] for a in aids), "jumps": mean(edit(a)["sim"] < 0.3 for a in aids),
            "words": mean(words_of(a) for a in aids), "world": mean(world_of(a) for a in aids),
            "depth": mean(depth_of(a) for a in aids)}


def mutation_sets(run: Run, g: Genes, sc: dict, mt: dict | None) -> dict:
    produced = [a for a in run.alleles if g.mutant(a)]
    living = lambda s: sorted({a for i in s["ids"] for a in run.genome[i] if g.mutant(a)})
    most = max(sc["snaps"], key=lambda s: sum(g.mutant(a) for i in s["ids"] for a in run.genome[i])
               / max(1, len(s["ids"])))                  # the checkpoint where mutants were most common
    out = {"produced": describe(produced, g),
           "spread": describe([a for a in produced if sc["peak"][a] >= SPREAD], g),
           "most": {**describe(living(most), g), "t": most["t"]},
           "alive": describe(living(sc["snaps"][-1]), g)}
    if mt:                                       # the mutation test: same instructions, no selection
        rows = [r for r in mt["variety"] if r["temp"] == 1.2 and r["new"]]
        out["no_selection"] = describe(
            range(len(rows)), g, edit=lambda i: rows[i], words_of=lambda i: len(rows[i]["new"].split()),
            world_of=lambda i: bool(content_words(rows[i]["new"]) & run.world),
            kind_of=lambda i: g.kind.get(rows[i]["prompt"]), depth_of=lambda i: 1)
    n_mut = sum(len(e.get("mutations", [])) for e in run.events if e["kind"] == "birth")
    births = sum(e["kind"] == "birth" for e in run.events)
    out["per_1000_births"] = 1000 * n_mut / max(1, births)
    out["tried_ok"] = run.summary.get("mutations")
    return out


def behaviour(run: Run, window: int) -> list[dict]:
    """Question 5: per window of ticks, from stats.csv (cumulative counters) and the deaths."""
    if not (run.path / "stats.csv").exists():
        return []
    c = run.col                                                  # "pred_" for the predators' columns
    with (run.path / "stats.csv").open() as f:
        rows = [r for r in csv.DictReader(f)          # a last row cut short by a hard stop has None values
                if (r.get(c + "pop") or "").isdigit() and None not in r.values()]
    if not rows:
        return []
    acts = [k[len(c) + 4:] for k in rows[0] if k.startswith(c + "act_")]   # the run's own actions (since 2026-10-05)
    deaths = [e for e in run.events if e["kind"] == "death"]
    # prey: deaths_pred = prey killed; predators: the same column counts their kills
    cum = [c + "births", c + "immigrants", c + "deaths_starve", "deaths_pred", c + "deaths_age", c + "decisions"] + \
          [f"{c}act_{a}" for a in acts]
    out, prev, prev_t = [], {k: 0 for k in cum}, 0
    last_t = int(rows[-1]["t"])
    ends = list(range(window, last_t + 1, window)) + ([last_t] if last_t % window else [])
    for end in ends:
        r = [x for x in rows if int(x["t"]) <= end][-1]
        win = [x for x in rows if prev_t < int(x["t"]) <= end]
        if not win:
            continue
        d = {k[len(c):] if k.startswith(c) else k: int(r[k]) - int(prev[k]) for k in cum}
        pop = mean(int(x[c + "pop"]) for x in win)
        at = pop * (end - prev_t) if pop else 0
        ages = [x["age"] for x in deaths if prev_t < x["t"] <= end]
        out.append({"from": prev_t, "to": end, "pop": pop, "energy": mean(float(x[c + "mean_energy"]) for x in win),
                    "births_per_1000": 1000 * d["births"] / at if at else None,
                    "kills_per_1000": 1000 * d["deaths_pred"] / at if at else None,
                    "starved_per_1000": 1000 * d["deaths_starve"] / at if at else None,
                    "newcomers": d["immigrants"], "lifespan": mean(ages),
                    "actions": ({a: d[f"act_{a}"] / d["decisions"] for a in acts} if acts and d["decisions"] else None)})
        prev, prev_t = r, end
    return out


def slot_name(locus: str) -> str:
    """The slot as the brain sees it: the action ("predator.hunt" -> "hunt")."""
    try:
        return species_of_locus(locus).action_of(locus)
    except ValueError:                           # slots removed on 2026-10-07
        return locus


def judge(run: Run, aids: list[str], cfg, max_calls: int) -> tuple[dict, int]:
    """Question 6 (optional): {gene: True if gemma4 says it still gives a usable rule for its slot}."""
    from promptevo.llm.ollama_client import client_from_config
    client = client_from_config(cfg)
    template = resolve(JUDGE_PROMPT).read_text(encoding="utf-8")
    schema = {"type": "object", "properties": {"answer": {"type": "string", "enum": ["yes", "no"]}},
              "required": ["answer"]}
    out = {}
    for aid in aids:
        if client.cache.misses >= max_calls:
            break
        a = run.alleles[aid]
        prompt = template.format(slot=slot_name(a["locus"]), topic=TOPIC.get(a["locus"], a["locus"]), text=a["text"])
        r = client.chat_json(cfg.policy.model, [{"role": "user", "content": prompt}], schema,
                             options={"seed": 0, "temperature": 0})
        out[aid] = r.get("answer") == "yes"
    return out, client.cache.misses


# --- report ----------------------------------------------------------------------------------
def rounded(x, d: int = 4):
    """Floats rounded to d decimals, in nested lists and dicts (smaller JSON)."""
    if isinstance(x, float):
        return round(x, d)
    if isinstance(x, dict):
        return {k: rounded(v, d) for k, v in x.items()}
    if isinstance(x, (list, tuple)):
        return [rounded(v, d) for v in x]
    return x


def pct(x, d: int = 0) -> str:
    return "—" if x is None else f"{x:.{d}%}"


def num(x, d: int = 1) -> str:
    return "—" if x is None else f"{x:,.{d}f}".replace(",", " ")


def pick(ts: list[int], n: int = 12) -> list[int]:
    """About n checkpoints, evenly spread, always the first and the last."""
    if len(ts) <= n:
        return ts
    idx = sorted({round(i * (len(ts) - 1) / (n - 1)) for i in range(n)})
    return [ts[i] for i in idx]


def report(run: Run, g: Genes, sc, rows, sw, lead, sets, drop, fit, beh, sense, judged_calls, a,
           tag: str, null: dict | None = None, test: dict | None = None) -> list[str]:
    s = run.summary
    deaths = s.get("deaths", {})
    nd = sum(deaths.values()) or 1
    ts = [x["t"] for x in sc["snaps"]]
    by = {(r["t"], r["slot"]): r for r in rows}
    avg = lambda t, k: mean(by[(t, l)][k] for l in run.loci if by[(t, l)].get(k) is not None)
    L = [f"# How the genes developed — {run.name}", "",
         f"Run: {s['ticks']} ticks, {s.get('max_gen', '?')} generations, {s.get('births', '?')} births, "
         f"{sum(deaths.values())} deaths (predators {deaths.get('predator', 0) / nd:.0%}), "
         f"{sum(len(e.get('mutations', [])) for e in run.events if e['kind'] == 'birth')} mutations, "
         f"brain `{s.get('backend', '?')}`, seed {s.get('seed', '?')}."
         + ("" if run.finished else " **Unfinished run**: read from the events written so far."),
         f"Checkpoints every {a.every} ticks; gene dropping with {a.drops} random inheritances; behaviour per "
         f"{a.window} ticks. Data: `{tag}_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.", "",
         "## 1. The gene pool over time", "",
         "Means over the 10 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: "
         "mutations since a founder text. *World*: share of genes using a word of the animal's world. "
         "*Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words "
         "shared by two animals with different texts in a slot (Jaccard).", "",
         "| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |",
         "|---|---|---|---|---|---|---|---|---|---|---|"]
    for t in pick(ts):
        r0 = by[(t, run.loci[0])]
        L.append(f"| {t} | {r0['pop']} | {num(r0['mean_gen'])} | {r0['families']} | {pct(avg(t, 'mutant_share'))} | "
                 f"{num(avg(t, 'depth'), 2)} | {num(avg(t, 'words'))} | {pct(avg(t, 'world'))} | {num(avg(t, 'distinct'))} | "
                 f"{num(avg(t, 'effective'))} | {num(avg(t, 'overlap'), 2)} |")
    t_end = ts[-1]
    L += ["", f"Slot by slot at the end (tick {t_end}):", "",
          "| slot | distinct | effective | most common gene | share | mutants | depth | world |",
          "|---|---|---|---|---|---|---|---|"]
    for l in run.loci:
        r = by[(t_end, l)]
        if r["pop"]:
            L.append(f"| {l} | {r['distinct']} | {num(r['effective'])} | {r['leader_text']} | {pct(r['leader_share'])} | "
                     f"{pct(r['mutant_share'])} | {num(r['depth'], 2)} | {pct(r['world'])} |")
    # 2
    L += ["", "## 2. Genes that rose and fell", "",
          f"Every gene that reached {SWEEP:.0%} of its slot at a checkpoint. Times are ticks after the gene first "
          "appeared; peak and fate at the checkpoints.", "",
          "| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |", "|---|---|---|---|---|---|---|---|---|"]
    for x in sw:
        L.append(f"| {x['slot']} | {x['text']} | {x['origin']}{'' if x['origin'] != 'mutant' else ', depth ' + str(x['depth'])} | "
                 f"{x['first']} | {x['to_25'] if x['to_25'] is not None else '—'} | {x['to_50'] if x['to_50'] is not None else '—'} | "
                 f"{x['to_90'] if x['to_90'] is not None else '—'} | {x['peak']:.0%} (t={x['peak_t']}) | {x['fate']} |")
    muts = [x for x in sw if x["origin"] == "mutant"]
    if muts:
        L += ["", "Lineages of the mutants that swept:", ""]
        for x in muts:
            L += [f"**{x['slot']}: \"{x['text']}\"** (peak {x['peak']:.0%}, {x['fate']})", "", "```text", *x["lineage"], "```", ""]
    L += ["", "Leader of each slot over time (a row when it changes):", ""]
    for l in run.loci:
        seq = lead.get(l, [])
        fmt = lambda x: f"t={x[0]} \"{x[2]}\" ({x[3]:.0%})"
        shown = [fmt(x) for x in seq]
        if len(seq) > 8:
            shown = shown[:3] + [f"… {len(seq) - 8} more changes …"] + shown[-5:]
        L.append(f"- **{l}** ({len(seq) - 1} changes): " + " → ".join(shown))
    # 3
    L += ["", "## 3. What mutation offers, and what selection keeps", "",
          f"Mutants: all produced; those that had at least {SPREAD} living carriers at once; those alive when "
          "mutants were most common, and at the end. "
          "*Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 "
          "in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its "
          "parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.", "",
          "| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |",
          "|---|---|---|---|---|---|---|---|---|"]
    names = {"produced": "all produced", "spread": f"{SPREAD}+ carriers at once",
             "most": f"alive when mutants were most common (t={sets['most'].get('t')})", "alive": "alive at the end",
             "no_selection": "mutation test (no selection, T = 1.2)"}
    keys = ["produced", "spread", "most"] + (["alive"] if sets["alive"]["n"] != sets["most"]["n"] else []) + ["no_selection"]
    for k in keys:
        d = sets.get(k)
        if d and d["n"]:
            L.append(f"| {names[k]} | {d['n']} | {num(d['depth'])} | {pct(d['small'])} | {pct(d['big'])} | {num(d['words_changed'])} | "
                     f"{pct(d['jumps'])} | {num(d['words'])} | {pct(d['world'])} |")
    tok = sets.get("tried_ok") or {}
    L += ["", f"{num(sets['per_1000_births'], 0)} mutations per 1 000 births"
          + (f"; {tok.get('tried', 0) - tok.get('ok', 0)} answers rejected by the guards." if run.finished and tok else ".")]
    # 4
    L += ["", "## 4. Selection or drift?", "",
          f"Gene dropping: the real family tree, with genes handed down at random ({a.drops} times: each child takes "
          "each slot from a random parent; real mutations kept). It separates two kinds of luck: which families "
          "do well (kept as it happened) and which genes a child gets from its parents (made random).", ""]
    if null:
        L += ["**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in "
              "reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds "
              "with at least as many.", "",
              "| genes | real | inheritance alone | P |", "|---|---|---|---|"]
        L += [f"| {k} | {v['real']} | {v['median']:.0f} ({v['lo']:.0f}–{v['hi']:.0f}) | {v['p_hi']:.3f} |"
              for k, v in null.items()]
        L.append("")
    if test and test["points"]:
        L += [f"**A gene singled out earlier.** \"{test['text']}\" was singled out at tick {test['from']}. Here "
              f"genes go down at random only after that tick, so this tests the idea on what came after.", "",
              "| tick | real share | expected | P(≥) |", "|---|---|---|---|"]
        L += [f"| {v['t']} | {v['share']:.0%} | {v['expected']:.0%} | {v['p_hi']:.3f} |"
              for v in test["points"] if v["t"] in pick([p["t"] for p in test["points"]], 10)]
        L.append("")
    L += ["**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share "
          "of those worlds where it did at least as well as in reality. These genes are listed *because* they "
          "swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to "
          "judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.", "",
          "| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |",
          "|---|---|---|---|---|"]
    for x in sw:
        d = drop.get(x["gene"]) or []
        pk = next((v for v in d if v["t"] == x["peak_t"]), None)
        en = d[-1] if d else None
        f = fit.get(x["gene"])
        fs = f"{f['fitness']:.2f} [{f['lo']:.2f}–{f['hi']:.2f}] ({f['n']})" if f else "—"
        cell = lambda v: "—" if v is None else f"{v['share']:.0%} / {v['expected']:.0%} / {v['p_hi']:.3f}"
        L.append(f"| {x['slot']} | {x['text']} | {cell(pk)} | {cell(en)} | {fs} |")
    common_i = next((i for i, sn in enumerate(sc["snaps"]) if sn["common"]), None)
    common_t = None if common_i is None else sc["snaps"][common_i]["t"]
    stays = common_i is not None and all(sn["common"] for sn in sc["snaps"][common_i:])
    L += ["", "Families: the founders and newcomers whose descendants are still alive.", "",
          "| tick | families with living descendants |", "|---|---|"]
    L += [f"| {sn['t']} | {sn['families']} |" for sn in sc["snaps"] if sn["t"] in pick(ts)]
    L += ["", (f"From tick {common_t}{' on' if stays else ''}, at least one founder or newcomer is an ancestor of every "
               "living animal (family trees mix within a few generations in a small population; genes don't: see "
               "the shares below)." if common_t is not None
               else "No single founder or newcomer is an ancestor of every living animal."),
          "", "Expected share of the living animals' genes coming from each family at the end (top 6):", ""]
    for r, v in sorted(sc["contrib"].items(), key=lambda kv: -kv[1])[:6]:
        ro = sc["roots"][r]
        L.append(f"- {ro['kind']} {ro['id']} (arrived t={ro['t']}): {v:.0%}")
    # 5
    L += ["", "## 5. Behaviour over time", ""]
    if beh:
        acts = list(beh[0]["actions"] or [])
        L += ["| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy |"
              + "".join(f" {x} |" for x in acts),
              "|---|---|---|---|---|---|---|---|" + "---|" * len(acts)]
        step = max(1, len(beh) // 15)
        for w in beh[::step] + ([beh[-1]] if (len(beh) - 1) % step else []):
            L.append(f"| {w['from']}–{w['to']} | {num(w['pop'])} | {num(w['births_per_1000'], 2)} | {num(w['kills_per_1000'], 2)} | "
                     f"{num(w['starved_per_1000'], 2)} | {w['newcomers']} | {num(w['lifespan'], 0)} | {num(w['energy'], 0)} |"
                     + ("".join(f" {pct(w['actions'][x])} |" for x in acts) if w["actions"] else ""))
        L += ["", "Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions"
              + ("." if acts else " are not in this run's stats.csv (runs before 2026-10-05).")]
    # 6
    L += ["", "## 6. Do genes stay meaningful?", "",
          "| tick | world words | words per gene | overlap | judged usable |", "|---|---|---|---|---|"]
    for t in pick(ts):
        L.append(f"| {t} | {pct(avg(t, 'world'))} | {num(avg(t, 'words'))} | {num(avg(t, 'overlap'), 2)} | {pct(avg(t, 'sense'))} |")
    if sense:
        yes = mean(sense.values())
        L += ["", f"Judge: gemma4:12b (`{JUDGE_PROMPT}`, temperature 0) on {len(sense)} genes that had at least "
              f"{JUDGE_MIN} carriers at once ({judged_calls} new calls): {yes:.0%} called usable. *Judged usable* "
              "above is the carrier-weighted share among judged genes. One model's opinion; a sample to check by "
              "hand:", "", "| slot | gene | judge |", "|---|---|---|"]
        rnd = random.Random(0)
        for aid in sorted(rnd.sample(sorted(sense), min(30, len(sense)))):
            L.append(f"| {g.al[aid]['locus']} | {g.text(aid)} | {'usable' if sense[aid] else 'not usable'} |")
    else:
        L += ["", "No judge in this report (run with --judge after the run, when the GPU is free)."]
    return L


# --- html ------------------------------------------------------------------------------------
PAGE = """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Gene timeline</title>
<style>
:root {{ --bg: #faf9f6; --fg: #1d1d1b; --muted: #6b6a64; --line: #d8d5cd; --card: #ffffff; }}
@media (prefers-color-scheme: dark) {{ :root:not([data-theme="light"]) {{ --bg: #151514; --fg: #ebeae6; --muted: #a2a19b; --line: #3a3935; --card: #1e1e1c; }} }}
:root[data-theme="dark"] {{ --bg: #151514; --fg: #ebeae6; --muted: #a2a19b; --line: #3a3935; --card: #1e1e1c; }}
body {{ background: var(--bg); color: var(--fg); font: 15px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif; margin: 0; padding: 16px; }}
main {{ max-width: 1000px; margin: 0 auto; }}
h1 {{ font-size: 1.5rem; margin: 0 0 .25rem; }} h2 {{ font-size: 1.15rem; margin: 2rem 0 .5rem; }}
p.lead, .note {{ color: var(--muted); }}
.grid {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 440px), 1fr)); gap: 14px; }}
.card {{ background: var(--card); border: 1px solid var(--line); border-radius: 8px; padding: 10px 12px; min-width: 0; }}
.card h3 {{ font-size: .95rem; margin: 0 0 4px; }}
svg {{ width: 100%; height: auto; display: block; }}
svg text {{ fill: var(--muted); font-size: 10px; }}
svg .axis {{ stroke: var(--line); }}
ul.legend {{ list-style: none; padding: 0; margin: 6px 0 0; font-size: .85rem; }}
ul.legend li {{ display: flex; gap: 6px; align-items: baseline; }}
.sw {{ display: inline-block; width: 10px; height: 10px; border-radius: 2px; flex: none; }}
.keys {{ display: flex; flex-wrap: wrap; gap: 2px 14px; font-size: .85rem; color: var(--muted); margin-top: 4px; }}
.key {{ display: inline-flex; align-items: center; gap: 5px; }}
.tw {{ overflow-x: auto; }}
table {{ border-collapse: collapse; font-size: .85rem; width: 100%; }}
th, td {{ border-bottom: 1px solid var(--line); padding: 4px 6px; text-align: left; vertical-align: top; }}
</style></head><body><main>
{body}
</main></body></html>
"""


def _x(t, t0, t1, w, pad):
    return pad + (t - t0) / max(1, t1 - t0) * (w - 2 * pad)


def svg_axes(t0, t1, w, h, pad, ylab=("0", "50 %", "100 %")) -> list[str]:
    out = [f'<line class="axis" x1="{pad}" y1="{h - pad}" x2="{w - pad}" y2="{h - pad}"/>']
    for i, lab in enumerate(ylab):
        y = h - pad - i * (h - 2 * pad) / (len(ylab) - 1)
        out.append(f'<text x="2" y="{y + 3:.1f}">{lab}</text>')
    for k in range(5):
        t = t0 + k * (t1 - t0) / 4
        out.append(f'<text x="{_x(t, t0, t1, w, pad) - 10:.1f}" y="{h - 2}">{int(round(t, -2))}</text>')
    return out


def colours(g: Genes, aids: list[str]) -> dict:
    fams = sorted({g.root[a] for a in aids})
    hue = {r: (i * 137.508) % 360 for i, r in enumerate(fams)}
    return {a: f"hsl({hue[g.root[a]]:.0f} 55% {min(80, 38 + 7 * g.depth[a])}%)" for a in aids}


def muller(g: Genes, locus: str, ts: list[int], sh: dict, w=460, h=150, pad=30) -> tuple[str, list]:
    """Stacked shares of the genes of one slot over time; a family's genes sit together, parents first."""
    present = [a for a in sh if g.al[a]["locus"] == locus]
    kids = defaultdict(list)
    for a in g.al:
        p = g.al[a].get("parent_id")
        if g.al[a]["locus"] == locus and g.mutant(a) and p:
            kids[p].append(a)
    first = lambda a: min(sh[a]) if a in sh else 10 ** 12
    order, seen = [], set()

    def dfs(a):
        if a in seen:
            return
        seen.add(a)
        order.append(a)
        for k in sorted(kids.get(a, []), key=lambda k: (first(k), k)):
            dfs(k)
    for r in sorted({g.root[a] for a in present}, key=lambda r: (first(r), r)):
        dfs(r)
    rare = [a for a in order if a in sh and max(sh[a].values()) < RARE]       # one grey band on top
    order = [a for a in order if a in sh and a not in rare]
    col = colours(g, order)
    t0, t1 = ts[0], ts[-1]
    y = lambda v: h - pad - v * (h - 2 * pad)
    parts, base = svg_axes(t0, t1, w, h, pad), [0.0] * len(ts)
    bands = [(a, [sh[a].get(t, 0.0) for t in ts], col[a], f"{g.text(a)} (peak {max(sh[a].values()):.0%})")
             for a in order]
    if rare:
        bands.append((None, [sum(sh[a].get(t, 0.0) for a in rare) for t in ts], "#9a9a94",
                      f"{len(rare)} rare genes (never {RARE:.0%} of the slot)"))
    for _, v, c, label in bands:
        top = [b + x for b, x in zip(base, v)]
        pts = [f"{_x(t, t0, t1, w, pad):.0f},{y(u):.0f}" for t, u in zip(ts, top)]          # whole pixels
        pts += [f"{_x(t, t0, t1, w, pad):.0f},{y(u):.0f}" for t, u in reversed(list(zip(ts, base)))]
        parts.append(f'<polygon points="{" ".join(pts)}" fill="{c}"><title>{html.escape(label)}</title></polygon>')
        base = top
    legend = [(col[a], g.text(a), max(sh[a].values())) for a in order if max(sh[a].values()) >= SWEEP]
    return f'<svg viewBox="0 0 {w} {h}" role="img" aria-label="gene shares in slot {locus}">{"".join(parts)}</svg>', legend


def lines(series: list[tuple[str, list, str]], ymax: float, ylab: tuple, w=460, h=150, pad=30) -> str:
    """Line chart, with its key below (series without data are left out)."""
    series = [(lab, s, col) for lab, s, col in series if any(v is not None for _, v in s)]
    pts_all = [t for _, s, _ in series for t, v in s if v is not None]
    if not pts_all:
        return ""
    t0, t1 = min(pts_all), max(pts_all)
    y = lambda v: h - pad - min(v, ymax) / ymax * (h - 2 * pad)
    parts = svg_axes(t0, t1, w, h, pad, ylab)
    for label, s, col in series:
        p = " ".join(f"{_x(t, t0, t1, w, pad):.1f},{y(v):.1f}" for t, v in s if v is not None)
        parts.append(f'<polyline points="{p}" fill="none" stroke="{col}" stroke-width="1.8"/>')
    keys = "".join(f'<span class="key"><span class="sw" style="background:{col}"></span>{html.escape(label)}</span>'
                   for label, _, col in series)
    return f'<svg viewBox="0 0 {w} {h}" role="img">{"".join(parts)}</svg><div class="keys">{keys}</div>'


def page(run: Run, g: Genes, sc, rows, sh, sw, beh, md_lines, tag: str) -> str:
    ts = [s["t"] for s in sc["snaps"]]
    by = {(r["t"], r["slot"]): r for r in rows}
    avg = lambda t, k: mean(by[(t, l)][k] for l in run.loci if by[(t, l)].get(k) is not None)
    s = run.summary
    body = [f"<h1>How the genes developed: {html.escape(run.name)}</h1>",
            f'<p class="lead">{html.escape(md_lines[2].replace("`", "").replace("**", ""))}</p>',
            "<h2>Gene shares in each slot</h2>",
            '<p class="note">Each band is one gene text; its height is the share of the living animals carrying it. '
            "Genes of one family (descendants of one founder text) share a colour, mutants lighter with each "
            "mutation. Hover a band for its text. Listed: genes that reached 25 %.</p>", '<div class="grid">']
    for l in run.loci:
        svg, legend = muller(g, l, ts, {a: v for a, v in sh.items()})
        items = "".join(f'<li><span class="sw" style="background:{c}"></span><span>{html.escape(t)} '
                        f'<em>({p:.0%})</em></span></li>' for c, t, p in legend)
        body.append(f'<div class="card"><h3>{l}</h3>{svg}<ul class="legend">{items}</ul></div>')
    body += ["</div>", "<h2>Population and genes</h2>", '<div class="grid">']
    pop = [(t, by[(t, run.loci[0])]["pop"]) for t in ts]
    gen = [(t, by[(t, run.loci[0])]["mean_gen"]) for t in ts]
    body.append('<div class="card"><h3>Animals and mean generation</h3>'
                + lines([("animals", pop, "#2a7ab9"), ("mean generation", gen, "#c2571a")],
                        max(max(v for _, v in pop), max((v or 0) for _, v in gen), 1) * 1.1,
                        ("0", "", f"{max(max(v for _, v in pop), max((v or 0) for _, v in gen)) * 1.1:.0f}")) + "</div>")
    body.append('<div class="card"><h3>Mutants, world words, judged usable (mean over slots)</h3>'
                + lines([("mutant genes", [(t, avg(t, "mutant_share")) for t in ts], "#7a3fb0"),
                         ("use a world word", [(t, avg(t, "world")) for t in ts], "#2e8b57"),
                         ("judged usable", [(t, avg(t, "sense")) for t in ts], "#c2571a")], 1.0,
                        ("0", "50 %", "100 %")) + "</div>")
    eff = [(t, avg(t, "effective")) for t in ts]
    dep = [(t, avg(t, "depth")) for t in ts]
    top = max([v or 0 for _, v in eff] + [v or 0 for _, v in dep] + [1]) * 1.1
    body.append('<div class="card"><h3>Effective genes per slot, mutation depth</h3>'
                + lines([("effective number", eff, "#2a7ab9"), ("depth", dep, "#7a3fb0")], top,
                        ("0", "", f"{top:.1f}")) + "</div>")
    body.append("</div>")
    if beh and beh[0]["actions"]:
        body += ["<h2>Behaviour</h2>", '<div class="card"><h3>Share of decisions per action, per window</h3>']
        acol = {"eat": "#2e8b57", "flee": "#c23b22", "follow": "#2a7ab9", "wander": "#9a8c2a",
                "rest": "#7a7a7a", "mate": "#c2571a", "attack": "#7a3fb0"}       # wander, attack: older runs
        series = [(a, [((w["from"] + w["to"]) / 2, w["actions"][a]) for w in beh if w["actions"]], acol[a])
                  for a in beh[0]["actions"]]
        top = max(v for _, s_, _ in series for _, v in s_) * 1.1
        body.append(lines(series, top, ("0", "", f"{top:.0%}"), h=190) + "</div>")
    if sw:
        body += ["<h2>Genes that swept</h2>", '<div class="tw"><table><tr><th>slot</th><th>gene</th><th>origin</th>'
                 "<th>first seen</th><th>peak</th><th>fate</th></tr>"]
        body += [f"<tr><td>{x['slot']}</td><td>{html.escape(x['text'])}</td><td>{x['origin']}</td><td>{x['first']}</td>"
                 f"<td>{x['peak']:.0%} (t={x['peak_t']})</td><td>{x['fate']}</td></tr>" for x in sw]
        body.append("</table></div>")
    body.append(f'<p class="note">Seed {s.get("seed", "?")}, brain {html.escape(str(s.get("backend", "?")))}. '
                f"Full tables: {html.escape(tag)}_timeline.md.</p>")
    return PAGE.format(body="\n".join(body))


# --- main ------------------------------------------------------------------------------------
def build(run_dir: Path, every: int, window: int, drops: int, judge_on: bool = False, judge_max: int = 3000,
          cfg=None, test: str | None = None, test_from: int = 0, species: str = "prey") -> dict:
    cfg = cfg or load_config("small")
    instructions = load_instructions(cfg.evolution.mutation_prompts)
    mt_path = resolve(cfg.paths.results_dir) / "mutation_test.json"
    mt = json.loads(mt_path.read_text(encoding="utf-8")) if mt_path.exists() else None
    run = Run(run_dir, species)
    g = Genes(run, instructions, mt["summary"]["per_prompt"] if mt else None)
    sc = scan(run, every)
    sense, calls = {}, 0
    if judge_on:
        sense, calls = judge(run, sorted(a for a in run.alleles if sc["peak"][a] >= JUDGE_MIN), cfg, judge_max)
    rows = pool(run, g, sc, sense)
    sh = shares(run, sc)
    sw = sweeps(g, sc, sh, instructions)
    mx = {}
    drop = gene_drop(run, every, [x["gene"] for x in sw], drops, maxima=mx)
    hyp = None
    if test:                                  # a gene singled out at tick test_from, tested on what came after
        aid = next(a for a in run.alleles if run.alleles[a]["text"] == test)
        hyp = {"gene": aid, "text": test, "from": test_from,
               "points": [v for v in gene_drop(run, every, [aid], drops, seed=1, start=test_from)[aid]
                          if v["t"] > test_from]}
    fit = run.fitness(min_carriers=1)["alleles"] if run.death else {}
    return {"run": run, "genes": g, "scan": sc, "rows": rows, "shares": sh, "sweeps": sw, "leaders": leaders(rows),
            "sets": mutation_sets(run, g, sc, mt), "drop": drop, "null": null_sweeps(run, g, sh, mx), "test": hyp,
            "fitness": fit, "behaviour": behaviour(run, window), "sense": sense, "judge_calls": calls}


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--every", type=int, default=500, help="ticks between checkpoints")
    ap.add_argument("--window", type=int, default=2000, help="ticks per behaviour window")
    ap.add_argument("--drops", type=int, default=500, help="random inheritances for gene dropping")
    ap.add_argument("--tag", default=None)
    ap.add_argument("--judge", action="store_true", help="ask gemma4:12b whether genes still make sense")
    ap.add_argument("--judge-max", type=int, default=3000, help="most new model calls for --judge")
    ap.add_argument("--test", default=None, help="a gene text singled out earlier, to test on what came after")
    ap.add_argument("--test-from", type=int, default=0, help="tick at which --test was singled out")
    ap.add_argument("--species", default="prey", choices=sorted(SPECIES))
    a = ap.parse_args()
    cfg = load_config("small")
    b = build(resolve(a.run), a.every, a.window, a.drops, a.judge, a.judge_max, cfg, a.test, a.test_from, a.species)
    run = b["run"]
    tag = a.tag or (run.name if a.species == "prey" else f"{run.name}_{a.species}")
    out = resolve(cfg.paths.results_dir)
    md = report(run, b["genes"], b["scan"], b["rows"], b["sweeps"], b["leaders"], b["sets"], b["drop"],
                b["fitness"], b["behaviour"], b["sense"], b["judge_calls"], a, tag, null=b["null"], test=b["test"])
    (out / f"{tag}_timeline.md").write_text("\n".join(md) + "\n", encoding="utf-8")
    cols = ["t", "slot", "pop", "mean_gen", "max_gen", "families", "distinct", "effective", "leader", "leader_text",
            "leader_share", "mutant_share", "depth", "words", "world", "overlap", "sense"]
    with (out / f"{tag}_timeline.csv").open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=cols, extrasaction="ignore")
        w.writeheader()
        w.writerows(b["rows"])
    sc = b["scan"]
    (out / f"{tag}_timeline.json").write_text(json.dumps(rounded({
        "run": run.name, "finished": run.finished, "every": a.every, "window": a.window, "drops": a.drops,
        "summary": run.summary, "checkpoints": [{k: v for k, v in s.items() if k != "ids"} | {"pop": len(s["ids"])}
                                                 for s in sc["snaps"]],
        "sweeps": b["sweeps"], "leaders": b["leaders"], "mutants": b["sets"],
        "gene_drop": {a: {k: [v[k] for v in vs] for k in ("t", "share", "expected", "p_hi", "p_lo")}
                      for a, vs in b["drop"].items()},
        "sweeps_vs_inheritance_alone": b["null"], "singled_out": b["test"],
        "families": {"roots": sc["roots"], "contribution_at_end": sc["contrib"]},
        "behaviour": b["behaviour"], "judge": {"calls": b["judge_calls"], "usable": b["sense"]}}),
        indent=1, ensure_ascii=False, default=float), encoding="utf-8")
    (out / f"{tag}_timeline.html").write_text(page(run, b["genes"], sc, b["rows"], b["shares"], b["sweeps"],
                                                   b["behaviour"], md, tag), encoding="utf-8")
    print("\n".join(md[2:3]))
    for x in b["sweeps"]:
        d = (b["drop"].get(x["gene"]) or [{}])[-1]
        print(f"{x['slot']:<7} {x['peak']:>4.0%} {x['fate']:<16} P(≥)={d.get('p_hi', float('nan')):.3f} {x['text'][:60]}")
    print(f"details: {out / (tag + '_timeline.md')} (+ .csv .json .html)")


if __name__ == "__main__":
    main()
