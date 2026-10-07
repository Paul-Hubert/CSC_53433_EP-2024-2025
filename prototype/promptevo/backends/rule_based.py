"""Transparent "ideal interpreter" (plan §A7): sensible defaults + gene keyword parsing.

Deliberately simple and readable — students can see exactly how text becomes
behaviour, and it is the fast reference for pipeline tests and control C5.
Gibberish or neutral genes contribute nothing. Both species (rev. 2026-10-07): each has
default logits and situation words of its own; the intensity words are shared.
"""
from __future__ import annotations

import re

import numpy as np

from ..perception import NEAR, Observation, PredatorObservation
from ..species import SPECIES
from .base import Query, batch_actions, normalise

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
    "rest": r"\b(rest|sleep|stay still|lie still|wait|save energy|stop)\b",
    "mate": r"\b(mate|partner|breed|offspring)\b",
    "hunt": r"\b(hunt|hunting|chase|chasing|attack|kill|prey|strike|pounce|stalk)\b",
}
CONDITIONS = {  # prey: phrase -> predicate on the observation
    r"hungry|starving|energy is low|low energy|weak": lambda o: o.energy == "low",
    r"energy is high|well fed|full|strong|fed": lambda o: o.energy == "high",
    r"tired": lambda o: o.energy == "low",
    r"predator|danger|threat|attack(s|ed)? you": lambda o: o.predator != "none",
    r"very close|right next to you|too close": lambda o: o.predator in NEAR or o.animal in NEAR,
    r"food is (close|near)|food nearby": lambda o: o.food in ("here", *NEAR),
    r"food is (far|scarce)|no food|nothing else to do": lambda o: o.food in ("none", "medium", "far"),
    r"alone|lost": lambda o: o.animal == "none",
    r"safe": lambda o: o.predator in ("none", "far"),
    r"old": lambda o: o.age == "adult",
    r"plentiful": lambda o: o.food in ("here", *NEAR),
}
PREDATOR_CONDITIONS = {
    r"hungry|starving|energy is low|low energy|weak": lambda o: o.energy == "low",
    r"energy is high|well fed|full|strong|fed": lambda o: o.energy == "high",
    r"tired": lambda o: o.energy == "low",
    r"prey is (close|near)|prey nearby|next to (the )?prey": lambda o: o.prey in NEAR,
    r"very close|right next to you|too close": lambda o: o.prey in NEAR or o.animal in NEAR,
    r"no prey|prey is (far|scarce)|nothing to hunt": lambda o: o.prey in ("none", "medium", "far"),
    r"alone|lost": lambda o: o.animal == "none",
    r"old": lambda o: o.age == "adult",
}
CONDITIONS_BY_SPECIES = {"prey": CONDITIONS, "predator": PREDATOR_CONDITIONS}
ENERGY_PUSH = {"low": 1.5, "medium": 0.5, "high": -1.0}     # hungry animals look for food


def prey_logits(o: Observation) -> dict:
    l = dict.fromkeys(SPECIES["prey"].actions, 0.0)
    if o.food == "none":         # no food in sight: eating means searching (a random walk)
        l["eat"] = 1.5
    else:
        l["eat"] = {"here": 3.0, "adjacent": 2.5, "close": 2.0, "medium": 1.5, "far": 1.0}[o.food]
        l["eat"] += ENERGY_PUSH[o.energy]
    l["flee"] = {"adjacent": 3.5, "close": 3.0, "medium": 1.0, "far": 0.0, "none": -3.0}[o.predator]
    l["follow"] = {"adjacent": -0.5, "close": -0.5, "medium": 0.3, "far": 0.3, "none": -2.5}[o.animal]
    l["rest"] = -0.5 + (0.5 if o.energy == "high" and o.predator in ("none", "far") else 0.0)
    ready = o.animal in NEAR and o.animal_ready and o.age == "adult" and o.energy != "low"
    l["mate"] = 2.5 if ready else -3.0
    return l


def predator_logits(o: PredatorObservation) -> dict:
    l = dict.fromkeys(SPECIES["predator"].actions, 0.0)
    l["hunt"] = {"adjacent": 3.5, "close": 3.0, "medium": 2.0, "far": 1.0,
                 "none": 1.5}[o.prey] + ENERGY_PUSH[o.energy]    # no prey in sight: hunting means searching
    l["rest"] = -0.5 + (1.0 if o.energy == "high" else 0.0)
    ready = o.animal in NEAR and o.animal_ready and o.age == "adult" and o.energy != "low"
    l["mate"] = 2.5 if ready else -3.0
    return l


def default_logits(o: Observation | PredatorObservation) -> np.ndarray:
    l = predator_logits(o) if isinstance(o, PredatorObservation) else prey_logits(o)
    return np.array([l[a] for a in SPECIES[o.species].actions])


def gene_weight(text: str, action: str, o: Observation | PredatorObservation) -> float:
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
    conditions = CONDITIONS_BY_SPECIES[o.species]
    unless = re.search(r"\bunless (.+)$", t)
    main = t[:unless.start()] if unless else t
    conds = [f for pat, f in conditions.items() if re.search(pat, main)]
    if conds and not any(f(o) for f in conds):
        w *= 0.2                             # condition not met: weak effect
    if unless:
        exc = [f for pat, f in conditions.items() if re.search(pat, unless.group(1))]
        if any(f(o) for f in exc):
            w = -0.5 * w                     # exception applies: flip mildly
    return w


class RuleBasedBackend:
    name = "rule_based"

    def __init__(self, temperature: float = 1.0):
        self.temperature = temperature

    def logits(self, q: Query) -> np.ndarray:
        o = q.obs
        l = default_logits(o)
        l += np.array([gene_weight(q.genes[a], a, o) for a in SPECIES[q.species].actions])
        return l

    def decide(self, queries: list[Query]) -> np.ndarray:
        out = np.zeros((len(queries), len(batch_actions(queries))))
        for i, q in enumerate(queries):
            z = self.logits(q) / self.temperature
            z = np.exp(z - z.max())
            out[i] = z
        return normalise(out)
