"""The two species (rev. 2026-10-07): prey animals and predators, both genetic animals.

Each species has its own small set of actions and one gene per action, read by the same
kind of LLM brain. A gene's locus id is the species prefix + the action: the prey keep
their old ids ("eat:3"), predator genes are "predator.hunt:0", so one allele registry
holds both species without clashes ("rest" and "mate" exist in both).
"""
from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class Species:
    name: str                       # "prey" | "predator" (events, stats, configs)
    actions: tuple[str, ...]        # one gene per action, in this order (also probability vectors)
    prefix: str                     # locus id = prefix + action
    config: str                     # config section with the species' parameters

    @property
    def loci(self) -> tuple[str, ...]:
        return tuple(self.prefix + a for a in self.actions)

    def action_of(self, locus: str) -> str:
        return locus[len(self.prefix):]


PREY = Species("prey", ("eat", "flee", "follow", "rest", "mate"), "", "agents")
PREDATOR = Species("predator", ("hunt", "follow", "rest", "mate"), "predator.", "predators")   # follow since v2
SPECIES = {s.name: s for s in (PREY, PREDATOR)}
ALL_LOCI = PREY.loci + PREDATOR.loci


def species_of_locus(locus: str) -> Species:
    for s in (PREDATOR, PREY):              # the prefixed species first: prey loci have no prefix
        if locus in s.loci:
            return s
    raise ValueError(f"unknown locus {locus}")


def species_cfg(cfg, species: str | Species):
    """The config section of a species: cfg.agents (prey) or cfg.predators."""
    s = SPECIES[species] if isinstance(species, str) else species
    return getattr(cfg, s.config)
