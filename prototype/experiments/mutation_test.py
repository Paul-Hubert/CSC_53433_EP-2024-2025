"""Mutation test: how random is the LLM mutation, and where do genes go when they are
mutated again and again? Pure mutation, no selection (a mutation-accumulation experiment).

    python -m experiments.mutation_test [--temps 0.9,1.2,1.5,2.0] [--seeds 8] [--steps 30]
                                        [--workers 4] [--model gemma4:12b]

Part 1, variety: every founder sentence x seeds x temperatures, one mutation each. For a
given (sentence, seed index) the instruction and the seed are the same at every temperature.
Part 2, lineages: one founder sentence of six slots, mutated step after step at each
temperature (a rejected answer keeps the sentence: the mutation didn't happen).
Each mutant is also read by the keyword brain (other genes neutral): behaviour distance d.
Writes results/mutation_test.json + .md; progress in logs/mutation_test.progress.json.
"""
from __future__ import annotations

import argparse
import difflib
import json
import threading
import time
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor

import numpy as np

from experiments.make_obs import load_obs
from promptevo import metrics as M
from promptevo.backends.base import Query
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import TEMPLATE, Mutator, clean
from promptevo.founder import AllelePools
from promptevo.genome import AlleleRegistry
from promptevo.llm.ollama_client import client_from_config, make_rewriter
from promptevo.progress import Progress

LINEAGE_LOCI = ("eat", "flee", "follow", "rest", "mate")


def words(t: str) -> list[str]:
    return t.lower().rstrip(".!?").replace(",", " ").replace(":", " ").split()


def compare(old: str, new: str) -> dict:
    """Edit size (words), similarity (0..1), length change and where the edit is (first/middle/last word)."""
    a, b = words(old), words(new)
    ops = [o for o in difflib.SequenceMatcher(None, a, b).get_opcodes() if o[0] != "equal"]
    touched = {min(i, len(a) - 1) for _, i1, i2, _, _ in ops for i in range(i1, max(i2, i1 + 1))}
    where = {"first": 0 in touched, "last": len(a) - 1 in touched,
             "middle": any(0 < i < len(a) - 1 for i in touched)}
    return {"size": sum(max(i2 - i1, j2 - j1) for _, i1, i2, j1, j2 in ops),
            "sim": round(difflib.SequenceMatcher(None, a, b).ratio(), 3), "dlen": len(b) - len(a), **where}


def reason(raw: str, old: str, max_words: int) -> str:
    new = clean(raw)
    n = len(new.split())
    if n == 0:
        return "empty"
    if n > max_words:
        return "too long"
    if new.lower() == old.lower():
        return "unchanged"
    return "characters"


class KeywordJudge:
    """Behaviour distance read by the keyword brain, with every other gene neutral."""

    def __init__(self, reg: AlleleRegistry, pools: AllelePools):
        self.reg, self.pools, self.obs, self.rb = reg, pools, load_obs(), RuleBasedBackend()
        self._p: dict = {}
        self._lock = threading.Lock()

    def probs(self, locus: str, text: str) -> np.ndarray:
        key = (locus, text)
        with self._lock:
            if key not in self._p:
                g = self.pools.neutral_genome().replace(locus, self.reg.add(locus, text, "mutant").id)
                self._p[key] = self.rb.decide([Query(self.reg.genome_key(g), self.reg.genes(g), o) for o in self.obs])
            return self._p[key]

    def d(self, locus: str, a: str, b: str) -> float:
        return round(M.behaviour_distance(self.probs(locus, a), self.probs(locus, b)), 4)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--temps", default="0.9,1.2,1.5,2.0")
    ap.add_argument("--seeds", type=int, default=8)
    ap.add_argument("--steps", type=int, default=30)
    ap.add_argument("--workers", type=int, default=4)
    ap.add_argument("--model", default=None)
    ap.add_argument("--tag", default="mutation_test")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    temps = [float(t) for t in a.temps.split(",")]
    model = a.model or cfg.ollama.mutator_model
    client = client_from_config(cfg)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    judge = KeywordJudge(reg, pools)
    muts = {t: Mutator(cfg, reg, make_rewriter(client, model, temperature=t), model) for t in temps}
    instructions = muts[temps[0]].instructions
    founders = [(l, reg.text(x)) for l in pools.founders for x in pools.founders[l] if reg.get(x).origin == "founder"]
    total = len(founders) * a.seeds * len(temps) + len(LINEAGE_LOCI) * a.steps * len(temps)
    prog = Progress(resolve(cfg.paths.logs_dir), a.tag, total=total)
    done, lock, t0 = [0], threading.Lock(), time.time()

    def tick():
        with lock:
            done[0] += 1
            if done[0] % 20 == 0:
                prog.update(done[0])

    def one(job):
        t, (locus, text), i, s = job
        mut = muts[t]
        new, k, seed = mut.mutate_text(locus, text, np.random.default_rng([i, s]))
        row = {"temp": t, "locus": locus, "parent": text, "seed_index": s, "prompt": k, "seed": seed, "new": new}
        if new is None:          # ask again (a cache hit, no new call) to see why the guard said no
            raw = mut.llm(TEMPLATE.format(instruction=mut.instructions[k], text=text), seed)
            row.update(raw=raw.strip()[:160], reason=reason(raw, text, mut.max_words[locus]))
        else:
            row.update(compare(text, new), d=judge.d(locus, text, new))
        tick()
        return row

    def lineage(job):
        t, li, locus = job
        mut = muts[t]
        rng = np.random.default_rng([100 + li, 7])           # same instructions and seeds at every temperature
        start = cur = reg.text(pools.founders[locus][0])
        steps = []
        for step in range(1, a.steps + 1):
            new, k, seed = mut.mutate_text(locus, cur, rng)
            if new is not None:
                cur = new
            steps.append({"step": step, "prompt": k, "accepted": new is not None, "text": cur,
                          "words": len(cur.split()), "sim_to_start": compare(start, cur)["sim"],
                          "d_to_start": judge.d(locus, start, cur)})
            tick()
        return {"temp": t, "locus": locus, "start": start, "steps": steps}

    jobs = [(t, f, i, s) for t in temps for i, f in enumerate(founders) for s in range(a.seeds)]
    try:
        with ThreadPoolExecutor(a.workers) as ex:
            variety = list(ex.map(one, jobs))
            lineages = list(ex.map(lineage, [(t, li, l) for t in temps for li, l in enumerate(LINEAGE_LOCI)]))
    except Exception as e:
        prog.fail(done[0], repr(e))
        raise
    res = {"model": model, "digest": client.digest(model), "temps": temps, "seeds": a.seeds, "steps": a.steps,
           "instructions": instructions, "variety": variety, "lineages": lineages,
           "elapsed_s": round(time.time() - t0, 1), "date": time.strftime("%Y-%m-%d")}
    res["summary"] = summarise(res)
    out = resolve(cfg.paths.results_dir)
    (out / f"{a.tag}.json").write_text(json.dumps(res, indent=1, ensure_ascii=False), encoding="utf-8")
    lines = report(res)
    (out / f"{a.tag}.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    prog.finish(done[0])
    print("\n".join(lines[:14]))
    print(f"details: {out / (a.tag + '.md')}")


def revisits(lineage: dict) -> int:
    """Accepted steps that return to a sentence the lineage already had (cycles like cold ↔ hot)."""
    seen, n = {lineage["start"]}, 0
    for s in lineage["steps"]:
        if s["accepted"]:
            n += s["text"] in seen
            seen.add(s["text"])
    return n


def summarise(res: dict) -> dict:
    by_t = {}
    for t in res["temps"]:
        rows = [r for r in res["variety"] if r["temp"] == t]
        ok = [r for r in rows if r["new"]]
        distinct = defaultdict(set)
        for r in ok:
            distinct[(r["locus"], r["parent"])].add(r["new"])
        n_parents = len({(r["locus"], r["parent"]) for r in rows})
        lin = [x for x in res["lineages"] if x["temp"] == t]
        fin = [x["steps"][-1]["text"] for x in lin]
        sims = lambda texts: float(np.mean([difflib.SequenceMatcher(None, words(p), words(q)).ratio()
                                            for i, p in enumerate(texts) for q in texts[i + 1:]]))
        by_t[t] = {
            "valid": len(ok) / len(rows), "reasons": dict(Counter(r["reason"] for r in rows if not r["new"])),
            "distinct_per_sentence": sum(len(v) for v in distinct.values()) / n_parents,
            "edit_words": float(np.mean([r["size"] for r in ok])), "one_word": float(np.mean([r["size"] == 1 for r in ok])),
            "big": float(np.mean([r["size"] >= 4 for r in ok])),
            "first": float(np.mean([r["first"] for r in ok])), "middle": float(np.mean([r["middle"] for r in ok])),
            "last": float(np.mean([r["last"] for r in ok])), "dlen": float(np.mean([r["dlen"] for r in ok])),
            "longer3": float(np.mean([r["dlen"] >= 3 for r in ok])), "sim": float(np.mean([r["sim"] for r in ok])),
            "jump": float(np.mean([r["sim"] < 0.3 for r in ok])), "neutral": float(np.mean([r["d"] < 1e-4 for r in ok])),
            "d_mean": float(np.mean([r["d"] for r in ok])), "d_max": float(np.max([r["d"] for r in ok])),
            "lineage_accepted": float(np.mean([s["accepted"] for x in lin for s in x["steps"]])),
            "lineage_words_end": float(np.mean([x["steps"][-1]["words"] for x in lin])),
            "lineage_words_start": float(np.mean([len(x["start"].split()) for x in lin])),
            "lineage_sim_end": float(np.mean([x["steps"][-1]["sim_to_start"] for x in lin])),
            "lineage_revisits": float(np.mean([revisits(x) for x in lin])),
            "finals_alike": sims(fin), "starts_alike": sims([x["start"] for x in lin])}
    per_prompt = {}
    for k, ins in enumerate(res["instructions"]):
        rows = [r for r in res["variety"] if r["prompt"] == k]
        ok = [r for r in rows if r["new"]]
        if rows:
            per_prompt[ins] = {"n": len(rows), "valid": len(ok) / len(rows),
                               "edit_words": float(np.mean([r["size"] for r in ok])) if ok else float("nan"),
                               "dlen": float(np.mean([r["dlen"] for r in ok])) if ok else float("nan"),
                               "jump": float(np.mean([r["sim"] < 0.3 for r in ok])) if ok else float("nan")}
    return {"by_temp": by_t, "per_prompt": per_prompt}


def report(res: dict) -> list[str]:
    S = res["summary"]
    T = res["temps"]
    n_par = len({(r["locus"], r["parent"]) for r in res["variety"]})
    L = [f"# Mutation test — {res['model']} (digest {res['digest']}), {res['date']}", "",
         f"Pure mutation, no selection. Variety: {n_par} founder sentences × {res['seeds']} seeds × "
         f"{len(T)} temperatures. Lineages: {len(LINEAGE_LOCI)} sentences × {res['steps']} steps per temperature. "
         f"{len(res['instructions'])} instructions (`prompts/mutate_v2.txt`). {res['elapsed_s']:.0f} s.", "",
         "| temperature | " + " | ".join(str(t) for t in T) + " |", "|---|" + "---|" * len(T)]
    rows = [("valid answers", "valid", "{:.0%}"), (f"distinct mutants per sentence (of {res['seeds']})", "distinct_per_sentence", "{:.1f}"),
            ("words changed per mutation", "edit_words", "{:.1f}"), ("one-word edits", "one_word", "{:.0%}"),
            ("big edits (≥ 4 words)", "big", "{:.0%}"), ("edit touches the first word", "first", "{:.0%}"),
            ("… a middle word", "middle", "{:.0%}"), ("… the last word", "last", "{:.0%}"),
            ("length change (words)", "dlen", "{:+.2f}"), ("≥ 3 words longer", "longer3", "{:.0%}"),
            ("similarity to parent", "sim", "{:.2f}"), ("jumps (similarity < 0.3)", "jump", "{:.0%}"),
            ("keyword brain: no effect", "neutral", "{:.0%}"), ("keyword brain: mean d", "d_mean", "{:.4f}"),
            ("keyword brain: max d", "d_max", "{:.3f}"), ("lineages: steps accepted", "lineage_accepted", "{:.0%}"),
            ("lineages: words start → end", None, None), ("lineages: similarity to start at the end", "lineage_sim_end", "{:.2f}"),
            ("lineages: returns to an earlier sentence", "lineage_revisits", "{:.1f}"),
            ("lineages: finals alike (starts alike)", None, None)]
    for label, key, fmt in rows:
        if key:
            L.append(f"| {label} | " + " | ".join(fmt.format(S["by_temp"][t][key]) for t in T) + " |")
        elif label.startswith("lineages: words"):
            L.append(f"| {label} | " + " | ".join(f"{S['by_temp'][t]['lineage_words_start']:.1f} → "
                                                   f"{S['by_temp'][t]['lineage_words_end']:.1f}" for t in T) + " |")
        else:
            L.append(f"| {label} | " + " | ".join(f"{S['by_temp'][t]['finals_alike']:.2f} "
                                                   f"({S['by_temp'][t]['starts_alike']:.2f})" for t in T) + " |")
    reasons = {t: S["by_temp"][t]["reasons"] for t in T}
    L += ["", "Rejected answers by reason: " + "; ".join(f"T {t}: {r or 'none'}" for t, r in reasons.items()), "",
          "## Per instruction (all temperatures)", "", "| instruction | n | valid | words changed | length change | jumps |",
          "|---|---|---|---|---|---|"]
    for ins, p in S["per_prompt"].items():
        L.append(f"| {ins} | {p['n']} | {p['valid']:.0%} | {p['edit_words']:.1f} | {p['dlen']:+.1f} | {p['jump']:.0%} |")
    L += ["", "## Lineages (each line: a step where the gene changed)", ""]
    for x in res["lineages"]:
        L.append(f"### {x['locus']}, T {x['temp']}: \"{x['start']}\"")
        L.append("")
        prev = x["start"]
        for s in x["steps"]:
            if s["text"] != prev:
                L.append(f"- {s['step']:>2}. {s['text']}  (d {s['d_to_start']:.3f})")
                prev = s["text"]
        L.append("")
    L += ["## Variety samples (first 3 founder sentences of eat, flee, risk)", ""]
    for locus in ("eat", "flee", "mate"):
        parent = next(r["parent"] for r in res["variety"] if r["locus"] == locus)
        L.append(f"### \"{parent}\"")
        L.append("")
        for t in T:
            outs = [r["new"] or f"✗ {r['reason']}" for r in res["variety"]
                    if r["parent"] == parent and r["temp"] == t]
            L.append(f"- **T {t}:** " + " · ".join(outs))
        L.append("")
    return L


if __name__ == "__main__":
    main()
