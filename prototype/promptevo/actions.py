"""Behaviour executors: each runs one tick of the chosen action (plan §A6).

Returns True if the agent moved. An action with nothing to act on in sight (no food,
predator, animal or ready partner) makes the agent wander (a random walk) instead and
sets agent.invalid = True for logging: eating with no food in sight means searching.
"""
from __future__ import annotations

from .perception import mate_ready
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


def _move(agent, ny, nx) -> bool:
    moved = (ny, nx) != (agent.y, agent.x)
    agent.y, agent.x = ny, nx
    return moved


def _wander(agent, world, cfg, rng) -> bool:
    y, x, agent.heading = world.step_heading(agent.y, agent.x, agent.heading,
                                             cfg.agents.wander_turn_p, rng)
    return _move(agent, y, x)


def _invalid(agent, world, cfg, rng) -> bool:
    agent.invalid = True
    return _wander(agent, world, cfg, rng)


def do_eat(agent, world, agents, cfg, rng) -> bool:
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


def do_flee(agent, world, agents, cfg, rng) -> bool:
    vis = int(cfg.agents.vision)
    near = [p for p in world.predators if cheb(agent.y, agent.x, p.y, p.x) <= vis]
    if not near:
        return _invalid(agent, world, cfg, rng)
    p = min(near, key=lambda p: cheb(agent.y, agent.x, p.y, p.x))
    return _move(agent, *world.step_toward(agent.y, agent.x, p.y, p.x, rng, away=True))


def do_follow(agent, world, agents, cfg, rng) -> bool:
    b, d = _nearest(agent, agents)
    if b is None or d > cfg.agents.vision:
        return _invalid(agent, world, cfg, rng)
    if d <= 1:
        return False
    return _move(agent, *world.step_toward(agent.y, agent.x, b.y, b.x, rng))


def do_rest(agent, world, agents, cfg, rng) -> bool:
    return False


def do_mate(agent, world, agents, cfg, rng) -> bool:
    ac = cfg.agents
    b, d = _nearest(agent, agents, pred=lambda o: mate_ready(o, ac))
    if b is None or d > ac.vision:
        return _invalid(agent, world, cfg, rng)
    if d <= 1:
        return False                     # breeding is resolved by the simulation
    return _move(agent, *world.step_toward(agent.y, agent.x, b.y, b.x, rng))


EXECUTORS = {"eat": do_eat, "flee": do_flee, "follow": do_follow, "rest": do_rest, "mate": do_mate}
