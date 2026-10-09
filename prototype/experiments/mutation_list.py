"""Every mutation of a run, both species, in one table.

    python -m experiments.mutation_list results/runs/<run> [--tag NAME] [--every 100] [--profile full]

For each mutant gene: species, slot, parent text, new text, the instruction drawn, words
changed (mutation_test.compare), whether it uses a word of the animal's world, the most living
carriers at once (at checkpoints every --every ticks) and the carriers at the end. Writes
results/<tag>_mutations.md (tag: the run's folder name) and prints a summary per instruction:
how many mutants each instruction made, how many words they changed, how many kept a world word.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict

from experiments.gene_report import run_instructions
from experiments.gene_timeline import build
from promptevo.config import load_config, resolve


def rows_of(run_dir, every: int, cfg) -> list[dict]:
    instr = run_instructions(run_dir)
    rows = []
    for species in ("prey", "predator"):
        b = build(run_dir, every, 4 * every, 1, False, 0, cfg, None, 0, species)   # 1 gene drop: not used here
        g, sc = b["genes"], b["scan"]
        alive = Counter(a for i in sc["snaps"][-1]["ids"] for a in g.run.genome[i])
        for aid, al in g.al.items():
            if not g.mutant(aid) or al["locus"] not in g.run.loci:
                continue
            k = g.instruction(aid)
            rows.append({"species": species, "slot": al["locus"], "parent": g.text(al["parent_id"]),
                         "text": g.text(aid), "instr": instr[k] if k is not None and k < len(instr) else "?",
                         "changed": (g.edit(aid) or {}).get("size"), "world": g.world(aid),
                         "peak": sc["peak"][aid], "end": alive[aid]})
    return rows


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--tag", default=None)
    ap.add_argument("--every", type=int, default=100, help="ticks between checkpoints for the carrier counts")
    ap.add_argument("--profile", default="full")
    a = ap.parse_args()
    run_dir = resolve(a.run)
    tag = a.tag or run_dir.name
    rows = rows_of(run_dir, a.every, load_config(a.profile))
    out = [f"# Mutations in {run_dir.name}", "",
           "Every mutant gene: its parent text, the mutation instruction, words changed, whether it uses a word of "
           f"the animal's world, the most living carriers at once (checkpoints every {a.every} ticks) and carriers "
           "at the end.", "",
           "| species | slot | parent | mutant | instruction | words changed | world word | peak carriers | at the end |",
           "|---|---|---|---|---|---|---|---|---|"]
    for r in sorted(rows, key=lambda r: (r["species"], r["slot"], -r["peak"])):
        out.append(f"| {r['species']} | {r['slot']} | {r['parent']} | {r['text']} | {r['instr']} | {r['changed']} | "
                   f"{'yes' if r['world'] else 'no'} | {r['peak']} | {r['end']} |")
    path = resolve("results") / f"{tag}_mutations.md"
    path.write_bytes(("\n".join(out) + "\n").encode("utf-8"))
    for sp in ("prey", "predator"):
        rs = [r for r in rows if r["species"] == sp]
        if not rs:
            continue
        ch = [r["changed"] for r in rs if r["changed"] is not None]
        print(f"{sp}: {len(rs)} mutant genes; words changed {sum(ch) / max(1, len(ch)):.1f} "
              f"(≥ 4 words {sum(c >= 4 for c in ch) / max(1, len(ch)):.0%}); world word "
              f"{sum(r['world'] for r in rs) / len(rs):.0%}; 5+ carriers {sum(r['peak'] >= 5 for r in rs)}; "
              f"alive at the end {sum(r['end'] > 0 for r in rs)}")
        by = defaultdict(list)
        for r in rs:
            by[r["instr"]].append(r)
        for ins, rr in sorted(by.items(), key=lambda kv: -len(kv[1]))[:7]:
            c = [r["changed"] for r in rr if r["changed"] is not None]
            print(f"   {len(rr):>4}  {sum(c) / max(1, len(c)):4.1f} words  "
                  f"{sum(r['world'] for r in rr) / len(rr):4.0%} world  {ins[:58]}")
    print("details:", path)


if __name__ == "__main__":
    main()
