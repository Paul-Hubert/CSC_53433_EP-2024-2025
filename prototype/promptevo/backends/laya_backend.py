"""Laya decision backend (plan §A7, doc 05 §4).

UNVERIFIED against the real package: written from the Laya model card
(laya.load(...).predict(state, questions) -> {"answers": {...}}). The S1.2 probe
confirms the answer schema; adjust `extract_probs` if the key names differ and
record the facts in STATUS.md.
"""
from __future__ import annotations

import logging
from typing import Any

import numpy as np

from ..cache import KVCache, make_key
from ..config import resolve
from ..genome import ACTIONS
from ..obs_text import render
from .base import Query, normalise

log = logging.getLogger(__name__)

QUESTION = "Which action does this animal take now?"
NEUTRAL_OPTIONS = {
    "eat": "go to food and eat it", "flee": "run away from danger",
    "follow": "move toward another animal", "wander": "explore the surroundings",
    "rest": "stay still and save energy", "mate": "approach a partner to breed",
    "attack": "fight another animal to take its energy",
}
PLACEMENTS = ("P1", "P2", "P3", "P4")


def build_request(genes: dict[str, str], situation: str, placement: str) -> tuple[dict, dict]:
    """(state, questions) for Laya. See plan §A7 for the four placements."""
    temperament = f"Temperament: {genes['risk']} {genes['social']} {genes['place']}"
    instincts = " ".join(f"{a}: {genes[a]}" for a in ACTIONS)
    if placement == "P1":
        state = {"situation": situation}
        criteria = {a: genes[a] for a in ACTIONS}
        instr = QUESTION
    elif placement == "P2":
        state = {"animal": f"Instincts: {instincts} {temperament}", "situation": situation}
        criteria, instr = dict(NEUTRAL_OPTIONS), QUESTION
    elif placement == "P3":
        state = {"situation": situation}
        criteria = dict(NEUTRAL_OPTIONS)
        instr = f"This animal's instincts: {instincts} {temperament}. {QUESTION}"
    elif placement == "P4":
        state = {"animal": temperament, "situation": situation}
        criteria = {a: genes[a] for a in ACTIONS}
        instr = QUESTION
    else:
        raise ValueError(f"unknown placement {placement}")
    return state, {"action": {"type": "choice", "instructions": instr, "criteria": criteria}}


_PROB_KEYS = ("probabilities", "probs", "distribution", "scores", "options", "option_probs")


def extract_probs(answer: dict[str, Any], labels=ACTIONS) -> tuple[np.ndarray, bool]:
    """Per-option probabilities from one Laya answer. Returns (probs, exact).

    exact=False means only choice+confidence was available and the rest of the
    mass was spread uniformly (weaker; flagged in results — plan §A12).
    """
    for k in _PROB_KEYS:
        v = answer.get(k)
        if isinstance(v, dict) and all(l in v for l in labels):
            return normalise([float(v[l]) for l in labels]), True
        if isinstance(v, list) and len(v) == len(labels):
            if all(isinstance(x, (int, float)) for x in v):
                return normalise(v), True
            if all(isinstance(x, dict) for x in v):
                m = {str(x.get("label", x.get("option"))): float(x.get("probability", x.get("prob", x.get("score", 0))))
                     for x in v}
                if all(l in m for l in labels):
                    return normalise([m[l] for l in labels]), True
    choice = answer.get("choice")
    conf = answer.get("confidence", answer.get("probability"))
    if choice in labels and isinstance(conf, (int, float)):
        p = np.full(len(labels), (1 - float(conf)) / (len(labels) - 1))
        p[list(labels).index(choice)] = float(conf)
        return normalise(p), False
    raise ValueError(f"cannot read probabilities from Laya answer keys {sorted(answer)}")


class LayaClient:
    """Thin wrapper so tests can swap in a fake with the same predict()."""

    def __init__(self, checkpoint: str, subfolder: str | None = None, device: str | None = None):
        import laya  # local install only
        kwargs = {"subfolder": subfolder} if subfolder else {}
        if device:
            kwargs["device"] = device
        try:
            self.agent = laya.load(checkpoint, **kwargs)
        except TypeError:                       # older/newer signature without device
            kwargs.pop("device", None)
            self.agent = laya.load(checkpoint, **kwargs)
        self.id = f"{checkpoint}/{subfolder or 'root'}"

    def predict(self, state: dict, questions: dict) -> dict:
        return self.agent.predict(state, questions)


class LayaBackend:
    name = "laya"

    def __init__(self, client, placement: str = "P4", style: str = "V1",
                 cache: KVCache | None = None, client_id: str | None = None):
        if placement not in PLACEMENTS:
            raise ValueError(placement)
        self.client, self.placement, self.style = client, placement, style
        self.cache = cache if cache is not None else KVCache()   # an empty KVCache is falsy (__len__)
        self.client_id = client_id or getattr(client, "id", "laya")
        self.approx = 0
        self.calls = 0

    @classmethod
    def from_config(cls, cfg, **over) -> "LayaBackend":
        lc = cfg.laya
        client = LayaClient(lc.checkpoint, lc.subfolder, lc.device)
        cache = KVCache(resolve(cfg.paths.cache_dir) / "laya.sqlite")
        return cls(client, over.get("placement", lc.placement), over.get("style", cfg.backend.obs_style), cache)

    def key(self, q: Query) -> str:
        return make_key("laya", self.client_id, self.placement, self.style, q.genome_key,
                        render(q.obs, self.style))

    def decide(self, queries: list[Query]) -> np.ndarray:
        out = np.zeros((len(queries), len(ACTIONS)))
        for i, q in enumerate(queries):
            k = self.key(q)
            hit = self.cache.get(k)
            if hit is None:
                state, questions = build_request(q.genes, render(q.obs, self.style), self.placement)
                res = self.client.predict(state, questions)
                self.calls += 1
                probs, exact = extract_probs(res["answers"]["action"])
                if not exact:
                    self.approx += 1
                    if self.approx == 1:
                        log.warning("Laya gave no per-option probabilities; using choice+confidence")
                hit = {"p": probs.tolist(), "exact": exact}
                self.cache.put(k, hit)
            out[i] = hit["p"]
        return out
