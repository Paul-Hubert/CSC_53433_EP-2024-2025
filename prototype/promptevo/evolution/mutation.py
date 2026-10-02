"""Gene mutation: an LLM makes one random change (plan §A8, rev. 2026-10-02).

Each gene of a child mutates with probability evolution.p_mut. The mutator LLM gets one
instruction drawn at random from evolution.mutation_prompts (all of them variants of
"make a random change") and the gene sentence, and nothing else: no world, no other
genes, no fitness. Mutation is blind; selection decides what survives. The randomness
comes from the drawn instruction, the seed and the sampling temperature.
The answer must pass the guards (clean, valid), otherwise the gene doesn't mutate this time.
`llm` is any callable llm(prompt, seed) -> str: Ollama in runs, a fake in tests.
"""
from __future__ import annotations

import re
from pathlib import Path
from typing import Callable

import numpy as np

from ..config import resolve
from ..genome import ACTION_LOCI, LOCI, AlleleRegistry, Genome

TEMPLATE = '{instruction}\n\n"{text}"\n\nReply with the new sentence only.'

LLM = Callable[[str, int], str]


def load_instructions(path: str | Path) -> list[str]:
    """One instruction per line; blank lines and lines starting with # are skipped."""
    lines = resolve(path).read_text(encoding="utf-8").splitlines()
    return [l.strip() for l in lines if l.strip() and not l.lstrip().startswith("#")]


# --- guards ------------------------------------------------------------------
def clean(text: str) -> str:
    quoted = re.search(r'["“]([^"“”]{3,})["”]', text)
    t = quoted.group(1) if quoted else text
    t = t.strip().strip('"“”\'`').strip()
    t = re.sub(r"^(here is|here's|modified|new|mutated|rewritten)[^:]*:\s*", "", t, flags=re.I)
    t = t.split("\n")[0].strip().strip('"“”\'`')
    m = re.match(r"(.+?[.!?])(\s|$)", t)       # first sentence only
    if m:
        t = m.group(1)
    t = " ".join(t.split())
    if t and t[-1] not in ".!?":
        t += "."
    return t[:1].upper() + t[1:] if t else t


def valid(new: str, old: str, max_words: int) -> bool:
    n = len(new.split())
    return (0 < n <= max_words and new.lower() != old.lower()
            and re.fullmatch(r"[A-Za-z0-9 ,.'’;:!?-]+", new) is not None)


class Mutator:
    def __init__(self, cfg, registry: AlleleRegistry, llm: LLM | None = None, model: str | None = None,
                 instructions: list[str] | None = None):
        ec = cfg.evolution
        self.p_mut = float(ec.p_mut) if llm else 0.0          # no mutator LLM: no mutation
        self.max_words = {l: int(ec.max_action_words if l in ACTION_LOCI else ec.max_temperament_words)
                          for l in LOCI}
        self.instructions = instructions or load_instructions(ec.mutation_prompts)
        self.registry, self.llm, self.model = registry, llm, model
        self.stats = {"tried": 0, "ok": 0}

    def mutate_text(self, locus: str, text: str, rng: np.random.Generator) -> tuple[str | None, int, int]:
        """One attempt: (new sentence or None if the guards reject it, instruction index, seed)."""
        k = int(rng.integers(len(self.instructions)))
        seed = int(rng.integers(2**31))
        answer = self.llm(TEMPLATE.format(instruction=self.instructions[k], text=text), seed)
        new = clean(answer)
        return (new if valid(new, text, self.max_words[locus]) else None), k, seed

    def mutate(self, g: Genome, rng: np.random.Generator) -> tuple[Genome, list[dict]]:
        events = []
        for locus, aid in zip(LOCI, g.alleles):
            if rng.random() >= self.p_mut:
                continue
            self.stats["tried"] += 1
            new, k, seed = self.mutate_text(locus, self.registry.text(aid), rng)
            if new is None:
                continue
            self.stats["ok"] += 1
            al = self.registry.add(locus, new, "mutant", parent_id=aid, operator=f"llm#{k}",
                                   seed=seed, model=self.model)
            g = g.replace(locus, al.id)
            events.append({"locus": locus, "parent": aid, "child": al.id, "prompt": k, "text": new})
        return g, events
