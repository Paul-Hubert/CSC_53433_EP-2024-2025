# Spike status — handoff file

> Update before every /compact or /clear. Keep ≤ 150 lines; move old detail to
> `notes/archive.md`. Plan: `../Docs/redesign/08-phase0-spike-plan.md`.

## Position
Session: S1 (offline parts pre-built in a cloud session) · Last updated: 2026-09-30 (docs: 09-progress-log.md)

## Next action
S1.3 — on the machine with Ollama (or with an Ollama Cloud key):
`pip install -e ".[dev]" && pytest -q`, then
`python -m experiments.e0_probe_ollama --teacher <brain-model> --mutator <small-model>`.
Read results/e0_ollama.md: pick policy.model + ollama.mutator_model, and policy.mode
(`logprobs` if "logprobs_mode_usable": true, else `points`). Fill "API facts — Ollama",
commit. Then `pytest -m ollama`, the decision-model gate (teacher_gate) and E1 with
`--backend llm` (plan revision box). S1.2 (Laya probe) is optional now.

## Revision 2026-09-30 — LLM brain, Laya parked
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
- Measured with a fake near-random LLM (small, 2 000 ticks): 5 349 decisions →
  2 211 LLM queries with points (0.41/decision). Table+prefetch k=8: 1 185 requests
  but 9 342 generated answers (4×) → only for request-limited cloud, not for speed.
Parked (kept, not deleted): laya_backend.py, e0_probe_laya.py, e0_budget.py,
make_dataset.py, label_teacher.py, S5 distillation.

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

## Progress
- [~] S1 scaffold ✔, probes ☐, founder pool drafted ✔ · H1 founder pool approved ☐
- [x] S2 world, sim, rule-based backend (provisional tuning; re-check on small+full)
- [~] S3 metrics ✔, obs set ✔, E1 script ✔ (`--backend llm`) · E1 on LLM ☐ · E2 (s/decision) ☐ · H2 ☐
- [~] S4 all scripts ✔ (offline-tested) · run gate ☐, LLM mutants ☐, dataset ☐, labels ☐ · H3 ☐
- [—] S5 distillation: PARKED (rev. 2026-09-30); G1–G3 measured on the LLM brain instead
- [ ] S6 evolution matrix · H4 preregistration approved
- [ ] S7 report, go/no-go · H5 decision

## Environment
(S1: OS, Python, CPU, GPU + VRAM, laya version, torch version, Ollama version)

## API facts — Laya
(S1.2: return schema, per-option probabilities?, criteria effect, head_max_len,
overflow behaviour, batch API, determinism, latency)

## API facts — Ollama
(S1.3: models + digests, JSON-schema output, seed determinism, logprobs,
tokens/s, embedding model)

## FT facts
(S5.1)

## Decisions
| Date | Decision | Why |
|---|---|---|
| 2026-09-28 | Provisional world tuning: food_regrow_p 0.001, cost_base 0.7, kill_p 0.3, predators 3 (base) / 2 (small) | rule_based 5k ticks small: pop ≈ 28 (< cap 40, food-limited), deaths split starvation 294 / predator 254, lifespan ≈ 300, 24 generations; random policy collapses (needs immigrants) → behaviour matters |
| 2026-09-28 | Invalid action → wander (logged) | plan §A6 |

## Key numbers
- Sim speed (small, rule_based): 5 000 ticks ≈ 12 s CPU; decision memo hit rate ≈ 0.75.
- E1 rule_based reference (small, 48 obs): MI_G founders 0.23, MI_G random 0.00,
  MI_O 0.83, directed sign acc 1.00, ΔP 0.48, gibberish→neutral 0.00.
  Note for H2: even the "ideal" keyword interpreter scores MI_G 0.23 < G2 threshold 0.25
  → consider whether G2's MI_G threshold is too strict (decide before S6).
  Locality ratio is 0 for rule_based because many word edits don't touch its keywords.

## Preregistration (frozen at H4)

## Background jobs
| Job | Started | Log | Progress file | State |
|---|---|---|---|---|

## S4 run order (local, once S1.3 picked the models)
1. `python -m experiments.teacher_gate --workers 4` → results/teacher_gate.md (H3).
2. `python -m experiments.make_mutants --mutator <small-model>` (LLM rewrites + OOD).
3. `python -m experiments.make_dataset --profile small` (re-run after step 2!).
4. `python -m experiments.label_teacher --limit 50` then `--check` (sanity), then
   `nohup python -m experiments.label_teacher --workers 4 > logs/label_teacher.log 2>&1 &`.
Offline dataset check (word mutants only): 3 000 rows; train 2 400 / val 300 / test 300;
contrast 50 %, regular 40 %, random-text 10 %; 0 leaks; every val/test row has ≥ 1 unseen gene.

## Open issues
- Own git server mirror: waiting for the URL + auth from the owner (see
  ../Docs/redesign/09-progress-log.md › Mirroring).
- Founder pool v1 is a draft by Claude — needs owner review (H1).
- LLM cost dominates E4: estimate 13–22 k LLM answers per 20 k-tick small run
  (4–6 h at ~1 s/answer). Decide run length / D / seeds after measuring s/decision (S1.3).
- Ollama logprobs response format unverified (logprobs mode falls back to points).
- (parked) Laya answer schema unknown; `extract_probs` falls back to choice+confidence.
