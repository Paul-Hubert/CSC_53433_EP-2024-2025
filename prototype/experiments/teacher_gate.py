"""Decision-model gate (was S4.3 "teacher gate"): does the LLM brain read the genes?

    python -m experiments.teacher_gate [--modes points,table] [--n-obs 24] [--workers 4] [--model TAG]

Since rev. 2026-09-30 the LLM is the decision backend itself, so this gate is the
main G1/G2 check (plan §A2), per mode. "table" is the cheap simulation mode; the
agreement column shows how much batching K situations per call changes answers.

Runs the E1 suite on a small set (contrast pairs + 10 founders + 10 random-text,
× n observations) for each teacher mode, compares modes (mean JSD between their
distributions), and writes results/teacher_gate.md. Pass criteria (plan S4.3):
directed sign accuracy ≥ 0.85 and MI_G(founders) clearly above MI_G(random).
Iterate on prompts/teacher_v*.md or the model at most 3 times; log each in STATUS.md.
"""
from __future__ import annotations

import argparse
import json

import numpy as np

from experiments.e1_sensitivity import build_sets, evaluate, report_lines
from experiments.make_obs import load_obs
from promptevo import metrics as M
from promptevo.backends.ollama_policy import TeacherBackend
from promptevo.config import load_config, resolve
from promptevo.founder import AllelePools
from promptevo.genome import AlleleRegistry
from promptevo.llm.ollama_client import client_from_config
from promptevo.progress import Progress

GATE_SIGN_ACC = 0.85
GATE_MI_MARGIN = 2.0          # MI_G founders must be ≥ 2× MI_G random text


def pick_obs(obs, n, rng):
    """Keep every directed-test tag represented, then fill randomly."""
    by_tag = {}
    for i, o in enumerate(obs):
        for t in o.tags():
            by_tag.setdefault(t, []).append(i)
    chosen = []
    for t in sorted(by_tag):
        for i in rng.permutation(by_tag[t])[:3]:
            if int(i) not in chosen:
                chosen.append(int(i))
    rest = [i for i in rng.permutation(len(obs)) if int(i) not in chosen]
    chosen += [int(i) for i in rest[: max(0, n - len(chosen))]]
    return [obs[i] for i in sorted(chosen[:n])]


def gate(res: dict) -> dict:
    ok_dir = res["sign_acc"] >= GATE_SIGN_ACC
    ok_mi = res["mi_g_founders"] >= GATE_MI_MARGIN * max(res["mi_g_random"], 1e-3)
    return {"directed": ok_dir, "mi_margin": ok_mi, "pass": ok_dir and ok_mi}


def run_gate(cfg, client, modes, n_obs, workers, prompt, model, logs=True, obs=None):
    rng = np.random.default_rng(cfg.seed)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    obs = pick_obs(obs or load_obs(), n_obs, rng)
    sets = build_sets(cfg, reg, pools, rng, dict(founders=10, random=10, edit_parents=0, edits=0))
    results, Ps = {}, {}
    for mode in modes:
        b = TeacherBackend(client, model, prompt, mode=mode, style=cfg.backend.obs_style,
                           k=int(cfg.ollama.ksample_k), workers=workers, table_k=int(cfg.policy.table_k))
        prog = Progress(resolve(cfg.paths.logs_dir), f"teacher_gate_{mode}") if logs else None
        res, P, _ = evaluate(b, reg, sets, obs, prog, chunk=16)
        res["failures"] = b.failures
        res["gate"] = gate(res)
        results[mode], Ps[mode] = res, P
        if prog:
            prog.finish(res["calls"])
    agreement = None
    if len(modes) == 2:
        agreement = float(np.mean(M.jsd(Ps[modes[0]], Ps[modes[1]])))
    return results, agreement


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--modes", default="points,table")
    ap.add_argument("--n-obs", type=int, default=24)
    ap.add_argument("--workers", type=int, default=1)
    ap.add_argument("--model", default=None)
    ap.add_argument("--prompt", default="prompts/teacher_v1.md")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    model = a.model or cfg.policy.model or cfg.ollama.teacher_model
    if not model:
        raise SystemExit("set policy.model in configs/base.yaml or pass --model")
    client = client_from_config(cfg)
    modes = a.modes.split(",")
    results, agreement = run_gate(cfg, client, modes, a.n_obs, a.workers, resolve(a.prompt), model)
    lines = [f"# Teacher gate — {model} ({a.prompt}, digest {client.digest(model)})", ""]
    lines.append("| mode | sign acc | ΔP | MI_G founders | MI_G random | MI_O | failures | pass |")
    lines.append("|---|---|---|---|---|---|---|---|")
    for m, r in results.items():
        lines.append(f"| {m} | {r['sign_acc']:.2f} | {r['mean_dp']:.3f} | {r['mi_g_founders']:.3f} | "
                     f"{r['mi_g_random']:.3f} | {r['mi_o_founders']:.3f} | {r['failures']} | {r['gate']['pass']} |")
    if agreement is not None:
        lines.append(f"\nMode agreement (mean JSD {modes[0]} vs {modes[1]}, lower = closer): {agreement:.3f}")
    for m, r in results.items():
        lines += [""] + report_lines(r, f"Detail — {m}")[4:]
    out = resolve(cfg.paths.results_dir)
    (out / "teacher_gate.md").write_text("\n".join(lines) + "\n")
    (out / "teacher_gate.json").write_text(json.dumps({"model": model, "prompt": a.prompt,
                                                        "results": results, "agreement": agreement},
                                                       indent=1, default=float))
    print("\n".join(lines[:4 + len(results) + (2 if agreement is not None else 0)]))


if __name__ == "__main__":
    main()
