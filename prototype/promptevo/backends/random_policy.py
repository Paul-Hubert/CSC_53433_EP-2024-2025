from __future__ import annotations

import numpy as np

from .base import N_ACTIONS, Query


class RandomBackend:
    name = "random"

    def decide(self, queries: list[Query]) -> np.ndarray:
        return np.full((len(queries), N_ACTIONS), 1.0 / N_ACTIONS)
