# Spike status — handoff file

> Update before every /compact or /clear. Keep ≤ 150 lines; move old detail to
> `notes/archive.md`. Plan: `../Docs/redesign/08-phase0-spike-plan.md`.

## Position
Session: S1.3 done locally, S4.3 gate run · Last updated: 2026-10-08 (stamina + speed + carcasses; mutation v3:
small edits of the rule; word list and word-change limit removed on request, measured)

## Next action
Mutation v3 2026-10-08: 9 instructions, redraws; word list + change limit removed (owner). Owner asked for other
ways to avoid random words (proposed in chat). Not yet run: an LLM run with mutation v3. WAIT for owner
(../Docs/prompt-genome/09 §4): #10 keep the values (predators at their cap with both brains) or let food limit
them; cost ≈ 12 calls per tick (5 000 ticks ≈ 6.5 h); G2 handling
(all-zero answers → neutral answer? cleaner control sentences? prompt iteration 1/3?); founder pools H1
(prey v2 + predator v2). Then longer LLM runs with both species + `gene_timeline --species prey|predator`. Rerun
`python -m experiments.teacher_gate --modes points --n-obs 12 --model gemma4:12b` (teacher_v5 + obs v2: new
answers, ≈ 420 calls), then E1 on the LLM brain in the background:
`nohup python -m experiments.e1_sensitivity --backend llm --tag llm_points > logs/e1_llm.log 2>&1 &`
(≈ 5 500 decisions, ≈ 50 min). Then several-seed LLM runs on the Lab 1 world (S6 prep).

## Progress
- [~] S1 scaffold ✔, Ollama probe ✔ (gemma4 26b, 12b), founder pool drafted ✔ · H1 founder pool approved ☐
- [x] S2 world, sim, rule-based backend · Lab 1 flat world tuned on small + full (2026-10-01)
- [~] S3 metrics ✔, obs set ✔, E1 script ✔ (`--backend llm`) · E1 on LLM ☐ · E2 (s/decision) ~ (≈ 0.5 s/call, 4 decisions/s in a run) · H2 ☐
- [~] S4 all scripts ✔ (offline-tested) · gate run ✔ (G1 ✔, G2 ✘) · LLM mutants / dataset / labels: parked · H3 ☐
- [—] S5 distillation: PARKED (rev. 2026-09-30); G1–G3 measured on the LLM brain instead
- [ ] S6 evolution matrix · H4 preregistration approved
- [ ] S7 report, go/no-go · H5 decision

## Environment
Windows 11 Pro, Python 3.13.5 (.venv), RTX 5080 16 GB, Ollama 0.35.1 (0.32 until 2026-10-02). Offline suite 76 passed.
Edit scripts: write files with write_bytes (Path.write_text writes CRLF here; most files are LF) and never put
backslashes in Bash heredocs (they lose one level): use the Write tool for scripts.
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
| 2026-10-07 | **Genome = 5 genes, one per action** (eat, flee, follow, rest, mate): risk, social, place (temperament) and the attack and wander genes AND actions removed; no fights between animals; an action with nothing in sight still wanders (eat = search); founder_pool_v2 (same texts); prompts/teacher_v2.md; evolution.max_words | owner: "simplify the genes to a minimum"; predators become genetic animals next |
| 2026-10-07 | **Predators = genetic animals** (species.py: genes hunt, rest, mate; loci `predator.<action>`; same LLM brain with prompts/predator_v1.md; energy, breeding, mutation; floor 3, cap 15 / small 10). hunt = step toward prey, strike when adjacent: kill_p 0.1, kill_gain 60, then digest 50 ticks (no decision). **Vision 20 + distance bands [1, 4, 10] for both**, text in cells; same speed (≤ 1 cell/tick); prompts/teacher_v3.md; data/observations_v2.jsonl; rule_based predators seek far partners when well fed | owner: "gene-based actor with an LLM exactly like the prey", limited actions, longer vision + some distance observation, same speed. Tuned with rule_based (4 seeds, 8 000 ticks): kill_p 0.2 or cheaper predators push the prey to the floor |
| 2026-10-08 | **Mutation v3**: prompts/mutate_v3.txt (9 small edits of the rule); redraw up to mutation_tries 5; still blind. The guards max_changed_words 3 + world vocabulary (493 words) were removed the same day | owner: random but small, meaningful most of the time (options 2 + 3), then "remove the word list entirely and the word change limit". "Something else close to its subject" moved genes to other slots (dropped) |
| 2026-10-07 | **Stamina + speed + carcasses**: 1 stamina per cell (prey 60, predators 30), +2 per still tick at 0.3 energy until full; hunt/flee run at `speed` (predators 2, prey 1), other moves walk 1; a kill leaves a carcass, 1 portion (30 energy, digest 25) for each of up to 2 other predators, rots after 100 ticks; obs: stamina (both), carcass (predators); teacher_v5 / predator_v3; keyword brain rests when out of breath, "tired" = stamina low | owner request. Keyword sweeps (3 seeds): portions 20 or kill_p 0.07 + portions 20 → predators below cap but starving in waves, prey at the floor up to 10 %; kill_p 0.07, digest 80, regen 1.5 → predators at cap; first values kept |
| 2026-10-01 | **Lab 1 world = flat**: water/mountain fractions 0, food uniformly random, food_regrow_p 0.0007; old noise terrain kept as configs/worlds/terrain_preview.yaml (food 0.001), `smoke_run --world terrain_preview` | Owner: the evolution lab becomes Lab 1; the terrain and foliage labs come later and change the world. Flat at 0.001 sat at the cap 25-68 % of the time. At 0.0007 (rule_based, 5k ticks, 3 seeds): small mean pop 25-28 (cap 40, never reached), deaths predator ≈ 230 / starvation ≈ 185, lifespan ≈ 290, 22-25 generations; full mean pop 47-49 (cap 60); random brain collapses to the floor (≈ 250 immigrants) |

## Key numbers
- Sim speed (small, rule_based): 5 000 ticks ≈ 12 s CPU; decision memo hit rate ≈ 0.6 (Lab 1 world).
- Mutation (2026-10-08, 36 founders x 8 seeds + 36 lineages x 30, T 1.2, gemma4 judge), old / v3 + checks / v3 alone:
  usable after 1/10/30 mutations 61/8/0 %, 92/67/44 %, 89/56/33 %; world word after 30: 22, 83, 33 %; words changed
  2.7, 1.8, 3.1. v3 alone drifts into office/game language. results/mutation_test_{old_rules,v3_checks,v3}.md.
- LLM brain, 12 h (2026-10-05/06, seed 1234 continues llm_60min): 57 061 ticks, 79 667 calls, 0 failures, 812
  mutations, peak generation 114. Rescued line thrived (29 animals, t 6-20k), then died out (t 27 337); 874
  newcomers in 30k ticks, no second rescue. Predation trap: kills ≈ constant → 1.9 vs 3.8 per 1 000
  animal-ticks at 29 vs 11 animals. Mutants 75 % of genes at t 24k, world words 97 → 78 %, judged usable
  82 → 54 %. Sweeps = drift (gene dropping: 16 mutants to 50 % vs 18 (13-24) by inheritance alone);
  "Never fight." not confirmed after t 5 269. Salad mate gene: P(mate) −7.7 points (gene_swap).
  results/llm_long_timeline.md/.html, Docs/prompt-genome/11.
- Earlier 2026-10-07 predator checks (LLM small 48 x 48 and 64 x 64, 2 000 ticks each): notes/archive.md, 06 §5.12-13.
- All 4 options (9aaac2d): one partner's mate is enough, predators follow (4 genes), full world 96 x 96 (prey 68/cap 135,
  predators 14/cap 34), breeding line in teacher_v4 / predator_v2; small predator cap 6. rule_based, no mutation, 5 000
  ticks, 3 seeds: full prey 74-93 (never at the floor), predators 15-21, 200-266 births, 18-19 gens; small prey 22-25,
  predators 5.3-5.8. Probe (8 founder predators, well fed): P(mate) with a ready partner adjacent 0.85 (0.60 with
  the old prompt), 2-4 cells 0.66 (0.34), 11-20 cells 0.20 (0.13). LLM 96 x 96 (seed 1234, results/runs/check_all4_llm):
  time limit 3 h at tick 1 673, 26 604 calls, 0 failures; predators 29 births, no newcomers, gen 4, 6-14 alive, 35
  starved, mate 28 %; prey 68 → 44 (t 100) → 135 = cap from t 1 000; 823 births, 584 starved, 172 killed; flee 4 %.
- Stamina (bb61a30), rule_based, no mutation, 5 000 ticks, 3 seeds: full prey 81-94 (floor 0-5 %), predators 28-34 (at
  cap 66-97 %), 126-152 births, 995-1 206 kills, 1 496-1 806 portions, few starve; small prey 28-31, predators 5.6-5.8.
  Memo prey 52-67 % (61-81 before). Random brain: predators at the floor. LLM probe (8 founders): predator hunt 0.82
  rested vs 0.25 out of breath (rest 0.69); carcass 2-4 cells, no prey: hunt 0.44 vs 0.09; prey out of breath, no
  predator: rest 0.79 vs 0.10. Prompts 1 527 / 1 633 chars (1 259 / 1 207 before). LLM 96 x 96 (seed 1234,
  results/runs/check_stamina_llm): 2 000 ticks in 2 h 37 min, 23 608 calls, 0 failures; predators 14 → 34 = cap by
  t 800, 131 births, no newcomers, gen 7, 110 starved; prey 38-83, no newcomers, 374 killed, 201 starved, flee 10 %;
  memo prey 35 %, predators 47 %.
- E1 rule_based reference (small, 48 obs, 10-gene genome): MI_G founders 0.23, MI_G random 0.00,
  MI_O 0.83, directed sign acc 1.00, ΔP 0.48, gibberish→neutral 0.00.
  Note for H2: even the "ideal" keyword interpreter scores MI_G 0.23 < G2 threshold 0.25
  → consider whether G2's MI_G threshold is too strict (decide before S6).
  Locality ratio is 0 for rule_based because many word edits don't touch its keywords.

## Preregistration (frozen at H4)

## Background jobs
| Job | Started | Log | Progress file | State |
|---|---|---|---|---|
| LLM check 96 x 96, 2 000 ticks, seed 1234 (`--out results/runs/check_all4_llm --minutes 180`) | 2026-10-07 14:23 | logs/check_all4_llm.log | logs/run_check_all4_llm.progress.json | done 17:23 (time limit, tick 1 673) |
| LLM check stamina 96 x 96, 2 000 ticks, seed 1234 (`--out results/runs/check_stamina_llm --minutes 180`) | 2026-10-07 18:53 | logs/check_stamina_llm.log | logs/run_check_stamina_llm.progress.json | done 21:31 (2 000 ticks) |

## Open issues
- Own git server mirror: waiting for the URL + auth from the owner (see
  ../Docs/redesign/09-progress-log.md › Mirroring).
- Founder pools (prey v2, predator v2) are drafts by Claude — need owner review (H1).
- Since the stamina change LLM predators fill their cap (34) in the 96 x 96 world and the prey stay at 38-83 without
  newcomers; before it predators stayed at 6-14 while the prey filled their cap (09 §4 #10).
- LLM cost dominates E4: measured ≈ 4 decisions/s (gemma4:12b, RTX 5080) → 5 000 ticks ≈ 1 h
  (small). Decide run length / D / seeds before S6.
- Logprobs: Ollama 0.32 returns them, but unusable with gemma4 (first token "f" = flee/follow,
  saturated top-1) → points mode.
- G2 fails for gemma4 12b/26b; all-zero answers on random-text genomes become uniform (see Gate
  results).
- Blind mutation leaves the animal's world after 10-15 mutations without selection (mutation test);
  with selection too: nonsense drifts to fixation under both brains (Docs/prompt-genome/10, 11).
- Lab 1 small world + LLM-read founders: viable only above ≈ 20 animals (predation trap); the floor
  of 10 + newcomers hides extinction. Owner levers: world/cap, predators, p_mut / v3 prompts.
- (parked) Laya answer schema unknown; `extract_probs` falls back to choice+confidence.
