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
