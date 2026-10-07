"""Alleles (gene texts), genomes (one allele per locus) and crossover. Plan §A5.

Both species use this module (rev. 2026-10-07): a genome knows its species, and one
registry holds the alleles of both (see species.py for the locus ids)."""
from __future__ import annotations

import hashlib
import json
from dataclasses import asdict, dataclass
from pathlib import Path

import numpy as np

from .species import ALL_LOCI, PREY, SPECIES, Species

ACTIONS = PREY.actions   # the prey's actions, in the order of their probability vectors
LOCI = PREY.loci         # one gene per action (rev. 2026-10-07: risk, social, place, attack, wander removed)


@dataclass(frozen=True)
class Allele:
    id: str
    locus: str
    text: str
    origin: str                  # founder | neutral | contrast | control | mutant | ood
    parent_id: str | None = None
    operator: str | None = None
    model: str | None = None
    seed: int | None = None


@dataclass(frozen=True)
class Genome:
    """One allele id per locus of its species, in the species' locus order."""
    alleles: tuple[str, ...]
    species: str = "prey"

    def __post_init__(self):
        n = len(self.sp.loci)
        if len(self.alleles) != n:
            raise ValueError(f"a {self.species} genome needs {n} alleles, got {len(self.alleles)}")

    @property
    def sp(self) -> Species:
        return SPECIES[self.species]

    def at(self, locus: str) -> str:
        return self.alleles[self.sp.loci.index(locus)]

    def replace(self, locus: str, allele_id: str) -> "Genome":
        a = list(self.alleles)
        a[self.sp.loci.index(locus)] = allele_id
        return Genome(tuple(a), self.species)


class AlleleRegistry:
    """All alleles seen in a run, both species. Same (locus, text) → same allele (first origin kept)."""

    def __init__(self):
        self._by_id: dict[str, Allele] = {}
        self._by_text: dict[tuple[str, str], str] = {}
        self._counter: dict[str, int] = {}
        self._gkey: dict[Genome, str] = {}

    def add(self, locus: str, text: str, origin: str, **meta) -> Allele:
        if locus not in ALL_LOCI:
            raise ValueError(f"unknown locus {locus}")
        text = " ".join(text.split())
        existing = self._by_text.get((locus, text))
        if existing:
            return self._by_id[existing]
        n = self._counter.get(locus, 0)
        self._counter[locus] = n + 1
        al = Allele(id=f"{locus}:{n}", locus=locus, text=text, origin=origin, **meta)
        self._by_id[al.id] = al
        self._by_text[(locus, text)] = al.id
        return al

    def get(self, allele_id: str) -> Allele:
        return self._by_id[allele_id]

    def text(self, allele_id: str) -> str:
        return self._by_id[allele_id].text

    def by_locus(self, locus: str, origin: str | None = None) -> list[Allele]:
        return [a for a in self._by_id.values()
                if a.locus == locus and (origin is None or a.origin == origin)]

    def __len__(self) -> int:
        return len(self._by_id)

    def genes(self, g: Genome) -> dict[str, str]:
        """{action: gene text}: what the brain reads (for the prey, action = locus)."""
        return {a: self.text(aid) for a, aid in zip(g.sp.actions, g.alleles)}

    def genome_key(self, g: Genome) -> str:
        """Run-independent hash of the gene texts (used for caches). Predator genomes also hash
        their species; prey keys are unchanged since 2026-09, so cached answers stay valid."""
        k = self._gkey.get(g)
        if k is None:
            texts = [self.text(a) for a in g.alleles]
            blob = json.dumps(texts if g.species == "prey" else [g.species, *texts], ensure_ascii=False)
            k = hashlib.sha256(blob.encode()).hexdigest()[:24]
            self._gkey[g] = k
        return k

    def make_genome(self, genes: dict[str, str], origin: str = "custom", species: str = "prey") -> Genome:
        """Genome from {action: text}."""
        sp = SPECIES[species]
        return Genome(tuple(self.add(l, genes[a], origin).id for a, l in zip(sp.actions, sp.loci)), species)

    def dump_jsonl(self, path: str | Path) -> None:
        with Path(path).open("w") as f:
            for a in self._by_id.values():
                f.write(json.dumps(asdict(a), ensure_ascii=False) + "\n")


def crossover_uniform(a: Genome, b: Genome, rng: np.random.Generator) -> Genome:
    if a.species != b.species:
        raise ValueError(f"cannot cross a {a.species} with a {b.species}")
    pick = rng.random(len(a.alleles)) < 0.5
    return Genome(tuple(x if p else y for x, y, p in zip(a.alleles, b.alleles, pick)), a.species)
