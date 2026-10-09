"""Cap rule (rev. 2026-10-09): at the cap births go on and random older animals migrate away."""
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.sim import Simulation
from promptevo.species import PREY


def _at_cap(rule: str):
    cfg = load_config("small", {"agents": {"cap_rule": rule}})
    sim = Simulation(cfg, RuleBasedBackend(), seed=2)
    while len(sim.agents) < cfg.agents.cap:                  # fill up to the cap
        sim._new_agent(5, 5, 50.0, sim.agents[0].genome)
    sim.t = 10                                               # everyone is older than this tick
    a, b = sim.agents[:2]
    for x in (a, b):
        x.age, x.energy, x.bred = cfg.agents.maturity + 1, 90.0, False
    (a.y, a.x), (b.y, b.x) = (20, 20), (20, 21)
    a.action = "mate"
    return cfg, sim


def test_migrate_lets_the_litter_be_born_and_random_older_animals_leave():
    cfg, sim = _at_cap("migrate")
    assert len(sim.agents) == cfg.agents.cap
    sim._breed(PREY)
    babies = [x for x in sim.agents if x.born == sim.t]
    assert 2 <= len(babies) <= 4 and len(sim.agents) == cfg.agents.cap + len(babies)
    sim._migrate(PREY)
    assert len(sim.agents) == cfg.agents.cap and all(x in sim.agents for x in babies)
    assert sim.cs["prey"].deaths["migrated"] == len(babies)


def test_block_is_the_old_rule():
    cfg, sim = _at_cap("block")
    sim._breed(PREY)
    sim._migrate(PREY)
    assert len(sim.agents) == cfg.agents.cap and sim.cs["prey"].births == 0
    assert sim.cs["prey"].deaths["migrated"] == 0
