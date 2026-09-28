"""S1.5 — token budget of the question segment (plan §A7) vs Laya's head_max_len.
    python -m experiments.e0_budget [--tokenizer NAME] [--n 1000]
Uses transformers.AutoTokenizer (the backbone's tokenizer) — an approximation of
Laya's exact sequence layout; keep a margin of ~10 %.
"""
from __future__ import annotations

import argparse

import numpy as np

from promptevo.backends.laya_backend import build_request
from promptevo.config import load_config
from promptevo.founder import AllelePools
from promptevo.genome import ACTIONS, LOCI, AlleleRegistry

SITUATION = "Energy: medium. Food: near. Predator: far. Animal: near, ready to mate, weaker. Age: adult."


def segment_texts(genes, placement):
    state, q = build_request(genes, SITUATION, placement)
    qa = q["action"]
    head = qa["instructions"] + " " + " ".join(f"[MASK] {k} {v}" for k, v in qa["criteria"].items())
    return head, " ".join(state.values())


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--model", default="english", choices=["english", "multilingual"])
    ap.add_argument("--tokenizer", default=None)
    ap.add_argument("--n", type=int, default=1000)
    a = ap.parse_args()
    cfg = load_config(a.profile)
    from transformers import AutoTokenizer
    tok = AutoTokenizer.from_pretrained(a.tokenizer or cfg.laya.tokenizer[a.model])
    limit = int(cfg.laya.head_max_len[a.model])
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    rng = np.random.default_rng(0)
    worst = {l: max((reg.text(x) for x in pools.founders[l]), key=len) for l in LOCI}
    padded = {l: " ".join((worst[l] + " ") .split()[:12] + ["word"] * max(0, 12 - len(worst[l].split())))
              for l in ACTIONS}
    worst_case = {**worst, **padded}
    print(f"tokenizer={tok.name_or_path} head_max_len={limit}")
    for placement in ("P1", "P2", "P3", "P4"):
        heads, states = [], []
        for _ in range(a.n):
            g = reg.genes(pools.sample_founder(rng))
            h, s = segment_texts(g, placement)
            heads.append(len(tok(h)["input_ids"]))
            states.append(len(tok(s)["input_ids"]))
        wh, ws = (len(tok(x)["input_ids"]) for x in segment_texts(worst_case, placement))
        flag = "OK" if wh <= 0.9 * limit else "OVER (≥90% of limit)"
        print(f"{placement}: head p50={int(np.median(heads))} p99={int(np.quantile(heads, .99))} "
              f"worst(12-word genes)={wh} | state p50={int(np.median(states))} worst={ws} → {flag}")


if __name__ == "__main__":
    main()
