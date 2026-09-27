# 00 — Vision (as stated by the course owner)

> Captured from the course owner's brief. Wording is condensed; intent is
> preserved. Items marked **(TBC)** need confirmation.

## Context

- Master 2 course. The current evolution lab is **outdated and has no real
  agentic AI**: a genetic algorithm evolves small neural networks that learn to
  find food, and that is essentially it.
- The implementation makes it **hard for students to interact with** the
  system. The whole system will be reworked.

## Core idea

1. **Agent brain = small local model with typed output.**
   Combine small, locally-run LLMs with a *"type-safe" / classification-style
   model* that takes text in and returns a decision from a fixed set of types.
   The model name was given verbally as something like "J-Laya" **(TBC — see
   [05](05-decision-backend.md#which-model-family-is-j-laya))**.
   It decides the agent's behaviour: direction, where to look, eat vs attack,
   follow vs run away, etc.

2. **Terrain matters.** Integrated with the terrain system; some areas are
   inaccessible (water, mountains).

3. **Genes are prompts.**
   Instead of the genome being neural-network weights mutated numerically, each
   gene is a **text prompt**. An agent has **10–20 genes** which are combined
   into one large prompt fed to the typed local model, which decides what the
   agent does.

4. **Sexual reproduction = gene swap.**
   When two agents reproduce, the child receives **half of each parent's
   genes** — the prompts are mixed, producing a different behaviour.

5. **Mutation by LLM.**
   With a small probability, a gene is mutated by a small local LLM that is
   prompted to *"make a random change to this prompt"*, **without further
   context**. The mutated gene goes back into the agent's genome. If it helps,
   it survives and spreads; if it breaks behaviour, the agent dies out.

6. **Readable outcome.**
   At the end of a run we can see **which prompts survived** and which scored
   best overall.

7. **Starting genes are the hard part.**
   Random text ⇒ random behaviour, and mutation will never climb out of that.
   Proposed: generate the ~10 starting genes randomly **but anchored to basic
   animal instincts** ("try not to die", "avoid predators", "try to eat"…).
   Possibly AI-generated, possibly hand-written. They should be **identical at
   the start of every simulation**, so all runs share a common origin and then
   diverge evolutionarily.

8. **Goal: a simple, extendable baseline for students**, not a finished
   product.

## Still to be provided by the course owner

- Terrain creation lab details (same course).
- Foliage generation lab details.
- (Anything else: grading, time budget, hardware available to students…)
