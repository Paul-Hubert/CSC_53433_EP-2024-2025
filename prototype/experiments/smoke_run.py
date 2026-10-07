"""Smoke run: python -m experiments.smoke_run [--profile full] --ticks 5000 [--backend rule_based|llm]
                                            [--world terrain_preview] [--minutes 720]

The default world is the full one, 96 x 96 (since 2026-10-07: in the 48 x 48 small world
the LLM-read prey didn't hold against the predators, and in 64 x 64 the few predators
didn't breed); --profile small (48 x 48) for quick checks.

Genes mutate through the mutator LLM (ollama.mutator_model, see evolution/mutation.py) with
every brain, so Ollama must be running; --no-mutation runs without it (crossover only).
With --backend llm the LLM also decides (policy.model). Start short (--ticks 500) and read
"llm_calls" to extrapolate.

Long runs:
- Stop cleanly by creating logs/run_<name>.stop (looked for every 100 ticks). Ctrl-C, a failed
  model call or any error also stop cleanly: alleles.jsonl, summary.json and
  final_population.json are written, and the progress file says why.
- Resume by running the same command again (with --minutes set to what is left). Every model
  answer is cached and the run is deterministic, so the part already done replays without
  model calls, then the run continues.
- Events and stats are flushed every 500 ticks and alleles.jsonl is saved every 5 000, so a
  hard stop (power cut) loses little; gene_report reads such a run.
- Windows is asked not to sleep while the run goes; run_info.json records what ran.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
import traceback
from pathlib import Path

from promptevo.backends.factory import MutatorUnavailable, make_backend, make_rewriter
from promptevo.config import CONFIG_DIR, ROOT, load_config, resolve
from promptevo.llm.ollama_client import client_from_config
from promptevo.progress import Progress, keep_awake
from promptevo.render import LEGEND, ascii_map
from promptevo.sim import Simulation

STOP_CHECK_EVERY = 100    # ticks between looks for the stop file
PROGRESS_EVERY = 500      # progress file; events and stats flushed
CHECKPOINT_EVERY = 5000   # events and stats synced to disk, alleles.jsonl saved


def run(sim: Simulation, ticks: int, minutes: float | None = None, stop_file: Path | None = None,
        prog: Progress | None = None, snapshot_every: int = 0) -> str:
    """Step `sim` up to tick `ticks`. Returns why it stopped: "ticks", "time limit", "stop file",
    "interrupted" or "error: ..." (traceback printed). Never raises, so the caller can save the run."""
    started = time.time()
    try:
        while sim.t < ticks:
            sim.step()
            if sim.t % PROGRESS_EVERY == 0:
                sim.log.flush()
                if prog:
                    prog.update(sim.t, pop=len(sim.agents), predators=len(sim.predators),
                                births=sim.c.births, mutations=sim.mutator.stats["ok"])
            if sim.t % CHECKPOINT_EVERY == 0:
                sim.checkpoint()
            if snapshot_every and sim.t % snapshot_every == 0:
                print(f"--- t={sim.t} prey={len(sim.agents)} predators={len(sim.predators)}", flush=True)
                print(ascii_map(sim.world, sim.agents, sim.predators, max_w=40, max_h=14), flush=True)
            if stop_file and sim.t % STOP_CHECK_EVERY == 0 and stop_file.exists():
                stop_file.unlink(missing_ok=True)          # used up: running the command again resumes
                return "stop file"
            if minutes and time.time() - started > 60 * minutes:
                return "time limit"
    except KeyboardInterrupt:
        return "interrupted"
    except Exception as e:                                 # e.g. a model call failed after its retries
        traceback.print_exc()
        return f"error: {type(e).__name__}: {e}"
    return "ticks"


def git(*args: str) -> str | None:
    try:
        return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, timeout=10).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        return None


def write_info(out: Path, a, cfg, brain: str, seed: int, mutator_model: str | None, client=None) -> None:
    """run_info.json: what ran (command, code version, models, settings), kept with the results."""
    info = {"command": "python -m experiments.smoke_run " + " ".join(sys.argv[1:]),
            "started": time.strftime("%Y-%m-%d %H:%M:%S"), "brain": brain, "seed": seed,
            "profile": a.profile, "world": a.world or "flat (Lab 1)", "ticks": a.ticks, "minutes": a.minutes,
            "git": {"commit": git("rev-parse", "--short", "HEAD"),
                    "uncommitted_files": len((git("status", "--porcelain", "--untracked-files=no") or "").splitlines())},
            "config": {k: cfg[k] for k in ("evolution", "policy", "agents", "predators", "perception", "world", "sim")}}
    if client is not None:
        models = sorted({m for m in (cfg.policy.model if brain == "llm" else None, mutator_model) if m})
        try:
            info["ollama"] = {"host": cfg.ollama.host, "version": client.version(),
                              "models": {m: client.digest(m) for m in models}}
        except RuntimeError as e:
            info["ollama"] = {"host": cfg.ollama.host, "error": str(e)[:200]}
    (out / "run_info.json").write_text(json.dumps(info, indent=1, default=str), encoding="utf-8")


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="full", help="world size: full 96 x 96 (default), small 48 x 48")
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
    backend = make_backend(name, cfg, strict=True)        # a failed model call stops the run, uncached
    client = client_from_config(cfg)
    try:
        rewriter, rw_model = make_rewriter(cfg, client=client, check=True)
    except MutatorUnavailable as e:
        raise SystemExit(f"{e}.\nStart Ollama (and `ollama pull {cfg.ollama.mutator_model}`), "
                         "or run with --no-mutation.")
    print(f"mutation: {rw_model} (T={cfg.evolution.temperature}, p_mut={cfg.evolution.p_mut})"
          if rewriter else "mutation: off")
    out = resolve(a.out)
    job = f"run_{out.name}"
    logs = resolve(cfg.paths.logs_dir)
    stop_file = logs / f"{job}.stop"
    if stop_file.exists():
        stop_file.unlink()
        print(f"removed an old stop file: {stop_file}")
    for f in ("summary.json", "final_population.json"):    # from an earlier attempt; written again at the end
        (out / f).unlink(missing_ok=True)
    sim = Simulation(cfg, backend, seed=a.seed, out_dir=out, rewriter=rewriter, rewriter_model=rw_model)
    write_info(out, a, cfg, name, sim.seed, rw_model, client if (name == "llm" or rewriter) else None)
    sim.checkpoint()                                          # alleles.jsonl with the founders, right away
    started = time.time()
    prog = Progress(logs, job, total=a.ticks, deadline=started + 60 * a.minutes if a.minutes else None)
    print(f"stop cleanly: create {stop_file}; resume: run the same command again", flush=True)
    if a.snapshots:
        print(LEGEND)
    with keep_awake():
        why = run(sim, a.ticks, a.minutes, stop_file, prog,
                  snapshot_every=max(1, a.ticks // a.snapshots) if a.snapshots else 0)
    s = sim.finish()
    s.update(stopped=why, minutes=round((time.time() - started) / 60, 1),
             llm_calls=getattr(backend, "calls", 0), failures=getattr(backend, "failures", 0),
             mutation_calls=client.cache.misses if rewriter else 0)   # model calls not answered by the cache
    (out / "summary.json").write_text(json.dumps(s, indent=1))
    if why in ("ticks", "time limit"):
        prog.finish(sim.t)
    elif why.startswith("error"):
        prog.fail(sim.t, why)
    else:
        prog.stop(sim.t, why)
    if why == "time limit":
        print(f"time limit: stopped after {a.minutes:g} min at t={sim.t}")
    elif why != "ticks":
        print(f"stopped at t={sim.t} ({why}). To resume, run the same command again: "
              "the part already done replays from the cache.")
    keep = {k: s[k] for k in ("ticks", "pop_final", "births", "immigrants", "deaths",
                              "mean_lifespan", "max_gen", "memo_hit_rate", "invalid_rate", "exhausted_share",
                              "backend_s", "alleles", "mutations")}
    print("prey:", json.dumps(keep))
    print("prey actions:", s["action_share"])
    pr = s["predators"]
    print("predators:", json.dumps({k: pr[k] for k in ("pop_final", "births", "immigrants", "deaths", "kills",
                                                       "portions", "mean_lifespan", "max_gen", "memo_hit_rate",
                                                       "invalid_rate", "exhausted_share")}))
    print("predator actions:", pr["action_share"])
    if hasattr(backend, "calls"):
        print(f"llm_calls={backend.calls} failures={getattr(backend, 'failures', 0)} "
              f"backend_queries={s['backend_queries'] + pr['backend_queries']} "
              f"(memo hit rate prey {s['memo_hit_rate']}, predators {pr['memo_hit_rate']})")
    if rewriter:
        print(f"mutation_calls={s['mutation_calls']} (not answered by the cache)")
    print("details:", out)
    if why.startswith("error"):
        raise SystemExit(1)


if __name__ == "__main__":
    main()
