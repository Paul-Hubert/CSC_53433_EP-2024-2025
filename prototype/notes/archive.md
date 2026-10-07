# STATUS archive

Older detail moved out of STATUS.md to keep it under 150 lines (moved 2026-10-01).

## Pre-built without models (2026-09-28, cloud session) — verify locally
Done and tested offline (28 tests green, `pytest -q`):
- S1.1 scaffold: config/profiles, rng streams, sqlite cache, progress, status, peek.
- S1.4 allele files: founder_pool_v1 (DRAFT → owner review H1), contrast, control.
- S2 complete: world, perception, obs text V1/V2, 7 executors, random + rule_based
  backends, lockstep sim with decision memo, sexual/asexual, floor/cap, event log,
  determinism test, smoke run. Provisional tuning (see Decisions).
- S3 parts: metrics.py (all §A9), make_obs.py (data/observations_v1.jsonl, small),
  e1_sensitivity.py (any backend). Reference run: results/e1_rule_based.md.
- S4 parts: mutation.py (word ops + LLM rewrite + guards), ollama_client.py (stdlib,
  cached), ollama_policy.py teacher (points / ksample, strict mode), prompts/teacher_v1.md,
  mutate_v1.md, novel_v1.md.
- S4 scripts (tested with a fake Ollama): teacher_gate.py (S4.3), make_mutants.py (S4.4;
  word ops only without --mutator), make_dataset.py (S4.5; allele-split, contrast groups,
  leak-checked), label_teacher.py (S4.6; resumable, --limit, --check, --workers).
  e1_sensitivity.py refactored into build_sets/evaluate/report_lines (shared by the gate).
Written but UNVERIFIED (need real models):
- backends/laya_backend.py — answer schema guessed from the model card (`extract_probs`).
- experiments/e0_probe_laya.py, e0_probe_ollama.py, e0_budget.py (needs `transformers`).
- tests/test_local_models.py (`pytest -m laya`, `pytest -m ollama`).
Not started: finetune_laya, e3_eval, run_matrix, common_garden, analyze, report.

## S4 run order (local, once S1.3 picked the models)
1. `python -m experiments.teacher_gate --workers 4` → results/teacher_gate.md (H3).
2. `python -m experiments.make_mutants --mutator <small-model>` (LLM rewrites + OOD).
3. `python -m experiments.make_dataset --profile small` (re-run after step 2!).
4. `python -m experiments.label_teacher --limit 50` then `--check` (sanity), then
   `nohup python -m experiments.label_teacher --workers 4 > logs/label_teacher.log 2>&1 &`.
Offline dataset check (word mutants only): 3 000 rows; train 2 400 / val 300 / test 300;
contrast 50 %, regular 40 %, random-text 10 %; 0 leaks; every val/test row has ≥ 1 unseen gene.

## Moved from STATUS.md › Key numbers (2026-10-06)
- LLM brain run (Lab 1 world, small, seed 1234, 500 ticks, gemma4:12b, 2026-10-01): 1 722 decisions,
  761 LLM calls (memo 0.56), 425 s, 0 failures. Pop fell to the floor (10): births 10, immigrants 9,
  deaths predator 22 / starvation 11; actions eat .33 wander .20 attack .16 follow .09 mate .09 rest .08
  flee .05. Rule-based, same 500 ticks: pop 17, births 23, immigrants 0, deaths 25 / 5, attack .02.
  results/runs/lab1_llm_500 (not committed).

## Moved from STATUS.md › Revision 2026-09-30 (2026-10-07)
- Measured with a fake near-random LLM (small, 2 000 ticks): 5 349 decisions →
  2 211 LLM queries with points (0.41/decision). Table+prefetch k=8: 1 185 requests
  but 9 342 generated answers (4×) → only for request-limited cloud, not for speed.

## Moved from STATUS.md (2026-10-07): Revision 2026-09-30 — LLM brain, Laya parked
Owner decision: no Laya fine-tuning for now. Decisions AND gene mutation by Ollama LLMs
(local or cloud). See ../Docs/redesign/05-decision-backend.md §0 and the revision box
at the top of plan 08. Implemented (offline-tested with fake Ollama, 38 tests):
- `LLMPolicyBackend` (backends/ollama_policy.py; `TeacherBackend` = alias) with modes
  points (default) / logprobs (1 token per decision; UNVERIFIED Ollama response format,
  auto-fallback to points) / table (+prefetch) / ksample; per-situation cache
  (cache/policy.sqlite) keyed by model+digest+prompt hash; parallel `workers`.
- backends/factory.py: make_backend(name) and make_rewriter(cfg) used by all scripts;
  smoke_run --backend llm also mutates genes with ollama.mutator_model.
- Ollama Cloud: ollama.host https://ollama.com + key in env OLLAMA_API_KEY
  (client_from_config); or cloud tags through the local server after `ollama signin`.
Parked (kept, not deleted): laya_backend.py, e0_probe_laya.py, e0_budget.py,
make_dataset.py, label_teacher.py, S5 distillation.

## Moved from STATUS.md › Key numbers (2026-10-07, before genetic predators)
- 5-gene genome (2026-10-07), rule_based, no mutation, 5 000 ticks, seeds 1234/7/42: small mean pop 25-31 (was 25-28),
  full 50-55 (was 47-49), 24 generations, invalid 11-13 %, eat (incl. search) 54-63 % of decisions. LLM brain
  500 ticks (small, seed 1234): pop 25 at the end (19-26), births 31, no newcomers (10-gene run: floor 10, 9
  newcomers); decisions eat .28 rest .23 mate .23 follow .22 flee .04; 973 calls in 13 min, 0 failures.

## Moved from STATUS.md › Key numbers (2026-10-07, predator checks before the 4 options)
- Genetic predators (2026-10-07), rule_based, no mutation, 5 000 ticks, seeds 1234/7/42: small prey 21-25 (at the
  floor 0-11 %), predators 3.7-4.8 (15-36 births, 8-9 generations); full prey 45-50, predators 3.8-7.2 (6-15
  generations); invalid 2-5 %; memo prey 0.49-0.73 (bands: was 0.77-0.89). LLM (small, seed 1234, 2 000 ticks,
  results/runs/check_predators_llm_2000): ≈ 4 600 calls ≈ 32 min fresh, 0 failures; prey at the floor t 600-1 500
  (35 newcomers), 28 at the end after the founder predators died of old age; predators 3 births, mate 11 %;
  prey flee 9 % (rule_based 21 %). Docs: ../Docs/prompt-genome/03 §3, §13; 05 §6; 06 §5.12.
- Partners seen across the vision (partner_range 20) + 64 x 64 default (08e0c57). rule_based 5 000 ticks: 64 x 64 prey
  35-48, predators 4.4-8.9 (33-69 births, ≤ 13 gens); 96 x 96: 96-118 / 7.7-18.4. LLM 64 x 64, 2 000 ticks (seed 1234,
  results/runs/check_predators_llm_full): ≈ 5 300 calls, 35 min, 0 failures; prey 30 → 12 (t 1 000), floor t 1 100-1 400
  (8 newcomers), 57 at the end; predators 0 births (P(mate) 0.60 with a ready partner adjacent, 0.13 at 11-20 cells).

## Moved from STATUS.md › Key numbers (2026-10-07, stamina change)
- LLM brain, 60 min (2026-10-02, small, seed 1234, gemma4:12b decides + mutates): 5 269 ticks, 19 211
  decisions (5.3/s), 6 784 calls, 0 failures. Pop at the floor (10) for 3 400 ticks (65 immigrants), then
  grew to 25-29; 4 immigrants (t 2 982-3 228) = 84 % of final ancestry; "Never fight." 23/25 at the end
  (≈ 30 % expected from ancestry). Predators 71 % of deaths; flee 4.5 %, attack 9.5 % of decisions.
  results/llm_60min_genes.md, Docs/prompt-genome/10 §7.
