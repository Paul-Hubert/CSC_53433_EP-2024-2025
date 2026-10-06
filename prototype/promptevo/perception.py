"""Discretised observations (plan §A6). Few hundred distinct values → cache-friendly."""
from __future__ import annotations

from dataclasses import dataclass

from .world import cheb


@dataclass(frozen=True)
class Observation:
    energy: str                 # low | medium | high
    food: str                   # none | far | near | here
    predator: str               # none | far | near
    animal: str                 # none | far | near
    animal_ready: bool | None = None     # only when animal == near
    age: str = "adult"          # young | adult

    def tags(self) -> set[str]:
        """Which directed tests this observation is relevant for (plan §A9)."""
        t = {"any"}
        if self.food != "none":
            t.add("food_present")
        if self.predator != "none":
            t.add("predator_present")
        if self.animal != "none":
            t.add("animal_present")
        if self.animal == "near":
            t.add("animal_near")
        return t


# Directed-test relevance: action locus -> tag the observation must carry.
RELEVANT_TAG = {"eat": "food_present", "flee": "predator_present", "follow": "animal_present",
                "rest": "any", "mate": "animal_near"}


def _bucket(d: int | None, near: int, vision: int) -> str:
    if d is None or d > vision:
        return "none"
    return "near" if d <= near else "far"


def mate_ready(a, ac) -> bool:
    return a.age >= ac.maturity and a.energy >= ac.mate_energy


def sense(agent, world, agents, cfg) -> Observation:
    ac = cfg.agents
    vision, near = int(ac.vision), int(ac.near)
    energy = "low" if agent.energy < ac.energy_low else "high" if agent.energy > ac.energy_high else "medium"
    nf = world.nearest_food(agent.y, agent.x, vision)
    if nf is None:
        food = "none"
    elif nf[2] == 0:
        food = "here"
    else:
        food = _bucket(nf[2], near, vision)
    dp = min((cheb(agent.y, agent.x, p.y, p.x) for p in world.predators), default=None)
    predator = _bucket(dp, near, vision)
    other, do = None, None
    for b in agents:
        if b is agent:
            continue
        d = cheb(agent.y, agent.x, b.y, b.x)
        if do is None or d < do:
            other, do = b, d
    animal = _bucket(do, near, vision)
    ready = mate_ready(other, ac) if animal == "near" else None
    age = "young" if agent.age < ac.maturity else "adult"
    return Observation(energy, food, predator, animal, ready, age)
