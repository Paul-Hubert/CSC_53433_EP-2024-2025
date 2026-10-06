"""S4.4 — data/mutants_v1.jsonl: mutated founder alleles + fully novel (OOD) alleles.

    python -m experiments.make_mutants [--mutator <model>]     # LLM mutants + OOD alleles

Mutants come from the same mutator as evolution (evolution/mutation.py: one random-change
instruction + the sentence). Without a mutator model no rows are written.
Rows: {locus, text, origin: mutant|ood, parent (founder text or null), operator, model, seed}.
Used by make_dataset.py (held-out alleles) and by E3 locality tests.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np

from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import Mutator, clean, valid
from promptevo.founder import AllelePools
from promptevo.genome import LOCI, AlleleRegistry

TOPICS = {"eat": "when and how to eat", "flee": "when to run from danger",
          "follow": "how to behave toward other animals of its kind",
          "rest": "when to rest and save energy", "mate": "when to look for a partner"}


def make_mutants(cfg, reg, pools, rng, per_allele: int, llm=None, model=None) -> list[dict]:
    """per_allele distinct mutants of every founder sentence (none without an llm)."""
    if llm is None:
        return []
    mut = Mutator(cfg, reg, llm, model)
    rows, seen = [], set()
    for locus in LOCI:
        for aid in pools.founders[locus]:
            if aid == pools.neutral[locus]:
                continue
            parent = reg.text(aid)
            made, tries = 0, 0
            while made < per_allele and tries < per_allele * 6:
                tries += 1
                new, k, seed = mut.mutate_text(locus, parent, rng)
                if not new or (locus, new.lower()) in seen:
                    continue
                seen.add((locus, new.lower()))
                rows.append({"locus": locus, "text": new, "origin": "mutant", "parent": parent,
                             "operator": f"llm#{k}", "model": model, "seed": seed})
                made += 1
    return rows


def make_ood(cfg, reg, pools, rng, client, model, per_locus: int) -> list[dict]:
    template = resolve("prompts/novel_v1.md").read_text()
    rows = []
    for locus in LOCI:
        max_words = int(cfg.evolution.max_words)
        known = {reg.text(a).lower() for a in pools.founders[locus]}
        examples = "; ".join(f'"{reg.text(a)}"' for a in pools.founders[locus][:4])
        made, tries = 0, 0
        while made < per_locus and tries < per_locus * 5:
            tries += 1
            seed = int(rng.integers(2**31))
            prompt = template.format(topic=TOPICS[locus], examples=examples, max_words=max_words)
            text = clean(client.chat(model, [{"role": "user", "content": prompt}],
                                     options={"seed": seed, "temperature": 1.0}))
            if not valid(text, "", max_words) or text.lower() in known:
                continue
            known.add(text.lower())
            rows.append({"locus": locus, "text": text, "origin": "ood", "parent": None,
                         "operator": "novel", "model": model, "seed": seed})
            made += 1
    return rows


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--per-allele", type=int, default=6)
    ap.add_argument("--ood-per-locus", type=int, default=5)
    ap.add_argument("--mutator", default=None, help="Ollama model (default ollama.mutator_model)")
    ap.add_argument("--out", default="data/mutants_v1.jsonl")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    rng = np.random.default_rng(cfg.seed + 101)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    llm = client = None
    model = a.mutator or cfg.ollama.mutator_model
    if model:
        from promptevo.llm.ollama_client import client_from_config, make_rewriter
        client = client_from_config(cfg)
        llm = make_rewriter(client, model, temperature=float(cfg.evolution.temperature))
    rows = make_mutants(cfg, reg, pools, rng, a.per_allele, llm, model)
    if client:
        rows += make_ood(cfg, reg, pools, rng, client, model, a.ood_per_locus)
    else:
        print("no mutator model: no mutants and no OOD alleles written")
    path = resolve(a.out)
    with Path(path).open("w") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    by = {}
    for r in rows:
        by[r["operator"]] = by.get(r["operator"], 0) + 1
    print(f"wrote {len(rows)} alleles → {path}; by operator: {by}")
    for r in rows[:: max(1, len(rows) // 6)][:6]:
        print(f"  [{r['locus']}/{r['operator']}] {r['parent']!r} → {r['text']!r}")


if __name__ == "__main__":
    main()
