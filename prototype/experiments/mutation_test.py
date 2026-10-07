"""Mutation test: how random is the LLM mutation, and where do genes go when they are
mutated again and again? Pure mutation, no selection (a mutation-accumulation experiment).

    python -m experiments.mutation_test [--rules current|old] [--temps 0.9,1.2,1.5,2.0] [--seeds 8]
                                        [--steps 30] [--lineages all|first] [--no-judge] [--tries N]
                                        [--workers 4] [--model gemma4:12b] [--tag mutation_test]

--rules: current = the config (since 2026-10-08: small edits of the rule, prompts/mutate_v3.txt,
redraws); old = the mutation of 2026-10-02 to 10-07 (prompts/mutate_v2.txt, one attempt).
Part 1, variety: every founder sentence of both species x seeds x temperatures, one mutation
each (with its redraws). For a given (sentence, seed index) the random draws are the same at
every temperature.
Part 2, lineages: founder sentences mutated step after step at each temperature (a rejected
mutation keeps the sentence): every founder sentence of both species (--lineages all, default)
or the first one of each prey slot (first, as on 2026-10-02).
Meaning: gemma4 judges whether a mutant still gives a usable rule for its slot (the question of
gene_timeline --judge, prompts/judge_sense_v1.txt; single mutations and lineage steps 1, 5, 10,
20, 30), whether it still uses a word of the animal's world, and whether it brings in a new
word from outside it (a word in neither the parent nor the world words: the founder genes'
words and gene_report.SITUATION_WORDS; a measure only, nothing is rejected for it). Prey
mutants are also read by the keyword brain (other genes neutral): behaviour distance d.
Writes results/<tag>.json + .md; progress in logs/<tag>.progress.json.
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

from experiments.gene_report import SITUATION_WORDS
from experiments.gene_report import words as content_words
from experiments.make_obs import load_obs
from promptevo import metrics as M
from promptevo.backends.base import Query
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import Mutator, words
from promptevo.founder import AllelePools
from promptevo.genome import AlleleRegistry
from promptevo.llm.ollama_client import client_from_config, make_rewriter
from promptevo.progress import Progress
from promptevo.species import PREDATOR, PREY

LINEAGE_LOCI = ("eat", "flee", "follow", "rest", "mate")       # --lineages first
OLD_RULES = {"mutation_prompts": "prompts/mutate_v2.txt", "mutation_tries": 1}   # mutation from 2026-10-02 to 2026-10-07
JUDGE_STEPS = (1, 5, 10, 20, 30)


def compare(old: str, new: str) -> dict:
    """Edit size (words), similarity (0..1), length change and where the edit is (first/middle/last word)."""
    a, b = words(old), words(new)
    ops = [o for o in difflib.SequenceMatcher(None, a, b).get_opcodes() if o[0] != "equal"]
    touched = {min(i, len(a) - 1) for _, i1, i2, _, _ in ops for i in range(i1, max(i2, i1 + 1))}
    where = {"first": 0 in touched, "last": len(a) - 1 in touched,
             "middle": any(0 < i < len(a) - 1 for i in touched)}
    return {"size": sum(max(i2 - i1, j2 - j1) for _, i1, i2, j1, j2 in ops),
            "sim": round(difflib.SequenceMatcher(None, a, b).ratio(), 3), "dlen": len(b) - len(a), **where}


class KeywordJudge:
    """Behaviour distance read by the keyword brain, with every other gene neutral (prey genes only)."""

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

    def d(self, locus: str, a: str, b: str) -> float | None:
        if locus not in PREY.loci:
            return None
        return round(M.behaviour_distance(self.probs(locus, a), self.probs(locus, b)), 4)


class SenseJudge:
    """Does a gene still give a usable rule for its slot? gemma4 answers yes or no (the question of
    gene_timeline --judge); every answer is cached, so reruns cost nothing."""

    def __init__(self, cfg, client):
        from experiments.gene_timeline import JUDGE_PROMPT, TOPIC, slot_name   # (gene_timeline imports this module)
        self.template = resolve(JUDGE_PROMPT).read_text(encoding="utf-8")
        self.topic, self.slot_name, self.client, self.model = TOPIC, slot_name, client, cfg.policy.model
        self.schema = {"type": "object", "properties": {"answer": {"type": "string", "enum": ["yes", "no"]}},
                       "required": ["answer"]}

    def usable(self, locus: str, text: str) -> bool:
        prompt = self.template.format(slot=self.slot_name(locus), topic=self.topic.get(locus, locus), text=text)
        r = self.client.chat_json(self.model, [{"role": "user", "content": prompt}], self.schema,
                                  options={"seed": 0, "temperature": 0})
        return r.get("answer") == "yes"


def mutate(mut: Mutator, locus: str, text: str, rng) -> tuple[str | None, int, int, list[str]]:
    """Like Mutator.mutate_text, also returning the outcome of every attempt ("ok" or why it was rejected)."""
    tries = []
    for _ in range(mut.tries):
        new, k, seed, why = mut.attempt(locus, text, rng)
        tries.append(why or "ok")
        if new is not None:
            break
    return new, k, seed, tries


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--rules", default="current", choices=["current", "old"])
    ap.add_argument("--temps", default="0.9,1.2,1.5,2.0")
    ap.add_argument("--seeds", type=int, default=8)
    ap.add_argument("--steps", type=int, default=30)
    ap.add_argument("--lineages", default="all", choices=["all", "first"])
    ap.add_argument("--no-judge", action="store_true", help="skip the usable-rule question (no extra calls)")
    ap.add_argument("--tries", type=int, default=None, help="override evolution.mutation_tries")
    ap.add_argument("--workers", type=int, default=4)
    ap.add_argument("--model", default=None)
    ap.add_argument("--tag", default="mutation_test")
    a = ap.parse_args()
    over = dict(OLD_RULES) if a.rules == "old" else {}
    if a.tries is not None:
        over["mutation_tries"] = a.tries
    cfg = load_config(a.profile, {"evolution": over})
    temps = [float(t) for t in a.temps.split(",")]
    model = a.model or cfg.ollama.mutator_model
    client = client_from_config(cfg)
    reg = AlleleRegistry()
    pools = {sp.name: AllelePools(reg, cfg.paths.data_dir, species=sp) for sp in (PREY, PREDATOR)}
    kw = KeywordJudge(reg, pools["prey"])
    sense = None if a.no_judge else SenseJudge(cfg, client)
    founders = [(l, reg.text(x)) for p in pools.values() for l in p.founders for x in p.founders[l]
                if reg.get(x).origin == "founder"]
    world = set(SITUATION_WORDS) | {w for _, t in founders for w in content_words(t)}
    muts = {t: Mutator(cfg, reg, make_rewriter(client, model, temperature=t), model) for t in temps}
    instructions = muts[temps[0]].instructions
    starts = (founders if a.lineages == "all"
              else [(l, reg.text(pools["prey"].founders[l][0])) for l in LINEAGE_LOCI])
    total = len(founders) * a.seeds * len(temps) + len(starts) * a.steps * len(temps)
    prog = Progress(resolve(cfg.paths.logs_dir), a.tag, total=total)
    done, lock, t0 = [0], threading.Lock(), time.time()

    def tick():
        with lock:
            done[0] += 1
            if done[0] % 20 == 0:
                prog.update(done[0])

    def meaning(locus: str, text: str, judge: bool, parent: str) -> dict:
        new = content_words(text) - content_words(parent) - world
        out = {"world": bool(content_words(text) & world), "outside": bool(new)}
        if judge and sense:
            out["usable"] = sense.usable(locus, text)
        return out

    def one(job):
        t, (locus, text), i, s = job
        new, k, seed, tries = mutate(muts[t], locus, text, np.random.default_rng([i, s]))
        row = {"temp": t, "locus": locus, "parent": text, "seed_index": s, "prompt": k, "seed": seed, "new": new,
               "tries": tries}
        if new is None:
            row["reason"] = tries[-1]
        else:
            row.update(compare(text, new), d=kw.d(locus, text, new), **meaning(locus, new, True, text))
        tick()
        return row

    def lineage(job):
        t, li, (locus, start) = job
        rng = np.random.default_rng([100 + li, 7])           # same draws at every temperature
        cur, steps = start, []
        for step in range(1, a.steps + 1):
            new, k, seed, tries = mutate(muts[t], locus, cur, rng)
            parent = cur
            if new is not None:
                cur = new
            steps.append({"step": step, "prompt": k, "accepted": new is not None, "tries": len(tries), "text": cur,
                          "words": len(cur.split()), "sim_to_start": compare(start, cur)["sim"],
                          "d_to_start": kw.d(locus, start, cur), **meaning(locus, cur, step in JUDGE_STEPS, parent)})
            tick()
        return {"temp": t, "locus": locus, "start": start, "steps": steps}

    jobs = [(t, f, i, s) for t in temps for i, f in enumerate(founders) for s in range(a.seeds)]
    try:
        with ThreadPoolExecutor(a.workers) as ex:
            variety = list(ex.map(one, jobs))
            lineages = list(ex.map(lineage, [(t, li, st) for t in temps for li, st in enumerate(starts)]))
    except Exception as e:
        prog.fail(done[0], repr(e))
        raise
    ec = cfg.evolution
    res = {"model": model, "digest": client.digest(model), "rules": a.rules, "prompts": ec.mutation_prompts,
           "guards": {"tries": muts[temps[0]].tries},
           "temps": temps, "seeds": a.seeds, "steps": a.steps, "instructions": instructions,
           "variety": variety, "lineages": lineages, "judge_model": None if a.no_judge else cfg.policy.model,
           "elapsed_s": round(time.time() - t0, 1), "date": time.strftime("%Y-%m-%d")}
    res["summary"] = summarise(res)
    out = resolve(cfg.paths.results_dir)
    (out / f"{a.tag}.json").write_text(json.dumps(res, indent=1, ensure_ascii=False), encoding="utf-8")
    lines = report(res)
    (out / f"{a.tag}.md").write_bytes(("\n".join(lines) + "\n").encode("utf-8"))
    prog.finish(done[0])
    print("\n".join(lines[:16]))
    print(f"details: {out / (a.tag + '.md')}")


def revisits(lineage: dict) -> int:
    """Accepted steps that return to a sentence the lineage already had (cycles like cold ↔ hot)."""
    seen, n = {lineage["start"]}, 0
    for s in lineage["steps"]:
        if s["accepted"]:
            n += s["text"] in seen
            seen.add(s["text"])
    return n


def share(rows, key) -> float | None:
    xs = [r[key] for r in rows if r.get(key) is not None]
    return float(np.mean(xs)) if xs else None


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
        ds = [r["d"] for r in ok if r.get("d") is not None]
        by_t[t] = {
            "valid": len(ok) / len(rows), "reasons": dict(Counter(r["reason"] for r in rows if not r["new"])),
            "attempts": float(np.mean([len(r["tries"]) for r in rows])),
            "rejected": dict(Counter(x for r in rows for x in r["tries"] if x != "ok")),
            "usable": share(ok, "usable"), "world": share(ok, "world"), "outside": share(ok, "outside"),
            "distinct_per_sentence": sum(len(v) for v in distinct.values()) / n_parents,
            "edit_words": float(np.mean([r["size"] for r in ok])), "one_word": float(np.mean([r["size"] == 1 for r in ok])),
            "big": float(np.mean([r["size"] >= 4 for r in ok])),
            "first": float(np.mean([r["first"] for r in ok])), "middle": float(np.mean([r["middle"] for r in ok])),
            "last": float(np.mean([r["last"] for r in ok])), "dlen": float(np.mean([r["dlen"] for r in ok])),
            "longer3": float(np.mean([r["dlen"] >= 3 for r in ok])), "sim": float(np.mean([r["sim"] for r in ok])),
            "jump": float(np.mean([r["sim"] < 0.3 for r in ok])), "neutral": float(np.mean([d < 1e-4 for d in ds])),
            "d_mean": float(np.mean(ds)), "d_max": float(np.max(ds)),
            "lineage_accepted": float(np.mean([s["accepted"] for x in lin for s in x["steps"]])),
            "lineage_words_end": float(np.mean([x["steps"][-1]["words"] for x in lin])),
            "lineage_words_start": float(np.mean([len(x["start"].split()) for x in lin])),
            "lineage_sim_end": float(np.mean([x["steps"][-1]["sim_to_start"] for x in lin])),
            "lineage_revisits": float(np.mean([revisits(x) for x in lin])),
            "lineage_meaning": {n: {k: share([x["steps"][n - 1] for x in lin if len(x["steps"]) >= n], k)
                                    for k in ("usable", "world", "outside")}
                                for n in JUDGE_STEPS if n <= res["steps"]},
            "finals_alike": sims(fin), "starts_alike": sims([x["start"] for x in lin])}
    per_prompt = {}
    for k, ins in enumerate(res["instructions"]):
        rows = [r for r in res["variety"] if r["prompt"] == k]
        ok = [r for r in rows if r["new"]]
        if rows:
            per_prompt[ins] = {"n": len(rows), "valid": len(ok) / len(rows),
                               "edit_words": float(np.mean([r["size"] for r in ok])) if ok else float("nan"),
                               "dlen": float(np.mean([r["dlen"] for r in ok])) if ok else float("nan"),
                               "jump": float(np.mean([r["sim"] < 0.3 for r in ok])) if ok else float("nan"),
                               "usable": share(ok, "usable")}
    return {"by_temp": by_t, "per_prompt": per_prompt}


def pct(x) -> str:
    return "—" if x is None else f"{x:.0%}"


def report(res: dict) -> list[str]:
    S = res["summary"]
    T = res["temps"]
    n_par = len({(r["locus"], r["parent"]) for r in res["variety"]})
    n_lin = len(res["lineages"]) // len(T)
    g = res.get("guards", {})
    rules = f"`{res.get('prompts', 'prompts/mutate_v2.txt')}`, up to {g.get('tries', 1)} attempts"
    if g.get("max_changed_words") or g.get("vocabulary"):              # results of the checks tried on 2026-10-08
        rules += f", at most {g.get('max_changed_words') or 'any'} words changed, vocabulary {g.get('vocabulary') or 'any'}"
    L = [f"# Mutation test — {res['model']} (digest {res['digest']}), {res['date']}", "",
         f"Pure mutation, no selection. Rules: {res.get('rules', 'old')} ({rules}). Variety: {n_par} founder "
         f"sentences × {res['seeds']} seeds × {len(T)} temperature(s). Lineages: {n_lin} sentences × {res['steps']} "
         f"steps per temperature. {len(res['instructions'])} instructions. {res['elapsed_s']:.0f} s.", ""]
    if res.get("judge_model") or any("outside" in r for r in res["variety"]):
        L += [f"**Meaning** (usable: {res.get('judge_model') or '—'} says the gene still gives a usable rule for its slot; "
              "world word: still uses a word of the animal's world; new outside word: brings in a word that is in "
              "neither its parent nor the world words).", "",
              "| temperature | " + " | ".join(str(t) for t in T) + " |", "|---|" + "---|" * len(T)]
        B = S["by_temp"]
        L.append("| single mutations: usable | " + " | ".join(pct(B[t]["usable"]) for t in T) + " |")
        L.append("| … use a world word | " + " | ".join(pct(B[t]["world"]) for t in T) + " |")
        L.append("| … bring in a new outside word | " + " | ".join(pct(B[t]["outside"]) for t in T) + " |")
        L.append("| attempts per mutation | " + " | ".join(f"{B[t]['attempts']:.2f}" for t in T) + " |")
        L.append("| rejected attempts | " + " | ".join(", ".join(f"{k} {v}" for k, v in B[t]["rejected"].items()) or "none"
                                                       for t in T) + " |")
        for n in B[T[0]]["lineage_meaning"]:
            m = {t: B[t]["lineage_meaning"][n] for t in T}
            L.append(f"| lineages after {n} steps: usable / world word | "
                     + " | ".join(f"{pct(m[t]['usable'])} / {pct(m[t]['world'])}" for t in T) + " |")
        L.append("")
    L += ["| temperature | " + " | ".join(str(t) for t in T) + " |", "|---|" + "---|" * len(T)]
    rows = [("valid answers (after redraws)", "valid", "{:.0%}"), (f"distinct mutants per sentence (of {res['seeds']})", "distinct_per_sentence", "{:.1f}"),
            ("words changed per mutation", "edit_words", "{:.1f}"), ("one-word edits", "one_word", "{:.0%}"),
            ("big edits (≥ 4 words)", "big", "{:.0%}"), ("edit touches the first word", "first", "{:.0%}"),
            ("… a middle word", "middle", "{:.0%}"), ("… the last word", "last", "{:.0%}"),
            ("length change (words)", "dlen", "{:+.2f}"), ("≥ 3 words longer", "longer3", "{:.0%}"),
            ("similarity to parent", "sim", "{:.2f}"), ("jumps (similarity < 0.3)", "jump", "{:.0%}"),
            ("keyword brain: no effect (prey genes)", "neutral", "{:.0%}"), ("keyword brain: mean d", "d_mean", "{:.4f}"),
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
    L += ["", "Mutations that failed (every attempt rejected), by the last reason: "
          + "; ".join(f"T {t}: {r or 'none'}" for t, r in reasons.items()), "",
          "## Per instruction (all temperatures)", "",
          "| instruction | n | valid | words changed | length change | jumps | usable |", "|---|---|---|---|---|---|---|"]
    for ins, p in S["per_prompt"].items():
        L.append(f"| {ins} | {p['n']} | {p['valid']:.0%} | {p['edit_words']:.1f} | {p['dlen']:+.1f} | {p['jump']:.0%} "
                 f"| {pct(p.get('usable'))} |")
    L += ["", "## Lineages (each line: a step where the gene changed)", ""]
    for x in res["lineages"]:
        L.append(f"### {x['locus']}, T {x['temp']}: \"{x['start']}\"")
        L.append("")
        prev = x["start"]
        for s in x["steps"]:
            if s["text"] != prev:
                d = "" if s.get("d_to_start") is None else f"  (d {s['d_to_start']:.3f})"
                L.append(f"- {s['step']:>2}. {s['text']}{d}")
                prev = s["text"]
        L.append("")
    L += ["## Variety samples (one founder sentence of eat, flee, mate)", ""]
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
