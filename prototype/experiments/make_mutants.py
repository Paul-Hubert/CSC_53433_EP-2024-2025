"""S4.4 — data/mutants_v1.jsonl: mutated founder alleles + fully novel (OOD) alleles.

    python -m experiments.make_mutants                         # word operators only (no LLM)
    python -m experiments.make_mutants --mutator <small-model> # + LLM rewrites + OOD alleles

Rows: {locus, text, origin: mutant|ood, parent (founder text or null), operator, style, model, seed}.
Used by make_dataset.py (held-out alleles) and by E3 locality tests.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np

from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import LLM_STYLES, Mutator, clean, valid
from promptevo.founder import AllelePools
from promptevo.genome import ACTION_LOCI, LOCI, AlleleRegistry

TOPICS = {"eat": "when and how to eat", "flee": "when to run from danger",
          "follow": "how to behave toward other animals of its kind",
          "wander": "when to explore or stay", "rest": "when to rest and save energy",
          "mate": "when to look for a partner", "attack": "when to fight other animals",
          "risk": "its attitude to risk (a temperament)", "social": "how sociable it is (a temperament)",
          "place": "what kind of place it prefers (a temperament)"}
WORD_OPS = ["intensity", "negate", "condition_swap", "synonym"]


def make_mutants(cfg, reg, pools, rng, per_allele: int, rewriter=None, model=None) -> list[dict]:
    mut = Mutator(cfg, reg, pools.founders, rewriter, model)
    ops = WORD_OPS + (["llm_rewrite"] * 2 if rewriter else [])     # LLM gets ~1/3 when present
    rows, seen = [], set()
    for locus in LOCI:
        for aid in pools.founders[locus]:
            if aid == pools.neutral[locus]:
                continue
            parent = reg.text(aid)
            made, tries = 0, 0
            while made < per_allele and tries < per_allele * 6:
                tries += 1
                op = ops[int(rng.integers(len(ops)))]
                seed = int(rng.integers(2**31))
                new = mut.mutate_text(locus, parent, op, rng, seed)
                if not new or (locus, new.lower()) in seen:
                    continue
                seen.add((locus, new.lower()))
                rows.append({"locus": locus, "text": new, "origin": "mutant", "parent": parent,
                             "operator": op, "model": model if op == "llm_rewrite" else None, "seed": seed})
                made += 1
    return rows


def make_ood(cfg, reg, pools, rng, client, model, per_locus: int) -> list[dict]:
    template = resolve("prompts/novel_v1.md").read_text()
    rows = []
    for locus in LOCI:
        max_words = int(cfg.evolution.max_action_words if locus in ACTION_LOCI else cfg.evolution.max_temperament_words)
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
    ap.add_argument("--mutator", default=None, help="Ollama model for LLM rewrites + OOD alleles")
    ap.add_argument("--out", default="data/mutants_v1.jsonl")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    rng = np.random.default_rng(cfg.seed + 101)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    rewriter = client = None
    model = a.mutator or cfg.ollama.mutator_model
    if model:
        from promptevo.llm.ollama_client import client_from_config, make_rewriter
        client = client_from_config(cfg)
        rewriter = make_rewriter(client, model, resolve("prompts/mutate_v1.md"),
                                 max_words=int(cfg.evolution.max_action_words))
    rows = make_mutants(cfg, reg, pools, rng, a.per_allele, rewriter, model)
    if client:
        rows += make_ood(cfg, reg, pools, rng, client, model, a.ood_per_locus)
    else:
        print("no mutator model: skipping LLM rewrites and OOD alleles (test split will be word-mutants only)")
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
