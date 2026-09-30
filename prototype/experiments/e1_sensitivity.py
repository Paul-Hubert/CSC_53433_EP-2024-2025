"""E1 — gene-sensitivity suite (plan §A9/§A10). Works with any backend.

    python -m experiments.e1_sensitivity --backend rule_based --tag rb
    python -m experiments.e1_sensitivity --backend laya --placement P4 --style V1 --tag laya_zs
    python -m experiments.e1_sensitivity --backend llm --tag llm_table     (LLM brain; needs policy.model)

Writes results/e1_<tag>.json + .md and prints a ≤ 20-line summary with G1–G3.
Long runs: launch with nohup; progress in logs/e1_<tag>.progress.json.
"""
from __future__ import annotations

import argparse
import difflib
import json

import numpy as np

from experiments.make_obs import load_obs
from promptevo import metrics as M
from promptevo.backends.base import Query
from promptevo.config import load_config, resolve
from promptevo.evolution.mutation import Mutator
from promptevo.founder import AllelePools
from promptevo.genome import ACTION_LOCI, ACTIONS, LOCI, AlleleRegistry, Genome
from promptevo.perception import RELEVANT_TAG
from promptevo.progress import Progress

SIZES = {"small": dict(founders=30, random=30, edit_parents=10, edits=4),
         "full": dict(founders=60, random=60, edit_parents=20, edits=6)}
GATES = {"G1_sign_acc": 0.85, "G1_mean_dp": 0.25, "G2_mi_g": 0.25, "G2_mi_o": 0.25,
         "G3_ratio": 0.5, "G3_rho": 0.3}


def make_backend(name, cfg, a):
    from promptevo.backends.factory import make_backend as mk
    return mk(name, cfg, placement=a.placement, style=a.style, workers=a.workers, mode=a.mode)


def single_edit(mut: Mutator, reg: AlleleRegistry, g: Genome, rng) -> Genome | None:
    for _ in range(20):
        locus = LOCI[int(rng.integers(len(LOCI)))]
        op = ["intensity", "negate", "condition_swap", "synonym"][int(rng.integers(4))]
        new = mut.mutate_text(locus, reg.text(g.at(locus)), op, rng, 0)
        if new:
            return g.replace(locus, reg.add(locus, new, "mutant", parent_id=g.at(locus), operator=op).id)
    return None


def text_distance(reg, g1: Genome, g2: Genome) -> float:
    a = " | ".join(reg.text(x) for x in g1.alleles)
    b = " | ".join(reg.text(x) for x in g2.alleles)
    return 1.0 - difflib.SequenceMatcher(None, a, b).ratio()


def build_sets(cfg, reg: AlleleRegistry, pools: AllelePools, rng, size: dict) -> dict:
    """Genome sets for the suite: founders, random-text controls, neutral, contrast pairs, edits."""
    mut = Mutator(cfg, reg, pools.founders)
    founders = list(dict.fromkeys(pools.sample_founder(rng) for _ in range(size["founders"])))
    controls = [pools.sample_control(rng) for _ in range(size["random"])]
    edits = []
    for parent in founders[: size["edit_parents"]]:
        for _ in range(size["edits"]):
            child = single_edit(mut, reg, parent, rng)
            if child is not None:
                edits.append((parent, child))
    return {"founders": founders, "controls": controls, "neutral": pools.neutral_genome(),
            "pairs": {l: pools.contrast_pair(l) for l in ACTION_LOCI}, "edits": edits}


def evaluate(backend, reg: AlleleRegistry, sets: dict, obs: list, prog=None, chunk: int = 64):
    """Run the backend on every (genome, observation) and compute the §A9 metrics.
    Returns (metrics dict, P[g, o, a], genome list)."""
    founders, controls, neutral = sets["founders"], sets["controls"], sets["neutral"]
    pairs, edits = sets["pairs"], sets["edits"]
    genomes = list(dict.fromkeys(founders + controls + [neutral]
                                 + [g for p in pairs.values() for g in p] + [c for _, c in edits]))
    gi = {g: i for i, g in enumerate(genomes)}
    queries = [Query(reg.genome_key(g), reg.genes(g), o) for g in genomes for o in obs]
    P = np.zeros((len(queries), len(ACTIONS)))
    for s in range(0, len(queries), chunk):
        P[s:s + chunk] = backend.decide(queries[s:s + chunk])
        if prog:
            prog.update(min(s + chunk, len(queries)))
    P = P.reshape(len(genomes), len(obs), len(ACTIONS))
    F = P[[gi[g] for g in founders]]
    C = P[[gi[g] for g in controls]]
    Pn = P[gi[neutral]]

    directed = {}
    for l, (pro, anti) in pairs.items():
        mask = np.array([RELEVANT_TAG[l] in o.tags() for o in obs])
        directed[l] = M.directed(P[gi[pro]], P[gi[anti]], ACTIONS.index(l), mask)
    cells = [(d["sign_acc"], d["n"]) for d in directed.values() if d["n"]]
    sign_acc = sum(s * n for s, n in cells) / max(1, sum(n for _, n in cells))
    mean_dp = float(np.mean([d["mean_dp"] for d in directed.values() if d["n"]]))

    pairs_f = [(i, j) for i in range(len(founders)) for j in range(i + 1, len(founders))]
    unrel = [M.behaviour_distance(F[i], F[j]) for i, j in pairs_f]
    unrel_t = [text_distance(reg, founders[i], founders[j]) for i, j in pairs_f]
    if edits:
        edit_d = [M.behaviour_distance(P[gi[p]], P[gi[c]]) for p, c in edits]
        edit_t = [text_distance(reg, p, c) for p, c in edits]
        loc = M.locality(edit_d, unrel)
        rho = M.spearman(edit_t + unrel_t, edit_d + unrel)
    else:
        loc, rho = {"median_edit": float("nan"), "median_unrelated": float("nan"), "ratio": float("nan")}, float("nan")
    gib = float(np.mean([M.behaviour_distance(C[i], Pn) for i in range(len(controls))])) if controls else float("nan")
    fn = float(np.mean([M.behaviour_distance(F[i], Pn) for i in range(len(founders))]))
    res = {"n_genomes": len(genomes), "n_obs": len(obs), "calls": len(queries),
           "mi_g_founders": M.mi_genome(F), "mi_g_random": M.mi_genome(C) if controls else float("nan"),
           "mi_o_founders": M.mi_obs(F), "directed": directed, "sign_acc": sign_acc, "mean_dp": mean_dp,
           "locality": loc, "rho_text_behaviour": rho, "gibberish_to_neutral": gib,
           "founder_to_neutral": fn, "approx_probs": getattr(backend, "approx", 0)}
    res["gates"] = {
        "G1": sign_acc >= GATES["G1_sign_acc"] and mean_dp >= GATES["G1_mean_dp"],
        "G2": res["mi_g_founders"] >= GATES["G2_mi_g"] and res["mi_o_founders"] >= GATES["G2_mi_o"] and gib <= 0.5 * fn,
        "G3": loc["ratio"] <= GATES["G3_ratio"] and rho >= GATES["G3_rho"]}
    return res, P, genomes


def report_lines(res: dict, title: str) -> list[str]:
    loc = res["locality"]
    lines = [f"# {title}", "", f"genomes={res['n_genomes']} obs={res['n_obs']} calls={res['calls']}", "",
             "| metric | value |", "|---|---|",
             f"| MI_G founders (bits) | {res['mi_g_founders']:.3f} |",
             f"| MI_G random text | {res['mi_g_random']:.3f} |",
             f"| MI_O founders | {res['mi_o_founders']:.3f} |",
             f"| directed sign accuracy | {res['sign_acc']:.2f} |",
             f"| directed mean ΔP | {res['mean_dp']:.3f} |",
             f"| locality ratio (edit/unrelated) | {loc['ratio']:.2f} |",
             f"| Spearman ρ text vs behaviour | {res['rho_text_behaviour']:.2f} |",
             f"| gibberish→neutral / founder→neutral | {res['gibberish_to_neutral']:.3f} / {res['founder_to_neutral']:.3f} |",
             f"| gates | {res['gates']} |", "", "| locus | ΔP | sign acc | n |", "|---|---|---|---|"]
    lines += [f"| {l} | {d['mean_dp']:.3f} | {d['sign_acc']:.2f} | {d['n']} |" for l, d in res["directed"].items()]
    return lines


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--backend", default="rule_based")
    ap.add_argument("--profile", default="small")
    ap.add_argument("--placement", default=None)
    ap.add_argument("--style", default=None)
    ap.add_argument("--obs", default=None)
    ap.add_argument("--tag", default=None)
    ap.add_argument("--workers", type=int, default=None)
    ap.add_argument("--mode", default=None, help="llm backend: table | points | ksample")
    ap.add_argument("--chunk", type=int, default=64)
    a = ap.parse_args()
    cfg = load_config(a.profile)
    tag = a.tag or a.backend
    rng = np.random.default_rng(cfg.seed)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    obs = load_obs(a.obs)
    backend = make_backend(a.backend, cfg, a)
    sets = build_sets(cfg, reg, pools, rng, SIZES[a.profile])
    n_calls = len(obs) * (len(sets["founders"]) + len(sets["controls"]) + 15 + len(sets["edits"]))
    prog = Progress(resolve(cfg.paths.logs_dir), f"e1_{tag}", total=n_calls)
    res, _, _ = evaluate(backend, reg, sets, obs, prog, a.chunk)
    res.update({"tag": tag, "backend": a.backend, "placement": a.placement, "style": a.style})
    out = resolve(cfg.paths.results_dir)
    (out / f"e1_{tag}.json").write_text(json.dumps(res, indent=1, default=float))
    lines = report_lines(res, f"E1 — {tag} (backend={a.backend} placement={a.placement} style={a.style})")
    (out / f"e1_{tag}.md").write_text("\n".join(lines) + "\n")
    prog.finish(res["calls"])
    print("\n".join(lines[2:15]))


if __name__ == "__main__":
    main()
