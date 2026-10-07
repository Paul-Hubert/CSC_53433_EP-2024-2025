"""Decision backend protocol (plan §A7). Both species since 2026-10-07: a query says whose
genes and observation it carries; one batch holds one species."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

import numpy as np

from ..genome import ACTIONS
from ..species import SPECIES


@dataclass(frozen=True)
class Query:
    genome_key: str             # run-independent hash of the gene texts
    genes: dict                 # action -> text
    obs: object                 # perception.Observation (prey) or PredatorObservation
    species: str = "prey"

    def __hash__(self):         # genes is derived from genome_key
        return hash((self.species, self.genome_key, self.obs))


class Backend(Protocol):
    name: str

    def decide(self, queries: list[Query]) -> np.ndarray:
        """Return probabilities, shape [len(queries), number of actions of their species], rows sum to 1."""
        ...


def batch_actions(queries: list[Query]) -> tuple[str, ...]:
    """The actions of the one species in a batch, in probability-vector order."""
    names = {q.species for q in queries}
    if len(names) > 1:
        raise ValueError(f"one species per batch, got {sorted(names)}")
    return SPECIES[names.pop() if names else "prey"].actions


def normalise(p: np.ndarray, eps: float = 1e-6) -> np.ndarray:
    p = np.clip(np.asarray(p, dtype=float), 0, None) + eps
    return p / p.sum(axis=-1, keepdims=True)


N_ACTIONS = len(ACTIONS)        # prey
