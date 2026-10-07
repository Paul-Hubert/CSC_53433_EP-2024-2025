"""Behaviour executors: each runs one tick of the chosen action (plan §A6), for both species.

executor(agent, world, prey, predators, cfg, rng) -> number of cells the agent moved.

Speed and stamina (rev. 2026-10-07): every move walks one cell (8 directions), except hunting
and fleeing, which run at the species' `speed` (predators 2 cells per tick, prey 1). Each cell
costs one point of stamina and nobody moves more cells than its stamina allows; standing still
brings stamina back (Simulation._act). Until then everyone moved at most one cell per tick.
An action with nothing to act on in sight (no food, predator, animal, prey, carcass or ready
partner) makes the agent wander (a random walk) instead and sets agent.invalid = True for
logging: eating with no food in sight means searching, and so does hunting with no prey in sight.
"""
from __future__ import annotations

from .perception import edible_carcass, mate_ready
from .species import species_cfg
from .world import Carcass, cheb


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


def cells(agent, cfg, run: bool = False) -> int:
    """Cells the agent can move this tick: 1 walking, its species' speed running, and never
    more than its stamina (one point per cell)."""
    want = int(species_cfg(cfg, agent.species).speed) if run else 1
    return max(0, min(want, int(agent.stamina)))


def _move(agent, ny, nx) -> int:
    moved = (ny, nx) != (agent.y, agent.x)
    agent.y, agent.x = ny, nx
    return int(moved)


def _go(agent, world, ty, tx, rng, n, away=False, stop=1) -> int:
    """Up to n greedy steps toward (or away from) (ty, tx); going toward stops `stop` cells
    from the target (1: next to it, 0: on it). Returns the cells moved."""
    moved = 0
    for _ in range(n):
        if not away and cheb(agent.y, agent.x, ty, tx) <= stop:
            break
        moved += _move(agent, *world.step_toward(agent.y, agent.x, ty, tx, rng, away=away))
    return moved


def _wander(agent, world, cfg, rng) -> int:
    if cells(agent, cfg) == 0:
        return 0
    y, x, agent.heading = world.step_heading(agent.y, agent.x, agent.heading,
                                             species_cfg(cfg, agent.species).wander_turn_p, rng)
    return _move(agent, y, x)


def _invalid(agent, world, cfg, rng) -> int:
    agent.invalid = True
    return _wander(agent, world, cfg, rng)


# --- prey ------------------------------------------------------------------------
def _graze(agent, world, ac) -> None:
    world.food[agent.y, agent.x] = False
    agent.energy = min(ac.energy_max, agent.energy + ac.eat_gain)
    agent.food_eaten += 1


def do_eat(agent, world, prey, predators, cfg, rng) -> int:
    ac = cfg.agents
    if world.food[agent.y, agent.x]:
        _graze(agent, world, ac)
        return 0
    nf = world.nearest_food(agent.y, agent.x, int(ac.vision))
    if nf is None:
        return _invalid(agent, world, cfg, rng)
    moved = _go(agent, world, nf[0], nf[1], rng, cells(agent, cfg), stop=0)
    if world.food[agent.y, agent.x]:            # arrived: eat in the same tick
        _graze(agent, world, ac)
    return moved


def do_flee(agent, world, prey, predators, cfg, rng) -> int:
    p, d = _nearest(agent, predators)
    if p is None or d > cfg.agents.vision:
        return _invalid(agent, world, cfg, rng)
    return _go(agent, world, p.y, p.x, rng, cells(agent, cfg, run=True), away=True)


def do_follow(agent, world, prey, predators, cfg, rng) -> int:
    b, d = _nearest(agent, _kin(agent, prey, predators))
    if b is None or d > species_cfg(cfg, agent.species).vision:
        return _invalid(agent, world, cfg, rng)
    return _go(agent, world, b.y, b.x, rng, cells(agent, cfg))


# --- both species -------------------------------------------------------------------
def do_rest(agent, world, prey, predators, cfg, rng) -> int:
    return 0


def do_mate(agent, world, prey, predators, cfg, rng) -> int:
    sc = species_cfg(cfg, agent.species)
    b, d = _nearest(agent, _kin(agent, prey, predators), pred=lambda o: mate_ready(o, sc))
    if b is None or d > sc.vision:
        return _invalid(agent, world, cfg, rng)
    return _go(agent, world, b.y, b.x, rng, cells(agent, cfg))     # breeding is resolved by the simulation


# --- predators ------------------------------------------------------------------------
def _meal(agent, pc, gain) -> None:
    """A predator eats: energy, a meal counted, then digestion (digest_ticks for a kill, in
    proportion to the energy for a carcass portion); see Simulation._act."""
    agent.energy = min(pc.energy_max, agent.energy + gain)
    agent.food_eaten += 1                    # a predator's meals: kills and carcass portions
    agent.digest = round(pc.digest_ticks * gain / pc.kill_gain)


def do_hunt(agent, world, prey, predators, cfg, rng) -> int:
    """Run toward the nearest prey animal or carcass this predator may eat from (up to `speed`
    cells, stopping next to it). Next to a carcass: eat a portion (carcass_gain). Next to a prey
    animal: strike, killing it with probability kill_p; the kill feeds the predator (kill_gain)
    and leaves a carcass with carcass_portions portions for other predators. The simulation
    removes the killed prey."""
    pc = cfg.predators
    target, d = _nearest(agent, prey, pred=lambda b: not b.killed)
    c, dc = edible_carcass(agent, world)
    if c is not None and (target is None or dc <= d):
        target, d = c, dc                    # a carcass is a sure meal: it goes first on a tie
    if target is None or d > pc.vision:
        return _invalid(agent, world, cfg, rng)
    moved = _go(agent, world, target.y, target.x, rng, cells(agent, cfg, run=True))
    if cheb(agent.y, agent.x, target.y, target.x) > 1:
        return moved
    if isinstance(target, Carcass):
        target.portions -= 1
        target.eaters.add(agent.id)
        world.portions_eaten += 1
        _meal(agent, pc, pc.carcass_gain)
    elif rng.random() < pc.kill_p:
        target.killed, target.killer = True, agent.id
        _meal(agent, pc, pc.kill_gain)
        if int(pc.carcass_portions) > 0:
            world.carcasses.append(Carcass(target.y, target.x, int(pc.carcass_portions), agent.id,
                                           int(pc.carcass_ticks)))
    return moved


EXECUTORS = {"eat": do_eat, "flee": do_flee, "follow": do_follow, "rest": do_rest, "mate": do_mate,
             "hunt": do_hunt}
