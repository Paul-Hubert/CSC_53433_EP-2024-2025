"""Discretised observations (plan §A6, rev. 2026-10-07: predators sense too, longer vision,
distances in bands).

Both species see `vision` cells far (Chebyshev distance, each species' config section) and
get every distance as a band: adjacent (1 cell), close, medium or far, with the edges from
`perception.bands`; "none" means nothing of that kind within vision. The bands keep the
number of distinct observations small (cache-friendly); the text tells the brain how many
cells each band covers (obs_text.py).
"""
from __future__ import annotations

from dataclasses import dataclass

from .species import species_cfg
from .world import cheb

DIST = ("adjacent", "close", "medium", "far")    # distance bands, nearest first
NEAR = ("adjacent", "close")                     # near enough to tell whether a partner is ready
DEFAULT_SCALE = (1, 4, 10, 20)                   # band edges in cells, the last one = vision (base.yaml)


@dataclass(frozen=True)
class Observation:
    """What a prey animal senses."""
    energy: str                 # low | medium | high
    food: str                   # here | adjacent | close | medium | far | none
    predator: str               # nearest predator: adjacent | close | medium | far | none
    animal: str                 # nearest other prey animal: adjacent | close | medium | far | none
    animal_ready: bool | None = None     # only when animal is adjacent or close
    age: str = "adult"          # young | adult
    scale: tuple = DEFAULT_SCALE         # band edges and vision in cells, for the text

    species = "prey"            # class attribute, not a field

    def tags(self) -> set[str]:
        """Which directed tests this observation is relevant for (plan §A9)."""
        t = {"any"}
        if self.food != "none":
            t.add("food_present")
        if self.predator != "none":
            t.add("predator_present")
        if self.animal != "none":
            t.add("animal_present")
        if self.animal in NEAR:
            t.add("animal_near")
        return t


@dataclass(frozen=True)
class PredatorObservation:
    """What a predator senses."""
    energy: str                 # low | medium | high
    prey: str                   # nearest prey animal: adjacent | close | medium | far | none
    animal: str                 # nearest other predator: adjacent | close | medium | far | none
    animal_ready: bool | None = None     # only when animal is adjacent or close
    age: str = "adult"          # young | adult
    scale: tuple = DEFAULT_SCALE

    species = "predator"

    def tags(self) -> set[str]:
        t = {"any"}
        if self.prey != "none":
            t.add("prey_present")
        if self.animal != "none":
            t.add("animal_present")
        if self.animal in NEAR:
            t.add("animal_near")
        return t


# Directed-test relevance: action -> tag the observation must carry (both species).
RELEVANT_TAG = {"eat": "food_present", "flee": "predator_present", "follow": "animal_present",
                "rest": "any", "mate": "animal_near", "hunt": "prey_present"}


def make_scale(cfg, species) -> tuple:
    """(band edges..., vision) in cells for one species."""
    bands = tuple(int(b) for b in cfg.perception.bands)
    vision = int(species_cfg(cfg, species).vision)
    if len(bands) != len(DIST) - 1 or list(bands) != sorted(set(bands)) or bands[0] < 1 or bands[-1] >= vision:
        raise ValueError(f"perception.bands needs {len(DIST) - 1} increasing distances (cells) below "
                         f"the vision ({vision}); got {list(bands)}")
    return bands + (vision,)


def band(d: int | None, scale: tuple) -> str:
    """Distance band of a Chebyshev distance; "none" when out of sight (or nothing there)."""
    if d is None or d > scale[-1]:
        return "none"
    for name, edge in zip(DIST, scale):
        if d <= edge:
            return name
    return "none"


def mate_ready(a, sc) -> bool:
    """sc: the config section of a's species."""
    return a.age >= sc.maturity and a.energy >= sc.mate_energy


def _nearest(agent, others):
    best, bd = None, None
    for b in others:
        if b is agent:
            continue
        d = cheb(agent.y, agent.x, b.y, b.x)
        if bd is None or d < bd:
            best, bd = b, d
    return best, bd


def sense(agent, world, prey, predators, cfg) -> Observation | PredatorObservation:
    """The observation of `agent` (either species): prey and predators are the living animals."""
    sc = species_cfg(cfg, agent.species)
    scale = make_scale(cfg, agent.species)
    energy = "low" if agent.energy < sc.energy_low else "high" if agent.energy > sc.energy_high else "medium"
    other, do = _nearest(agent, prey if agent.species == "prey" else predators)
    animal = band(do, scale)
    ready = mate_ready(other, sc) if animal in NEAR else None
    age = "young" if agent.age < sc.maturity else "adult"
    if agent.species == "prey":
        nf = world.nearest_food(agent.y, agent.x, scale[-1])
        food = "none" if nf is None else "here" if nf[2] == 0 else band(nf[2], scale)
        dp = min((cheb(agent.y, agent.x, p.y, p.x) for p in predators), default=None)
        return Observation(energy, food, band(dp, scale), animal, ready, age, scale)
    dq = min((cheb(agent.y, agent.x, b.y, b.x) for b in prey if not b.killed), default=None)
    return PredatorObservation(energy, band(dq, scale), animal, ready, age, scale)
