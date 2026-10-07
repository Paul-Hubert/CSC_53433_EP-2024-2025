"""Stamina, speed and carcasses (rev. 2026-10-07)."""
import numpy as np
import pytest

from promptevo.actions import do_eat, do_hunt
from promptevo.backends.base import Query
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.founder import AllelePools
from promptevo.obs_text import render
from promptevo.perception import Observation, PredatorObservation, sense
from promptevo.sim import Simulation
from promptevo.species import PREDATOR, PREY
from promptevo.world import Carcass


def _sim(over=None, seed=1):
    cfg = load_config("small", over)
    return cfg, Simulation(cfg, RuleBasedBackend(), seed=seed)


def test_moving_costs_stamina_and_standing_still_brings_it_back():
    cfg, sim = _sim()
    ac = cfg.agents
    a = sim.agents[0]
    sim.agents, sim.predators = [a], []
    a.y, a.x = 20, 20
    sim.world.food[:] = False
    sim.world.food[20, 26] = True
    assert a.stamina == ac.stamina_max == 60
    a.action, e = "eat", a.energy
    sim._act(PREY)                                       # walks one cell toward the food
    assert (a.y, a.x) == (20, 21) and a.stamina == 59
    assert a.energy == pytest.approx(e - ac.cost_base - ac.cost_move)
    a.action, e = "rest", a.energy
    sim._act(PREY)                                       # still: stamina comes back, at a price
    assert a.stamina == 60 and a.energy == pytest.approx(e - ac.cost_rest - ac.cost_regen)
    e = a.energy
    sim._act(PREY)                                       # full: resting costs only cost_rest
    assert a.stamina == 60 and a.energy == pytest.approx(e - ac.cost_rest)


def test_without_stamina_nobody_moves():
    cfg, sim = _sim({"predators": {"kill_p": 0.0}})
    a, p = sim.agents[0], sim.predators[0]
    sim.agents, sim.predators = [a], [p]
    (a.y, a.x), (p.y, p.x) = (20, 10), (20, 20)
    rng = np.random.default_rng(0)
    p.stamina = 0.0
    assert do_hunt(p, sim.world, sim.agents, sim.predators, cfg, rng) == 0 and (p.y, p.x) == (20, 20)
    p.stamina = 1.5                                      # enough for one cell, not two
    assert do_hunt(p, sim.world, sim.agents, sim.predators, cfg, rng) == 1
    sim.world.food[:] = False
    sim.world.food[20, 14] = True
    a.stamina = 0.0
    assert do_eat(a, sim.world, sim.agents, sim.predators, cfg, rng) == 0 and (a.y, a.x) == (20, 10)
    n = sim.cs["prey"].exhausted
    a.action = "eat"
    sim._act(PREY)                                       # counted as out of breath; stood still: recovers
    assert sim.cs["prey"].exhausted == n + 1 and a.stamina == cfg.agents.stamina_regen


def test_a_carcass_feeds_up_to_two_other_predators_then_rots():
    cfg, sim = _sim({"predators": {"kill_p": 1.0}})
    pc = cfg.predators
    a = sim.agents[0]
    killer, b, c, d = sim.predators[:4]
    sim.agents, sim.predators = [a], [killer, b, c, d]
    (a.y, a.x), (killer.y, killer.x) = (20, 20), (20, 21)
    for q, cell in zip((b, c, d), ((19, 19), (21, 19), (19, 20))):  # next to the prey
        (q.y, q.x), q.energy = cell, 40.0
    rng = np.random.default_rng(0)
    do_hunt(killer, sim.world, sim.agents, sim.predators, cfg, rng)
    [carcass] = sim.world.carcasses
    assert a.killed and not carcass.edible_by(killer)   # the leftovers are for the others
    for q in (b, c):
        do_hunt(q, sim.world, sim.agents, sim.predators, cfg, rng)
        assert q.energy == 40.0 + pc.carcass_gain and q.food_eaten == 1 and not carcass.edible_by(q)
        assert q.digest == round(pc.digest_ticks * pc.carcass_gain / pc.kill_gain) == 25
    assert carcass.portions == 0 and sim.world.portions_eaten == 2
    do_hunt(d, sim.world, sim.agents, sim.predators, cfg, rng)          # nothing left: searches
    assert d.energy == 40.0 and d.invalid
    sim.world.age_carcasses()
    assert sim.world.carcasses == []
    w = sim.world
    w.carcasses.append(Carcass(10, 10, 2, killer=-1, rot=pc.carcass_ticks))
    for _ in range(pc.carcass_ticks - 1):
        w.age_carcasses()
    assert len(w.carcasses) == 1
    w.age_carcasses()
    assert w.carcasses == []                             # rotten


def test_no_carcass_with_zero_portions():
    cfg, sim = _sim({"predators": {"kill_p": 1.0, "carcass_portions": 0}})
    a, p = sim.agents[0], sim.predators[0]
    sim.agents, sim.predators = [a], [p]
    (a.y, a.x), (p.y, p.x) = (20, 20), (20, 21)
    do_hunt(p, sim.world, sim.agents, sim.predators, cfg, np.random.default_rng(0))
    assert a.killed and sim.world.carcasses == []       # the rule before 2026-10-07


def test_stamina_and_carcass_are_sensed_and_written():
    cfg, sim = _sim({"predators": {"kill_p": 1.0}})
    a, p, q = sim.agents[0], sim.predators[0], sim.predators[1]
    sim.agents, sim.predators = [a], [p, q]
    (a.y, a.x), (p.y, p.x), (q.y, q.x) = (20, 20), (20, 21), (20, 24)
    o = sense(q, sim.world, sim.agents, sim.predators, cfg)
    assert (o.prey, o.carcass, o.stamina) == ("close", "none", "high")
    do_hunt(p, sim.world, sim.agents, sim.predators, cfg, np.random.default_rng(0))
    q.stamina = 5.0
    o = sense(q, sim.world, sim.agents, sim.predators, cfg)
    assert (o.prey, o.carcass, o.stamina) == ("none", "close", "low")
    assert sense(p, sim.world, sim.agents, sim.predators, cfg).carcass == "none"   # not its own kill
    assert "Stamina: low. Prey: none within 20 cells. Carcass: 2-4 cells away. Other predator:" in render(o, "V1")
    assert "I am out of breath." in render(o, "V2") and "The nearest carcass is 2-4 cells away." in render(o, "V2")
    prey_obs = sense(a, sim.world, sim.agents, sim.predators, cfg)
    assert prey_obs.stamina == "high" and render(prey_obs, "V1").startswith(f"Energy: {prey_obs.energy}. Stamina: high. Food:")
    assert "Stamina" not in render(Observation("low", "close", "none", "none"), "V1")      # not sensed: as before


def test_keyword_brain_catches_its_breath_and_goes_for_carcasses(cfg):
    from promptevo.genome import AlleleRegistry
    reg = AlleleRegistry()
    b = RuleBasedBackend()
    g = AllelePools(reg, cfg.paths.data_dir, species=PREDATOR).neutral_genome()
    rest, hunt = PREDATOR.actions.index("rest"), PREDATOR.actions.index("hunt")
    ask = lambda genome, obs, sp: b.decide([Query(reg.genome_key(genome), reg.genes(genome), o, sp) for o in obs])
    p = ask(g, [PredatorObservation("medium", "close", "none", stamina=s) for s in ("high", "low")], "predator")
    assert p[1, rest] > p[0, rest]                       # out of breath: rests more
    p = ask(g, [PredatorObservation("medium", "none", "none", carcass=c) for c in ("none", "close")], "predator")
    assert p[1, hunt] > p[0, hunt]                       # a carcass in sight draws it like prey
    tired = reg.make_genome({**dict.fromkeys(PREY.actions, "No preference."), "rest": "Rest when you are tired."})
    p = ask(tired, [Observation("low", "close", "none", "none", stamina=s) for s in ("high", "low")], "prey")
    assert p[1, PREY.actions.index("rest")] > p[0, PREY.actions.index("rest")]   # "tired" = out of breath
