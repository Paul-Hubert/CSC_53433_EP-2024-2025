"""S2.6 smoke run: python -m experiments.smoke_run --profile small --ticks 5000 [--backend rule_based]"""
from __future__ import annotations

import argparse
import json

from promptevo.backends.random_policy import RandomBackend
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config, resolve
from promptevo.render import ascii_map
from promptevo.sim import Simulation


def make_backend(name: str, cfg):
    if name == "rule_based":
        return RuleBasedBackend()
    if name == "random":
        return RandomBackend()
    if name == "laya":
        from promptevo.backends.laya_backend import LayaBackend
        return LayaBackend.from_config(cfg)
    raise SystemExit(f"unknown backend {name}")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--ticks", type=int, default=5000)
    ap.add_argument("--backend", default=None)
    ap.add_argument("--seed", type=int, default=None)
    ap.add_argument("--out", default="results/runs/smoke")
    ap.add_argument("--snapshots", type=int, default=3)
    a = ap.parse_args()
    cfg = load_config(a.profile)
    backend = make_backend(a.backend or cfg.backend.name, cfg)
    sim = Simulation(cfg, backend, seed=a.seed, out_dir=resolve(a.out))
    every = max(1, a.ticks // max(1, a.snapshots))
    for i in range(a.ticks):
        sim.step()
        if a.snapshots and sim.t % every == 0:
            print(f"--- t={sim.t} pop={len(sim.agents)}")
            print(ascii_map(sim.world, sim.agents, max_w=40, max_h=14))
    s = sim.finish()
    keep = {k: s[k] for k in ("ticks", "pop_final", "births", "immigrants", "deaths",
                              "mean_lifespan", "max_gen", "memo_hit_rate", "invalid_rate",
                              "backend_s", "alleles")}
    print(json.dumps(keep))
    print("actions:", s["action_share"])
    print("details:", resolve(a.out))


if __name__ == "__main__":
    main()
