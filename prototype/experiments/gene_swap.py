"""Does one gene change behaviour? Swap it in real genomes and ask the brain.

    python -m experiments.gene_swap results/runs/llm_long --tick 23000 --slot mate \\
        --carrying "A salad is hiding under a bicycle." \\
        --texts "Look partner for a when energy is high." "No preference."

Takes the distinct genomes alive at --tick (only those carrying --carrying in --slot, if given,
at most --genomes of them), puts each text in --slot (the carried text first), and asks the LLM
brain (points mode, cached) about the E1 situations relevant to the slot (data/observations_v1.jsonl;
for mate and attack: another animal near). Prints the mean probability of each action per text, and
the change against the first text with a 95 % interval over genomes. Stops at --max-calls new calls.
"""
from __future__ import annotations

import argparse
import random

import numpy as np

from experiments.gene_report import Run
from experiments.make_obs import load_obs
from promptevo.backends.base import Query
from promptevo.backends.factory import make_backend
from promptevo.config import load_config, resolve
from promptevo.genome import ACTIONS, LOCI, AlleleRegistry
from promptevo.perception import RELEVANT_TAG


def genomes_at(run: Run, tick: int) -> list[list[str]]:
    """Genomes of the animals alive at the end of `tick`."""
    alive = {}
    for e in run.events:
        if e["t"] > tick:
            break
        if e["kind"] in ("founder", "immigrant", "birth"):
            alive[e["id"]] = e["genome"]
        elif e["kind"] == "death":
            alive.pop(e["id"], None)
    return list(alive.values())


def swap(run: Run, backend, tick: int, slot: str, texts: list[str], carrying: str | None = None,
         n_genomes: int = 8, max_calls: int = 500, seed: int = 0) -> dict:
    """{"P": array (texts, genomes, actions), "genomes", "obs"}: mean action probabilities over the
    relevant situations, for each genome with each text in `slot`."""
    if run.loci != LOCI:
        raise SystemExit(f"{run.name} has the slots {run.loci}; gene_swap asks today's brain, which reads {LOCI}")
    li = LOCI.index(slot)
    gs = sorted({tuple(g) for g in genomes_at(run, tick) if carrying is None or run.text(g[li]) == carrying})
    gs = sorted(random.Random(seed).sample(gs, min(n_genomes, len(gs))))
    tag = RELEVANT_TAG.get(slot, "any")
    obs = [o for o in load_obs() if tag in o.tags()]
    reg = AlleleRegistry()
    P = np.full((len(texts), len(gs), len(ACTIONS)), np.nan)
    for j, g in enumerate(gs):
        genes = {l: run.text(aid) for l, aid in zip(LOCI, g)}
        for i, text in enumerate(texts):
            if getattr(backend, "calls", 0) >= max_calls:
                break
            gg = reg.make_genome({**genes, slot: text})
            P[i, j] = backend.decide([Query(reg.genome_key(gg), reg.genes(gg), o) for o in obs]).mean(axis=0)
    return {"P": P, "genomes": len(gs), "obs": len(obs)}


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--tick", type=int, required=True)
    ap.add_argument("--slot", required=True, choices=LOCI)
    ap.add_argument("--texts", nargs="+", required=True, help="texts to put in the slot")
    ap.add_argument("--carrying", default=None, help="only genomes carrying this text in the slot (tested first)")
    ap.add_argument("--genomes", type=int, default=8)
    ap.add_argument("--max-calls", type=int, default=500)
    a = ap.parse_args()
    cfg = load_config("small")
    backend = make_backend("llm", cfg, strict=True)
    texts = ([a.carrying] if a.carrying else []) + a.texts
    r = swap(Run(resolve(a.run)), backend, a.tick, a.slot, texts, a.carrying, a.genomes, a.max_calls)
    P = r["P"]
    print(f"{r['genomes']} genomes alive at t={a.tick}, {r['obs']} situations relevant to {a.slot}; "
          f"{backend.calls} new brain calls")
    print(f"{'text':<45} " + " ".join(f"{x:>6}" for x in ACTIONS))
    for i, t in enumerate(texts):
        print(f"{t[:45]:<45} " + " ".join(f"{v:>6.1%}" for v in np.nanmean(P[i], axis=0)))
    k = ACTIONS.index(a.slot) if a.slot in ACTIONS else None
    if k is not None:
        for i, t in enumerate(texts[1:], 1):
            d = P[i, :, k] - P[0, :, k]
            d = d[~np.isnan(d)]
            se = d.std(ddof=1) / np.sqrt(len(d)) if len(d) > 1 else float("nan")
            print(f"P({a.slot}) with \"{t[:40]}\" minus with \"{texts[0][:30]}\": {d.mean():+.1%} "
                  f"[{d.mean() - 1.96 * se:+.1%}, {d.mean() + 1.96 * se:+.1%}] over {len(d)} genomes")


if __name__ == "__main__":
    main()
