"""Cap (rev. 2026-10-09): one cap for all animals together (sim.cap). With cap_rule migrate births
go on at the cap and random older animals of either species leave; block stops births."""
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.sim import Simulation
from promptevo.species import PREY


def _at_cap(rule: str):
    cfg = load_config("small", {"sim": {"cap_rule": rule}})
    sim = Simulation(cfg, RuleBasedBackend(), seed=2)
    while len(sim.agents) + len(sim.predators) < cfg.sim.cap:     # fill up to the cap with prey
        sim._new_agent(5, 5, 50.0, sim.agents[0].genome)
    sim.t = 10                                                   # everyone is older than this tick
    a, b = sim.agents[:2]
    for x in (a, b):
        x.age, x.energy, x.bred = cfg.agents.maturity + 1, 90.0, False
    (a.y, a.x), (b.y, b.x) = (20, 20), (20, 21)
    a.action = "mate"
    return cfg, sim


def test_migrate_lets_the_litter_be_born_and_random_older_animals_leave():
    cfg, sim = _at_cap("migrate")
    n_pred = len(sim.predators)
    sim._breed(PREY)
    babies = [x for x in sim.agents if x.born == sim.t]
    assert 2 <= len(babies) <= 4 and len(sim.agents) + n_pred == cfg.sim.cap + len(babies)
    sim._migrate()
    assert len(sim.agents) + len(sim.predators) == cfg.sim.cap and all(x in sim.agents for x in babies)
    left = sim.cs["prey"].deaths["migrated"] + sim.cs["predator"].deaths["migrated"]
    assert left == len(babies)                                    # from either species


def test_migrants_come_from_both_species():
    cfg, sim = _at_cap("migrate")
    for _ in range(3 * cfg.sim.cap):                             # far over the cap: many leave
        sim._new_agent(6, 6, 50.0, sim.agents[0].genome).born = 0
    sim._migrate()
    assert len(sim.agents) + len(sim.predators) == cfg.sim.cap
    assert sim.cs["prey"].deaths["migrated"] > 0 and sim.cs["predator"].deaths["migrated"] > 0


def test_block_stops_births_at_the_cap():
    cfg, sim = _at_cap("block")
    sim._breed(PREY)
    sim._migrate()
    assert len(sim.agents) + len(sim.predators) == cfg.sim.cap and sim.cs["prey"].births == 0
    assert sim.cs["prey"].deaths["migrated"] == 0
