"""Smoke run: python -m experiments.smoke_run --profile small --ticks 5000 [--backend rule_based|llm]
                                            [--world terrain_preview]

Genes mutate through the mutator LLM (ollama.mutator_model, see evolution/mutation.py) with
every brain, so Ollama must be running; --no-mutation runs without it (crossover only).
With --backend llm the LLM also decides (policy.model). Start short (--ticks 500) and read
"llm_calls" to extrapolate.
"""
from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

from promptevo.backends.factory import MutatorUnavailable, make_backend, make_rewriter
from promptevo.config import CONFIG_DIR, load_config, resolve
from promptevo.progress import Progress
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
    ap.add_argument("--no-mutation", action="store_true", help="no gene mutation (no mutator LLM needed)")
    ap.add_argument("--minutes", type=float, default=None,
                    help="stop after this much wall-clock time, even before --ticks (time-boxed runs)")
    a = ap.parse_args()
    cfg = load_config(a.profile, extra_files=[CONFIG_DIR / "worlds" / f"{a.world}.yaml"] if a.world else None,
                      overrides={"evolution": {"p_mut": 0.0}} if a.no_mutation else None)
    name = a.backend or cfg.backend.name
    backend = make_backend(name, cfg)
    try:
        rewriter, rw_model = make_rewriter(cfg, check=True)
    except MutatorUnavailable as e:
        raise SystemExit(f"{e}.\nStart Ollama (and `ollama pull {cfg.ollama.mutator_model}`), "
                         "or run with --no-mutation.")
    print(f"mutation: {rw_model} (T={cfg.evolution.temperature}, p_mut={cfg.evolution.p_mut})"
          if rewriter else "mutation: off")
    sim = Simulation(cfg, backend, seed=a.seed, out_dir=resolve(a.out),
                     rewriter=rewriter, rewriter_model=rw_model)
    every = max(1, a.ticks // max(1, a.snapshots))
    prog = Progress(resolve(cfg.paths.logs_dir), f"run_{Path(a.out).name}", total=a.ticks)
    if a.snapshots:
        print(LEGEND)
    started = time.time()
    for i in range(a.ticks):
        sim.step()
        if sim.t % 500 == 0:
            prog.update(sim.t, pop=len(sim.agents), births=sim.c.births, mutations=sim.mutator.stats["ok"])
        if a.snapshots and sim.t % every == 0:
            print(f"--- t={sim.t} pop={len(sim.agents)}", flush=True)
            print(ascii_map(sim.world, sim.agents, max_w=40, max_h=14), flush=True)
        if a.minutes and time.time() - started > 60 * a.minutes:
            print(f"time limit: stopped after {a.minutes:g} min at t={sim.t}")
            break
    s = sim.finish()
    prog.finish(sim.t)
    keep = {k: s[k] for k in ("ticks", "pop_final", "births", "immigrants", "deaths",
                              "mean_lifespan", "max_gen", "memo_hit_rate", "invalid_rate",
                              "backend_s", "alleles", "mutations")}
    print(json.dumps(keep))
    print("actions:", s["action_share"])
    if hasattr(backend, "calls"):
        print(f"llm_calls={backend.calls} failures={getattr(backend, 'failures', 0)} "
              f"backend_queries={s['backend_queries']} (memo hit rate {s['memo_hit_rate']})")
    print("details:", resolve(a.out))


if __name__ == "__main__":
    main()
