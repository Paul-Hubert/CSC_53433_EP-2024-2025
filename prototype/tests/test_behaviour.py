import numpy as np

from promptevo.actions import do_eat, do_flee
from promptevo.backends.base import Query
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.genome import ACTIONS
from promptevo.obs_text import render
from promptevo.perception import Observation
from promptevo.sim import Agent, Simulation
from promptevo.world import cheb


def _open_cell(w):
    for y in range(5, w.h - 5):
        for x in range(5, w.w - 5):
            if w.walkable[y - 4:y + 5, x - 4:x + 5].all():
                return y, x
    raise AssertionError("no open area")


def test_flee_increases_distance(cfg, reg_pools):
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    w = sim.world
    y, x = _open_cell(w)
    w.predators[0].y, w.predators[0].x = y, x + 2
    a = Agent(999, y, x, 50.0, sim.agents[0].genome)
    before = cheb(a.y, a.x, y, x + 2)
    do_flee(a, w, [a], cfg, np.random.default_rng(0))
    assert cheb(a.y, a.x, y, x + 2) > before


def test_eat_on_food_gains_energy(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    w = sim.world
    y, x = _open_cell(w)
    w.food[:] = False
    w.food[y, x] = True
    a = Agent(999, y, x, 50.0, sim.agents[0].genome)
    do_eat(a, w, [a], cfg, np.random.default_rng(0))
    assert a.energy == 50.0 + cfg.agents.eat_gain and not w.food[y, x]


def test_invalid_eat_falls_back_to_wander(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    sim.world.food[:] = False
    a = sim.agents[0]
    do_eat(a, sim.world, sim.agents, cfg, np.random.default_rng(0))
    assert a.invalid


def test_obs_text_styles():
    o = Observation("low", "near", "none", "near", True, False, "adult")
    assert "ready to mate" in render(o, "V1") and "weaker" in render(o, "V1")
    assert render(o, "V2").startswith("I am hungry")


def test_rule_based_directed_and_gibberish(reg_pools):
    reg, pools = reg_pools
    b = RuleBasedBackend()
    o = Observation("medium", "near", "near", "near", True, False, "adult")
    for l in ACTIONS:
        pro, anti = pools.contrast_pair(l)
        p = b.decide([Query(reg.genome_key(g), reg.genes(g), o) for g in (pro, anti)])
        assert p[0, ACTIONS.index(l)] > p[1, ACTIONS.index(l)], l
    ctrl = pools.sample_control(np.random.default_rng(0))
    neu = pools.neutral_genome()
    p = b.decide([Query(reg.genome_key(g), reg.genes(g), o) for g in (ctrl, neu)])
    assert np.allclose(p[0], p[1])
