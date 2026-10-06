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
