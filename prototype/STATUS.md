# Spike status — handoff file

> Update before every /compact or /clear. Keep ≤ 150 lines; move old detail to
> `notes/archive.md`. Plan: `../Docs/redesign/08-phase0-spike-plan.md`.

## Position
Session: S1.3 done locally, S4.3 gate run · Last updated: 2026-10-08 (stamina + speed + carcasses; mutation v3:
small edits of the rule; word list and word-change limit removed on request, measured; JEV brain + CPU mutator)

## Next action
JEV brain ready (2026-10-08, owner): `docker compose -f docker/compose.yaml up -d jev`, then `--backend jev`;
mutator qwen3.5:0.8b on the CPU (Ollama). To do: batching (owner: later). Running (owner): JEV + egg bank and JEV + cover (Background jobs; crash options f1da89a: results/crash_v3.md, best in the screen = predators.prey_per_predator 3).
Mutation v4 2026-10-08: context line + 7 instructions (mutate_v4.txt), redraws; word list + change limit removed
(owner). Long LLM run with v4 stopped by the owner at t 7 000 (Key numbers); not yet written up in 06/09. To
continue it: the same command (Background jobs) replays from the cache. WAIT for owner
(../Docs/prompt-genome/09 §4): #10 keep the values (predators at their cap with both brains) or let food limit
them; cost ≈ 12 calls per tick (5 000 ticks ≈ 6.5 h); G2 handling
(all-zero answers → neutral answer? cleaner control sentences? prompt iteration 1/3?); founder pools H1
(prey v2 + predator v2). Then longer LLM runs with both species + `gene_timeline --species prey|predator`. Gate rerun
on gemma4:12b with teacher_v5 + obs v2 done 2026-10-08 (`--tag gemma4-12b_v5`, Gate results); then E1 on the LLM brain in the background:
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

## API facts — JEV / vLLM (2026-10-08; docker/README.md)
- prithivMLmods/JEV-9B-GGUF = bare Qwen3.5-9B backbone (no System 1 LoRA, no head): not usable as JEV.
- autotrust/JEV-9B @b63f651c served by vLLM nightly-81198e97 (v0.31.1rc1) in Docker, LoRA `jev-decision`
  (adapter_vllm), FP8 online: weights 10.5 GiB; 14.4 GB of 16 GB used. vLLM's profile came out < 0 at any
  --gpu-memory-utilization (counts Windows' 1.3 GiB + 2 GiB activation peak) → --kv-cache-memory-bytes 768M,
  max-model-len 1024. Start-up ≈ 5-8 min (17 GB over the 9P Windows share, ≈ 2.5 min CUDA graphs).
- No room for a 2nd vLLM → mutator = Ollama qwen3.5:0.8b, num_gpu 0 (0 VRAM, 0.3-0.8 s/call; more random than gemma4:12b: 1.19 attempts, world word after 10 steps 47 % vs 83 %, results/mutation_test_qwen35_0.8b_cpu.md). jev_check: 12 decisions/s sequential, 5/5 contrast pairs in the right direction
  (results/jev_check.json); prefix cache hit 0 % (vLLM caches this model in 528-token blocks).

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

## Gate results (points, --n-obs 12, same 35 genomes x 12 situations; results/teacher_gate_<tag>.md)
| brain | sign acc | ΔP | MI_G founders | MI_G random | gib→neutral / founder→neutral | G1 | G2 |
|---|---|---|---|---|---|---|---|
| gemma4:12b 10-01 (older prompt/obs) | 0.96 | 0.75 | 0.250 | 0.297 | 0.16 / 0.23 | ✔ | ✘ |
| gemma4:12b 10-08 teacher_v5 + obs v2 (`gemma4-12b_v5`) | 0.88 | 0.44 | 0.239 | 0.265 | 0.115 / 0.097 | ✔ | ✘ |
| JEV-9B FP8 10-08 (`--backend jev --tag jev`, 26 s) | 1.00 | 0.38 | 0.143 | 0.014 | 0.010 / 0.056 | ✔ | ✘ MI_G < 0.25 |
E1 JEV (results/e1_jev.md, 5 232 decisions, 6 min): sign acc 1.00, ΔP 0.35, MI_G 0.135 / random 0.011, MI_O 0.98, gib / founder→neutral 0.013 / 0.056, locality 0.10, ρ 0.39 → G1 ✔ G3 ✔ G2 ✘ only on MI_G ≥ 0.25. JEV ignores irrelevant text (gemma's G2 failure; the 2× margin passes) but genes move it less than situations do.
gemma4 mate sign acc 0.70, eat 0.88. 10-01 gemma4:26b row, point totals, all-zero answers: notes/archive.md.
JEV run test (results/runs/jev_test, 96 x 96, seed 1234, 1 000 ticks): 7.6 min = 0.46 s/tick (gemma ≈ 4 s/tick), 6 396 calls, 0 failures, 21 mutations; t 1 000 prey 20 / predators 10 (gemma run: 40 / 32). Ran on top of another session's uncommitted sim/world/perception/actions edits.

## Decisions
| Date | Decision | Why |
|---|---|---|
| 2026-10-08 | Brain option `--backend jev` (JEV-9B System 1, vLLM FP8, jev.host); mutator → mutator.* section, qwen3.5:0.8b on CPU | owner: decisions smart + fast, mutator small (random anyway); FP8 JEV fills the GPU |
| 2026-10-01 | Brain (policy.model) = gemma4:12b; mutator stays gemma4:26b | gate: reads genes as well as 26b (sign acc 0.96 vs 0.99, ΔP 0.75 vs 0.62), ~3× less compute per decision (0.9 vs 2.8 s), fits 100 % in 16 GB VRAM |
| 2026-10-02 | **Mutation = one blind LLM operator**: instruction drawn from prompts/mutate_v2.txt (16 "random change" variants) + the gene, nothing else; temperature 1.2; word operators, styles, founder_reintroduce removed; mutator_model gemma4:12b; every run with mutation needs Ollama (`--no-mutation` otherwise) | owner: "evolution and mutation does not care about state and success, pure random"; review notes/mutation-review.md; test results/mutation_test.md |
| 2026-10-07 | **Genome = 5 genes, one per action** (eat, flee, follow, rest, mate): risk, social, place (temperament) and the attack and wander genes AND actions removed; no fights between animals; an action with nothing in sight still wanders (eat = search); founder_pool_v2 (same texts); prompts/teacher_v2.md; evolution.max_words | owner: "simplify the genes to a minimum"; predators become genetic animals next |
| 2026-10-07 | **Predators = genetic animals** (species.py: genes hunt, rest, mate; loci `predator.<action>`; same LLM brain with prompts/predator_v1.md; energy, breeding, mutation; floor 3, cap 15 / small 10). hunt = step toward prey, strike when adjacent: kill_p 0.1, kill_gain 60, then digest 50 ticks (no decision). **Vision 20 + distance bands [1, 4, 10] for both**, text in cells; same speed (≤ 1 cell/tick); prompts/teacher_v3.md; data/observations_v2.jsonl; rule_based predators seek far partners when well fed | owner: "gene-based actor with an LLM exactly like the prey", limited actions, longer vision + some distance observation, same speed. Tuned with rule_based (4 seeds, 8 000 ticks): kill_p 0.2 or cheaper predators push the prey to the floor |
| 2026-10-08 | **Mutation v4**: evolution.mutation_context "The sentence below is a rule that a wild animal follows." + prompts/mutate_v4.txt (7 small edits; v3's "a little stronger" and "slightly different words" dropped). Before that v3 (9 small edits); redraw up to mutation_tries 5; still blind. The guards max_changed_words 3 + world vocabulary (493 words) were removed the same day | owner: random but small, meaningful most of the time (options 2 + 3), then "remove the word list entirely and the word change limit". "Something else close to its subject" moved genes to other slots (dropped) |
| 2026-10-09 | **Evolution-test world**: 192 x 192 (from 96), prey init 136 / cap 270, predators 28 / 68 (floors 10 / 3 kept), kill_p 0.1 → 0.2, food_regrow_p 0.005 → 0.003, hungry cover on 20 % of cells (cover_food false, seek 6) | owner: "stable but doesn't prove any gene evolution" (proposals 2, 4, 5) |
| 2026-10-09 | **Cap rule migrate** (`cap_rule`, both species): at the cap births go on; at the end of the tick random older animals leave (deaths "migrated") down to the cap; `block` = before. Screen (llm_table, 8 x 20 000, floors 0): 8/8; prey births 5 300 → 21 200, deaths 63 % migrated / 26 % killed / 10 % starved; generations at t 20 000 prey 42 → 114, predators 31 → 106 (results/crash_v6_migrate.md) | owner request |
| 2026-10-08 | **Litters + plentiful food**: a mating makes `litter` [2, 4] babies (uniform), each its own crossover and mutations, child_energy each (cut to what the parents can pay, not below 2, and to the cap); food_regrow_p 0.0007 → 0.005, food_initial_fraction 0.08 → 0.3 | owner request (crashes; egg bank and prey_per_predator rejected) |
| 2026-10-07 | **Stamina + speed + carcasses**: 1 stamina per cell (prey 60, predators 30), +2 per still tick at 0.3 energy until full; hunt/flee run at `speed` (predators 2, prey 1), other moves walk 1; a kill leaves a carcass, 1 portion (30 energy, digest 25) for each of up to 2 other predators, rots after 100 ticks; obs: stamina (both), carcass (predators); teacher_v5 / predator_v3; keyword brain rests when out of breath, "tired" = stamina low | owner request. Keyword sweeps (3 seeds): portions 20 or kill_p 0.07 + portions 20 → predators below cap but starving in waves, prey at the floor up to 10 %; kill_p 0.07, digest 80, regen 1.5 → predators at cap; first values kept |
| 2026-10-01 | **Lab 1 world = flat**: water/mountain fractions 0, food uniformly random, food_regrow_p 0.0007; old noise terrain kept as configs/worlds/terrain_preview.yaml (food 0.001), `smoke_run --world terrain_preview` | Owner: the evolution lab becomes Lab 1; the terrain and foliage labs come later and change the world. Flat at 0.001 sat at the cap 25-68 % of the time. At 0.0007 (rule_based, 5k ticks, 3 seeds): small mean pop 25-28 (cap 40, never reached), deaths predator ≈ 230 / starvation ≈ 185, lifespan ≈ 290, 22-25 generations; full mean pop 47-49 (cap 60); random brain collapses to the floor (≈ 250 immigrants) |

## Key numbers
- Sim speed (small, rule_based): 5 000 ticks ≈ 12 s CPU; decision memo hit rate ≈ 0.6 (Lab 1 world).
- Mutation (2026-10-08, 36 founders x 8 seeds + 36 lineages x 30, T 1.2, gemma4 judge), old / v3 + checks / v3 alone:
  usable after 1/10/30 mutations 61/8/0 %, 92/67/44 %, 89/56/33 %; world word after 30: 22, 83, 33 %; words changed
  2.7, 1.8, 3.1. v3 alone drifts into office/game language. v4 (context + 7): usable 89/50/36 %, world word after 30
  72 %, 2.8 words; wildlife words (village, forest), slot drift, firm words 25 → 8-14 %. results/mutation_test_*.md.
- JEV + litters + food + migrate (2026-10-09, results/runs/jev_migrate, seed 1234): 10 h = 22 817 ticks, 324 369 calls,
  0 failures, 0 newcomers. Prey 120-135 (one dip to 103 / predators 13 at t ≈ 21k), predators 28-34; generations 132 / 119.
  Prey deaths 57 % migrated, 28 % killed, 15 % starved. Prey flee 7 → 18 % of decisions, kills per 1 000 prey-ticks 2.29 → 1.93.
  Flee slot: "...run into them immediately" 97 %, then "...do so immediately" 83 % (JEV reads it as flee). Sweeps at the
  inheritance-alone rate (mutants to 50 %: prey 7 vs 9, predators 6 vs 10). No judge (GPU = JEV). results/jev_migrate*_timeline.md.
- Crashes (2026-10-08, results/crash_v1-v7.md, `crash_sweep`: floors 0, no mutation, brain = the LLM's answers by situation,
  `llm_table`, crashes like the LLM: 0/8). Lasted 8/8: cap 25, prey_per_predator 3 (owner: bad logic), litters + food; cover
  20 % 5/8, ridges 0/8, egg bank (owner: not valid). JEV cover run (old rules): 96 + 3 newcomers by t 9 000.
- Evolution-test world (2026-10-09, crash_v7_big, kill_p 0.2, 8 x 10 000): 8/8; prey ≈ 270, predators ≈ 64 (one dip to 4);
  prey deaths 62 % migrated, 23 % killed, 14 % starved (digest 50 ticks caps the kills); 237 s per run (keyword speed).
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
| JEV + litters + food + migrate 96 x 96, seed 1234, 10 h (`nohup .venv/Scripts/python.exe -m experiments.smoke_run --backend jev --seed 1234 --ticks 50000 --minutes 600 --snapshots 25 --out results/runs/jev_migrate`; 1ed0331) | 2026-10-09 03:58 | logs/jev_migrate.log | logs/run_jev_migrate.progress.json | done 13:58 (time limit, t 22 817; first try failed at t 152: the Ollama app shut down at 03:55:53, now a bare `ollama serve`, logs/ollama_serve.log) |
| JEV + cover 20 % 96 x 96, seed 1234, 20 000 ticks or 10 h (same, `--set world.cover_fraction=0.2 --set world.cover_seek=6 --out results/runs/jev_cover`; f1da89a) | 2026-10-08 | logs/jev_cover.log | logs/run_jev_cover.progress.json | killed ≈ 00:15 (system low on memory) after t 13 000: 171 prey + 3 predator newcomers; data to t 13 000, alleles to t 10 000, no summary |

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
