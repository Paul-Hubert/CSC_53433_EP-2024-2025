"""Options against boom-bust crashes (2026-10-08, all off by default; experiments/crash_sweep.py)."""
import numpy as np

from promptevo.actions import do_flee, do_hunt, strike_p
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.perception import sense
from promptevo.sim import Simulation
from promptevo.species import PREDATOR, PREY
from promptevo.world import MOUNTAIN, World, largest_component


def _sim(over=None, seed=1):
    cfg = load_config("small", over)
    return cfg, Simulation(cfg, RuleBasedBackend(), seed=seed)


def _ready(a, cfg_species):
    a.age, a.energy, a.bred = cfg_species.maturity + 1, cfg_species.energy_max, False


def test_options_are_off_by_default():
    cfg, sim = _sim()
    assert sim.world.cover is None and not sim.world.in_cover(5, 5)
    p, a = sim.predators[0], sim.agents[0]
    assert strike_p(p, a, sim.predators, cfg.predators) == cfg.predators.kill_p
    assert not sim._hatch(PREY) and sim._hunting_ground(p, cfg.predators)


def test_prey_in_cover_are_hidden_and_fleeing_prey_run_into_cover():
    cfg, sim = _sim({"world": {"cover_fraction": 0.1, "cover_seek": 6}, "predators": {"kill_p": 1.0}})
    w = sim.world
    w.cover[:] = False
    w.cover[20, 14] = True
    a, p = sim.agents[0], sim.predators[0]
    sim.agents, sim.predators = [a], [p]
    (a.y, a.x), (p.y, p.x) = (20, 14), (20, 16)
    assert sense(p, w, sim.agents, sim.predators, cfg).prey == "none"    # hidden
    rng = np.random.default_rng(0)
    do_hunt(p, w, sim.agents, sim.predators, cfg, rng)
    assert not a.killed and p.invalid                                    # nothing to hunt: wanders
    assert do_flee(a, w, sim.agents, sim.predators, cfg, rng) == 0       # stays in cover
    a.y, a.x = 20, 17                                                    # out of cover, 3 cells away from it
    do_flee(a, w, sim.agents, sim.predators, cfg, rng)
    assert a.x < 17                                                      # runs toward the cover, not away


def test_crowding_predators_lower_the_strike_chance():
    cfg, sim = _sim({"predators": {"interference": 1.0, "interference_radius": 3}})
    a = sim.agents[0]
    p, q = sim.predators[:2]
    (a.y, a.x), (p.y, p.x), (q.y, q.x) = (20, 20), (20, 21), (22, 20)
    assert strike_p(p, a, [p, q], cfg.predators) == cfg.predators.kill_p / 2
    q.y = 30
    assert strike_p(p, a, [p, q], cfg.predators) == cfg.predators.kill_p


def test_ridges_split_the_world_into_connected_patches():
    cfg = load_config("small", {"world": {"patches": 3, "wall_gap": 4}})
    w = World(cfg, np.random.default_rng(0))
    row = w.h // 3
    assert (w.terrain[row] == MOUNTAIN).sum() == w.w - 3 * 4                # one 4-cell gap per segment
    assert largest_component(w.walkable).sum() == w.walkable.sum()      # all patches reachable


def test_predators_breed_only_with_enough_prey():
    for over in ({"prey_per_predator": 100}, {"breed_prey_seen": 1000}):
        cfg, sim = _sim({"predators": over})
        p, q = sim.predators[:2]
        sim.predators = [p, q]
        for b in (p, q):
            _ready(b, cfg.predators)
        (p.y, p.x), (q.y, q.x) = (20, 20), (20, 21)
        p.action = "mate"
        sim._breed(PREDATOR)
        assert sim.cs["predator"].births == 0, over
    cfg, sim = _sim({"predators": {"breed_prey_seen": 1}})
    p, q = sim.predators[:2]
    sim.predators = [p, q]
    for b in (p, q):
        _ready(b, cfg.predators)
    (p.y, p.x), (q.y, q.x) = (20, 20), (20, 21)
    sim.agents[0].y, sim.agents[0].x = 22, 22
    p.action = "mate"
    sim._breed(PREDATOR)
    assert sim.cs["predator"].births == 1


def test_egg_bank_refills_the_floor_with_real_offspring():
    cfg, sim = _sim({"evolution": {"egg_bank": True, "egg_ticks": 2000, "p_mut": 0.0}})
    a, b = sim.agents[:2]
    for x in (a, b):
        _ready(x, cfg.agents)
    (a.y, a.x), (b.y, b.x) = (20, 20), (20, 21)
    a.action = "mate"
    sim._breed(PREY)
    assert sim.cs["prey"].births == 1 and len(sim.eggs["prey"]) == 1
    sim.agents = []                                              # every prey animal dies
    sim.step()
    c = sim.cs["prey"]
    assert c.hatched == 1 and c.immigrants == cfg.agents.floor - 1      # one egg, then founders
    hatched = [x for x in sim.agents if x.parents == (a.id, b.id)]
    assert len(hatched) == 1 and hatched[0].generation == 1
