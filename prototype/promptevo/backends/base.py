"""Decision backend protocol (plan §A7)."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

import numpy as np

from ..genome import ACTIONS
from ..perception import Observation


@dataclass(frozen=True)
class Query:
    genome_key: str             # run-independent hash of the gene texts
    genes: dict                 # locus -> text
    obs: Observation

    def __hash__(self):         # genes is derived from genome_key
        return hash((self.genome_key, self.obs))


class Backend(Protocol):
    name: str

    def decide(self, queries: list[Query]) -> np.ndarray:
        """Return probabilities, shape [len(queries), len(ACTIONS)], rows sum to 1."""
        ...


def normalise(p: np.ndarray, eps: float = 1e-6) -> np.ndarray:
    p = np.clip(np.asarray(p, dtype=float), 0, None) + eps
    return p / p.sum(axis=-1, keepdims=True)


N_ACTIONS = len(ACTIONS)
