import numpy as np

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.rng import Streams
from promptevo.sim import Simulation
from promptevo.world import GRASS, World


def test_world_fractions_and_connectivity(cfg):
    s = Streams(5)
    w = World(cfg, s.get("world"), s.get("predators"))
    assert 0.6 <= w.walkable.mean() <= 0.8
    assert not w.food[~w.walkable].any()
    w2 = World(cfg, Streams(5).get("world"), Streams(5).get("predators"))
    assert np.array_equal(w.terrain, w2.terrain)


def test_nobody_enters_blocked_cells(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=3)
    for _ in range(1000):
        sim.step()
        for a in sim.agents:
            assert sim.world.terrain[a.y, a.x] == GRASS
        for p in sim.world.predators:
            assert sim.world.terrain[p.y, p.x] == GRASS
