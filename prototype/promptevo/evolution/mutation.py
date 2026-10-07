"""Gene mutation: an LLM makes one random change (plan §A8, rev. 2026-10-02 and 2026-10-08).

Each gene of a child mutates with probability evolution.p_mut. The mutator LLM gets one
instruction drawn at random from evolution.mutation_prompts and the gene sentence, and
nothing else: no world, no other genes, no fitness. Mutation is blind; selection decides
what survives. The randomness comes from the drawn instruction, the seed and the sampling
temperature. Since 2026-10-08 the instructions (mutate_v3.txt) ask for small edits of what
the rule says ("a little stronger", "add a short condition", "say the opposite"); v2 asked
for random word edits, big ones included.

Guards: the answer must be a clean sentence (clean, valid), and since 2026-10-08 it may
differ from its parent in at most evolution.max_changed_words words and use only words of
evolution.vocabulary (the animal's world plus everyday words). A rejected answer is drawn
again (new instruction, new seed), up to evolution.mutation_tries attempts; if all fail the
gene doesn't mutate this time. The guards look only at the sentence, never at fitness.
`llm` is any callable llm(prompt, seed) -> str: Ollama in runs, a fake in tests.
"""
from __future__ import annotations

import difflib
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


def changed_words(old: str, new: str) -> int:
    """How many words an edit replaced, added or removed."""
    ops = difflib.SequenceMatcher(None, words(old), words(new)).get_opcodes()
    return sum(max(i2 - i1, j2 - j1) for op, i1, i2, j1, j2 in ops if op != "equal")


ENDINGS = ("'s", "s", "es", "ed", "d", "ing", "ly", "er", "est")


def load_vocabulary(path: str | Path) -> set[str]:
    """Words separated by blanks; lines starting with # are skipped."""
    lines = resolve(path).read_text(encoding="utf-8").splitlines()
    return {w.lower() for l in lines if not l.lstrip().startswith("#") for w in l.split()}


def known(word: str, vocab: set[str]) -> bool:
    """In the vocabulary, maybe with a simple ending ("chasing", "quickly", "tired")."""
    if word in vocab:
        return True
    for end in ENDINGS:
        stem = word[:-len(end)]
        if word.endswith(end) and len(stem) >= 2:
            if stem in vocab or stem + "e" in vocab or (len(stem) > 2 and stem[-1] == stem[-2] and stem[:-1] in vocab):
                return True
    return False


def unknown_words(text: str, vocab: set[str]) -> list[str]:
    return [w for w in re.findall(r"[a-z']+", text.lower().replace("’", "'")) if not known(w.strip("'"), vocab)]


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
        self.max_changed = ec.get("max_changed_words")        # None: any edit size
        vocab = ec.get("vocabulary")
        self.vocab = load_vocabulary(vocab) if vocab else None
        self.tries = max(1, int(ec.get("mutation_tries") or 1))
        self.instructions = instructions or load_instructions(ec.mutation_prompts)
        self.registry, self.llm, self.model = registry, llm, model
        self.stats = {"tried": 0, "ok": 0, "calls": 0, "rejected": Counter()}

    def reject(self, new: str, old: str) -> str | None:
        """Why an answer can't be the mutant (None: it can)."""
        if not valid(new, old, self.max_words):
            return "invalid"
        n = changed_words(old, new)
        if n == 0:
            return "unchanged"
        if self.max_changed is not None and n > int(self.max_changed):
            return "too big"
        if self.vocab is not None and unknown_words(new, self.vocab):
            return "unknown word"
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
