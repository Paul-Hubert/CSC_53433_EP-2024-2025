# Spike status — handoff file

> Update before every /compact or /clear. Keep ≤ 150 lines; move old detail to
> `notes/archive.md`. Plan: `../Docs/redesign/08-phase0-spike-plan.md`.

## Position
Session: S1.3 done locally, S4.3 gate run · Last updated: 2026-10-02 (mutation = one blind LLM
operator, results/mutation_test.md; docs in ../Docs/prompt-genome/)

## Next action
WAIT for owner decisions (../Docs/prompt-genome/09-status-and-roadmap.md §4): G2 handling
(all-zero answers → neutral answer? cleaner control sentences? prompt iteration 1/3?), founder
pool H1. Then rerun
`python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b` (cached, cheap),
then E1 on the LLM brain in the background:
`nohup python -m experiments.e1_sensitivity --backend llm --tag llm_points > logs/e1_llm.log 2>&1 &`
(≈ 5 500 decisions, ≈ 50 min). Then several-seed LLM runs on the Lab 1 world (S6 prep).

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

## Progress
- [~] S1 scaffold ✔, Ollama probe ✔ (gemma4 26b, 12b), founder pool drafted ✔ · H1 founder pool approved ☐
- [x] S2 world, sim, rule-based backend · Lab 1 flat world tuned on small + full (2026-10-01)
- [~] S3 metrics ✔, obs set ✔, E1 script ✔ (`--backend llm`) · E1 on LLM ☐ · E2 (s/decision) ~ (≈ 0.5 s/call, 4 decisions/s in a run) · H2 ☐
- [~] S4 all scripts ✔ (offline-tested) · gate run ✔ (G1 ✔, G2 ✘) · LLM mutants / dataset / labels: parked · H3 ☐
- [—] S5 distillation: PARKED (rev. 2026-09-30); G1–G3 measured on the LLM brain instead
- [ ] S6 evolution matrix · H4 preregistration approved
- [ ] S7 report, go/no-go · H5 decision

## Environment
Windows 11 Pro, Python 3.13.5 (.venv), RTX 5080 16 GB, Ollama 0.32.0. Offline suite 43 passed on Windows.
Windows: experiments.status used os.kill(pid, 0); signal 0 is CTRL_C_EVENT on Windows, so live jobs
showed as DEAD? → fixed (OpenProcess + GetExitCodeProcess).

## API facts — Laya
(S1.2: return schema, per-option probabilities?, criteria effect, head_max_len,
overflow behaviour, batch API, determinism, latency)

## API facts — Ollama
(2026-10-01, results/e0_ollama.md) gemma4:26b digest 001e5dafc3c7 (25.2B MoE, Q4_K_M, 18 GB, thinking model).
- MUST send `think: false` and a fixed `num_ctx` (ollama.options/think in base.yaml): default ctx 256k
  spills 32 % to CPU and any change of num_ctx between requests reloads the model (60-100 s each).
  With num_ctx 4096 it runs 100 % GPU.
- JSON-schema output OK; seed + temperature 0 → identical 3/3.
- Speed warm: 26 gen tok/s, 210 prompt tok/s, ~290 prompt tokens; points ≈ 2.9 s/decision (3.5 s in gate).
- logprobs: returned (top_logprobs), but first token is "f" (flee/follow ambiguous) → parser returns None;
  and top-1 vs top-2 gap ≈ 18 nats → effectively argmax anyway. Use points mode.
- gemma4:12b (digest 6114515d63c1, 8 GB, 100 % GPU): 90 gen tok/s, 0.9 s compute/decision (26b: 2.8 s).
  Wall time per call varied 1.6-34 s (load_duration) while ML-Agents training + 3 Unity editors shared the GPU.
- Mutator (same model) samples sensible: "Avoid food whenever it is near." No embed model pulled.

## FT facts
(S5.1)

## Gate results 2026-10-01 (points, --n-obs 12, 420 calls each; results/teacher_gate_gemma4-*.md)
| model | sign acc | ΔP | MI_G founders | MI_G random | gib→neutral / founder→neutral | G1 | G2 |
|---|---|---|---|---|---|---|---|
| gemma4:12b | 0.96 | 0.75 | 0.250 | 0.297 | 0.16 / 0.23 | ✔ | ✘ |
| gemma4:26b | 0.99 | 0.62 | 0.244 | 0.314 | 0.20 / 0.13 | ✔ | ✘ |
Both read directed genes well, but irrelevant/shuffled text moves behaviour as much as founder
genes (gate wants founders ≥ 2× random). Control texts contain world words (mountains, river,
bread...). Owner decision needed before prompt iteration (≤ 3 tries) or gate change.
Point totals (60 gate decisions, 12b, 2026-10-01): 44 (73 %) exactly 100, 13 at 56-98, 3 at 0; none above.
Harmless (points_to_probs divides by the total), EXCEPT all-zero answers: 3/17 random-text answers
were all zeros (none for founder/contrast/neutral genomes) and normalise(eps) turns them into a
uniform 1/7 distribution → likely inflates MI_G random (G2). Candidate fix (needs owner OK): treat
all-zero as "no effect" → use the neutral genome's answer for that situation. Speed with a free
GPU: 60 decisions in 29 s (0.48 s/decision). Gate answers before 2026-10-01 were never cached
(empty-KVCache bug, fixed ed485c5).

## Decisions
| Date | Decision | Why |
|---|---|---|
| 2026-09-28 | Provisional world tuning: food_regrow_p 0.001, cost_base 0.7, kill_p 0.3, predators 3 (base) / 2 (small) | rule_based 5k ticks small: pop ≈ 28 (< cap 40, food-limited), deaths split starvation 294 / predator 254, lifespan ≈ 300, 24 generations; random policy collapses (needs immigrants) → behaviour matters |
| 2026-09-28 | Invalid action → wander (logged) | plan §A6 |
| 2026-10-01 | Brain (policy.model) = gemma4:12b; mutator stays gemma4:26b | gate: reads genes as well as 26b (sign acc 0.96 vs 0.99, ΔP 0.75 vs 0.62), ~3× less compute per decision (0.9 vs 2.8 s), fits 100 % in 16 GB VRAM |
| 2026-10-02 | **Mutation = one blind LLM operator**: instruction drawn from prompts/mutate_v2.txt (16 "random change" variants) + the gene, nothing else; temperature 1.2; word operators, styles, founder_reintroduce removed; mutator_model gemma4:12b; every run with mutation needs Ollama (`--no-mutation` otherwise) | owner: "evolution and mutation does not care about state and success, pure random"; review notes/mutation-review.md; test results/mutation_test.md |
| 2026-10-01 | **Lab 1 world = flat**: water/mountain fractions 0, food uniformly random, food_regrow_p 0.0007; old noise terrain kept as configs/worlds/terrain_preview.yaml (food 0.001), `smoke_run --world terrain_preview` | Owner: the evolution lab becomes Lab 1; the terrain and foliage labs come later and change the world. Flat at 0.001 sat at the cap 25-68 % of the time. At 0.0007 (rule_based, 5k ticks, 3 seeds): small mean pop 25-28 (cap 40, never reached), deaths predator ≈ 230 / starvation ≈ 185, lifespan ≈ 290, 22-25 generations; full mean pop 47-49 (cap 60); random brain collapses to the floor (≈ 250 immigrants) |

## Key numbers
- Sim speed (small, rule_based): 5 000 ticks ≈ 12 s CPU; decision memo hit rate ≈ 0.6 (Lab 1 world).
- LLM brain run (Lab 1 world, small, seed 1234, 500 ticks, gemma4:12b, 2026-10-01): 1 722 decisions,
  761 LLM calls (memo 0.56), 425 s, 0 failures. Pop fell to the floor (10): births 10, immigrants 9,
  deaths predator 22 / starvation 11; actions eat .33 wander .20 attack .16 follow .09 mate .09 rest .08
  flee .05. Rule-based, same 500 ticks: pop 17, births 23, immigrants 0, deaths 25 / 5, attack .02.
  results/runs/lab1_llm_500 (not committed).
- Mutation test (2026-10-02, gemma4:12b, 2 000 calls, 223 s, no selection): single mutations 99 % valid,
  7.0-7.3 distinct of 8 per sentence (T 0.9 → 2.0: temperature barely matters; the instruction sets
  the step: 1-2 words vs 6-7 for the 4 "big" ones), +0.1 word, 50-55 % neutral for rule_based.
  Lineages (24 × 30 steps): genes using a world word 83 % after 1, 58 % after 10, 21 % after 15,
  12 % after 30; 4.7 → 6.5-7.8 words; "toaster" in 18/24 lineages. ≈ 270 mutation calls per
  10 000 ticks (rule_based, small).
- E1 rule_based reference (small, 48 obs): MI_G founders 0.23, MI_G random 0.00,
  MI_O 0.83, directed sign acc 1.00, ΔP 0.48, gibberish→neutral 0.00.
  Note for H2: even the "ideal" keyword interpreter scores MI_G 0.23 < G2 threshold 0.25
  → consider whether G2's MI_G threshold is too strict (decide before S6).
  Locality ratio is 0 for rule_based because many word edits don't touch its keywords.

## Preregistration (frozen at H4)

## Background jobs
| Job | Started | Log | Progress file | State |
|---|---|---|---|---|

## Open issues
- Own git server mirror: waiting for the URL + auth from the owner (see
  ../Docs/redesign/09-progress-log.md › Mirroring).
- Founder pool v1 is a draft by Claude — needs owner review (H1).
- LLM cost dominates E4: measured ≈ 4 decisions/s (gemma4:12b, RTX 5080) → 5 000 ticks ≈ 1 h
  (small). Decide run length / D / seeds before S6.
- Logprobs: Ollama 0.32 returns them, but unusable with gemma4 (first token "f" = flee/follow,
  saturated top-1) → points mode.
- G2 fails for gemma4 12b/26b; all-zero answers on random-text genomes become uniform (see Gate
  results).
- Blind mutation leaves the animal's world after 10-15 mutations without selection (mutation test);
  a gene meets ≈ 1.4 mutations per 10 000-tick run, so watch long runs (rule_based: nonsense is
  mostly neutral → drift).
- (parked) Laya answer schema unknown; `extract_probs` falls back to choice+confidence.
