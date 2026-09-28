"""Transparent "ideal interpreter" (plan §A7): sensible defaults + gene keyword parsing.

Deliberately simple and readable — students can see exactly how text becomes
behaviour, and it is the fast reference for pipeline tests and control C5.
Gibberish or neutral genes contribute nothing.
"""
from __future__ import annotations

import re

import numpy as np

from ..genome import ACTIONS
from ..perception import Observation
from .base import Query, normalise

INTENSITY = [  # first match wins (longest phrases first)
    (r"\b(never|do not|don't|avoid|refuse)\b", -2.5),
    (r"\b(rarely|seldom|hardly)\b", -1.2),
    (r"\b(always|whatever happens|at all costs)\b", 2.5),
    (r"\b(whenever|often|usually|eagerly|quickly)\b", 1.2),
    (r"\b(sometimes|occasionally|maybe)\b", 0.3),
]
# Mentioning the action at all (without an intensity word) is a mild push.
ACTION_WORDS = {
    "eat": r"\b(eat|eating|food|feed|graze|forage)\b",
    "flee": r"\b(flee|run|escape|hide|danger|predator)\b",
    "follow": r"\b(follow|stay close|close to other|group|companion|herd)\b",
    "wander": r"\b(explore|wander|roam|new places?|moving|travel)\b",
    "rest": r"\b(rest|sleep|stay still|save energy|stop)\b",
    "mate": r"\b(mate|partner|breed|offspring)\b",
    "attack": r"\b(attack|fight|bite|steal)\b",
}
CONDITIONS = {  # phrase -> predicate on the observation
    r"hungry|starving|energy is low|low energy|weak": lambda o: o.energy == "low",
    r"energy is high|well fed|full|strong|fed": lambda o: o.energy == "high",
    r"tired": lambda o: o.energy == "low",
    r"predator|danger|threat|attack(s|ed)? you": lambda o: o.predator != "none",
    r"very close|right next to you|too close": lambda o: o.predator == "near" or o.animal == "near",
    r"food is (close|near)|food nearby": lambda o: o.food in ("near", "here"),
    r"food is (far|scarce)|no food|nothing else to do": lambda o: o.food in ("none", "far"),
    r"alone|lost": lambda o: o.animal == "none",
    r"safe": lambda o: o.predator == "none",
    r"old": lambda o: o.age == "adult",
    r"plentiful": lambda o: o.food in ("near", "here"),
}
TEMPERAMENT = [  # (pattern, {action: delta})
    (r"cautious|careful|safety|nervous|shy", {"flee": 1.0, "attack": -1.0, "wander": -0.3}),
    (r"bold|brave|risk|reckless", {"flee": -0.8, "attack": 0.6, "wander": 0.4}),
    (r"social|group|together|friendly|curious", {"follow": 1.0, "mate": 0.3}),
    (r"solitary|alone|wary|strangers", {"follow": -1.0, "attack": 0.2}),
    (r"restless|somewhere new|explor|open ground", {"wander": 1.0, "rest": -0.5}),
    (r"familiar|attached|home|water", {"wander": -0.6, "rest": 0.4}),
]


def default_logits(o: Observation) -> np.ndarray:
    l = dict.fromkeys(ACTIONS, 0.0)
    l["eat"] = {"here": 3.0, "near": 2.0, "far": 1.0, "none": -2.0}[o.food]
    l["eat"] += {"low": 1.5, "medium": 0.5, "high": -1.0}[o.energy]
    l["flee"] = {"near": 3.0, "far": 0.5, "none": -3.0}[o.predator]
    l["follow"] = {"near": -0.5, "far": 0.3, "none": -2.5}[o.animal]
    l["wander"] = 0.5 + (1.0 if o.food == "none" else 0.0)
    l["rest"] = -0.5 + (0.5 if o.energy == "high" and o.predator == "none" else 0.0)
    ready = o.animal == "near" and o.animal_ready and o.age == "adult" and o.energy != "low"
    l["mate"] = 2.5 if ready else -3.0
    l["attack"] = -3.0 if o.animal != "near" else (0.5 if (o.energy == "low" and not o.animal_stronger) else -1.0)
    return np.array([l[a] for a in ACTIONS])


def gene_weight(text: str, action: str, o: Observation) -> float:
    t = text.lower()
    w = 0.0
    for pat, val in INTENSITY:
        if re.search(pat, t):
            w = val
            break
    else:
        if re.search(ACTION_WORDS[action], t):
            w = 0.8
    if w == 0.0:
        return 0.0
    unless = re.search(r"\bunless (.+)$", t)
    main = t[:unless.start()] if unless else t
    conds = [f for pat, f in CONDITIONS.items() if re.search(pat, main)]
    if conds and not any(f(o) for f in conds):
        w *= 0.2                             # condition not met: weak effect
    if unless:
        exc = [f for pat, f in CONDITIONS.items() if re.search(pat, unless.group(1))]
        if any(f(o) for f in exc):
            w = -0.5 * w                     # exception applies: flip mildly
    return w


def temperament_deltas(texts: list[str]) -> np.ndarray:
    d = dict.fromkeys(ACTIONS, 0.0)
    for text in texts:
        t = text.lower()
        for pat, deltas in TEMPERAMENT:
            if re.search(pat, t):
                for a, v in deltas.items():
                    d[a] += v
    return np.array([d[a] for a in ACTIONS])


class RuleBasedBackend:
    name = "rule_based"

    def __init__(self, temperature: float = 1.0):
        self.temperature = temperature

    def logits(self, q: Query) -> np.ndarray:
        o = q.obs
        l = default_logits(o)
        l += np.array([gene_weight(q.genes[a], a, o) for a in ACTIONS])
        l += temperament_deltas([q.genes["risk"], q.genes["social"], q.genes["place"]])
        return l

    def decide(self, queries: list[Query]) -> np.ndarray:
        out = np.zeros((len(queries), len(ACTIONS)))
        for i, q in enumerate(queries):
            z = self.logits(q) / self.temperature
            z = np.exp(z - z.max())
            out[i] = z
        return normalise(out)
