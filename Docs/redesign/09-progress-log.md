# 09 — Progress log & handoff

One page covering what exists, what was decided, what is next, and how to pick
the work up. Latest state: **2026-09-30**, branch
`claude/ai-agents-course-redesign-71pc1u`.

## Timeline

| Date | What happened |
|---|---|
| 2026-09-27 | Vision captured (00). Current lab audited (01). Honest assessment (02), architecture (03), genome/evolution design (04). |
| 2026-09-27 | "J-Laya" identified as the **Jev / Laya** decision models. Laya chosen as local decision backend; Ollama Cloud for generation (05). |
| 2026-09-28 | Phase 0 spike planned: spec, preregistered gates G1–G5, 7-session runbook with `/compact` points (08). |
| 2026-09-28 | `prototype/` created: offline core written and tested (world, sim, rule-based brain, metrics, mutation, probes, E1 suite). |
| 2026-09-28 | Teacher pipeline written: decision gate, mutants, allele-split dataset, resumable labelling. |
| 2026-09-30 | **Decision:** no Laya fine-tuning for now. **Ollama LLMs (local or cloud) decide and mutate** (05 §0, revision box in 08). Laya and the distillation pipeline are parked, not deleted. |
| 2026-10-05 | `Docs/system/` (reference docs of the prototype) and `Docs/unity/` (proposed Unity architecture, patterns, editor tooling, change scenarios, Claude Design prompt) written. |

## What exists

**Design docs** (`Docs/redesign/`)

| Doc | Status |
|---|---|
| 00–04 | stable |
| 05 | §0 is current (LLM brain); the Laya analysis below it is kept |
| 06 | placeholder: needs the owner's terrain & foliage details |
| 07 | open questions + decision log |
| 08 | plan + runbook (read its revision box first) |
| 09 | this page |

**Prototype** (`prototype/`, Python ≥ 3.10; start with `prototype/README.md`)

| Part | State |
|---|---|
| World (water, mountains, food, predators), simulation, 7 actions, sexual reproduction, logging | working, tested, provisionally tuned |
| Rule-based brain (keyword reading of genes) | working; fast reference and control |
| **LLM brain** `--backend llm`: points / logprobs / table / ksample modes, caches, parallel requests, cloud key | written; tested against a fake Ollama only |
| Gene mutation (word operators + LLM rewrite with guards) | working (LLM part fake-tested) |
| Metrics + E1 gene-sensitivity suite + decision-model gate | working (rule-based reference in `prototype/results/`) |
| Ollama probe (logprobs usable? seconds per decision) | written; must be run locally |
| Laya backend, Laya probe, token budget, dataset, labelling | parked |
| Evolution matrix, common garden, analysis, report (S6–S7) | not written yet |

Tests: `cd prototype && pytest -q` → 38 passed. `pytest -m ollama` runs
against a real model.

## Key numbers so far (offline, small profile)

- **Simulation speed:** 5 000 ticks ≈ 12 s on CPU with the rule-based brain.
- **Tuned world:** population ≈ 28 and limited by food. Deaths split
  between starvation and predators; 24 generations per 5 000 ticks. A random
  brain collapses, so behaviour matters.
- **Rule-based E1 reference:** directed tests 100 % correct (ΔP 0.48);
  gibberish genes have no effect; MI_G 0.23 bits, just under the G2
  threshold of 0.25.
- **LLM load (fake LLM):**
  - 2 000 ticks → 5 349 decisions → 2 211 LLM answers in points mode.
  - Table+prefetch halves the requests but generates 4–6× more answers.
  - Estimate: 4–6 h per 20 000-tick run at ~1 s/answer. This is the main
    cost risk.

## Decisions waiting on the owner

1. Review the draft founder genes (`prototype/data/founder_pool_v1.json`, H1).
2. Choose the decision model and the mutator model (local or cloud), after
   the Ollama probe.
3. Decide whether the G2 threshold (MI_G ≥ 0.25) stays, given the rule-based
   reference scores 0.23.
4. Set the E4 budget: run length, decision period, seeds.
5. Provide the terrain & foliage lab details (06).
6. Give the details of your own git server (below).

## Next steps (on a machine with Ollama or an Ollama Cloud key)

```bash
cd prototype && python -m venv .venv && source .venv/bin/activate
pip install -e ".[dev]" && pytest -q
python -m experiments.e0_probe_ollama --teacher <brain-model> --mutator <small-model>
python -m experiments.teacher_gate --modes points,logprobs --workers 4
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
