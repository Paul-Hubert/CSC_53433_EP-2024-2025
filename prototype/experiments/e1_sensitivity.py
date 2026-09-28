"""E1 — gene-sensitivity suite (plan §A9/§A10). Works with any backend.

    python -m experiments.e1_sensitivity --backend rule_based --tag rb
    python -m experiments.e1_sensitivity --backend laya --placement P4 --style V1 --tag laya_zs
    python -m experiments.e1_sensitivity --backend teacher --tag teacher   (slow)

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
    if name == "rule_based":
        from promptevo.backends.rule_based import RuleBasedBackend
        return RuleBasedBackend()
    if name == "random":
        from promptevo.backends.random_policy import RandomBackend
        return RandomBackend()
    if name == "laya":
        from promptevo.backends.laya_backend import LayaBackend
        return LayaBackend.from_config(cfg, placement=a.placement or cfg.laya.placement,
                                       style=a.style or cfg.backend.obs_style)
    if name == "teacher":
        from promptevo.backends.ollama_policy import TeacherBackend
        from promptevo.cache import KVCache
        from promptevo.llm.ollama_client import OllamaClient
        oc = cfg.ollama
        client = OllamaClient(oc.host, KVCache(resolve(cfg.paths.cache_dir) / "ollama.sqlite"), oc.timeout_s)
        return TeacherBackend(client, oc.teacher_model, resolve("prompts/teacher_v1.md"),
                              oc.teacher_mode, a.style or cfg.backend.obs_style, oc.ksample_k,
                              workers=a.workers)
    raise SystemExit(f"unknown backend {name}")


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


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--backend", default="rule_based")
    ap.add_argument("--profile", default="small")
    ap.add_argument("--placement", default=None)
    ap.add_argument("--style", default=None)
    ap.add_argument("--obs", default=None)
    ap.add_argument("--tag", default=None)
    ap.add_argument("--workers", type=int, default=1)
    ap.add_argument("--chunk", type=int, default=64)
    a = ap.parse_args()
    cfg = load_config(a.profile)
    tag = a.tag or a.backend
    size = SIZES[a.profile]
    rng = np.random.default_rng(cfg.seed)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    mut = Mutator(cfg, reg, pools.founders)
    obs = load_obs(a.obs)
    backend = make_backend(a.backend, cfg, a)

    founders = list(dict.fromkeys(pools.sample_founder(rng) for _ in range(size["founders"])))
    controls = [pools.sample_control(rng) for _ in range(size["random"])]
    neutral = pools.neutral_genome()
    pairs = {l: pools.contrast_pair(l) for l in ACTION_LOCI}
    edits = []
    for parent in founders[: size["edit_parents"]]:
        for _ in range(size["edits"]):
            child = single_edit(mut, reg, parent, rng)
            if child is not None:
                edits.append((parent, child))
    genomes = list(dict.fromkeys(founders + controls + [neutral]
                                 + [g for p in pairs.values() for g in p] + [c for _, c in edits]))
    gi = {g: i for i, g in enumerate(genomes)}

    queries = [Query(reg.genome_key(g), reg.genes(g), o) for g in genomes for o in obs]
    prog = Progress(resolve(cfg.paths.logs_dir), f"e1_{tag}", total=len(queries))
    P = np.zeros((len(queries), len(ACTIONS)))
    for s in range(0, len(queries), a.chunk):
        P[s:s + a.chunk] = backend.decide(queries[s:s + a.chunk])
        prog.update(min(s + a.chunk, len(queries)))
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

    edit_d = [M.behaviour_distance(P[gi[p]], P[gi[c]]) for p, c in edits]
    edit_t = [text_distance(reg, p, c) for p, c in edits]
    unrel = [M.behaviour_distance(F[i], F[j]) for i in range(len(founders)) for j in range(i + 1, len(founders))]
    unrel_t = [text_distance(reg, founders[i], founders[j])
               for i in range(len(founders)) for j in range(i + 1, len(founders))]
    loc = M.locality(edit_d, unrel)
    rho = M.spearman(edit_t + unrel_t, edit_d + unrel)
    gib = float(np.mean([M.behaviour_distance(C[i], Pn) for i in range(len(controls))]))
    fn = float(np.mean([M.behaviour_distance(F[i], Pn) for i in range(len(founders))]))

    res = {"tag": tag, "backend": a.backend, "placement": a.placement, "style": a.style,
           "n_genomes": len(genomes), "n_obs": len(obs), "calls": len(queries),
           "mi_g_founders": M.mi_genome(F), "mi_g_random": M.mi_genome(C),
           "mi_o_founders": M.mi_obs(F), "directed": directed, "sign_acc": sign_acc, "mean_dp": mean_dp,
           "locality": loc, "rho_text_behaviour": rho, "gibberish_to_neutral": gib,
           "founder_to_neutral": fn, "approx_probs": getattr(backend, "approx", 0)}
    res["gates"] = {
        "G1": sign_acc >= GATES["G1_sign_acc"] and mean_dp >= GATES["G1_mean_dp"],
        "G2": res["mi_g_founders"] >= GATES["G2_mi_g"] and res["mi_o_founders"] >= GATES["G2_mi_o"] and gib <= 0.5 * fn,
        "G3": loc["ratio"] <= GATES["G3_ratio"] and rho >= GATES["G3_rho"]}
    out = resolve(cfg.paths.results_dir)
    (out / f"e1_{tag}.json").write_text(json.dumps(res, indent=1, default=float))
    lines = [f"# E1 — {tag}", "",
             f"backend={a.backend} placement={a.placement} style={a.style} genomes={len(genomes)} obs={len(obs)}", "",
             "| metric | value |", "|---|---|",
             f"| MI_G founders (bits) | {res['mi_g_founders']:.3f} |",
             f"| MI_G random text | {res['mi_g_random']:.3f} |",
             f"| MI_O founders | {res['mi_o_founders']:.3f} |",
             f"| directed sign accuracy | {sign_acc:.2f} |", f"| directed mean ΔP | {mean_dp:.3f} |",
             f"| locality ratio (edit/unrelated) | {loc['ratio']:.2f} |",
             f"| Spearman ρ text vs behaviour | {rho:.2f} |",
             f"| gibberish→neutral / founder→neutral | {gib:.3f} / {fn:.3f} |",
             f"| gates | {res['gates']} |", "", "| locus | ΔP | sign acc | n |", "|---|---|---|---|"]
    lines += [f"| {l} | {d['mean_dp']:.3f} | {d['sign_acc']:.2f} | {d['n']} |" for l, d in directed.items()]
    (out / f"e1_{tag}.md").write_text("\n".join(lines) + "\n")
    prog.finish(len(queries))
    print("\n".join(lines[2:14]))


if __name__ == "__main__":
    main()
