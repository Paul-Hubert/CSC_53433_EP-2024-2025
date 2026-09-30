# 07 — Open questions & roadmap

## Decisions needed from the course owner

1. ~~Model family~~ → **Laya** local decision model; Jev optional
   baseline; pooled **Ollama Cloud** for generation (see [05](05-decision-backend.md)).
   Still open: English vs multilingual checkpoint (context 512 vs 1 024).
   Which Ollama plan (Team with shared credits vs several Pro seats)?
2. **Option A vs B** (LLM per decision vs LLM at birth) — or both, with B as
   the default lab. See [02](02-assessment.md#the-design-decision-i-would-push-hardest-on).
3. **Student hardware** and whether a shared lab inference server is possible.
4. **Scope of the ecology:** single species, or prey + predators (two
   genomes / two founder pools)?
5. **Unity version upgrade** (2021.3 → Unity 6 LTS)?
6. **Time budget:** how many lab sessions for this part, and what is graded?
7. Terrain & foliage details ([06](06-world-terrain-foliage.md)).

## De-risked build order

**Phase 0 — Headless feasibility spike (before any Unity work, ~days)**
→ Full plan and session runbook: [08](08-phase0-spike-plan.md).
- Python script, 2D grid world, food + walls/water, 20–40 agents.
- Candidate backends: rule-based, **Laya zero-shot**, **Laya fine-tuned**
  (teacher labels from Ollama Cloud), optionally one small generative LLM.
- Compare gene placements in Laya's input: option criteria vs question
  instructions vs state.
- Distillation pilot: ~2–5k teacher labels with contrast sets → expert head
  (layaMOE-style) or Kaggle fine-tune → re-run the sensitivity test.
- **Gene-sensitivity test:** fixed set of ~50 observations × varied
  genomes → do action distributions differ? Are small edits → small
  changes? Is the difference larger than between two random-text genomes?
- **Evolution test:** does mean lifespan / offspring rise over generations
  vs a **random-text control** and a **no-mutation control**?
- Measure decisions/second on a laptop CPU.
- *Go/no-go:* if genes don't measurably change behaviour, change backend or
  switch to option B before continuing.

**Phase 1 — Unity core**
- Fixed-step lockstep sim, config asset, seeds, logging.
- Agent split into Perception / Decision / Executor / Metabolism.
- `RuleBasedBackend` + `NeuralNetBackend` (port of existing code).
- Walkability from terrain.

**Phase 2 — Prompt genome**
- Genome with loci, founder pool v1, crossover, non-LLM mutation operators.
- Inference service client (batching, cache); option B then option A.
- LLM mutation operator with logging.

**Phase 3 — Student-facing**
- Genome browser / plots, lineage view.
- Lab handout with graded exercises, e.g.:
  1. Write a new perception sense + its text rendering.
  2. Add a new action and locus (e.g. drink / thirst).
  3. Design a mutation operator and measure its effect on diversity.
  4. Compare NN vs prompt genome vs option A vs B on the same world.
  5. Introduce a predator species; analyse co-evolution of fear genes.
- Headless batch runs, reference results for the founder pool.

## Log of decisions

| Date | Decision | By |
|---|---|---|
| 2026-09-27 | Idea captured; docs created. | course owner / Claude |
| 2026-09-30 | Laya fine-tuning dropped for now. Decisions and gene mutation by Ollama LLMs (local or cloud); Laya/distillation parked as an optional later project. | course owner |
| 2026-09-28 | Phase 0 spike planned: local only (Laya + local Ollama), 7 sessions, preregistered gates G1–G5. | course owner / Claude |
| 2026-09-27 | "J-Laya" = Jev/Laya decision models. Laya (local) chosen as decision backend; Ollama Cloud (pooled) for generative jobs. | course owner |
