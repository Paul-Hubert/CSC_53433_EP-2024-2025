"""Do both species last without rescue? Boom-bust screening without model calls.

    python -m experiments.crash_sweep [--variants baseline,cover20] [--kill-p 0.1,0.2] [--seeds 4]
                                      [--ticks 20000] [--workers 24] [--brain llmlike|keyword] [--tag crash_v1]

Every run has floor 0 for both species (no newcomers with founder genes: an extinction is
final) and no mutation (no model calls), on the full 96 x 96 world. A run stops at the first
extinction. Each variant is one stabilising mechanism (VARIANTS); --kill-p repeats every
variant at each predator strike chance. The default brain (--brain llmtable) answers with the
LLM's own answers from the run long_v4_llm, looked up by situation (experiments/llm_table.py):
the keyword brain's prey flee too well to show the crashes, also with the LLM's action mix
(llmlike).

Per variant and kill_p: runs where both species lasted, median ticks to the first extinction
(runs that lasted count as --ticks), who died out, population ranges, how often the predators
sat at the cap (all animals together since 2026-10-09; before, per species), and the mean
generation at the end (evolution time).
Writes results/<tag>.md and .json; logs/<tag>.progress.json while it runs.
"""
from __future__ import annotations

import argparse
import json
import time
from concurrent.futures import ProcessPoolExecutor, as_completed

import numpy as np

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import CONFIG_DIR, load_config, resolve
from promptevo.progress import Progress
from promptevo.sim import Simulation
from promptevo.species import SPECIES
from experiments.llm_table import obs_id

# --brain llmlike: the keyword brain plus fixed per-action logit offsets, fitted (11 rounds of
# log(target / observed), 2 seeds x 1 500 ticks, no mutation) so that both species' action shares
# match the LLM run long_v4_llm (2026-10-08; prey eat 34 %, mate 27 %, rest 21 %, follow 13 %,
# flee 6 %; predators mate 37 %, hunt 32 %, rest 20 %, follow 11 %) to within 3 points. Keyword
# prey flee 20 % and mate 5 %: they hold out where LLM prey crash.
LLMLIKE = {"prey": {"eat": -2.82, "mate": 3.32, "rest": 0.86, "follow": 0.41, "flee": -1.77},
           "predator": {"mate": -0.54, "hunt": -0.84, "rest": 0.74, "follow": 0.64}}


class LLMLike(RuleBasedBackend):
    name = "llmlike"

    def logits(self, q):
        return super().logits(q) + np.array([LLMLIKE[q.species][a] for a in SPECIES[q.species].actions])


class LLMTable(LLMLike):
    """--brain llmtable: the LLM brain's own answers, averaged over the genomes of a run
    (experiments/llm_table.py), looked up by situation; situations missing from the table fall
    back to LLMLike (counted in .missing)."""
    name = "llmtable"

    def __init__(self, path: str = "data/llm_table_long_v4.json"):
        super().__init__()
        d = json.loads(resolve(path).read_bytes())
        self.table = {sp: {k: np.asarray(v["p"]) / sum(v["p"]) for k, v in d[sp]["table"].items()}
                      for sp in ("prey", "predator")}
        self.asked = self.missing = 0

    def decide(self, queries):
        out = super().decide(queries)
        for i, q in enumerate(queries):
            self.asked += 1
            p = self.table[q.species].get(obs_id(q.obs))
            if p is None:
                self.missing += 1
            else:
                out[i] = p
        return out


def make_brain(brain: str):
    return {"llmtable": LLMTable, "llmlike": LLMLike, "keyword": RuleBasedBackend}[brain]()


NO_RESCUE = {"agents": {"floor": 0}, "predators": {"floor": 0}, "evolution": {"p_mut": 0.0}}
VARIANTS = {   # name: (overrides, world overlay file or None, what it tests)
    "baseline": ({}, None, "current rules"),
    # cap25 / cap20 / cap15 (a lower predator cap; results/crash_v2-v3.md) left with the per-species caps (2026-10-09)
    "food_low": ({"world": {"food_regrow_p": 0.0005}}, None, "less food (paradox of enrichment)"),
    "terrain": ({}, "terrain_preview", "noise terrain: water and mountains"),
    "cover10": ({"world": {"cover_fraction": 0.10, "cover_seek": 6}}, None,
                "cover on 10 % of cells, fleeing prey run to cover within 6 cells"),
    "cover20": ({"world": {"cover_fraction": 0.20, "cover_seek": 6}}, None, "cover on 20 %, seek 6"),
    "cover10_passive": ({"world": {"cover_fraction": 0.10, "cover_seek": 0}}, None,
                        "cover on 10 %, prey don't run to it"),
    "interf1": ({"predators": {"interference": 1.0, "interference_radius": 3}}, None,
                "kill_p / (1 + other predators within 3 cells of the prey)"),
    "interf3": ({"predators": {"interference": 3.0, "interference_radius": 3}}, None,
                "kill_p / (1 + 3 x other predators within 3 cells)"),
    "lg3": ({"predators": {"prey_per_predator": 3}}, None, "predators breed only with ≥ 3 prey per predator"),
    "lg5": ({"predators": {"prey_per_predator": 5}}, None, "predators breed only with ≥ 5 prey per predator"),
    "lg4": ({"predators": {"prey_per_predator": 4}}, None, "predators breed only with ≥ 4 prey per predator"),
    "seen5": ({"predators": {"breed_prey_seen": 5}}, None, "predators breed only with ≥ 5 prey within their vision"),
    "seen10": ({"predators": {"breed_prey_seen": 10}}, None, "predators breed only with ≥ 10 prey within their vision"),
    "seen15": ({"predators": {"breed_prey_seen": 15}}, None, "predators breed only with ≥ 15 prey within their vision"),
    "lg3_eggs": ({"predators": {"prey_per_predator": 3, "floor": 3}, "agents": {"floor": 10},
                  "evolution": {"egg_bank": True, "egg_ticks": 2000}}, None, "lg3 + floors refilled by eggs"),
    "seen10_eggs": ({"predators": {"breed_prey_seen": 10, "floor": 3}, "agents": {"floor": 10},
                     "evolution": {"egg_bank": True, "egg_ticks": 2000}}, None, "seen10 + floors refilled by eggs"),
    "pred_litter12": ({"predators": {"litter": [1, 2]}}, None, "predator litters of 1-2 (prey 2-4)"),
    "pred_slow": ({"predators": {"litter": [1, 1], "maturity": 300}}, None,
                  "predators: one cub per mating, adult at 300 ticks (prey 2-4, 150)"),
    "patches3": ({"world": {"patches": 3, "wall_gap": 4}}, None, "ridges: 3 x 3 patches, 4-cell gaps"),
    "patches4": ({"world": {"patches": 4, "wall_gap": 2}}, None, "ridges: 4 x 4 patches, 2-cell gaps"),
    "eggs": ({"agents": {"floor": 10}, "predators": {"floor": 3}, "evolution": {"egg_bank": True, "egg_ticks": 2000}},
             None, "floors back on, refilled by eggs of the last 2 000 ticks (founders only if none)"),
}


def merge(a: dict, b: dict) -> dict:
    out = dict(a)
    for k, v in b.items():
        out[k] = merge(out[k], v) if isinstance(v, dict) and isinstance(out.get(k), dict) else v
    return out


def one_run(variant: str, kill_p: float, seed: int, ticks: int, brain: str = "keyword") -> dict:
    over, world, _ = VARIANTS[variant]
    cfg = load_config("full", overrides=merge(merge(NO_RESCUE, over), {"predators": {"kill_p": kill_p}}),
                      extra_files=[CONFIG_DIR / "worlds" / f"{world}.yaml"] if world else None)
    backend = make_brain(brain)
    sim = Simulation(cfg, backend, seed=seed)
    cap = int(cfg.sim.cap)
    series, extinct = [], None
    started = time.time()
    while sim.t < ticks:
        sim.step()
        if sim.t % 100 == 0:
            series.append((sim.t, len(sim.agents), len(sim.predators)))
        if not sim.agents or not sim.predators:
            extinct = "prey" if not sim.agents else "predators"
            break
    prey = np.array([s[1] for s in series] or [0])
    pred = np.array([s[2] for s in series] or [0])
    gen = lambda ms: float(np.mean([a.generation for a in ms])) if ms else None
    return {"variant": variant, "kill_p": kill_p, "seed": seed, "ticks": sim.t, "extinct": extinct,
            "prey_min": int(prey.min()), "prey_mean": float(prey.mean()), "prey_max": int(prey.max()),
            "prey_cv": float(prey.std() / max(prey.mean(), 1e-9)),
            "pred_min": int(pred.min()), "pred_mean": float(pred.mean()), "pred_max": int(pred.max()),
            "at_cap": float(np.mean(prey + pred >= cap)),
            "gen_prey": gen(sim.agents), "gen_pred": gen(sim.predators),
            "births": sim.cs["prey"].births, "pred_births": sim.cs["predator"].births,
            "kills": sim.cs["prey"].deaths.get("predator", 0),
            "prey_starved": sim.cs["prey"].deaths.get("starvation", 0),
            "pred_starved": sim.cs["predator"].deaths.get("starvation", 0),
            "prey_migrated": sim.cs["prey"].deaths.get("migrated", 0),
            "pred_migrated": sim.cs["predator"].deaths.get("migrated", 0),
            "prey_ticks": int(sum(s[1] for s in series) * 100),
            "founders_added": sim.cs["prey"].immigrants + sim.cs["predator"].immigrants,
            "hatched": sim.cs["prey"].hatched + sim.cs["predator"].hatched,
            "table_missing": getattr(backend, "missing", 0) / max(1, getattr(backend, "asked", 0)),
            "series": series, "seconds": round(time.time() - started, 1)}


def summarise(runs: list[dict], ticks: int) -> list[dict]:
    rows = []
    for (v, kp) in sorted({(r["variant"], r["kill_p"]) for r in runs}, key=lambda x: (list(VARIANTS).index(x[0]), x[1])):
        rs = [r for r in runs if r["variant"] == v and r["kill_p"] == kp]
        m = lambda k: float(np.mean([r[k] for r in rs]))
        rows.append({"variant": v, "kill_p": kp, "n": len(rs), "lasted": sum(r["extinct"] is None for r in rs),
                     "median_ticks": float(np.median([r["ticks"] for r in rs])),
                     "prey_died": sum(r["extinct"] == "prey" for r in rs),
                     "pred_died": sum(r["extinct"] == "predators" for r in rs),
                     "prey": f"{min(r['prey_min'] for r in rs)}-{max(r['prey_max'] for r in rs)} (mean {m('prey_mean'):.0f})",
                     "pred": f"{min(r['pred_min'] for r in rs)}-{max(r['pred_max'] for r in rs)} (mean {m('pred_mean'):.0f})",
                     "prey_cv": m("prey_cv"), "at_cap": m("at_cap"),
                     "gen_prey": float(np.mean([r["gen_prey"] or 0 for r in rs if r["extinct"] is None] or [0])),
                     "gen_pred": float(np.mean([r["gen_pred"] or 0 for r in rs if r["extinct"] is None] or [0])),
                     "founders_added": m("founders_added"), "hatched": m("hatched"),
                     "table_missing": m("table_missing")})
    return rows


def report(rows: list[dict], a, minutes: float) -> list[str]:
    lines = [f"# Boom-bust screening — {a.tag}", "",
             f"Brain: {a.brain} (llmtable = the LLM's answers from long_v4_llm by situation; llmlike = keyword "
             f"brain with the LLM run's action mix), full 96 x 96 world, floor 0 for both species (no newcomers), no mutation; "
             f"{a.seeds} seeds x {a.ticks} ticks, stopped at the first extinction. {minutes:.0f} min.", "",
             "| variant | what | kill_p | lasted | median ticks | prey / predators died out first | prey min-max (mean) "
             "| predators min-max (mean) | prey CV | all animals at the cap | generation prey / pred (lasted) "
             "| founder newcomers / eggs hatched per run |",
             "|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        lines.append(f"| {r['variant']} | {VARIANTS[r['variant']][2]} | {r['kill_p']} | {r['lasted']}/{r['n']} "
                     f"| {r['median_ticks']:.0f} | {r['prey_died']} / {r['pred_died']} | {r['prey']} | {r['pred']} "
                     f"| {r['prey_cv']:.2f} | {r['at_cap']:.0%} "
                     f"| {r['gen_prey']:.0f} / {r['gen_pred']:.0f} | {r['founders_added']:.0f} / {r['hatched']:.0f} |")
    return lines


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--variants", default=",".join(VARIANTS))
    ap.add_argument("--kill-p", default="0.1,0.2")
    ap.add_argument("--seeds", type=int, default=4)
    ap.add_argument("--ticks", type=int, default=20000)
    ap.add_argument("--workers", type=int, default=24)
    ap.add_argument("--tag", default="crash_sweep")
    ap.add_argument("--brain", default="llmtable", choices=["llmtable", "llmlike", "keyword"])
    ap.add_argument("--profile", default="full", help="only full (the screening world)")
    a = ap.parse_args()
    jobs = [(v, float(kp), s, a.ticks, a.brain) for v in a.variants.split(",") for kp in a.kill_p.split(",")
            for s in range(1, a.seeds + 1)]
    prog = Progress(resolve("logs"), a.tag, total=len(jobs))
    started, runs = time.time(), []
    with ProcessPoolExecutor(max_workers=a.workers) as ex:
        futs = [ex.submit(one_run, *j) for j in jobs]
        for f in as_completed(futs):
            runs.append(f.result())
            prog.update(len(runs))
    prog.finish(len(runs))
    rows = summarise(runs, a.ticks)
    minutes = (time.time() - started) / 60
    out = resolve("results") / a.tag
    out.with_suffix(".json").write_bytes(json.dumps({"args": vars(a), "rows": rows, "runs": runs}).encode("utf-8"))
    out.with_suffix(".md").write_bytes(("\n".join(report(rows, a, minutes)) + "\n").encode("utf-8"))
    for r in rows:
        print(f"{r['variant']:12} kill_p {r['kill_p']}: lasted {r['lasted']}/{r['n']}, median {r['median_ticks']:.0f} ticks, "
              f"died prey/pred {r['prey_died']}/{r['pred_died']}, prey {r['prey']}, pred {r['pred']}, "
              f"at cap {r['at_cap']:.0%}")
    print(f"details: {out}.md")


if __name__ == "__main__":
    main()
