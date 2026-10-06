"""Load founder / contrast / control allele files and build genome sets. Plan §A5."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np

from .config import resolve
from .genome import LOCI, AlleleRegistry, Genome


class AllelePools:
    def __init__(self, registry: AlleleRegistry, data_dir: str | Path = "data",
                 version: str = "v2", control_file: str = "control_alleles_v1.json"):
        d = resolve(data_dir)
        self.registry = registry
        founder = json.loads((d / f"founder_pool_{version}.json").read_text(encoding="utf-8"))
        contrast = json.loads((d / f"contrast_alleles_{version}.json").read_text(encoding="utf-8"))
        control = json.loads((d / control_file).read_text(encoding="utf-8"))
        self.founders: dict[str, list[str]] = {}
        self.neutral: dict[str, str] = {}
        for locus in LOCI:
            entry = founder["loci"][locus]
            self.neutral[locus] = registry.add(locus, entry["neutral"], "neutral").id
            ids = [registry.add(locus, t, "founder").id for t in entry["alleles"]]
            self.founders[locus] = ids + [self.neutral[locus]]
        self.contrast: dict[str, tuple[str, str]] = {
            l: (registry.add(l, contrast[l]["pro"], "contrast").id,
                registry.add(l, contrast[l]["anti"], "contrast").id)
            for l in LOCI}
        self.control_texts: list[str] = control["shuffled"] + control["irrelevant"]

    # --- genome builders -------------------------------------------------
    def neutral_genome(self) -> Genome:
        return Genome(tuple(self.neutral[l] for l in LOCI))

    def sample_founder(self, rng: np.random.Generator) -> Genome:
        return Genome(tuple(self.founders[l][rng.integers(len(self.founders[l]))] for l in LOCI))

    def sample_control(self, rng: np.random.Generator) -> Genome:
        """Random-text genome (controls C4 / gibberish tests)."""
        ids = []
        for l in LOCI:
            t = self.control_texts[rng.integers(len(self.control_texts))]
            ids.append(self.registry.add(l, t, "control").id)
        return Genome(tuple(ids))

    def contrast_pair(self, locus: str) -> tuple[Genome, Genome]:
        """(pro, anti) genomes that differ only at `locus`; everything else neutral."""
        base = self.neutral_genome()
        pro, anti = self.contrast[locus]
        return base.replace(locus, pro), base.replace(locus, anti)
