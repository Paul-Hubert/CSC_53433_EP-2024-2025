"""Observation → text. V1 terse, V2 first person (plan §A6)."""
from __future__ import annotations

from .perception import Observation


def render(o: Observation, style: str = "V1") -> str:
    if style == "V1":
        animal = f"Animal: {o.animal}"
        if o.animal == "near":
            animal += (", ready to mate" if o.animal_ready else ", not ready to mate")
            animal += (", stronger" if o.animal_stronger else ", weaker")
        return (f"Energy: {o.energy}. Food: {o.food}. Predator: {o.predator}. "
                f"{animal}. Age: {o.age}.")
    if style == "V2":
        e = {"low": "I am hungry and weak.", "medium": "I have some energy.",
             "high": "I am well fed and strong."}[o.energy]
        f = {"none": "I see no food.", "far": "There is food far away.",
             "near": "Food is close.", "here": "I am standing on food."}[o.food]
        p = {"none": "No predator in sight.", "far": "A predator is far away.",
             "near": "A predator is very close!"}[o.predator]
        a = {"none": "I am alone.", "far": "Another animal is far away.",
             "near": "Another animal is next to me."}[o.animal]
        if o.animal == "near":
            a += " It is ready to mate." if o.animal_ready else " It is not ready to mate."
            a += " It is stronger than me." if o.animal_stronger else " It is weaker than me."
        g = "I am young." if o.age == "young" else "I am an adult."
        return " ".join([e, f, p, a, g])
    raise ValueError(f"unknown obs style {style}")
