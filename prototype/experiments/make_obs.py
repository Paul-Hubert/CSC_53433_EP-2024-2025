"""S3.3 — build data/observations_v2.jsonl: synthetic key situations + frequent
situations sampled from a rule-based run.   python -m experiments.make_obs --profile small

Prey situations only. v2 (2026-10-07) uses the distance bands (adjacent, close, medium, far);
observations_v1.jsonl (near/far) belongs to the code before that date.
"""
from __future__ import annotations

import argparse
import itertools
import json
from collections import Counter
from dataclasses import asdict

import numpy as np

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config, resolve
from promptevo.perception import Observation, sense
from promptevo.sim import Simulation

SIZES = {"small": (32, 48), "full": (54, 150)}   # (synthetic, total)


def synthetic() -> list[Observation]:
    animals = [("none", None), ("adjacent", True), ("adjacent", False)]
    out = []
    for e, f, p, (an, ready) in itertools.product(
            ["low", "medium", "high"], ["none", "far", "close"], ["none", "close"], animals):
        out.append(Observation(e, f, p, an, ready, "adult"))
    return out


def obs_from_dict(row: dict) -> Observation:
    d = {k: v for k, v in row.items() if k in Observation.__annotations__}
    if "scale" in d:
        d["scale"] = tuple(d["scale"])
    return Observation(**d)


def load_obs(path=None) -> list[Observation]:
    path = resolve(path or "data/observations_v2.jsonl")
    obs = [obs_from_dict(json.loads(l)) for l in path.read_text().splitlines()]
    return list(dict.fromkeys(obs))


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--ticks", type=int, default=3000)
    ap.add_argument("--out", default="data/observations_v2.jsonl")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    n_syn, n_total = SIZES[a.profile]
    rng = np.random.default_rng(cfg.seed)
    syn = synthetic()
    syn = [syn[i] for i in sorted(rng.choice(len(syn), size=min(n_syn, len(syn)), replace=False))]
    sim = Simulation(cfg, RuleBasedBackend())
    seen = Counter()
    for _ in range(a.ticks):
        if sim.t % int(cfg.sim.decision_period) == 0:
            for ag in sim.agents:
                seen[sense(ag, sim.world, sim.agents, sim.predators, cfg)] += 1
        sim.step()
    sampled = [o for o, _ in seen.most_common() if o not in set(syn)][: n_total - len(syn)]
    rows = [(o, "synthetic") for o in syn] + [(o, "sampled") for o in sampled]
    path = resolve(a.out)
    with path.open("w", newline="\n") as f:          # LF on every OS (the committed file)
        for o, src in rows:
            f.write(json.dumps({**asdict(o), "source": src, "tags": sorted(o.tags())}) + "\n")
    tags = Counter(t for o, _ in rows for t in o.tags())
    print(f"wrote {len(rows)} observations ({len(syn)} synthetic, {len(sampled)} sampled) → {path}")
    print("tag counts:", dict(tags))


if __name__ == "__main__":
    main()
