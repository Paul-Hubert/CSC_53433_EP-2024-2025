"""Mutation operators on gene texts, with guards (plan §A8).

Word-level operators need no model. `llm_rewrite` takes any callable
rewriter(text, style, seed) -> str (e.g. OllamaClient.rewrite), so it can be
tested with a fake and swapped between local Ollama and the cloud later.
"""
from __future__ import annotations

import re
from typing import Callable

import numpy as np

from ..genome import ACTION_LOCI, LOCI, AlleleRegistry, Genome

LADDER = ["never", "rarely", "sometimes", "often", "always"]
NEGATIONS = [("always", "never"), ("seek", "avoid"), ("stay close to", "keep your distance from"),
             ("run from", "stand up to"), ("follow", "ignore"), ("keep moving", "stay put")]
CONDITIONS = ["when hungry", "when full", "when threatened", "when alone",
              "when food is close", "when food is scarce", "when tired", "when safe"]
SYNONYMS = {"close": "near", "near": "close", "food": "something to eat", "run": "dash",
            "quickly": "fast", "fast": "quickly", "other animals": "others", "others": "other animals",
            "danger": "a threat", "rest": "sleep", "sleep": "rest", "fight": "attack",
            "attack": "fight", "explore": "roam", "roam": "explore", "partner": "mate",
            "tired": "exhausted", "weaker": "smaller", "strongest": "biggest"}
LLM_STYLES = ["random change", "invert", "exaggerate", "soften", "add a condition",
              "make more specific", "make more general"]

Rewriter = Callable[[str, str, int], str]


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


def _sub_once(text: str, pat: str, repl: str) -> str | None:
    def keep_case(m: re.Match) -> str:
        return repl[:1].upper() + repl[1:] if m.group(0)[:1].isupper() else repl
    new, n = re.subn(rf"\b{re.escape(pat)}\b", keep_case, text, count=1, flags=re.I)
    return new if n else None


def _decap(t: str) -> str:
    return t[:1].lower() + t[1:]


# --- word operators ------------------------------------------------------------
def op_intensity(text: str, rng: np.random.Generator) -> str:
    low = text.lower()
    for i, w in enumerate(LADDER):
        if re.search(rf"\b{w}\b", low):
            j = int(np.clip(i + rng.choice([-1, 1]), 0, len(LADDER) - 1))
            if j == i:
                j = i - 1 if i > 0 else i + 1
            return _sub_once(text, w, LADDER[j])
    return f"{LADDER[int(rng.choice([1, 3]))].capitalize()} {_decap(text)}"


def op_negate(text: str, rng: np.random.Generator) -> str:
    for a, b in NEGATIONS:
        for x, y in ((a, b), (b, a)):
            new = _sub_once(text, x, y)
            if new:
                return new
    if re.match(r"(do not|never) ", text, re.I):
        return re.sub(r"^(do not|never) ", "", text, flags=re.I).capitalize()
    if re.match(r"^\w+:", text):                     # temperament style "Bold: ..."
        return re.sub(r"^Not ", "", text) if text.startswith("Not ") else f"Not {_decap(text)}"
    return f"Do not {_decap(text)}"


def op_condition_swap(text: str, rng: np.random.Generator) -> str:
    for c in CONDITIONS:
        if c in text.lower():
            others = [o for o in CONDITIONS if o != c]
            return _sub_once(text, c, others[int(rng.integers(len(others)))])
    c = CONDITIONS[int(rng.integers(len(CONDITIONS)))]
    return re.sub(r"[.!?]$", "", text) + f" {c}."


def op_synonym(text: str, rng: np.random.Generator) -> str | None:
    found = [w for w in SYNONYMS if re.search(rf"\b{re.escape(w)}\b", text, re.I)]
    if not found:
        return None
    w = found[int(rng.integers(len(found)))]
    return _sub_once(text, w, SYNONYMS[w])


WORD_OPS = {"intensity": op_intensity, "negate": op_negate,
            "condition_swap": op_condition_swap, "synonym": op_synonym}


class Mutator:
    def __init__(self, cfg, registry: AlleleRegistry, founders: dict[str, list[str]],
                 rewriter: Rewriter | None = None, rewriter_model: str | None = None):
        ec = cfg.evolution
        self.p_mut = float(ec.p_mut)
        self.max_words = {l: int(ec.max_action_words if l in ACTION_LOCI else ec.max_temperament_words)
                          for l in LOCI}
        ops = dict(ec.operators)
        if rewriter is None:
            ops["llm_rewrite"] = 0.0
        self.op_names = [k for k, v in ops.items() if v > 0]
        w = np.array([ops[k] for k in self.op_names], dtype=float)
        self.op_p = w / w.sum()
        self.registry, self.founders = registry, founders
        self.rewriter, self.rewriter_model = rewriter, rewriter_model
        self.stats = {k: {"tried": 0, "ok": 0} for k in ops}

    def mutate_text(self, locus: str, text: str, op: str, rng: np.random.Generator,
                    seed: int) -> str | None:
        if op == "llm_rewrite":
            style = LLM_STYLES[int(rng.integers(len(LLM_STYLES)))]
            for attempt in range(3):
                new = clean(self.rewriter(text, style, seed + attempt))
                if valid(new, text, self.max_words[locus]):
                    return new
            return None
        if op == "founder_reintroduce":
            cands = [a for a in self.founders[locus] if self.registry.text(a) != text]
            return self.registry.text(cands[int(rng.integers(len(cands)))]) if cands else None
        new = WORD_OPS[op](text, rng)
        if new is None:
            return None
        new = clean(new)
        return new if valid(new, text, self.max_words[locus]) else None

    def mutate(self, g: Genome, rng: np.random.Generator) -> tuple[Genome, list[dict]]:
        events = []
        for locus, aid in zip(LOCI, g.alleles):
            if rng.random() >= self.p_mut:
                continue
            op = self.op_names[int(rng.choice(len(self.op_names), p=self.op_p))]
            seed = int(rng.integers(2**31))
            old = self.registry.text(aid)
            self.stats[op]["tried"] += 1
            new = self.mutate_text(locus, old, op, rng, seed)
            if new is None and op == "llm_rewrite":             # fall back to a word operator
                op = ["intensity", "negate", "condition_swap"][int(rng.integers(3))]
                new = self.mutate_text(locus, old, op, rng, seed)
            if new is None:
                continue
            self.stats[op]["ok"] += 1
            al = self.registry.add(locus, new, "mutant", parent_id=aid, operator=op, seed=seed,
                                   model=self.rewriter_model if op == "llm_rewrite" else None)
            g = g.replace(locus, al.id)
            events.append({"locus": locus, "parent": aid, "child": al.id, "op": op, "text": new})
        return g, events
