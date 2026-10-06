from experiments.gene_report import Run
from experiments.gene_swap import genomes_at, swap
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.genome import ACTIONS
from promptevo.sim import Simulation


def test_swap_changes_only_the_slot(tmp_path):
    cfg = load_config("small", {"evolution": {"p_mut": 0.0}})
    sim = Simulation(cfg, RuleBasedBackend(), seed=2, out_dir=tmp_path)
    sim.run(600)
    run = Run(tmp_path)
    alive = genomes_at(run, 600)
    assert sorted(map(tuple, alive)) == sorted(tuple(a.genome.alleles) for a in sim.agents)
    r = swap(run, RuleBasedBackend(), 600, "eat", ["Always eat.", "Never eat."], n_genomes=4)
    P = r["P"]
    assert P.shape == (2, 4, len(ACTIONS)) and r["obs"] > 0
    k = ACTIONS.index("eat")
    assert (P[0, :, k] > P[1, :, k]).all()                     # the keyword brain reads always / never
