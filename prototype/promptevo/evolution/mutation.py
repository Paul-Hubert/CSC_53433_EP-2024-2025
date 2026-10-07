"""Gene mutation: an LLM makes one random change (plan §A8, rev. 2026-10-02 and 2026-10-08).

Each gene of a child mutates with probability evolution.p_mut. The mutator LLM gets one
instruction drawn at random from evolution.mutation_prompts and the gene sentence, and
nothing else: no world, no other genes, no fitness. Mutation is blind; selection decides
what survives. The randomness comes from the drawn instruction, the seed and the sampling
temperature. Since 2026-10-08 the instructions (mutate_v3.txt) ask for small edits of what
the rule says ("a little stronger", "add a short condition", "say the opposite"); v2 asked
for random word edits, big ones included.

Guards: the answer must be a clean sentence (clean, valid) that differs from its parent. A
rejected answer is drawn again (new instruction, new seed), up to evolution.mutation_tries
attempts; if all fail the gene doesn't mutate this time. The guards look only at the
sentence, never at fitness. (A word-change limit and a world vocabulary were tried on
2026-10-08 and removed the same day: results/mutation_test_v3_checks.md.)
`llm` is any callable llm(prompt, seed) -> str: Ollama in runs, a fake in tests.
"""
from __future__ import annotations

import re
from collections import Counter
from pathlib import Path
from typing import Callable

import numpy as np

from ..config import resolve
from ..genome import AlleleRegistry, Genome

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


def words(t: str) -> list[str]:
    """The words of a sentence, lower case, without punctuation (for edit sizes)."""
    return t.lower().rstrip(".!?").replace(",", " ").replace(":", " ").replace(";", " ").split()


def valid(new: str, old: str, max_words: int) -> bool:
    n = len(new.split())
    return (0 < n <= max_words and new.lower() != old.lower()
            and re.fullmatch(r"[A-Za-z0-9 ,.'’;:!?-]+", new) is not None)


class Mutator:
    def __init__(self, cfg, registry: AlleleRegistry, llm: LLM | None = None, model: str | None = None,
                 instructions: list[str] | None = None):
        ec = cfg.evolution
        self.p_mut = float(ec.p_mut) if llm else 0.0          # no mutator LLM: no mutation
        self.max_words = int(ec.max_words)
        self.tries = max(1, int(ec.get("mutation_tries") or 1))
        self.instructions = instructions or load_instructions(ec.mutation_prompts)
        self.registry, self.llm, self.model = registry, llm, model
        self.stats = {"tried": 0, "ok": 0, "calls": 0, "rejected": Counter()}

    def reject(self, new: str, old: str) -> str | None:
        """Why an answer can't be the mutant (None: it can)."""
        if not valid(new, old, self.max_words):
            return "invalid"
        if words(new) == words(old):                           # only punctuation changed
            return "unchanged"
        return None

    def attempt(self, locus: str, text: str, rng: np.random.Generator) -> tuple[str | None, int, int, str | None]:
        """One call: (new sentence or None, instruction index, seed, why it was rejected or None)."""
        k = int(rng.integers(len(self.instructions)))
        seed = int(rng.integers(2**31))
        answer = self.llm(TEMPLATE.format(instruction=self.instructions[k], text=text), seed)
        self.stats["calls"] += 1
        new = clean(answer)
        why = self.reject(new, text)
        if why:
            self.stats["rejected"][why] += 1
        return (None if why else new), k, seed, why

    def mutate_text(self, locus: str, text: str, rng: np.random.Generator) -> tuple[str | None, int, int]:
        """Up to `tries` attempts: (new sentence or None if every answer was rejected, instruction
        index, seed) of the last attempt."""
        for _ in range(self.tries):
            new, k, seed, _why = self.attempt(locus, text, rng)
            if new is not None:
                break
        return new, k, seed

    def mutate(self, g: Genome, rng: np.random.Generator) -> tuple[Genome, list[dict]]:
        events = []
        for locus, aid in zip(g.sp.loci, g.alleles):     # either species
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
