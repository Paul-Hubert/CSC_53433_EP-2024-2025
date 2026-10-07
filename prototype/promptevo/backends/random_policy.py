from __future__ import annotations

import numpy as np

from .base import Query, batch_actions


class RandomBackend:
    name = "random"

    def decide(self, queries: list[Query]) -> np.ndarray:
        k = len(batch_actions(queries))
        return np.full((len(queries), k), 1.0 / k)
