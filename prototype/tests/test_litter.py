"""Litters (rev. 2026-10-08): a mating makes 2-4 babies, each with its own genes and mutations."""
import pytest

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.sim import Simulation
from promptevo.species import PREY


def _pair(over=None, seed=1, energy=90.0):
    cfg = load_config("small", over)
    sim = Simulation(cfg, RuleBasedBackend(), seed=seed)
    a, b = sim.agents[:2]
    for x in (a, b):
        x.age, x.energy, x.bred = cfg.agents.maturity + 1, energy, False
    (a.y, a.x), (b.y, b.x) = (20, 20), (20, 21)
    return cfg, sim, a, b


def test_a_mating_makes_a_litter_and_the_parents_pay_for_each_baby():
    cfg, sim, a, b = _pair()
    n = len(sim.agents)
    sim._birth([a, b], room=100)
    k = len(sim.agents) - n
    assert 2 <= k <= 4 and sim.cs["prey"].births == k
    share = cfg.agents.child_energy / 2
    assert a.energy == pytest.approx(90.0 - share * k) and b.offspring == k
    assert all(c.energy == cfg.agents.child_energy and c.parents == (a.id, b.id) for c in sim.agents[-k:])


def test_litter_sizes_cover_the_range_and_babies_get_their_own_genes():
    sizes, mixed = set(), False
    for seed in range(12):
        _, sim, a, b = _pair(seed=seed)
        n = len(sim.agents)
        sim._birth([a, b], room=100)
        babies = sim.agents[n:]
        sizes.add(len(babies))
        mixed |= len({c.genome.alleles for c in babies}) > 1     # separate crossovers
    assert sizes == {2, 3, 4} and mixed


def test_the_litter_is_cut_to_what_the_parents_can_pay_and_to_the_cap():
    _, sim, a, b = _pair(energy=50.0)
    n = len(sim.agents)
    sim._birth([a, b], room=100)                                    # 50 pays for 2 babies (20 each)
    assert len(sim.agents) - n == 2 and a.energy == pytest.approx(10.0)
    _, sim, a, b = _pair()
    n = len(sim.agents)
    sim._birth([a, b], room=1)                                      # one place left under the cap
    assert len(sim.agents) - n == 1


def test_litter_one_is_the_old_rule():
    _, sim, a, b = _pair({"agents": {"litter": [1, 1]}})
    n = len(sim.agents)
    sim._birth([a, b], room=100)
    assert len(sim.agents) - n == 1
