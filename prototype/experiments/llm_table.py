"""The LLM brain's answers from a run, as a table: situation -> action distribution.

    python -m experiments.llm_table results/runs/long_v4_llm [--genomes 400] [--out data/llm_table_long_v4.json]

No model calls. The policy cache (cache/policy.sqlite) keeps every answer the LLM gave, keyed
by a hash of (model, prompt, genome, situation). For the genomes that lived longest in the run
and every possible situation of each species, this rebuilds the key and looks it up. The table
averages the answers found over genomes, weighted by the ticks each genome was carried. Used by
`crash_sweep --brain llmtable`: the population dynamics of LLM-read animals at keyword-brain
speed (genes have no effect there: one average animal per species).
"""
from __future__ import annotations

import argparse
import ast
import hashlib
import itertools
import json
import sqlite3
from collections import defaultdict

import numpy as np

from promptevo.backends.base import Query
from promptevo.backends.ollama_policy import LLMPolicyBackend
from promptevo.config import load_config, resolve
from promptevo.perception import DIST, Observation, PredatorObservation, make_scale
from promptevo.species import PREDATOR, PREY

BANDS = DIST + ("none",)
LEVELS = ("low", "medium", "high")


def animals():
    """(animal band, ready): readiness is sensed whenever another animal is in sight (partner_range = vision)."""
    yield "none", None
    for b in DIST:
        yield b, True
        yield b, False


def situations(species: str, scale: tuple):
    if species == "prey":
        for e, f, p, (a, r), age, s in itertools.product(LEVELS, ("here",) + BANDS, BANDS, animals(),
                                                         ("young", "adult"), LEVELS):
            yield Observation(e, f, p, a, r, age, scale, s)
    else:
        for e, q, (a, r), age, s, c in itertools.product(LEVELS, BANDS, animals(), ("young", "adult"), LEVELS, BANDS):
            yield PredatorObservation(e, q, a, r, age, scale, s, c)


def obs_id(o) -> str:
    """A compact situation key shared with crash_sweep: the observation's fields, scale left out."""
    if o.species == "prey":
        return f"{o.energy}|{o.food}|{o.predator}|{o.animal}|{o.animal_ready}|{o.age}|{o.stamina}"
    return f"{o.energy}|{o.prey}|{o.animal}|{o.animal_ready}|{o.age}|{o.stamina}|{o.carcass}"


def genome_ticks(run: str):
    """{species: {tuple of allele ids: animal-ticks}} and the allele texts."""
    al = {}
    for line in open(f"{run}/alleles.jsonl", encoding="utf-8"):
        a = json.loads(line)
        al[a["id"]] = a
    born, died, end = {}, {}, 0
    for line in open(f"{run}/events.jsonl", encoding="utf-8"):
        e = json.loads(line)
        end = max(end, int(e["t"]))
        if e["kind"] in ("founder", "birth", "immigrant"):
            g = e["genome"]
            born[e["id"]] = (int(e["t"]), tuple(ast.literal_eval(g) if isinstance(g, str) else g))
        elif e["kind"] == "death":
            died[e["id"]] = int(e["t"])
    ticks = {"prey": defaultdict(int), "predator": defaultdict(int)}
    for i, (t, g) in born.items():
        sp = "predator" if al[g[0]]["locus"].startswith("predator.") else "prey"
        ticks[sp][g] += max(1, died.get(i, end) - t)
    return ticks, al


def genome_key(texts: list[str], species: str) -> str:
    """As AlleleRegistry.genome_key."""
    blob = json.dumps(texts if species == "prey" else [species, *texts], ensure_ascii=False)
    return hashlib.sha256(blob.encode()).hexdigest()[:24]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--genomes", type=int, default=400, help="longest-carried genomes per species")
    ap.add_argument("--out", default="data/llm_table_long_v4.json")
    ap.add_argument("--profile", default="full")
    a = ap.parse_args()
    run = str(resolve(a.run))
    info = json.load(open(f"{run}/run_info.json", encoding="utf-8"))
    cfg = load_config(a.profile, overrides={"policy": info["config"]["policy"]})
    backend = LLMPolicyBackend.from_config(cfg)
    con = sqlite3.connect(f"file:{resolve(cfg.paths.cache_dir) / 'policy.sqlite'}?mode=ro", uri=True)
    cache = dict(con.execute("select k, v from kv"))
    ticks, al = genome_ticks(run)
    out = {"run": a.run, "model": cfg.policy.model, "digest": backend.client.digest(cfg.policy.model),
           "prompts": backend.prompt_ids, "genomes": a.genomes}
    for sp in (PREY, PREDATOR):
        top = sorted(ticks[sp.name].items(), key=lambda kv: -kv[1])[:a.genomes]
        covered = sum(w for _, w in top) / sum(ticks[sp.name].values())
        sums, wsum, nhit = defaultdict(lambda: np.zeros(len(sp.actions))), defaultdict(float), defaultdict(int)
        obs = list(situations(sp.name, make_scale(cfg, sp)))
        for g, w in top:
            texts = [al[x]["text"] for x in g]
            gk = genome_key(texts, sp.name)
            genes = dict(zip(sp.actions, texts))
            for o in obs:
                v = cache.get(backend._key(Query(gk, genes, o, sp.name)))
                if v is not None:
                    oid = obs_id(o)
                    sums[oid] += w * np.asarray(json.loads(v))
                    wsum[oid] += w
                    nhit[oid] += 1
        table = {oid: {"p": [round(float(x), 5) for x in sums[oid] / wsum[oid]], "genomes": nhit[oid]}
                 for oid in sums}
        out[sp.name] = {"actions": list(sp.actions), "genome_ticks_covered": round(covered, 3),
                        "situations": len(obs), "found": len(table), "table": table}
        print(f"{sp.name}: {len(top)} genomes ({covered:.0%} of animal-ticks), {len(table)}/{len(obs)} situations "
              f"answered, median {int(np.median(list(nhit.values())) if nhit else 0)} genomes per situation")
    resolve(a.out).write_bytes(json.dumps(out).encode("utf-8"))
    print("details:", a.out)


if __name__ == "__main__":
    main()
