"""Observation → text. V1 terse, V2 first person (plan §A6). Rev. 2026-10-07: both species,
and every distance band is written as its range of cells ("2-4 cells away", "none within
20 cells"), so the brain knows how far things are and how far it can see. Stamina (both
species) and the nearest carcass (predators) are written when sensed (not None).
"""
from __future__ import annotations

from .perception import DIST, Observation, PredatorObservation

ENERGY_V2 = {"low": "I am hungry and weak.", "medium": "I have some energy.", "high": "I am well fed and strong."}
STAMINA_V2 = {"low": "I am out of breath.", "medium": "I am getting tired.", "high": "I am rested."}


def distance(v: str, scale: tuple) -> str:
    """'here', '1 cell away', '2-4 cells away', ... or 'none within 20 cells'."""
    if v == "here":
        return "here"
    if v == "none":
        return f"none within {scale[-1]} cells"
    i = DIST.index(v)
    lo, hi = (1 if i == 0 else scale[i - 1] + 1), scale[i]
    span = str(lo) if lo == hi else f"{lo}-{hi}"
    return "1 cell away" if span == "1" else f"{span} cells away"


def _ready(o) -> str:
    if o.animal_ready is None:
        return ""
    return ", ready to mate" if o.animal_ready else ", not ready to mate"


def _ready_v2(o) -> str:
    if o.animal_ready is None:
        return ""
    return " It is ready to mate." if o.animal_ready else " It is not ready to mate."


def _nearest_v2(v: str, what: str, scale: tuple) -> str:
    return f"No {what} within {scale[-1]} cells." if v == "none" else f"The nearest {what} is {distance(v, scale)}."


def render(o: Observation | PredatorObservation, style: str = "V1") -> str:
    s = o.scale
    age_v2 = "I am young." if o.age == "young" else "I am an adult."
    stamina = f"Stamina: {o.stamina}. " if o.stamina else ""
    stamina_v2 = [STAMINA_V2[o.stamina]] if o.stamina else []
    if isinstance(o, PredatorObservation):
        carcass = f"Carcass: {distance(o.carcass, s)}. " if o.carcass else ""
        carcass_v2 = [_nearest_v2(o.carcass, "carcass", s)] if o.carcass else []
        if style == "V1":
            return (f"Energy: {o.energy}. {stamina}Prey: {distance(o.prey, s)}. {carcass}"
                    f"Other predator: {distance(o.animal, s)}{_ready(o)}. Age: {o.age}.")
        if style == "V2":
            return " ".join([ENERGY_V2[o.energy], *stamina_v2, _nearest_v2(o.prey, "prey", s), *carcass_v2,
                             _nearest_v2(o.animal, "other predator", s) + _ready_v2(o), age_v2])
        raise ValueError(f"unknown obs style {style}")
    if style == "V1":
        return (f"Energy: {o.energy}. {stamina}Food: {distance(o.food, s)}. Predator: {distance(o.predator, s)}. "
                f"Animal: {distance(o.animal, s)}{_ready(o)}. Age: {o.age}.")
    if style == "V2":
        food = "I am standing on food." if o.food == "here" else _nearest_v2(o.food, "food", s)
        return " ".join([ENERGY_V2[o.energy], *stamina_v2, food, _nearest_v2(o.predator, "predator", s),
                         _nearest_v2(o.animal, "other animal", s) + _ready_v2(o), age_v2])
    raise ValueError(f"unknown obs style {style}")
