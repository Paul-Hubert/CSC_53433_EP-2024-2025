import numpy as np
import pytest

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import CONFIG_DIR, load_config
from promptevo.render import ascii_map
from promptevo.rng import Streams
from promptevo.sim import Simulation
from promptevo.world import GRASS, World


@pytest.fixture
def terrain_cfg():
    return load_config("small", extra_files=[CONFIG_DIR / "worlds" / "terrain_preview.yaml"])


def test_lab1_world_is_flat_with_uniform_food(cfg):
    w = World(cfg, Streams(5).get("world"))
    assert w.walkable.all() and not w.near_water.any()
    assert np.unique(w.regrow_p).size == 1                 # same regrowth chance everywhere
    assert abs(w.food.mean() - cfg.world.food_initial_fraction) < 0.02


def test_world_fractions_and_connectivity(terrain_cfg):
    cfg = terrain_cfg
    s = Streams(5)
    w = World(cfg, s.get("world"))
    assert 0.6 <= w.walkable.mean() <= 0.8
    assert not w.food[~w.walkable].any()
    w2 = World(cfg, Streams(5).get("world"))
    assert np.array_equal(w.terrain, w2.terrain)


def test_nobody_enters_blocked_cells(terrain_cfg):
    cfg = terrain_cfg
    sim = Simulation(cfg, RuleBasedBackend(), seed=3)
    for _ in range(1000):
        sim.step()
        for a in sim.agents:
            assert sim.world.terrain[a.y, a.x] == GRASS
        for p in sim.predators:
            assert sim.world.terrain[p.y, p.x] == GRASS


def test_ascii_map_symbols_are_unambiguous(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=2)
    sim.step()
    p = sim.predators[0]
    sim.agents = [a for a in sim.agents if (a.y, a.x) != (p.y, p.x)]
    grid = ascii_map(sim.world, sim.agents, sim.predators, max_w=sim.world.w, max_h=sim.world.h).splitlines()
    assert grid[p.y][p.x] == "P"
