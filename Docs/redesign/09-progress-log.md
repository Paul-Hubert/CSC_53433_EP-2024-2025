# 09 — Progress log & handoff

One page covering what exists, what was decided, what is next, and how to pick
the work up. Latest state: **2026-10-01**, branch
`claude/ai-agents-course-redesign-71pc1u`.
The full reference documentation is in [`Docs/prompt-genome/`](../prompt-genome/README.md).

## Timeline

| Date | What happened |
|---|---|
| 2026-09-27 | Vision captured (00). Current lab audited (01). Honest assessment (02), architecture (03), genome/evolution design (04). |
| 2026-09-27 | "J-Laya" identified as the **Jev / Laya** decision models. Laya chosen as local decision backend; Ollama Cloud for generation (05). |
| 2026-09-28 | Phase 0 spike planned: spec, preregistered gates G1–G5, 7-session runbook with `/compact` points (08). |
| 2026-09-28 | `prototype/` created: offline core written and tested (world, sim, rule-based brain, metrics, mutation, probes, E1 suite). |
| 2026-09-28 | Teacher pipeline written: decision gate, mutants, allele-split dataset, resumable labelling. |
| 2026-09-30 | **Decision:** no Laya fine-tuning for now. **Ollama LLMs (local or cloud) decide and mutate** (05 §0, revision box in 08). Laya and the distillation pipeline are parked, not deleted. |
| 2026-10-01 | First local tests (Windows 11, RTX 5080 16 GB, Ollama 0.32). gemma4:26b and 12b probed and gated: genes steer behaviour (G1 ✔) but random text does too (G2 ✘). **Brain = gemma4:12b.** Fixed: model reloads (`think: false`, fixed `num_ctx`), request cache never written, Windows `status`, LLM mutation in rule-based runs. **Decision: the evolution lab is Lab 1 on a flat world with food at random**; terrain and foliage labs follow. First LLM-brain run. Reference documentation written (`Docs/prompt-genome/`). |

## What exists

**Design docs** (`Docs/redesign/`)

| Doc | Status |
|---|---|
| 00–04 | stable |
| 05 | §0 is current (LLM brain); the Laya analysis below it is kept |
| 06 | placeholder: needs the owner's terrain & foliage details (Lab 1 starts flat) |
| 07 | open questions + decision log |
| 08 | plan + runbook (read its revision box first) |
| 09 | this page |

**Prototype** (`prototype/`, Python ≥ 3.10; start with `prototype/README.md`)

| Part | State |
|---|---|
| World (flat Lab 1 world with random food; optional noise terrain), predators, simulation, 7 actions, sexual reproduction, logging | working, tested; Lab 1 world tuned 2026-10-01 |
| Rule-based brain (keyword reading of genes) | working; fast reference and control |
| **LLM brain** `--backend llm`: points / logprobs / table / ksample modes, caches, parallel requests, cloud key | working end to end with gemma4:12b (local Ollama); logprobs mode unusable with gemma4 |
| Gene mutation (word operators + LLM rewrite with guards) | working; LLM rewrite tested with gemma4:26b |
| Metrics + E1 gene-sensitivity suite + decision-model gate | working (rule-based reference in `prototype/results/`) |
| Ollama probe (logprobs usable? seconds per decision) | run on gemma4 26b and 12b (`prototype/results/e0_ollama*.md`) |
| Laya backend, Laya probe, token budget, dataset, labelling | parked |
| Evolution matrix, common garden, analysis, report (S6–S7) | not written yet |

Tests: `cd prototype && pytest -q` → 41 passed. `pytest -m ollama` passes
against gemma4:12b.

## Key numbers so far (small profile)

- **Simulation speed:** 5 000 ticks ≈ 12 s on CPU with the rule-based brain.
- **Lab 1 flat world** (food regrowth 0.0007, rule-based brain): population
  25–28 and limited by food, never at the cap; deaths ≈ 230 predator / 185
  starvation per 5 000 ticks; 22–25 generations. A random brain collapses,
  so behaviour matters.
- **Rule-based E1 reference:** directed tests 100 % correct (ΔP 0.48);
  gibberish genes have no effect; MI_G 0.23 bits, just under the G2
  threshold of 0.25.
- **LLM load (fake LLM):**
  - 2 000 ticks → 5 349 decisions → 2 211 LLM answers in points mode.
  - Table+prefetch halves the requests but generates 4–6× more answers.
  - Estimate: 4–6 h per 20 000-tick run at ~1 s/answer. This is the main
    cost risk.
- **LLM brain, measured (gemma4:12b, RTX 5080, 2026-10-01):** 500 ticks →
  1 722 decisions → 761 LLM calls, 425 s (≈ 4 decisions/s; G5 asks for 50).
  The population fell to the floor (LLM-read founders attack 16 % of the time),
  against 17 animals with the rule-based brain over the same ticks.
- **Decision-model gate** (420 decisions per model): sign accuracy 0.96 (12b) /
  0.99 (26b); MI_G founders 0.250 / 0.244 vs random text 0.297 / 0.314 → G2 ✘.

## Decisions waiting on the owner

1. Review the draft founder genes (`prototype/data/founder_pool_v1.json`, H1).
2. ~~Choose the decision model~~ → gemma4:12b (2026-10-01). Still open: the
   mutator model (26b now; 12b would avoid model swaps on a 16 GB GPU).
3. Decide whether the G2 threshold (MI_G ≥ 0.25) stays, given the rule-based
   reference scores 0.23.
4. Set the E4 budget: run length, decision period, seeds.
5. Provide the terrain & foliage lab details (06).
6. Give the details of your own git server (below).
7. How to handle G2 (random text moves behaviour): all-zero answers, control
   sentences, prompt iterations.
8. Lab 1 platform (Python prototype or Unity) and format (sessions, grading).

Details and context: [`Docs/prompt-genome/09-status-and-roadmap.md`](../prompt-genome/09-status-and-roadmap.md#4-decisions-waiting-on-the-course-owner).

## Next steps (on a machine with Ollama or an Ollama Cloud key)

```bash
cd prototype && python -m venv .venv && source .venv/bin/activate
pip install -e ".[dev]" && pytest -q
ollama pull gemma4:12b
python -m experiments.e0_probe_ollama --teacher gemma4:12b --mutator gemma4:12b
python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b
python -m experiments.e1_sensitivity --backend llm --tag llm_points
python -m experiments.smoke_run --backend llm --ticks 500
```

Or run `claude` inside `prototype/` and paste the kickoff prompt from
08 §B0. `prototype/STATUS.md` says where to resume.

## Mirroring to your own git server (pending)

This needs your server's URL; it was not provided yet. From any clone:

```bash
git remote add personal <https-or-ssh-url-of-your-repo>
git push personal claude/ai-agents-course-redesign-71pc1u   # this branch
git push personal master                                     # optional: original course
```

- **HTTPS:** use a personal access token through your credential helper;
  never put it in the URL or in any committed file.
- **From a Claude cloud session:** the environment's network access must
  allow the server's hostname, and the token should come from an
  environment variable set in the environment settings.
