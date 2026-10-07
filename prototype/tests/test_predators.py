"""Predators as genetic animals (rev. 2026-10-07): hunt, digest, breed, newcomers, equal speed."""
import hashlib
import json

import numpy as np

from promptevo.actions import do_hunt
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.genome import AlleleRegistry
from promptevo.sim import Simulation
from promptevo.species import PREDATOR, PREY
from promptevo.world import cheb

SURE_KILL = {"predators": {"kill_p": 1.0}}


def _alone(sim, prey_pos, pred_pos):
    """Keep one prey animal and one predator, at the given cells."""
    a, p = sim.agents[0], sim.predators[0]
    sim.agents, sim.predators = [a], [p]
    (a.y, a.x), (p.y, p.x) = prey_pos, pred_pos
    return a, p


def test_hunt_steps_strikes_and_feeds():
    cfg = load_config("small", SURE_KILL)
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    a, p = _alone(sim, (24, 20), (24, 23))
    p.energy = 30.0
    moved = do_hunt(p, sim.world, sim.agents, sim.predators, cfg, np.random.default_rng(0))
    assert moved and cheb(a.y, a.x, p.y, p.x) == 2 and not a.killed      # one step, not next to it yet
    do_hunt(p, sim.world, sim.agents, sim.predators, cfg, np.random.default_rng(0))
    assert a.killed and a.killer == p.id and p.food_eaten == 1
    assert p.energy == 30.0 + cfg.predators.kill_gain and p.digest == cfg.predators.digest_ticks


def test_hunt_without_prey_in_sight_searches():
    cfg = load_config("small")
    sim = Simulation(cfg, RuleBasedBackend(), seed=1)
    a, p = _alone(sim, (2, 2), (45, 45))                     # 43 cells apart, vision 20
    do_hunt(p, sim.world, sim.agents, sim.predators, cfg, np.random.default_rng(0))
    assert p.invalid and not a.killed


def test_kill_removes_the_prey_and_the_predator_digests(tmp_path):
    cfg = load_config("small", SURE_KILL)
    sim = Simulation(cfg, RuleBasedBackend(), seed=2, out_dir=tmp_path)
    sim.step()                                                 # t = 1: not a decision tick
    a, p = _alone(sim, (24, 20), (24, 22))
    a.action, p.action, p.digest = "rest", "hunt", 0
    kills = sim.cs["prey"].deaths["predator"]
    sim.step()
    assert a not in sim.agents and sim.cs["prey"].deaths["predator"] == kills + 1
    where = (p.y, p.x)
    for _ in range(cfg.predators.digest_ticks - 1):            # digesting: no move, no decision
        sim.step()
        assert (p.y, p.x) == where and p.digest > 0
    sim.step()
    assert p.digest == 0 and p.action is None                  # decides again next tick
    sim.step()
    assert p.action in PREDATOR.actions
    sim.finish()
    events = [json.loads(l) for l in (tmp_path / "events.jsonl").read_text().splitlines()]
    death = next(e for e in events if e["kind"] == "death" and e["id"] == a.id)
    assert death["cause"] == "predator" and death["killer"] == p.id and "species" not in death
    summary = json.loads((tmp_path / "summary.json").read_text())
    assert summary["predators"]["kills"] == summary["deaths"]["predator"] >= 1


def test_predators_breed_with_their_own_genes(tmp_path):
    cfg = load_config("small", {"predators": {"kill_p": 0.0}})
    sim = Simulation(cfg, RuleBasedBackend(), seed=3, out_dir=tmp_path)
    sim.step()
    p, q = sim.predators[0], sim.predators[1]
    (p.y, p.x), (q.y, q.x) = (10, 10), (10, 11)
    for x in (p, q):
        x.age, x.energy, x.action, x.digest = 200, 90.0, "mate", 0
    n = len(sim.predators)
    sim._breed(PREDATOR)
    child = sim.predators[-1]
    assert len(sim.predators) == n + 1 and child.species == "predator" and child.parents == (p.id, q.id)
    assert len(child.genome.alleles) == 3 and all(a.startswith("predator.") for a in child.genome.alleles)
    sim.finish()
    births = [json.loads(l) for l in (tmp_path / "events.jsonl").read_text().splitlines() if '"birth"' in l]
    assert births[-1]["species"] == "predator" and births[-1]["id"] == child.id


def test_both_species_have_newcomers_below_their_floor():
    cfg = load_config("small")
    sim = Simulation(cfg, RuleBasedBackend(), seed=4)
    sim.agents, sim.predators = [], []
    sim.step()
    assert len(sim.agents) == cfg.agents.floor and len(sim.predators) == cfg.predators.floor
    assert sim.cs["predator"].immigrants == cfg.predators.floor


def test_everyone_moves_at_most_one_cell_per_tick():
    """Same speed for both species: at most one cell (8 directions) per tick."""
    cfg = load_config("small")
    sim = Simulation(cfg, RuleBasedBackend(), seed=5)
    moved = {"prey": 0, "predator": 0}
    for _ in range(400):
        before = {a.id: (a.y, a.x) for a in sim.agents + sim.predators}
        sim.step()
        for a in sim.agents + sim.predators:
            if a.id in before:
                d = cheb(*before[a.id], a.y, a.x)
                assert d <= 1, (a.species, d)
                moved[a.species] += d
    assert moved["prey"] and moved["predator"]


def test_one_registry_holds_both_species(cfg):
    sim = Simulation(cfg, RuleBasedBackend(), seed=6)
    reg = sim.registry
    loci = {reg.get(aid).locus for a in sim.agents + sim.predators for aid in a.genome.alleles}
    assert "predator.rest" in loci and "rest" in loci and loci <= set(PREY.loci) | set(PREDATOR.loci)
    r = AlleleRegistry()
    texts = {"hunt": "No preference.", "rest": "No preference.", "mate": "No preference."}
    g = r.make_genome(texts, species="predator")
    assert g.species == "predator" and r.genes(g) == texts
    assert g.alleles == ("predator.hunt:0", "predator.rest:0", "predator.mate:0")
    prey = r.make_genome(dict.fromkeys(PREY.actions, "No preference."))
    assert prey.alleles[3] == "rest:0"                          # same text, other species: other allele
    as_prey = hashlib.sha256(json.dumps(list(texts.values())).encode()).hexdigest()[:24]
    assert r.genome_key(g) != as_prey                           # the species is part of a predator's key
