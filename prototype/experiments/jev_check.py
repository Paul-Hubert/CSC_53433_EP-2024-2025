"""Check the JEV decision server (and the mutator server): speed, sanity, contrast genes.

    python -m experiments.jev_check [--n 60] [--no-mutator]

Asks the live JEV server (jev.host) about founder genomes x E1 situations (data/observations_v2.jsonl),
uncached, one request at a time (no batching yet), and prints decisions/s, input tokens, how often each
action wins, and for each prey action the contrast pair (pro vs anti gene, all else neutral): the mean
probability of that action should be higher with the pro gene. Then times a few mutator calls.
Details: results/jev_check.json.
"""
from __future__ import annotations

import argparse
import json
import time

import numpy as np

from experiments.make_obs import load_obs
from promptevo.backends.base import Query
from promptevo.backends.factory import make_backend, make_rewriter
from promptevo.config import load_config, resolve
from promptevo.founder import AllelePools
from promptevo.genome import AlleleRegistry
from promptevo.species import PREY


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--n", type=int, default=60, help="founder-genome decisions to time")
    ap.add_argument("--no-mutator", action="store_true")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    b = make_backend("jev", cfg)
    b.cache = None                                      # time the server, not the sqlite cache
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    obs = [o for o in load_obs() if o.species == "prey"]
    rng = np.random.default_rng(0)
    q = lambda g, o: Query(reg.genome_key(g), reg.genes(g), o, "prey")

    qs = [q(pools.sample_founder(rng), obs[i % len(obs)]) for i in range(a.n)]
    t0 = time.time()
    p = np.concatenate([b.decide([x]) for x in qs])
    dt = time.time() - t0
    wins = np.bincount(p.argmax(1), minlength=len(PREY.actions)) / len(p)
    print(f"server {cfg.jev.host}  model {b.model.id}")
    print(f"{a.n} decisions in {dt:.1f} s = {a.n / dt:.1f} /s (sequential), too long: {b.too_long}")
    print("argmax share: " + "  ".join(f"{act} {w:.2f}" for act, w in zip(PREY.actions, wins)))
    print(f"mean max prob {p.max(1).mean():.2f}")

    contrast = {}
    for i, act in enumerate(PREY.actions):
        pro, anti = pools.contrast_pair(act)
        sub = obs[:12]
        pp = np.concatenate([b.decide([q(pro, o)]) for o in sub])[:, i].mean()
        pa = np.concatenate([b.decide([q(anti, o)]) for o in sub])[:, i].mean()
        contrast[act] = [float(pp), float(pa)]
        print(f"contrast {act:7s} pro {pp:.2f}  anti {pa:.2f}  {'ok' if pp > pa else 'WRONG'}")

    mut = {}
    if not a.no_mutator:
        ask, model = make_rewriter(load_config(a.profile, {"evolution": {"p_mut": 0.1}}), check=True)
        t0 = time.time()
        outs = [ask(f"Rewrite this rule with one small change: \"Run away when a predator is close.\"", 100 + s)
                for s in range(5)]
        mut = {"model": model, "s_per_call": (time.time() - t0) / 5, "samples": outs}
        print(f"mutator {model} at {cfg.mutator.host}: {mut['s_per_call']:.2f} s/call; e.g. {outs[0]!r}")

    out = resolve(cfg.paths.results_dir) / "jev_check.json"
    out.write_text(json.dumps({"info": b.info(), "n": a.n, "seconds": dt, "too_long": b.too_long,
                               "argmax_share": wins.tolist(), "contrast": contrast, "mutator": mut},
                              indent=1), encoding="utf-8")
    print(f"details: {out}")


if __name__ == "__main__":
    main()
