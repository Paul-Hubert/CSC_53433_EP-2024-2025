"""Smoke run: python -m experiments.smoke_run --profile small --ticks 5000 [--backend rule_based|llm]
                                            [--world terrain_preview]

With --backend llm the LLM decides (policy.model) and, if ollama.mutator_model is set,
also rewrites genes. Other brains use the word operators only, so they need no model.
Start short (--ticks 500) and read "llm_calls" to extrapolate.
"""
from __future__ import annotations

import argparse
import json

from promptevo.backends.factory import make_backend, make_rewriter
from promptevo.config import CONFIG_DIR, load_config, resolve
from promptevo.render import LEGEND, ascii_map
from promptevo.sim import Simulation


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--ticks", type=int, default=5000)
    ap.add_argument("--backend", default=None)
    ap.add_argument("--seed", type=int, default=None)
    ap.add_argument("--out", default="results/runs/smoke")
    ap.add_argument("--snapshots", type=int, default=3)
    ap.add_argument("--world", default=None,
                    help="world overlay from configs/worlds/ (default: the flat Lab 1 world)")
    a = ap.parse_args()
    cfg = load_config(a.profile, extra_files=[CONFIG_DIR / "worlds" / f"{a.world}.yaml"] if a.world else None)
    name = a.backend or cfg.backend.name
    backend = make_backend(name, cfg)
    rewriter, rw_model = make_rewriter(cfg) if name in ("llm", "teacher", "ollama_policy") else (None, None)
    sim = Simulation(cfg, backend, seed=a.seed, out_dir=resolve(a.out),
                     rewriter=rewriter, rewriter_model=rw_model)
    every = max(1, a.ticks // max(1, a.snapshots))
    if a.snapshots:
        print(LEGEND)
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
    if hasattr(backend, "calls"):
        print(f"llm_calls={backend.calls} failures={getattr(backend, 'failures', 0)} "
              f"backend_queries={s['backend_queries']} (memo hit rate {s['memo_hit_rate']})")
    print("details:", resolve(a.out))


if __name__ == "__main__":
    main()
