from experiments.gene_report import Run
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.genome import LOCI
from promptevo.sim import Simulation


def test_gene_report_invariants(tmp_path):
    cfg = load_config("small", {"evolution": {"p_mut": 0.0}})
    sim = Simulation(cfg, RuleBasedBackend(), seed=3, out_dir=tmp_path)
    sim.run(1500)
    run = Run(tmp_path)
    f = run.fitness(min_carriers=1)
    assert f["n_dead"] == sum(sim.c.deaths.values())
    for locus in LOCI:   # every dead animal carries one allele per slot: carrier-weighted fitness is exactly 1
        xs = [v for aid, v in f["alleles"].items() if run.alleles[aid]["locus"] == locus]
        assert abs(sum(v["n"] * v["fitness"] for v in xs) / sum(v["n"] for v in xs) - 1) < 1e-9
    freq = run.frequencies(500)
    t0, pop0, counts0 = freq[0]
    assert t0 == 0 and pop0 == cfg.agents.init_pop
    for locus in LOCI:
        assert sum(n for aid, n in counts0.items() if run.alleles[aid]["locus"] == locus) == pop0
    assert freq[-1][1] == len(sim.agents)


def test_unfinished_run_is_rebuilt_from_events(tmp_path):
    cfg = load_config("small", {"evolution": {"p_mut": 0.3}})
    made = []

    def rewriter(prompt, seed):
        made.append(seed)
        return f"Mutant gene number {len(made)}."
    sim = Simulation(cfg, RuleBasedBackend(), seed=4, out_dir=tmp_path, rewriter=rewriter, rewriter_model="fake")
    for _ in range(600):
        sim.step()
    sim.checkpoint()                                  # last alleles.jsonl save
    n_saved = len(made)
    for _ in range(400):
        sim.step()
    sim.log.flush()                                   # then a hard stop: no summary, no final population
    with (tmp_path / "events.jsonl").open("a", encoding="utf-8") as f:
        f.write('{"kind": "death", "t": 10')          # a line cut short mid-write
    assert len(made) > n_saved                        # mutants exist that only the events know
    run = Run(tmp_path)
    assert not run.finished and run.summary["ticks"] == 1000
    assert sorted(x["id"] for x in run.final) == sorted(a.id for a in sim.agents)
    assert run.summary["births"] == sim.c.births and run.summary["deaths"] == dict(sim.c.deaths)
    for x in run.final:
        assert all(run.text(aid) for aid in x["genome"])
    assert run.frequencies(500)[-1][1] == len(sim.agents) and run.predation(500)
