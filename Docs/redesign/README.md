# Course Redesign — "Prompt-Genome" Evolutionary Agents

Design notes for reworking the **Crowds & Evolution** lab (and the surrounding
terrain / foliage labs) of this Master 2 course. Status: **idea capture &
pre-design** — nothing here is implemented yet.

| # | Document | What it holds |
|---|----------|---------------|
| 00 | [Vision](00-vision.md) | The idea as stated by the course owner, lightly structured. Source of truth for intent. |
| 01 | [Current system audit](01-current-system.md) | What exists in the repo today and why it is hard for students to work with. |
| 02 | [Honest assessment](02-assessment.md) | Strengths, risks, and the design decisions that make or break the idea. |
| 03 | [Architecture](03-architecture.md) | Proposed module boundaries, interfaces, data flow, simulation loop. |
| 04 | [Genome & evolution](04-genome-and-evolution.md) | Genes, loci, founder pool, crossover, mutation, selection, logging. |
| 05 | [Decision backend](05-decision-backend.md) | Laya vs Jev, gene → Laya input mapping, fine-tuning by distillation, Unity integration, Ollama Cloud for generation, performance budget. |
| 06 | [World: terrain & foliage](06-world-terrain-foliage.md) | Terrain, water, mountains, food/foliage — **to be filled in by the course owner**. |
| 07 | [Open questions & roadmap](07-open-questions-and-roadmap.md) | Decisions pending, and a de-risked build order. |

## One-paragraph summary

Replace the neural-network genome with a **genome of text genes** (≈10–20
short natural-language prompts, e.g. *"When hungry, favour food over
safety"*). At decision time the genes are concatenated with a textual
observation of the agent's surroundings and fed to **Laya**, a local
open-weights *decision model* that returns calibrated probabilities over
typed options (no text generation), to pick an action (move / turn / look / eat / attack /
follow / flee / mate …). Reproduction is **gene-level crossover** between two
parents; mutation is done by an **LLM rewriting one gene** at a low rate
(pooled Ollama Cloud subscription, local Ollama as fallback). Selection is implicit (survive, eat, reproduce). All runs start from the
**same fixed founder pool** of instinct-like genes, so different runs diverge
from a common origin and the surviving prompts can be read and compared at
the end. The system must be a **simple, extendable baseline** for students.
