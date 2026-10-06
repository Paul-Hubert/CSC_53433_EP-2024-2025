"""Alleles (gene texts), genomes (one allele per locus) and crossover. Plan §A5."""
from __future__ import annotations

import hashlib
import json
from dataclasses import asdict, dataclass
from pathlib import Path

import numpy as np

ACTIONS = ("eat", "flee", "follow", "rest", "mate")   # action order used for every probability vector
LOCI = ACTIONS   # one gene per action (rev. 2026-10-07: risk, social, place, attack, wander removed)


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
    """One allele id per locus, in LOCI order."""
    alleles: tuple[str, ...]

    def __post_init__(self):
        if len(self.alleles) != len(LOCI):
            raise ValueError(f"genome needs {len(LOCI)} alleles, got {len(self.alleles)}")

    def at(self, locus: str) -> str:
        return self.alleles[LOCI.index(locus)]

    def replace(self, locus: str, allele_id: str) -> "Genome":
        a = list(self.alleles)
        a[LOCI.index(locus)] = allele_id
        return Genome(tuple(a))


class AlleleRegistry:
    """All alleles seen in a run. Same (locus, text) → same allele (first origin kept)."""

    def __init__(self):
        self._by_id: dict[str, Allele] = {}
        self._by_text: dict[tuple[str, str], str] = {}
        self._counter: dict[str, int] = {}
        self._gkey: dict[Genome, str] = {}

    def add(self, locus: str, text: str, origin: str, **meta) -> Allele:
        if locus not in LOCI:
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
        return {locus: self.text(aid) for locus, aid in zip(LOCI, g.alleles)}

    def genome_key(self, g: Genome) -> str:
        """Run-independent hash of the gene texts (used for caches)."""
        k = self._gkey.get(g)
        if k is None:
            blob = json.dumps([self.text(a) for a in g.alleles], ensure_ascii=False)
            k = hashlib.sha256(blob.encode()).hexdigest()[:24]
            self._gkey[g] = k
        return k

    def make_genome(self, genes: dict[str, str], origin: str = "custom") -> Genome:
        return Genome(tuple(self.add(l, genes[l], origin).id for l in LOCI))

    def dump_jsonl(self, path: str | Path) -> None:
        with Path(path).open("w") as f:
            for a in self._by_id.values():
                f.write(json.dumps(asdict(a), ensure_ascii=False) + "\n")


def crossover_uniform(a: Genome, b: Genome, rng: np.random.Generator) -> Genome:
    pick = rng.random(len(LOCI)) < 0.5
    return Genome(tuple(x if p else y for x, y, p in zip(a.alleles, b.alleles, pick)))
