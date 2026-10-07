"""Behaviour executors: each runs one tick of the chosen action (plan §A6), for both species.

executor(agent, world, prey, predators, cfg, rng) -> True if the agent moved. Everyone moves
at most one cell per tick (8 directions), prey and predators alike (rev. 2026-10-07).
An action with nothing to act on in sight (no food, predator, animal, prey or ready partner)
makes the agent wander (a random walk) instead and sets agent.invalid = True for logging:
eating with no food in sight means searching, and so does hunting with no prey in sight.
"""
from __future__ import annotations

from .perception import mate_ready
from .species import species_cfg
from .world import cheb


def _nearest(agent, agents, pred=None):
    best, bd = None, None
    for b in agents:
        if b is agent or (pred and not pred(b)):
            continue
        d = cheb(agent.y, agent.x, b.y, b.x)
        if bd is None or d < bd:
            best, bd = b, d
    return best, bd


def _kin(agent, prey, predators):
    return prey if agent.species == "prey" else predators


def _move(agent, ny, nx) -> bool:
    moved = (ny, nx) != (agent.y, agent.x)
    agent.y, agent.x = ny, nx
    return moved


def _wander(agent, world, cfg, rng) -> bool:
    y, x, agent.heading = world.step_heading(agent.y, agent.x, agent.heading,
                                             species_cfg(cfg, agent.species).wander_turn_p, rng)
    return _move(agent, y, x)


def _invalid(agent, world, cfg, rng) -> bool:
    agent.invalid = True
    return _wander(agent, world, cfg, rng)


# --- prey ------------------------------------------------------------------------
def do_eat(agent, world, prey, predators, cfg, rng) -> bool:
    ac = cfg.agents
    if world.food[agent.y, agent.x]:
        world.food[agent.y, agent.x] = False
        agent.energy = min(ac.energy_max, agent.energy + ac.eat_gain)
        agent.food_eaten += 1
        return False
    nf = world.nearest_food(agent.y, agent.x, int(ac.vision))
    if nf is None:
        return _invalid(agent, world, cfg, rng)
    moved = _move(agent, *world.step_toward(agent.y, agent.x, nf[0], nf[1], rng))
    if world.food[agent.y, agent.x]:            # arrived: eat in the same tick
        world.food[agent.y, agent.x] = False
        agent.energy = min(ac.energy_max, agent.energy + ac.eat_gain)
        agent.food_eaten += 1
    return moved


def do_flee(agent, world, prey, predators, cfg, rng) -> bool:
    p, d = _nearest(agent, predators)
    if p is None or d > cfg.agents.vision:
        return _invalid(agent, world, cfg, rng)
    return _move(agent, *world.step_toward(agent.y, agent.x, p.y, p.x, rng, away=True))


def do_follow(agent, world, prey, predators, cfg, rng) -> bool:
    b, d = _nearest(agent, _kin(agent, prey, predators))
    if b is None or d > species_cfg(cfg, agent.species).vision:
        return _invalid(agent, world, cfg, rng)
    if d <= 1:
        return False
    return _move(agent, *world.step_toward(agent.y, agent.x, b.y, b.x, rng))


# --- both species -------------------------------------------------------------------
def do_rest(agent, world, prey, predators, cfg, rng) -> bool:
    return False


def do_mate(agent, world, prey, predators, cfg, rng) -> bool:
    sc = species_cfg(cfg, agent.species)
    b, d = _nearest(agent, _kin(agent, prey, predators), pred=lambda o: mate_ready(o, sc))
    if b is None or d > sc.vision:
        return _invalid(agent, world, cfg, rng)
    if d <= 1:
        return False                     # breeding is resolved by the simulation
    return _move(agent, *world.step_toward(agent.y, agent.x, b.y, b.x, rng))


# --- predators ------------------------------------------------------------------------
def do_hunt(agent, world, prey, predators, cfg, rng) -> bool:
    """Step toward the nearest prey animal; a predator next to it (after its step) strikes and
    kills with probability kill_p. A kill feeds it (kill_gain) and it digests for digest_ticks
    (it stays still and does not decide; see Simulation._act). The simulation removes the prey."""
    pc = cfg.predators
    target, d = _nearest(agent, prey, pred=lambda b: not b.killed)
    if target is None or d > pc.vision:
        return _invalid(agent, world, cfg, rng)
    moved = False
    if d > 1:
        moved = _move(agent, *world.step_toward(agent.y, agent.x, target.y, target.x, rng))
        d = cheb(agent.y, agent.x, target.y, target.x)
    if d <= 1 and rng.random() < pc.kill_p:
        target.killed, target.killer = True, agent.id
        agent.energy = min(pc.energy_max, agent.energy + pc.kill_gain)
        agent.food_eaten += 1                # a predator's food is prey: kills
        agent.digest = int(pc.digest_ticks)
    return moved


EXECUTORS = {"eat": do_eat, "flee": do_flee, "follow": do_follow, "rest": do_rest, "mate": do_mate,
             "hunt": do_hunt}
