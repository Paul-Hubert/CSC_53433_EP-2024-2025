"""The teacher LLM used as a decision backend (plan §A7/§A8).

Modes: "points" (one call, integer points per action) and "ksample"
(k calls with different seeds, empirical distribution).
"""
from __future__ import annotations

from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np

from ..genome import ACTIONS
from ..obs_text import render
from .base import Query, normalise

POINTS_SCHEMA = {"type": "object",
                 "properties": {a: {"type": "integer", "minimum": 0, "maximum": 100} for a in ACTIONS},
                 "required": list(ACTIONS)}
CHOICE_SCHEMA = {"type": "object",
                 "properties": {"action": {"type": "string", "enum": list(ACTIONS)}},
                 "required": ["action"]}


def teacher_prompt(template: str, genes: dict, situation: str, mode: str) -> str:
    gene_lines = "\n".join(f'- {a}: "{genes[a]}"' for a in ACTIONS)
    temperament = " ".join(f'"{genes[l]}"' for l in ("risk", "social", "place"))
    if mode == "points":
        ask = "Distribute 100 points across the actions according to how likely this animal is to choose each."
    else:
        ask = "Answer with the single action this animal takes now."
    return template.format(genes=gene_lines, temperament=temperament, situation=situation, ask=ask)


class TeacherBackend:
    name = "ollama_policy"

    def __init__(self, client, model: str, prompt_path: str | Path, mode: str = "points",
                 style: str = "V1", k: int = 8, seed: int = 0, workers: int = 1,
                 keep_alive: str = "30m"):
        self.client, self.model, self.mode, self.style = client, model, mode, style
        self.template = Path(prompt_path).read_text()
        self.k, self.seed, self.workers, self.keep_alive = k, seed, workers, keep_alive
        self.failures = 0

    def _one(self, q: Query) -> np.ndarray:
        prompt = teacher_prompt(self.template, q.genes, render(q.obs, self.style), self.mode)
        msgs = [{"role": "user", "content": prompt}]
        try:
            if self.mode == "points":
                r = self.client.chat_json(self.model, msgs, POINTS_SCHEMA, keep_alive=self.keep_alive,
                                          options={"seed": self.seed, "temperature": 0})
                return normalise([max(0, int(r.get(a, 0))) for a in ACTIONS], eps=0.01)
            counts = np.zeros(len(ACTIONS))
            for i in range(self.k):
                r = self.client.chat_json(self.model, msgs, CHOICE_SCHEMA, keep_alive=self.keep_alive,
                                          options={"seed": self.seed + i, "temperature": 0.8})
                if r.get("action") in ACTIONS:
                    counts[ACTIONS.index(r["action"])] += 1
            return normalise(counts + 1.0)          # add-one smoothing
        except Exception:
            self.failures += 1
            return np.full(len(ACTIONS), 1.0 / len(ACTIONS))

    def decide(self, queries: list[Query]) -> np.ndarray:
        if self.workers > 1:
            with ThreadPoolExecutor(self.workers) as ex:
                return np.array(list(ex.map(self._one, queries)))
        return np.array([self._one(q) for q in queries])
