"""Which genes did best? Ranks genes by how the animals that carried them did.

    python -m experiments.gene_report results/runs/long_1234 [results/runs/long_7 ...]
                                      [--min-carriers 100] [--tag long]

Fitness = mean number of offspring of the animals that carried the gene and died during the
run (complete lives), each divided by the mean of the animals that died in the same 5 000
ticks, so animals are compared with their contemporaries (1.00 = average; 95 % interval by
normal approximation). Mutation spreads a population over thousands of texts, so genes are
also grouped by what the keyword brain reads in them (strength, action, conditions).
Marks use every run given (seeds): ★ clearly above average, ▲ steady leader (above average
in every run), ✗ clearly below average. Also: share killed by predators, frequency over
time, predator kills per 1 000 animal-ticks, and the lineage of the most common genes.
Writes results/<tag>_genes.md + .json and prints the marked genes.
"""
from __future__ import annotations

import argparse
import csv
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

from promptevo.backends.rule_based import ACTION_WORDS, CONDITIONS, INTENSITY, TEMPERAMENT
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
    """Records of a JSONL file. A last line cut short (a run stopped hard mid-write) is dropped."""
    lines = [l for l in path.read_text(encoding="utf-8").splitlines() if l.strip()]
    out = []
    for i, l in enumerate(lines):
        try:
            out.append(json.loads(l))
        except json.JSONDecodeError:
            if i < len(lines) - 1:
                raise
    return out


def words(t: str) -> set[str]:
    return {w for w in re.findall(r"[a-z']+", t.lower()) if w not in STOP}


STRENGTH = {-2.5: "never", -1.2: "rarely", 0.3: "sometimes", 0.8: "names the action", 1.2: "often", 2.5: "always"}


def first_alt(pattern: str) -> str:
    """Readable name of a keyword pattern: its first alternative ("food is (close|near)|…" -> "food is close")."""
    depth, cut = 0, len(pattern)
    for i, ch in enumerate(pattern):
        depth += (ch == "(") - (ch == ")")
        if ch == "|" and depth == 0:
            cut = i
            break
    return re.sub(r"\(([^|)]*)(?:\|[^)]*)?\)\??", r"\1", pattern[:cut])


def reading(locus: str, text: str) -> str:
    """What the keyword brain reads in a gene (same rules as rule_based.gene_weight / temperament_deltas)."""
    t = text.lower()
    if locus not in ACTION_WORDS:
        hits = [first_alt(p) for p, _ in TEMPERAMENT if re.search(p, t)]
        return " + ".join(hits) if hits else "no effect"
    w = next((v for p, v in INTENSITY if re.search(p, t)), None)
    if w is None:
        w = 0.8 if re.search(ACTION_WORDS[locus], t) else 0.0
    if w == 0.0:
        return "no effect"
    unless = re.search(r"\bunless (.+)$", t)
    main = t[:unless.start()] if unless else t
    conds = [first_alt(p) for p in CONDITIONS if re.search(p, main)]
    exc = [first_alt(p) for p in CONDITIONS if unless and re.search(p, unless.group(1))]
    label = f"{locus} {w:+.1f} ({STRENGTH.get(w, w)})"
    if conds:
        label += ", when " + " & ".join(conds)
    if exc:
        label += ", unless " + " & ".join(exc)
    return label


class Run:
    def __init__(self, path: Path):
        self.path, self.name = path, path.name
        self.events = jsonl(path / "events.jsonl")
        self.alleles = {a["id"]: a for a in jsonl(path / "alleles.jsonl")} if (path / "alleles.jsonl").exists() else {}
        info = path / "run_info.json"
        self.info = json.loads(info.read_text(encoding="utf-8")) if info.exists() else {}
        self.genome, self.death, alive = {}, {}, {}
        for e in self.events:
            if e["kind"] in ("founder", "immigrant", "birth"):
                self.genome[e["id"]] = e["genome"]
                alive[e["id"]] = {"id": e["id"], "gen": e.get("gen", 0), "genome": e["genome"]}
                for m in e.get("mutations", []):        # mutants newer than the last alleles.jsonl save
                    self.alleles.setdefault(m["child"], {
                        "id": m["child"], "locus": m["locus"], "text": m["text"], "origin": "mutant",
                        "parent_id": m["parent"], "operator": f"llm#{m['prompt']}", "model": None, "seed": None})
            elif e["kind"] == "death":
                self.death[e["id"]] = e
                alive.pop(e["id"], None)
        # a run still going, or stopped hard, has no summary yet: rebuild what's needed from the events
        self.finished = (path / "summary.json").exists() and (path / "final_population.json").exists()
        if self.finished:
            self.summary = json.loads((path / "summary.json").read_text())
            self.final = json.loads((path / "final_population.json").read_text())
        else:
            self.final = list(alive.values())
            self.summary = self._rebuilt_summary()
        self._reading: dict[str, str] = {}
        self.world = set(SITUATION_WORDS)
        for a in self.alleles.values():
            if a["origin"] in ("founder", "neutral"):
                self.world |= words(a["text"])

    def _rebuilt_summary(self) -> dict:
        ticks = self.events[-1]["t"] if self.events else 0
        if (self.path / "stats.csv").exists():
            with (self.path / "stats.csv").open() as f:
                ts = [int(r["t"]) for r in csv.DictReader(f) if (r.get("t") or "").isdigit()]
            ticks = max([ticks, *ts])
        kinds = Counter(e["kind"] for e in self.events)
        ok = sum(len(e.get("mutations", [])) for e in self.events if e["kind"] == "birth")
        return {"ticks": ticks, "seed": self.info.get("seed", "?"), "backend": self.info.get("brain", "?"),
                "pop_final": len(self.final), "births": kinds["birth"], "immigrants": kinds["immigrant"],
                "deaths": dict(Counter(e["cause"] for e in self.events if e["kind"] == "death")),
                "max_gen": max((x["gen"] for x in self.final), default=0),
                "mutations": {"tried": ok, "ok": ok}}       # attempts aren't logged, only successes

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

    def dead(self, window: int = 5000) -> list[tuple[list, dict, float]]:
        """(genome, death event, relative offspring) for every animal that died. Relative offspring =
        offspring ÷ the mean of the animals that died in the same window of ticks, so animals are only
        compared with their contemporaries (early on, while the population grows, everyone has more)."""
        rows = [(self.genome[i], d) for i, d in self.death.items() if i in self.genome]
        by_w = defaultdict(list)
        for _, d in rows:
            by_w[d["t"] // window].append(d["offspring"])
        mean_w = {w: float(np.mean(v)) for w, v in by_w.items()}
        return [(g, d, d["offspring"] / mean_w[d["t"] // window] if mean_w[d["t"] // window] else 1.0) for g, d in rows]

    def fitness(self, min_carriers: int) -> dict:
        """{allele: stats} over animals that died (complete lives)."""
        dead = self.dead()
        by = defaultdict(list)
        for g, d, rel in dead:
            for aid in g:
                by[aid].append((d, rel))
        res = {aid: {**stats([rel for _, rel in ds]), "lifespan": float(np.mean([d["age"] for d, _ in ds])),
                     "predator": float(np.mean([d["cause"] == "predator" for d, _ in ds]))}
               for aid, ds in by.items() if len(ds) >= min_carriers}
        return {"mean_offspring": float(np.mean([d["offspring"] for _, d, _ in dead])),
                "predator_share": float(np.mean([d["cause"] == "predator" for _, d, _ in dead])),
                "n_dead": len(dead), "alleles": res}

    def read(self, aid: str) -> str:
        if aid not in self._reading:
            self._reading[aid] = reading(self.alleles[aid]["locus"], self.text(aid))
        return self._reading[aid]

    def reading_values(self) -> dict:
        """{(locus, reading): [(relative offspring, killed by a predator, text)]} over the animals that died."""
        out = defaultdict(list)
        for g, d, rel in self.dead():
            for locus, aid in zip(LOCI, g):
                out[(locus, self.read(aid))].append((rel, d["cause"] == "predator", self.text(aid)))
        return out

    def by_reading(self, min_carriers: int) -> dict:
        """Genes grouped by what the keyword brain reads in them: {locus: {reading: stats}}."""
        return group_stats(self.reading_values(), min_carriers)

    def reading_shares(self, freq) -> list[tuple[int, dict]]:
        """[(t, {(locus, reading): share of the living animals})] at each checkpoint."""
        out = []
        for t, pop, counts in freq:
            sh = Counter()
            for aid, n in counts.items():
                sh[(self.alleles[aid]["locus"], self.read(aid))] += n / max(1, pop)
            out.append((t, sh))
        return out

    def predation(self, every: int) -> list[dict]:
        """Deaths per window of `every` ticks, with predator kills per 1 000 animal-ticks."""
        pops = defaultdict(list)
        with (self.path / "stats.csv").open() as f:
            for row in csv.DictReader(f):
                if (row.get("pop") or "").isdigit():      # skips a last row cut short by a hard stop
                    pops[(int(row["t"]) - 1) // every].append(int(row["pop"]))
        wins = defaultdict(Counter)
        for d in self.death.values():
            wins[d["t"] // every][d["cause"]] += 1
        out = []
        for w in sorted(wins):
            animal_ticks = float(np.mean(pops[w])) * every if pops.get(w) else float("nan")
            out.append({"from": w * every, "to": (w + 1) * every, "predator": wins[w]["predator"],
                        "starvation": wins[w]["starvation"], "pop": float(np.mean(pops[w])) if pops.get(w) else float("nan"),
                        "kill_rate": 1000 * wins[w]["predator"] / animal_ticks})
        return out

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


def stats(rel: list[float]) -> dict:
    """Fitness = mean relative offspring, with a 95 % interval (normal approximation)."""
    x = np.asarray(rel, float)
    se = x.std(ddof=1) / np.sqrt(len(x)) if len(x) > 1 else float("inf")
    return {"n": len(x), "fitness": float(x.mean()), "lo": float(x.mean() - 1.96 * se), "hi": float(x.mean() + 1.96 * se)}


def group_stats(values: dict, min_carriers: int) -> dict:
    """{locus: {reading: stats}} from {(locus, reading): [(rel, killed by predator, text)]}."""
    out = {locus: {} for locus in LOCI}
    for (locus, rd), vs in values.items():
        if len(vs) < min_carriers:
            continue
        texts = Counter(t for _, _, t in vs)
        out[locus][rd] = {**stats([r for r, _, _ in vs]), "predator": float(np.mean([p for _, p, _ in vs])),
                          "texts": len(texts), "examples": texts.most_common(3)}
    return out


def origin(run: Run, aid: str) -> str:
    a = run.alleles[aid]
    return a["origin"] if a["origin"] != "mutant" else f"mutant ({a['operator']})"


def fmt_fit(x: dict) -> str:
    """1.08 [1.03–1.13], bold when the interval is above 1, italic when below."""
    v = f"{x['fitness']:.2f}"
    v = f"**{v}**" if x["lo"] > 1 else f"*{v}*" if x["hi"] < 1 else v
    return f"{v} [{x['lo']:.2f}–{x['hi']:.2f}]"


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
    R = {r.name: r.by_reading(a.min_carriers) for r in runs}
    RM = R[main_run.name]
    freq = main_run.frequencies(a.every)
    shares = main_run.reading_shares(freq)
    mid = min(shares, key=lambda x: abs(x[0] - main_run.summary["ticks"] / 2))
    s = main_run.summary
    deaths = s["deaths"]
    n_deaths = sum(deaths.values())
    living = [aid for x in main_run.final for aid in x["genome"]]
    mut = s["mutations"] if "ok" in s["mutations"] else {     # runs before 2026-10-02: one entry per operator
        k: sum(v[k] for v in s["mutations"].values()) for k in ("tried", "ok")}
    merged = defaultdict(list)                    # all runs together: the marks use every seed
    for r in runs:
        for k, v in r.reading_values().items():
            merged[k] += v
    P = group_stats(merged, a.min_carriers)
    star, lead = {}, {}
    for locus in LOCI:
        if P[locus]:
            best = max(P[locus], key=lambda r: P[locus][r]["fitness"])
            if P[locus][best]["lo"] > 1.0:
                star[locus] = best
        # ▲ consistent leader: above average in every run that has it (at least 2 runs, or the only run)
        steady = [rd for rd in P[locus] if rd != "no effect"
                  and len([r for r in runs if rd in R[r.name][locus]]) >= min(2, len(runs))
                  and all(R[r.name][locus][rd]["fitness"] > 1.0 for r in runs if rd in R[r.name][locus])]
        if steady and locus not in star:
            lead[locus] = max(steady, key=lambda rd: P[locus][rd]["fitness"])

    def share(reading_key, point):
        return point[1].get(reading_key, 0.0)

    lines = [f"# Which genes did best — {main_run.name}", "",
             f"Run: {s['ticks']} ticks, {s['max_gen']} generations, {s['births']} births, {n_deaths} deaths "
             f"(predators {deaths.get('predator', 0) / n_deaths:.0%}, starvation {deaths.get('starvation', 0) / n_deaths:.0%}, "
             f"old age {deaths.get('old_age', 0) / n_deaths:.0%}), {mut['ok']} mutations "
             f"({mut['tried']} tried), brain `{s['backend']}`, seed {s['seed']}."
             + (f" Replicates: {', '.join(r.name for r in runs[1:])}." if len(runs) > 1 else "")
             + ("" if main_run.finished else " **Unfinished run**: totals rebuilt from the events; "
                "mutation attempts unknown (only successes are logged)."),
             f"Living animals at the end: {len(main_run.final)}; their genes: "
             f"{np.mean([main_run.alleles[x]['origin'] == 'mutant' for x in living]):.0%} mutants, "
             f"{np.mean([bool(words(main_run.text(x)) & main_run.world) for x in living]):.0%} still use a word of the "
             f"animal's world, {np.mean([len(main_run.text(x).split()) for x in living]):.1f} words on average.", "",
             f"**Fitness** = mean number of offspring of the animals that carried the gene and died during the "
             f"run ({F['n_dead']} animals here), each divided by the mean of the animals that died in the same "
             f"5 000 ticks (so animals are compared with their contemporaries). 1.00 = average; 95 % interval in "
             f"brackets; **bold** = clearly above average, *italic* = clearly below. Mutation spreads the "
             f"population over thousands of different texts, so genes are grouped by what the keyword brain reads "
             f"in them (strength, action, conditions): texts that read the same behave the same. Marks"
             + (f" (all {len(runs)} runs together)" if len(runs) > 1 else "")
             + f": **★** best reading of its slot and clearly above average; **▲** best of the readings that are "
             f"above average in every run (a steady lead, too small to be sure of); **✗** clearly below average. "
             f"Readings carried by fewer than {a.min_carriers} animals are left out.", "",
             "## Marked genes", ""]
    for locus in LOCI:
        pick = star.get(locus) or lead.get(locus)
        if pick:
            x = P[locus][pick]
            ex = "; ".join(f'"{t}"' for t, _ in x["examples"])
            per_run = ", ".join(f"{R[r.name][locus][pick]['fitness']:.2f}" for r in runs if pick in R[r.name][locus])
            lines.append(f"- {'★' if locus in star else '▲'} **{locus}** — {pick}: fitness {fmt_fit(x)} over {x['n']} "
                         f"carriers (per run: {per_run}), killed by predators {x['predator']:.0%}. "
                         f"Most carried texts: {ex}.")
        else:
            lines.append(f"- **{locus}** — no steady leader.")
        for rd, x in P[locus].items():
            if x["hi"] < 1:
                ex = "; ".join(f'"{t}"' for t, _ in x["examples"][:2])
                lines.append(f"  - ✗ {rd}: {fmt_fit(x)}, killed by predators {x['predator']:.0%} ({ex})")
    # predation over time
    lines += ["", "## Selection by predators over time", "",
              "| ticks | mean population | killed by predators | starved | predator kills per 1 000 animal-ticks |",
              "|---|---|---|---|---|"]
    lines += [f"| {w['from']}–{w['to']} | {w['pop']:.0f} | {w['predator']} | {w['starvation']} | {w['kill_rate']:.2f} |"
              for w in main_run.predation(a.every)]
    for r in runs[1:]:
        p = r.predation(a.every)
        lines.append(f"\n{r.name}: predator kills per 1 000 animal-ticks {p[0]['kill_rate']:.2f} in the first "
                     f"{a.every} ticks, {p[-1]['kill_rate']:.2f} in the last.")
    # readings, slot by slot
    lines += ["", "## Slot by slot: what the keyword brain reads in each gene", "",
              f"Living share: share of the living animals carrying that reading at the start, at tick {mid[0]} and at the end."]
    for locus in LOCI:
        ranked = sorted(RM[locus], key=lambda r: -RM[locus][r]["fitness"])
        lines += ["", f"### {locus}", "", "| | reading | carriers | fitness | killed by predators | living share | different texts | most carried texts |",
                  "|---|---|---|---|---|---|---|---|"]
        for rd in ranked:
            x = RM[locus][rd]
            mark = "★" if star.get(locus) == rd else "▲" if lead.get(locus) == rd else \
                "✗" if rd in P[locus] and P[locus][rd]["hi"] < 1 else ""
            ex = "<br>".join(f'"{t}" ({n})' for t, n in x["examples"])
            lines.append(f"| {mark} | {rd} | {x['n']} | {fmt_fit(x)} | {x['predator']:.0%} | "
                         f"{share((locus, rd), shares[0]):.0%} → {share((locus, rd), mid):.0%} → {share((locus, rd), shares[-1]):.0%} | "
                         f"{x['texts']} | {ex} |")
    if len(runs) > 1:
        lines += ["", "## Does it replicate? The same readings in every run", "",
                  "| slot | reading | " + " | ".join(r.name for r in runs) + " | all runs |",
                  "|---|---|" + "---|" * (len(runs) + 1)]
        for locus in LOCI:
            for rd in sorted(P[locus], key=lambda r: -P[locus][r]["fitness"]):
                cells = [fmt_fit(R[r.name][locus][rd]) if rd in R[r.name][locus] else "—" for r in runs]
                mark = "★ " if star.get(locus) == rd else "▲ " if lead.get(locus) == rd else \
                    "✗ " if P[locus][rd]["hi"] < 1 else ""
                lines.append(f"| {locus} | {mark}{rd} | " + " | ".join(cells) + f" | {fmt_fit(P[locus][rd])} |")
    # single texts
    lines += ["", "## Single texts", "",
              f"The same fitness for each exact text carried by at least {a.min_carriers} animals"
              + (" (no text is clearly above average)." if not any(x["lo"] > 1 for x in F["alleles"].values()) else "."),
              "", "| slot | gene | origin | reading | carriers | fitness | killed by predators |", "|---|---|---|---|---|---|---|"]
    for aid in sorted(F["alleles"], key=lambda aid: -F["alleles"][aid]["fitness"])[:15]:
        x = F["alleles"][aid]
        lines.append(f"| {main_run.alleles[aid]['locus']} | {main_run.text(aid)} | {origin(main_run, aid)} | "
                     f"{main_run.read(aid)} | {x['n']} | {fmt_fit(x)} | {x['predator']:.0%} |")
    spread = sorted({aid for aid in set(living) if main_run.alleles[aid]["origin"] == "mutant"},
                    key=lambda aid: (-living.count(aid), aid))[:5]      # ties in a fixed order
    lines += ["", "## The most common genes at the end, and where they came from", ""]
    for aid in spread:
        lines += [f"**{main_run.alleles[aid]['locus']}: \"{main_run.text(aid)}\"** — {main_run.read(aid)} "
                  f"({living.count(aid)} of {len(main_run.final)} living animals)", "", "```text"]
        lines += main_run.lineage(aid, instructions) + ["```", ""]
    out = resolve(cfg.paths.results_dir)
    (out / f"{tag}_genes.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out / f"{tag}_genes.json").write_text(json.dumps(
        {"runs": [r.name for r in runs], "min_carriers": a.min_carriers, "stars": star,
         "readings": {r.name: R[r.name] for r in runs}, "readings_all_runs": P,
         "predation": {r.name: r.predation(a.every) for r in runs},
         "texts": {r.name: {aid: {**v, "text": r.text(aid), "locus": r.alleles[aid]["locus"], "origin": origin(r, aid),
                                  "reading": r.read(aid)} for aid, v in fit[r.name]["alleles"].items()} for r in runs}},
        indent=1, ensure_ascii=False, default=float), encoding="utf-8")
    print("\n".join(lines[:3]))
    for locus in LOCI:
        pick = star.get(locus) or lead.get(locus)
        if pick:
            x = P[locus][pick]
            print(f"{'★' if locus in star else '▲'} {locus:<7} {x['fitness']:.2f} [{x['lo']:.2f}–{x['hi']:.2f}] "
                  f"n={x['n']:<6} {pick}")
        for rd, x in P[locus].items():
            if x["hi"] < 1:
                print(f"✗ {locus:<7} {x['fitness']:.2f} [{x['lo']:.2f}–{x['hi']:.2f}] n={x['n']:<6} {rd}")
    print(f"details: {out / (tag + '_genes.md')}")


if __name__ == "__main__":
    main()
