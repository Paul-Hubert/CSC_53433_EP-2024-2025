"""Named, independent, reproducible random streams (no global RNG anywhere)."""
from __future__ import annotations

import hashlib

import numpy as np


def stable_int(name: str) -> int:
    return int.from_bytes(hashlib.sha256(name.encode()).digest()[:4], "little")


class Streams:
    """streams.get("world") always returns the same Generator for a given seed."""

    def __init__(self, seed: int):
        self.seed = int(seed)
        self._gens: dict[str, np.random.Generator] = {}

    def get(self, name: str) -> np.random.Generator:
        if name not in self._gens:
            ss = np.random.SeedSequence([self.seed, stable_int(name)])
            self._gens[name] = np.random.default_rng(ss)
        return self._gens[name]

    def fresh(self, name: str) -> np.random.Generator:
        """A new generator for `name` from its start (does not disturb get())."""
        return np.random.default_rng(np.random.SeedSequence([self.seed, stable_int(name)]))
