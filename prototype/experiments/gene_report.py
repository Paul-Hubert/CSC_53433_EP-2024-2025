"""Which genes did best? Ranks every allele of a run by how the animals that carried it did.

    python -m experiments.gene_report results/runs/long_1234 [results/runs/long_7 ...]
                                      [--min-carriers 100] [--tag long]

Fitness of an allele = mean number of offspring of the animals that carried it and died during
the run (complete lives), divided by the mean of all animals that died (1.00 = average).
★ marks the best allele of each gene slot when its 95 % interval (normal approximation)
stays above 1.00. Also: lifespan, share killed by predators, frequency in the living
population over time, and the lineage of mutant genes. The first run is the main one; extra
runs (other seeds) show whether the founder alleles rank the same way.
Writes results/<tag>_genes.md + .json and prints the marked genes.
"""
from __future__ import annotations

import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import load_instructions
from promptevo.genome import LOCI

STOP = set("a an the is are be to of and or in on at for with by from any you your it its it's this that when "
           "whenever if only then before after unless until while do not no never always often rarely sometimes "
           "very too more less than what where there here other others one some all every each so as but".split())
SITUATION_WORDS = {"energy", "food", "predator", "predators", "animal", "animals", "mate", "young", "adult", "near",
                   "far", "hungry", "eat", "flee", "follow", "wander", "rest", "attack", "fight", "run", "hide",
                   "danger", "safe", "safety", "partner", "hunger", "prey", "sleep", "tired", "strong", "weak", "alone"}


def jsonl(path: Path) -> list[dict]:
    return [json.loads(l) for l in path.read_text(encoding="utf-8").splitlines() if l.strip()]


def words(t: str) -> set[str]:
    return {w for w in re.findall(r"[a-z']+", t.lower()) if w not in STOP}


class Run:
    def __init__(self, path: Path):
        self.path, self.name = path, path.name
        self.alleles = {a["id"]: a for a in jsonl(path / "alleles.jsonl")}
        self.summary = json.loads((path / "summary.json").read_text())
        self.events = jsonl(path / "events.jsonl")
        self.genome, self.death = {}, {}
        for e in self.events:
            if e["kind"] in ("founder", "immigrant", "birth"):
                self.genome[e["id"]] = e["genome"]
            elif e["kind"] == "death":
                self.death[e["id"]] = e
        self.final = json.loads((path / "final_population.json").read_text())
        self.world = set(SITUATION_WORDS)
        for a in self.alleles.values():
            if a["origin"] in ("founder", "neutral"):
                self.world |= words(a["text"])

    def text(self, aid: str) -> str:
        return self.alleles[aid]["text"]

    def frequencies(self, every: int) -> list[tuple[int, int, dict]]:
        """[(t, population, {allele: count})] for the living population every `every` ticks."""
        alive, out, nxt = {}, [], 0
        for e in self.events:
            while e["t"] > nxt:
                out.append((nxt, len(alive), Counter(a for g in alive.values() for a in g)))
                nxt += every
            if e["kind"] in ("founder", "immigrant", "birth"):
                alive[e["id"]] = e["genome"]
            elif e["kind"] == "death":
                alive.pop(e["id"], None)
        out.append((self.summary["ticks"], len(self.final), Counter(a for x in self.final for a in x["genome"])))
        return out

    def fitness(self, min_carriers: int) -> dict:
        """{allele: stats} over animals that died (complete lives)."""
        dead = [(self.genome[i], d) for i, d in self.death.items() if i in self.genome]
        mean_all = float(np.mean([d["offspring"] for _, d in dead]))
        by = defaultdict(list)
        for g, d in dead:
            for aid in g:
                by[aid].append(d)
        pred_all = float(np.mean([d["cause"] == "predator" for _, d in dead]))
        res = {}
        for aid, ds in by.items():
            if len(ds) < min_carriers:
                continue
            off = np.array([d["offspring"] for d in ds], float)
            se = off.std(ddof=1) / np.sqrt(len(off))
            pred = float(np.mean([d["cause"] == "predator" for d in ds]))
            res[aid] = {"n": len(ds), "fitness": off.mean() / mean_all,
                        "lo": (off.mean() - 1.96 * se) / mean_all, "hi": (off.mean() + 1.96 * se) / mean_all,
                        "lifespan": float(np.mean([d["age"] for d in ds])), "predator": pred,
                        "predator_lo": pred - 2 * np.sqrt(pred * (1 - pred) / len(ds))}
        return {"mean_offspring": mean_all, "predator_share": pred_all, "n_dead": len(dead), "alleles": res}

    def lineage(self, aid: str, instructions: list[str]) -> list[str]:
        chain, cur = [], aid
        while cur:
            a = self.alleles[cur]
            how = a.get("operator") or a["origin"]
            m = re.match(r"llm#(\d+)$", how or "")
            if m and int(m.group(1)) < len(instructions):
                how = f'"{instructions[int(m.group(1))]}"'
            chain.append(f'{a["text"]}  ← {how}')
            cur = a.get("parent_id")
        return chain[::-1]


def origin(run: Run, aid: str) -> str:
    a = run.alleles[aid]
    return a["origin"] if a["origin"] != "mutant" else f"mutant ({a['operator']})"


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("runs", nargs="+")
    ap.add_argument("--min-carriers", type=int, default=100)
    ap.add_argument("--every", type=int, default=10000)
    ap.add_argument("--tag", default=None)
    a = ap.parse_args()
    cfg = load_config("small")
    instructions = load_instructions(cfg.evolution.mutation_prompts)
    runs = [Run(resolve(p)) for p in a.runs]
    main_run = runs[0]
    tag = a.tag or main_run.name
    fit = {r.name: r.fitness(a.min_carriers) for r in runs}
    F = fit[main_run.name]
    freq = main_run.frequencies(a.every)
    s = main_run.summary
    deaths = s["deaths"]
    n_deaths = sum(deaths.values())
    living = [aid for x in main_run.final for aid in x["genome"]]
    mut = s["mutations"] if "ok" in s["mutations"] else {     # runs before 2026-10-02: one entry per operator
        k: sum(v[k] for v in s["mutations"].values()) for k in ("tried", "ok")}
    lines = [f"# Which genes did best — {main_run.name}", "",
             f"Run: {s['ticks']} ticks, {s['max_gen']} generations, {s['births']} births, {n_deaths} deaths "
             f"(predators {deaths.get('predator', 0) / n_deaths:.0%}, starvation {deaths.get('starvation', 0) / n_deaths:.0%}, "
             f"old age {deaths.get('old_age', 0) / n_deaths:.0%}), {mut['ok']} mutations "
             f"({mut['tried']} tried), brain `{s['backend']}`, seed {s['seed']}.",
             f"Living animals at the end: {len(main_run.final)}; their genes: "
             f"{np.mean([main_run.alleles[x]['origin'] == 'mutant' for x in living]):.0%} mutants, "
             f"{np.mean([bool(words(main_run.text(x)) & main_run.world) for x in living]):.0%} still use a word of the "
             f"animal's world, {np.mean([len(main_run.text(x).split()) for x in living]):.1f} words on average.", "",
             f"**Fitness** = mean offspring of the {F['n_dead']} animals that carried the gene and died during the run, "
             f"÷ the mean of all of them ({F['mean_offspring']:.2f}); 1.00 = average. 95 % interval in brackets; "
             f"bold when the whole interval is above 1.00. "
             f"**★** = best gene of its slot, interval above 1.00. Genes carried by fewer than {a.min_carriers} "
             f"animals are not ranked. Killed by predators: share of carriers (all animals: {F['predator_share']:.0%}).", ""]

    def row(aid, mark=""):
        x = F["alleles"][aid]
        f0, fend = freq[0], freq[-1]
        start = f0[2].get(aid, 0) / max(1, f0[1])
        peak = max(c.get(aid, 0) / max(1, p) for _, p, c in freq)
        end = fend[2].get(aid, 0) / max(1, fend[1])
        fit_txt = f"**{x['fitness']:.2f}**" if x["lo"] > 1 else f"{x['fitness']:.2f}"
        return (f"| {mark} | {main_run.alleles[aid]['locus']} | {main_run.text(aid)} | {origin(main_run, aid)} | {x['n']} | "
                f"{fit_txt} [{x['lo']:.2f}–{x['hi']:.2f}] | {x['lifespan']:.0f} | {x['predator']:.0%} | "
                f"{start:.0%} → {peak:.0%} → {end:.0%} |")

    header = ["| | slot | gene | origin | carriers | fitness | lifespan | killed by predators | frequency start → peak → end |",
              "|---|---|---|---|---|---|---|---|---|"]
    stars = {}
    for locus in LOCI:
        ranked = sorted((aid for aid in F["alleles"] if main_run.alleles[aid]["locus"] == locus),
                        key=lambda aid: -F["alleles"][aid]["fitness"])
        if ranked and F["alleles"][ranked[0]]["lo"] > 1.0:
            stars[locus] = ranked[0]
    top = sorted(F["alleles"], key=lambda aid: -F["alleles"][aid]["fitness"])[:12]
    lines += ["## The best genes, all slots", ""] + header
    lines += [row(aid, "★" if aid in stars.values() else "") for aid in top]
    lines += ["", "## Slot by slot", ""]
    for locus in LOCI:
        ranked = sorted((aid for aid in F["alleles"] if main_run.alleles[aid]["locus"] == locus),
                        key=lambda aid: -F["alleles"][aid]["fitness"])
        lines += [f"### {locus}", ""] + header + [row(aid, "★" if stars.get(locus) == aid else "") for aid in ranked] + [""]
    mutant_wins = [aid for aid in top if main_run.alleles[aid]["origin"] == "mutant"]
    spread = sorted({aid for aid in set(living) if main_run.alleles[aid]["origin"] == "mutant"},
                    key=lambda aid: -living.count(aid))[:5]
    lines += ["## Where the mutant genes came from", ""]
    for aid in list(dict.fromkeys(mutant_wins + spread)):
        lines += [f"**{main_run.alleles[aid]['locus']}: \"{main_run.text(aid)}\"** "
                  f"({living.count(aid)} of {len(main_run.final)} living animals)", "", "```text"]
        lines += main_run.lineage(aid, instructions) + ["```", ""]
    if len(runs) > 1:
        lines += ["## Does it replicate? Founder genes in every run", "",
                  "| slot | founder gene | " + " | ".join(r.name for r in runs) + " |",
                  "|---|---|" + "---|" * len(runs)]
        for locus in LOCI:
            texts = sorted({r.alleles[aid]["text"] for r in runs for aid in fit[r.name]["alleles"]
                            if r.alleles[aid]["locus"] == locus and r.alleles[aid]["origin"] in ("founder", "neutral")})
            for t in texts:
                cells = []
                for r in runs:
                    aid = next((x for x in fit[r.name]["alleles"] if r.alleles[x]["locus"] == locus
                                and r.alleles[x]["text"] == t), None)
                    x = fit[r.name]["alleles"].get(aid) if aid else None
                    cells.append("—" if x is None else (f"**{x['fitness']:.2f}**" if x["lo"] > 1 else f"{x['fitness']:.2f}"))
                lines.append(f"| {locus} | {t} | " + " | ".join(cells) + " |")
        lines += ["", "Bold: interval above 1.00 in that run."]
    out = resolve(cfg.paths.results_dir)
    (out / f"{tag}_genes.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out / f"{tag}_genes.json").write_text(json.dumps(
        {"runs": [r.name for r in runs], "min_carriers": a.min_carriers, "stars": stars,
         "fitness": {r.name: {aid: {**v, "text": r.text(aid), "locus": r.alleles[aid]["locus"], "origin": origin(r, aid)}
                              for aid, v in fit[r.name]["alleles"].items()} for r in runs}},
        indent=1, ensure_ascii=False, default=float), encoding="utf-8")
    print("\n".join(lines[:3]))
    for locus, aid in stars.items():
        x = F["alleles"][aid]
        print(f"★ {locus:<7} {x['fitness']:.2f} [{x['lo']:.2f}–{x['hi']:.2f}] n={x['n']:<5} {main_run.text(aid)}")
    print(f"details: {out / (tag + '_genes.md')}")


if __name__ == "__main__":
    main()
