"""Load founder / contrast / control allele files and build genome sets. Plan §A5.

One pool per species: data/founder_pool_v2.json (prey) and data/predator_founder_pool_v1.json,
with the matching contrast_alleles files. The JSON files name the slots by action.
"""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np

from .config import resolve
from .genome import AlleleRegistry, Genome
from .species import PREY, Species

FILE_PREFIX = {"prey": "", "predator": "predator_"}
DEFAULT_VERSION = {"prey": "v2", "predator": "v1"}


class AllelePools:
    def __init__(self, registry: AlleleRegistry, data_dir: str | Path = "data",
                 version: str | None = None, control_file: str = "control_alleles_v1.json",
                 species: Species = PREY):
        d = resolve(data_dir)
        self.registry, self.species = registry, species
        pre, version = FILE_PREFIX[species.name], version or DEFAULT_VERSION[species.name]
        founder = json.loads((d / f"{pre}founder_pool_{version}.json").read_text(encoding="utf-8"))
        contrast = json.loads((d / f"{pre}contrast_alleles_{version}.json").read_text(encoding="utf-8"))
        control = json.loads((d / control_file).read_text(encoding="utf-8"))
        self.founders: dict[str, list[str]] = {}
        self.neutral: dict[str, str] = {}
        for action, locus in zip(species.actions, species.loci):
            entry = founder["loci"][action]
            self.neutral[locus] = registry.add(locus, entry["neutral"], "neutral").id
            ids = [registry.add(locus, t, "founder").id for t in entry["alleles"]]
            self.founders[locus] = ids + [self.neutral[locus]]
        self.contrast: dict[str, tuple[str, str]] = {
            l: (registry.add(l, contrast[a]["pro"], "contrast").id,
                registry.add(l, contrast[a]["anti"], "contrast").id)
            for a, l in zip(species.actions, species.loci)}
        self.control_texts: list[str] = control["shuffled"] + control["irrelevant"]

    # --- genome builders -------------------------------------------------
    def _genome(self, ids) -> Genome:
        return Genome(tuple(ids), self.species.name)

    def neutral_genome(self) -> Genome:
        return self._genome(self.neutral[l] for l in self.species.loci)

    def sample_founder(self, rng: np.random.Generator) -> Genome:
        return self._genome(self.founders[l][rng.integers(len(self.founders[l]))] for l in self.species.loci)

    def sample_control(self, rng: np.random.Generator) -> Genome:
        """Random-text genome (controls C4 / gibberish tests)."""
        ids = []
        for l in self.species.loci:
            t = self.control_texts[rng.integers(len(self.control_texts))]
            ids.append(self.registry.add(l, t, "control").id)
        return self._genome(ids)

    def contrast_pair(self, locus: str) -> tuple[Genome, Genome]:
        """(pro, anti) genomes that differ only at `locus` (or action); everything else neutral."""
        if locus not in self.species.loci:
            locus = self.species.prefix + locus
        base = self.neutral_genome()
        pro, anti = self.contrast[locus]
        return base.replace(locus, pro), base.replace(locus, anti)
