import json

import pytest

from experiments.gene_timeline import build, gene_drop
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.genome import LOCI
from promptevo.sim import Simulation


def write_run(d, events, alleles, ticks, final=True):
    d.mkdir(parents=True, exist_ok=True)
    (d / "events.jsonl").write_text("".join(json.dumps(e) + "\n" for e in events), encoding="utf-8")
    (d / "alleles.jsonl").write_text("".join(json.dumps(a) + "\n" for a in alleles), encoding="utf-8")
    if final:
        (d / "summary.json").write_text(json.dumps({"ticks": ticks, "births": 1, "deaths": {"predator": 2},
                                                   "max_gen": 1, "mutations": {"tried": 1, "ok": 1}}))
        (d / "final_population.json").write_text(json.dumps(
            [{"id": e["id"], "gen": e.get("gen", 0), "genome": e["genome"]} for e in events
             if e["kind"] == "birth"]))


def test_tiny_run_gives_known_numbers(tmp_path):
    """Two founders; their child carries a mutant eat gene; both founders die: the mutant sweeps."""
    founder = lambda n: [f"{l}:{n}" for l in LOCI]
    alleles = [{"id": f"{l}:{n}", "locus": l, "text": f"{l.capitalize()} rule {n}.", "origin": "founder"}
               for l in LOCI for n in (0, 1)]
    alleles.append({"id": "eat:2", "locus": "eat", "text": "Eat rule fast.", "origin": "mutant",
                    "parent_id": "eat:0", "operator": "llm#0"})
    child = ["eat:2"] + founder(0)[1:]
    events = [{"kind": "founder", "t": 0, "id": 0, "genome": founder(0)},
              {"kind": "founder", "t": 0, "id": 1, "genome": founder(1)},
              {"kind": "birth", "t": 200, "id": 2, "parents": [0, 1], "gen": 1, "genome": child,
               "mutations": [{"locus": "eat", "parent": "eat:0", "child": "eat:2", "prompt": 0, "text": "Eat rule fast."}]},
              {"kind": "death", "t": 300, "id": 0, "cause": "predator", "age": 300, "gen": 0, "food": 1, "offspring": 1, "steals": 0},
              {"kind": "death", "t": 400, "id": 1, "cause": "predator", "age": 400, "gen": 0, "food": 1, "offspring": 1, "steals": 0}]
    write_run(tmp_path, events, alleles, ticks=500)
    b = build(tmp_path, every=250, window=500, drops=400)
    eat = {r["t"]: r for r in b["rows"] if r["slot"] == "eat"}
    assert [t for t in eat] == [0, 250, 500]
    assert eat[0]["distinct"] == 2 and eat[0]["effective"] == 2 and eat[0]["mutant_share"] == 0
    assert eat[250]["distinct"] == 3 and eat[250]["effective"] == pytest.approx(3)
    assert eat[500]["pop"] == 1 and eat[500]["leader"] == "eat:2" and eat[500]["mutant_share"] == 1
    assert eat[500]["depth"] == 1 and eat[500]["families"] == 2
    sw = {x["gene"]: x for x in b["sweeps"]}["eat:2"]
    assert (sw["first"], sw["to_25"], sw["to_50"], sw["to_90"]) == (200, 50, 300, 300)
    assert sw["peak"] == 1 and sw["fixed_t"] == 500 and sw["fate"] == "fixed"
    assert sw["lineage"][0].startswith("Eat rule 0.") and sw["lineage"][-1].startswith("Eat rule fast.")
    assert b["scan"]["snaps"][-1]["common"] == [0, 1]           # the child descends from both founders
    assert b["sets"]["produced"]["n"] == 1 and b["sets"]["spread"]["n"] == 0 and b["sets"]["alive"]["n"] == 1
    drop = gene_drop(b["run"], 250, ["eat:2", "flee:0"], 400, seed=1)
    assert drop["eat:2"][-1]["expected"] == 1                   # the mutation happened in the child
    end = drop["flee:0"][-1]                                    # the child got flee from founder 0
    assert end["share"] == 1 and 0.4 < end["expected"] < 0.6 and 0.4 < end["p_hi"] < 0.6


def test_timeline_invariants_on_a_short_run(tmp_path):
    cfg = load_config("small", {"evolution": {"p_mut": 0.3}})
    made = []

    def rewriter(prompt, seed):
        made.append(seed)
        return f"Mutant rule number {len(made)}."
    sim = Simulation(cfg, RuleBasedBackend(), seed=8, out_dir=tmp_path, rewriter=rewriter, rewriter_model="fake")
    for _ in range(1500):
        sim.step()
    sim.checkpoint()                                  # still running: no summary yet
    b = build(tmp_path, every=250, window=500, drops=50)
    run, g = b["run"], b["genes"]
    assert not run.finished and b["scan"]["snaps"][-1]["t"] == 1500 and made
    assert b["scan"]["snaps"][0]["families"] == cfg.agents.init_pop
    for s in b["scan"]["snaps"]:
        for locus in LOCI:
            total = sum(by_t.get(s["t"], 0) for a, by_t in b["shares"].items() if run.alleles[a]["locus"] == locus)
            assert total == pytest.approx(1)          # the shares of a slot add up at every checkpoint
    for r in b["rows"]:
        assert 1 - 1e-9 <= r["effective"] <= r["distinct"] + 1e-9
    for a in run.alleles:                             # every lineage ends at a founder text
        assert run.alleles[g.root[a]]["origin"] != "mutant"
    eat = [a for a in run.alleles if run.alleles[a]["locus"] == "eat"]
    drop = gene_drop(run, 250, eat, 50)
    for i, t in enumerate(x["t"] for x in drop[eat[0]]):
        assert sum(drop[a][i]["expected"] for a in eat) == pytest.approx(1)
    assert all(abs(drop[a][0]["expected"] - drop[a][0]["share"]) < 1e-9 for a in eat)    # tick 0: founders only


def test_judge_asks_once_per_gene_within_budget(tmp_path, monkeypatch):
    from experiments import gene_timeline
    from promptevo.cache import KVCache
    from promptevo.llm.ollama_client import OllamaClient
    asked = []

    def transport(method, path, payload):
        if path == "/api/tags":
            return {"models": []}
        prompt = payload["messages"][0]["content"]
        asked.append(prompt)
        return {"message": {"content": json.dumps({"answer": "no" if "Never eat" in prompt else "yes"})}}
    cache = KVCache()
    monkeypatch.setattr("promptevo.llm.ollama_client.client_from_config",
                        lambda cfg: OllamaClient(cache=cache, transport=transport))
    founder = lambda n: [f"{l}:{n}" for l in LOCI]
    alleles = [{"id": f"{l}:{n}", "locus": l, "text": ("Never eat." if (l, n) == ("eat", 1) else f"{l} rule {n}."),
                "origin": "founder"} for l in LOCI for n in (0, 1)]
    write_run(tmp_path, [{"kind": "founder", "t": 0, "id": i, "genome": founder(i)} for i in (0, 1)], alleles, 10)
    run = gene_timeline.Run(tmp_path)
    usable, calls = gene_timeline.judge(run, ["eat:0", "eat:1", "flee:0"], load_config("small"), max_calls=2)
    assert usable == {"eat:0": True, "eat:1": False} and calls == 2          # the budget stops it
    assert '"Never eat."' in asked[1] and "when and how to eat" in asked[1]
    usable, _ = gene_timeline.judge(run, ["eat:0", "eat:1"], load_config("small"), max_calls=10)
    assert usable == {"eat:0": True, "eat:1": False} and len(asked) == 2     # answered from the cache
