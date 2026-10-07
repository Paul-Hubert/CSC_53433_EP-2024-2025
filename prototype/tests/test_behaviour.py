import numpy as np

from promptevo.actions import do_eat, do_flee
from promptevo.backends.base import Query
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.genome import ACTIONS
from promptevo.obs_text import render
from promptevo.perception import DEFAULT_SCALE, Observation, PredatorObservation, band, make_scale, sense
from promptevo.sim import Agent, Simulation
from promptevo.species import PREDATOR
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
    p = sim.predators[0]
    p.y, p.x = y, x + 2
    a = Agent(999, y, x, 50.0, sim.agents[0].genome)
    before = cheb(a.y, a.x, p.y, p.x)
    do_flee(a, w, [a], [p], cfg, np.random.default_rng(0))
    assert cheb(a.y, a.x, p.y, p.x) > before


def test_eat_on_food_gains_energy(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    w = sim.world
    y, x = _open_cell(w)
    w.food[:] = False
    w.food[y, x] = True
    a = Agent(999, y, x, 50.0, sim.agents[0].genome)
    do_eat(a, w, [a], [], cfg, np.random.default_rng(0))
    assert a.energy == 50.0 + cfg.agents.eat_gain and not w.food[y, x]


def test_invalid_eat_falls_back_to_wander(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    sim.world.food[:] = False
    a = sim.agents[0]
    do_eat(a, sim.world, sim.agents, sim.predators, cfg, np.random.default_rng(0))
    assert a.invalid


def test_distance_bands_and_vision(cfg):
    assert make_scale(cfg, "prey") == make_scale(cfg, "predator") == DEFAULT_SCALE   # same vision for both
    s = DEFAULT_SCALE
    assert [band(d, s) for d in (0, 1, 2, 4, 5, 10, 11, 20, 21)] == \
        ["adjacent", "adjacent", "close", "close", "medium", "medium", "far", "far", "none"]
    assert band(None, s) == "none"
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    a, p = sim.agents[0], sim.predators[0]
    sim.agents, sim.predators = [a], [p]
    a.y, a.x = 24, 10
    for dx, want in ((3, "close"), (8, "medium"), (15, "far"), (30, "none")):
        p.y, p.x = 24, 10 + dx
        assert sense(a, sim.world, sim.agents, sim.predators, cfg).predator == want
        assert sense(p, sim.world, sim.agents, sim.predators, cfg).prey == want   # both see equally far


def test_partner_readiness_is_seen_far_away(cfg):
    from promptevo.config import load_config
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    a, b = sim.agents[0], sim.agents[1]
    sim.agents = [a, b]
    (a.y, a.x), (b.y, b.x) = (24, 5), (24, 20)                 # 15 cells: far
    b.age, b.energy = 300, 90.0
    o = sense(a, sim.world, sim.agents, sim.predators, cfg)
    assert o.animal == "far" and o.animal_ready is True       # partner_range 20: the whole vision
    old = load_config("small", {"perception": {"partner_range": 4}})
    assert sense(a, sim.world, sim.agents, sim.predators, old).animal_ready is None   # the rule before 2026-10-07
    b.y, b.x = 24, 7
    assert sense(a, sim.world, sim.agents, sim.predators, old).animal_ready is True
    p, q = sim.predators[0], sim.predators[1]
    sim.predators = [p, q]
    (p.y, p.x), (q.y, q.x) = (40, 5), (40, 23)                 # predators too
    q.age, q.energy = 300, 90.0
    assert sense(p, sim.world, sim.agents, sim.predators, cfg).animal_ready is True


def test_obs_text_styles():
    o = Observation("low", "close", "none", "adjacent", True, "adult")
    v1 = render(o, "V1")
    assert v1 == ("Energy: low. Food: 2-4 cells away. Predator: none within 20 cells. "
                  "Animal: 1 cell away, ready to mate. Age: adult.")
    assert render(o, "V2").startswith("I am hungry") and "The nearest food is 2-4 cells away." in render(o, "V2")
    assert "Food: here." in render(Observation("low", "here", "far", "none"), "V1")
    q = PredatorObservation("high", "medium", "far", None, "young")
    assert render(q, "V1") == ("Energy: high. Prey: 5-10 cells away. Other predator: 11-20 cells away. "
                               "Age: young.")
    assert "The nearest prey is 5-10 cells away." in render(q, "V2")
    wide = Observation("low", "far", "none", "none", None, "adult", (2, 5, 12, 30))
    assert "Food: 13-30 cells away. Predator: none within 30 cells." in render(wide, "V1")


def test_rule_based_directed_and_gibberish(reg_pools):
    reg, pools = reg_pools
    b = RuleBasedBackend()
    o = Observation("medium", "close", "close", "adjacent", True, "adult")
    for l in ACTIONS:
        pro, anti = pools.contrast_pair(l)
        p = b.decide([Query(reg.genome_key(g), reg.genes(g), o) for g in (pro, anti)])
        assert p[0, ACTIONS.index(l)] > p[1, ACTIONS.index(l)], l
    ctrl = pools.sample_control(np.random.default_rng(0))
    neu = pools.neutral_genome()
    p = b.decide([Query(reg.genome_key(g), reg.genes(g), o) for g in (ctrl, neu)])
    assert np.allclose(p[0], p[1])


def test_rule_based_reads_predator_genes(cfg, reg_pools):
    from promptevo.founder import AllelePools
    reg, _ = reg_pools
    pools = AllelePools(reg, cfg.paths.data_dir, species=PREDATOR)
    b = RuleBasedBackend()
    o = PredatorObservation("medium", "close", "adjacent", True, "adult")
    for k, a in enumerate(PREDATOR.actions):
        pro, anti = pools.contrast_pair(a)
        p = b.decide([Query(reg.genome_key(g), reg.genes(g), o, "predator") for g in (pro, anti)])
        assert p.shape == (2, 3) and p[0, k] > p[1, k], a
    hungry_far = PredatorObservation("low", "far", "none")
    full_far = PredatorObservation("high", "far", "none")
    g = pools.neutral_genome()
    p = b.decide([Query(reg.genome_key(g), reg.genes(g), x, "predator") for x in (hungry_far, full_far)])
    assert p[0, 0] > p[1, 0] and p[1, 1] > p[0, 1]      # hungry hunts, full rests
