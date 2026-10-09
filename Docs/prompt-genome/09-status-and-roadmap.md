# 09 — Status, decisions and roadmap

State on **2026-10-09**, branch `claude/ai-agents-course-redesign-71pc1u`.
The live, step-by-step state of the spike is in `prototype/STATUS.md`, and the
owner-facing log is in `Docs/redesign/09-progress-log.md`.

## Contents

1. [Where things stand](#1-where-things-stand)
2. [Timeline](#2-timeline)
3. [Decisions taken](#3-decisions-taken)
4. [Decisions waiting on the course owner](#4-decisions-waiting-on-the-course-owner)
5. [Roadmap](#5-roadmap)
6. [Parked work](#6-parked-work)

---

## 1. Where things stand

| Part | State |
|---|---|
| World, simulation, logging, seeds | working and tested; **Lab 1 flat world**, since 2026-10-09 192 × 192 with hungry cover on 20 % of cells, slower food (0.0015) and doubled populations; litters of 2–4 and migration at each species' cap keep both species alive without newcomers; reference numbers measured again (2026-10-09, [03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)) |
| Genome | prey: 5 genes, one per action (eat, flee, follow, rest, mate); predators: 4 genes (hunt, follow, rest, mate); both since 2026-10-07; founder pools waiting for review (H1) |
| Predators | genetic animals since 2026-10-07: the same brain, energy, breeding and mutation as the prey; 4 genes (hunt, follow, rest, mate); vision 20 and distance bands for both species; stamina for both: predators run 2 cells per tick when hunting with half the prey's stamina, and a kill leaves a carcass for up to 2 more predators; one partner's `mate` is enough to breed; with the LLM brain they breed in the 96 × 96 world and, since the stamina change, fill their cap; `kill_p` 0.5 since 2026-10-09 |
| Random and rule-based brains | working |
| LLM brain (`--backend llm`, gemma4:12b, points mode) | working end to end; ≈ 4 decisions/s on a 16 GB GPU; not the default for runs since 2026-10-08 |
| JEV brain (`--backend jev`, JEV-9B on vLLM in Docker) | working since 2026-10-08; ≈ 12 decisions/s; a 10-hour run (22 817 ticks) and two 1-hour runs; one CUDA error in 443 178 answers, recovered by Docker ([05 §7](05-decision-backends.md#7-jev-a-distilled-decision-model)) |
| Mutation: one blind small change by the LLM (a context line, 7 instructions, temperature) | working; mutator on the CPU since 2026-10-08 (qwen3.5:0.8b; gemma4:26b makes much smaller, more meaningful edits, [04 §5](04-genome-and-evolution.md#the-mutator-model)); since 2026-10-08 genes keep their meaning longer without selection (usable after 10 mutations: 50 % against 8 %; still using a world word after 30: 72 % against 22 %) (`results/mutation_test_v4.md`, [06 §5.17](06-experiments-and-results.md#517-small-mutations-that-keep-their-meaning)) |
| Metrics, E1 suite, decision-model gate | working; gate run on gemma4 12b and 26b, and on JEV-9B with E1 |
| Persistent answer caches | working (request-cache bug fixed 2026-10-01) |
| Tests | 100 offline tests pass; real-model test passed on gemma4:12b (10-gene genome) |
| Analysis tools | `gene_report` (gene fitness, frequency, mutant lineages) since 2026-10-02; `gene_timeline` (the gene pool over time, gene dropping) and `gene_swap` since 2026-10-06; `--species predator` since 2026-10-07; `mutation_list` (every mutation of a run) since 2026-10-09; `crash_sweep` and `llm_table` (screening population dynamics with the LLM's answers) since 2026-10-08 |
| Evolution matrix (E4), common garden (E5), report | not written |
| Unity version | not started |
| Laya brain and distillation | parked |

| Gate | Status |
|---|---|
| G1 Semantics | ✔ (gemma4 12b and 26b; JEV-9B) |
| G2 Information | ✘: gemma is moved by random text as much as by real genes; JEV ignores random text but genes move it too little (MI_G 0.14 < 0.25) |
| G3 Locality | ✔ on JEV-9B (E1); not measured on gemma |
| G4 Evolution | not measured; the 10-hour JEV run showed sweeps at the drift rate |
| G5 Throughput | gemma ≈ 4, JEV ≈ 12 decisions/s vs ≥ 50 targeted → fails at current settings |

## 2. Timeline

| Date | What happened |
|---|---|
| 2026-09-27 | Vision captured; current lab audited; assessment, architecture and genome design written (`Docs/redesign/00–04`). Laya chosen as the decision model (05). |
| 2026-09-28 | Phase 0 spike planned: preregistered gates G1–G5, 7-session runbook (08). Offline core built and tested: world, simulation, rule-based brain, metrics, mutation, probes, E1. Teacher/distillation pipeline written. |
| 2026-09-30 | Decision: no Laya fine-tuning; Ollama LLMs decide and mutate. Laya parked. |
| 2026-10-01 | First local tests (Windows 11, RTX 5080, Ollama 0.32). gemma4:26b and 12b probed. Fixes: `think: false` and a fixed `num_ctx` (model reloads), Windows liveness check in `status`, request cache never written, `smoke_run` LLM mutation with every brain, ambiguous ASCII map symbols. Decision-model gate on both models (G1 ✔, G2 ✘). Brain switched to gemma4:12b. Point-total analysis (all-zero answers on random text). **Lab 1 = flat world with random food**, food regrowth retuned; noise terrain moved to `configs/worlds/terrain_preview.yaml`. First 500-tick run with the LLM brain. This documentation. |
| 2026-10-02 | Mutation reviewed (`prototype/notes/mutation-review.md`), then replaced by one blind operator: the LLM gets a random-change instruction and the gene, nothing else. Mutation test (`results/mutation_test.md`). Three 50 000-tick runs with predators and the new `gene_report` ([10](10-natural-selection-runs.md)): no gene clearly best, careless temperaments clearly selected against. First hour-long LLM-brain run (5 269 ticks): the population escaped the floor after newcomers brought a workable gene set, and "Never fight." spread to 23 of 25 animals ([10 §7](10-natural-selection-runs.md#7-one-hour-with-the-llm-brain)). |
| 2026-10-06 | `smoke_run` made safe for long runs (stop file, clean stops, resume by replay, failed brain calls never cached). One 12-hour LLM-brain run: the rescued population thrived about 60 generations, then died out; nonsense genes swept as often as random inheritance predicts; a predation trap below about 20 animals. New analysis: `gene_timeline` (gene dropping), `gene_swap` ([11](11-gene-development.md)). |
| 2026-10-07 | Owner request: the genome cut to 5 genes, one per action (eat, flee, follow, rest, mate). The temperament genes (risk, social, place) and the attack and wander actions are gone, so animals no longer fight; an action with nothing to act on in sight makes the animal search. New prompt `teacher_v2.md`, founder pool v2, one length guard (`evolution.max_words`). Lab 1 reference numbers measured again: 25–31 animals (small), 50–55 (full) ([03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world)). `gene_report` and `gene_timeline` still read the older 10-gene runs. Then, on the owner's request, **predators became genetic animals** like the prey: 3 genes (hunt, rest, mate), the same LLM brain with its own prompt (`predator_v1.md`), energy, breeding, mutation, a floor and a cap. A hunting predator next to its prey kills with probability 0.1, then digests for 50 ticks. Both species see 20 cells, with distances in bands (1, 2–4, 5–10, 11–20 cells), and move one cell per tick. Tuned with the keyword brain for coexistence: small world 21–25 prey and 4–5 predators. First LLM-brain run with both species (gemma4:12b, small world, seed 1234, 2 000 ticks, about 4 600 calls and 32 minutes from an empty cache, 0 failures): the prey sat at their floor from tick 600 to 1 500 and needed 35 newcomers, then grew to 28 once the founder predators died of old age; predators had 3 births and 4 newcomers ([06 §5.12](06-experiments-and-results.md#512-first-llm-brain-run-with-genetic-predators)). Then, on the owner's request, a **bigger world and partners seen farther**: readiness to mate is seen across the whole vision (`perception.partner_range` 20, was 4) and `smoke_run` runs the 64 × 64 world. With the keyword brain predators now breed in every world size (64 × 64: 4–9 predators, up to 13 generations). With the LLM brain (2 000 ticks): the prey fell to their floor (8 newcomers), then grew to 57; the predators never bred, because the brain chooses `mate` mostly when the ready partner is already next to them ([06 §5.13](06-experiments-and-results.md#513-llm-brain-in-the-64--64-world-partners-seen-across-the-vision)). Then **all four options for both species**: one partner's `mate` is enough to breed, predators can `follow` (4 genes), the full world is 96 × 96, and both prompts state the breeding rule. Keyword brain: 15–21 predators, 18–19 generations in 5 000 ticks. LLM brain (3 hours, 1 673 ticks): predators bred 29 times without newcomers, but stayed at 6–14 and often starved, while the prey filled the world up to their cap ([06 §5.14](06-experiments-and-results.md#514-llm-brain-in-the-96--96-world-with-all-four-changes)). Then **stamina, speed and carcasses** (owner request): moving costs stamina, and standing still brings it back at a cost in energy; predators run 2 cells per tick when hunting, with half the prey's stamina; a kill leaves a carcass that up to two more predators eat from. Keyword brain: the predators fill their cap (28–34 in the full world) and rarely starve; the prey stay at 81–94 ([06 §5.15](06-experiments-and-results.md#515-stamina-speed-and-carcasses)). LLM brain (2 000 ticks, 2 h 37 min, 23 608 calls): the predators filled their cap (34) by tick 800, with 131 births and no newcomers; the prey stayed at 38–83 without newcomers, limited by predation (374 killed, 201 starved) ([06 §5.16](06-experiments-and-results.md#516-llm-brain-with-stamina-and-carcasses)). |
| 2026-10-08 | Mutation reworked on the owner's request: small edits of the rule (`mutate_v3.txt`, 9 instructions), a rejected answer drawn again up to 5 times. Two checks (at most 3 words changed, only words of the animal's world) were tried and removed the same day. Measured without selection: genes still giving a usable rule after 10 mutations 56 % (8 % before, 67 % with the checks), after 30 33 % (0 %, 44 %); without the checks, genes drift into office and game language after 20–30 mutations. Then a context line before every mutation prompt ("The sentence below is a rule that a wild animal follows.") and 7 instructions (`mutate_v4.txt`, "a little stronger" and "say it in slightly different words" dropped): genes stay about animals (72 % still use a world word after 30 mutations) ([06 §5.17](06-experiments-and-results.md#517-small-mutations-that-keep-their-meaning)). |
| 2026-10-08 (cont.) | First run with mutation v4 (gemma4:12b, 96 × 96): boom-and-bust cycles of about 3 500 ticks emptied both species twice, and founder newcomers restarted their evolution each time; stopped by the owner at tick 7 000 ([06 §5.18](06-experiments-and-results.md#518-the-long-llm-run-with-mutation-v4)). Owner: stopping the crashes is the hard part, and founder newcomers destroy the evolution model. Screening tool `crash_sweep` with the LLM's own answers by situation (`llm_table`); mechanisms tried: lower predator cap, terrain, cover, predator interference, predators breeding by prey count (rejected: bad logic), ridges, egg bank (rejected: not a valid idea) ([06 §5.19](06-experiments-and-results.md#519-boom-and-bust-how-to-stop-the-crashes)). Another session added the **JEV-9B brain** on vLLM and moved the mutator to qwen3.5:0.8b on the CPU ([05 §7](05-decision-backends.md#7-jev-a-distilled-decision-model)). JEV runs with the egg bank (stopped) and with cover (killed for lack of memory): crashes went on. |
| 2026-10-09 | **Litters of 2–4** (each baby its own genes and mutations) and plentiful food stopped the crashes in the screen; then **migration at the cap** instead of blocked births. A 10-hour JEV run: stable, 132 generations, no newcomer, but sweeps at the drift rate ([06 §5.20](06-experiments-and-results.md#520-jev-runs-egg-bank-cover-litters-and-migration)). Owner: "stable but doesn't prove any gene evolution": the **evolution-test world** (192 × 192, doubled populations, `kill_p` 0.5, food 0.0015, hungry cover on 20 %); one shared cap of 500 tried and reverted. 1-hour JEV runs with the qwen3.5:0.8b and gemma4:26b CPU mutators ([06 §5.21–5.22](06-experiments-and-results.md#521-an-evolution-test-world)). The Ollama app's updater stopped the mutator server twice and the JEV engine hit one CUDA error. This documentation updated. |

## 3. Decisions taken

| Date | Decision | Why |
|---|---|---|
| 2026-09-27 | Genes are sentences in fixed loci; founder pool frozen and shared by all runs | crossover between matching slots; comparable runs (02, 04) |
| 2026-09-28 | Provisional world tuning (predators, energy costs) | food-limited population; deaths split between predators and starvation |
| 2026-09-28 | Invalid action → wander, counted | plan A6 |
| 2026-09-30 | Ollama LLMs decide and mutate; Laya parked | owner decision |
| 2026-10-01 | Send `think: false` and `num_ctx: 4096` with every request | without them gemma4 spilled to CPU and reloaded on every call (60–100 s) |
| 2026-10-01 | Brain = gemma4:12b (mutator stays gemma4:26b) | reads genes as well as 26b, ≈ 3× faster, fits fully in 16 GB |
| 2026-10-01 | **Lab 1 world is flat, food uniformly random, regrowth 0.0007** | owner: evolution is Lab 1; terrain and foliage labs come later. At 0.001 the flat world sat at the cap 25–68 % of the time; 0.0007 keeps it food-limited |
| 2026-10-01 | `smoke_run` uses LLM mutation only with the LLM brain (superseded 2026-10-02) | matches the script's documentation; rule-based runs need no model |
| 2026-10-02 | **Mutation = one blind random change by the LLM**: an instruction drawn from 16 "random change" variants + the gene, nothing else; the word operators, rewrite styles and founder reintroduction are gone; mutator = gemma4:12b; every run with mutation needs the model (`--no-mutation` otherwise) | owner: mutation doesn't care about state or success, pure random; review in `prototype/notes/mutation-review.md` |
| 2026-10-07 | **Genome = 5 genes, one per action** (eat, flee, follow, rest, mate); the attack and wander actions removed (no fights between animals); temperament (risk, social, place) removed | owner request: genes kept to a minimum before predators become genetic animals in the same way |
| 2026-10-07 | **Predators are genetic animals like the prey**: genes hunt, rest, mate (loci `predator.<action>`), the same LLM brain with `prompts/predator_v1.md`, energy, breeding, mutation, floor and cap; the scripted chase and `predators.count` removed | owner request: "a gene-based actor with an LLM exactly like the prey", with a limited set of actions |
| 2026-10-07 | **Vision 20 cells for both species, distances in bands** (adjacent 1, close 2–4, medium 5–10, far 11–20, none), written as cell ranges in the situation text; **same speed** (one cell per tick); the prey prompt `teacher_v3.md` and the predator prompt state the equal speed | owner request: longer vision and some distance observation for both, the same speed for now |
| 2026-10-07 | **Partners seen across the vision** (`perception.partner_range` 20, was 4), both species; the keyword brain goes to a ready partner at any distance (one rule for both species, replacing the predators' far-partner workaround). **`smoke_run` runs the 64 × 64 world by default**; small (48 × 48) stays for tests and quick checks | owner: "bigger world, further distance for seeing partner"; in the small world the LLM-read prey didn't hold and predators rarely met |
| 2026-10-07 | **All four options, both species**: an animal that chose `mate` breeds with a ready partner next to it, whatever the partner chose; predators get `follow` (genes hunt, follow, rest, mate; `predator_founder_pool_v2.json`); the full world (`smoke_run`'s default) is 96 × 96 with populations scaled by area; `teacher_v4.md` and `predator_v2.md` state the breeding rule; the small world's predator cap is 6 | owner: "all 4 options for both animals, including follow" |
| 2026-10-07 | **Stamina and speed for both species; carcasses**: each cell moved costs one point of stamina (prey 60, predators 30) and `cost_move` energy; a tick without moving brings back 2 points and costs 0.3 energy until stamina is full; `hunt` and `flee` run at the species' speed (predators 2 cells per tick, prey 1), every other move walks one cell; a kill leaves a carcass with one portion (30 energy, 25 ticks of digestion) for each of up to 2 other predators, rotting after 100 ticks; both species sense their stamina, predators the nearest carcass they may eat from; prompts `teacher_v5.md` and `predator_v3.md`; the keyword brain rests when out of breath and reads *tired* as out of breath | owner request: stamina drains when moving and costs energy to regain; predators faster but with lower stamina than prey; a kill leaves part of the carcass for up to 2 more predators. Values checked with the keyword brain ([03 §3](03-world-and-simulation.md#3-predators)) |
| 2026-10-08 | **Mutation: small edits of the rule**: 9 instructions in `prompts/mutate_v3.txt` (a little stronger or weaker, change when it applies, add or remove a condition, near/far/much, the opposite, a related word, other words); a rejected answer is drawn again, up to 5 attempts; still blind (the mutator sees only the instruction and the gene). A word-change limit (3 words) and a world word list were added the same morning and removed later that day | owner: "managing mutations in a random but small way, staying in the realm of meaning most of the time" (options 2 + 3 of the proposal), then "remove the word list entirely and the word change limit", then "1 global mutation contextual prompt about it being animals and 5 drop": `mutation_context` and `mutate_v4.txt` (7 instructions). Without selection, genes still giving a usable rule after 10 / 30 mutations: 50 / 36 % (8 / 0 % before); still using a world word after 30: 72 % (22 %) |
| 2026-10-08 | **JEV-9B decides** (`--backend jev`, vLLM in Docker, FP8); the mutator gets its own `mutator.*` section and runs qwen3.5:0.8b on the CPU | owner: decisions smart and fast, the mutator small (mutations are random anyway); JEV fills the GPU |
| 2026-10-08 | Against crashes, **not adopted**: predators breeding only with ≥ q prey per predator (lasted 24/24 in the screen) and the egg bank (lineages survive crashes) | owner: the first is "the worst logic" (nobody could count the prey), the second "not a valid idea"; both stay in the code, off |
| 2026-10-09 | **Litters of 2–4** for both species, each baby with its own crossover and mutations, each costing the parents `child_energy` | owner request |
| 2026-10-09 | **Cap rule `migrate`**: births go on at the cap; random older animals leave down to it (deaths `migrated`). A shared cap of 500 for both species was tried and reverted (crashes, 2/8): one cap per species | owner request, then "go back to caps" |
| 2026-10-09 | **Evolution-test world**: 192 × 192, prey 136 / cap 270, predators 28 / cap 68, `kill_p` 0.5, food regrowth 0.0015 and 10 % at the start, hungry cover on 20 % of cells (no food in cover, fleeing prey run into it) | owner: the stable setup "doesn't prove any gene evolution"; make deaths depend on behaviour, more room against drift |
| 2026-10-07 | Predator rules and tuning: strike when next to the prey after a step; `kill_p` 0.1, `digest_ticks` 50, `kill_gain` 60, the prey's metabolism; floor 3 in both worlds; the keyword brain's predators seek a partner they see farther away when well fed | coexistence with the keyword brain over 4 seeds ([03 §3](03-world-and-simulation.md#3-predators)); without the partner search predators rarely met |

## 4. Decisions waiting on the course owner

| # | Decision | Context |
|---|---|---|
| 1 | Approve or edit the founder pools (H1) | `prototype/data/founder_pool_v2.json` and `predator_founder_pool_v1.json`, [04 §3](04-genome-and-evolution.md#3-the-founder-pool) |
| 2 | How to handle G2: all-zero answers → neutral answer? rewrite the control sentences? prompt iterations? keep the MI_G ≥ 0.25 threshold? | [06 §6](06-experiments-and-results.md#6-known-issues-and-open-questions) |
| 3 | Lab 1 platform (Python prototype or Unity) and format (sessions, deliverables, grading) | [02 §8](02-lab1.md#8-open-decisions-for-the-course-owner) |
| 4 | Default brain in class and student hardware (lab server, cloud plan) | [02 §6](02-lab1.md#6-compute-budget) |
| 5 | Budget for the evolution matrix: run length, decision period, seeds | with JEV in the 192 × 192 world about 1 300 ticks (≈ 8 generations) per hour on one GPU; 10 hours ≈ 13 000 ticks |
| 6 | Details of the terrain and foliage labs | `Docs/redesign/06-world-terrain-foliage.md` |
| 7 | URL of your own git server for mirroring | `Docs/redesign/09-progress-log.md` |
| 8 | Lab 1 small world against the LLM brain: populations survive only above about 20 animals. Keep it, or change the world size or cap, the predators, or the mutation (`p_mut`, prompts that keep to the gene's world)? | [11 §9](11-gene-development.md#9-what-it-means) |
| 9 | `gene_timeline` does what Lab 1 activity D asks students to write (frequency curves per slot, diversity, instructions of the genes that spread): keep it as the teacher's solution, or change the activity to interpreting it (selection against drift, `gene_swap`)? | [02 activity D](02-lab1.md#d-watch-evolution), [11 §10](11-gene-development.md#10-reproduce-it-and-use-it-in-lab-1) |
| 10 | Predators and balance: are the rules right (a strike next to the prey, digestion 50 ticks, the prey's metabolism; since the stamina change, speed 2 when hunting, half the prey's stamina, carcasses for two more predators)? With the keyword brain the predators now fill their cap (28–34 in the full world) and rarely starve, so the cap, not food, limits them. Before the change, LLM predators stayed few (6–14 in the 96 × 96 world) and often starved, while the LLM-read prey filled the world up to their cap. With the LLM brain (2 000 ticks) the predators also filled their cap, by tick 800, and the prey stayed at 38–83 without newcomers, limited by predation rather than food ([06 §5.16](06-experiments-and-results.md#516-llm-brain-with-stamina-and-carcasses)). Keep these values, or let food rather than the cap limit the predators (smaller portions, a lower cap)? The full world costs about 12 calls per tick: 5 000 ticks ≈ 6.5 hours. **Overtaken on 2026-10-09** by litters, migration and the evolution-test world; what remains: kills are capped by the 50-tick digestion, so a higher `kill_p` adds few kills. | [03 §3](03-world-and-simulation.md#3-predators), [03 §13](03-world-and-simulation.md#13-reference-numbers-for-the-lab-1-world) |
| 11 | The default mutator: keep qwen3.5:0.8b (small, varied, often meaningless), switch to gemma4:26b on the CPU (small meaningful edits, but repeats itself and needs about 19 GB of RAM), and if so raise its temperature for variety? | [04 §5](04-genome-and-evolution.md#the-mutator-model), [06 §5.22](06-experiments-and-results.md#522-a-bigger-cpu-mutator) |
| 12 | Make cover something genes can use: add it to the prey's observation ("Cover: here / close / far / none") and the prompt? Needs the gate and the screen redone | [03 §1](03-world-and-simulation.md#cover-since-2026-10-09) |
| 13 | More selective deaths, if the evolution-test world isn't enough: the weakest migrate instead of random animals, shorter digestion, seasons | [06 §5.21](06-experiments-and-results.md#521-an-evolution-test-world) |
| 14 | The neutral gene "No preference." isn't a rule, and mutation turns it into vague text ("Preference for water."): keep it, word it as a rule, or exempt it from mutation? | [06 §5.18](06-experiments-and-results.md#518-the-long-llm-run-with-mutation-v4) |

## 5. Roadmap

### Phase 0 — finish the spike (Python)

**Done on 2026-10-07: predators are genetic animals** (owner request). Both
species evolve and see 20 cells with distances in bands; one partner's `mate`
is enough to breed, predators can follow. Both species have stamina: predators
run 2 cells per tick when hunting but tire twice as fast, and a kill leaves a
carcass for two more predators.

**Done on 2026-10-08/09: a stable world for evolution.** JEV-9B decides, about
three times as fast as gemma. Litters, migration at each species' cap and
separate caps keep both species alive without newcomers; the 192 × 192
evolution-test world makes most deaths depend on behaviour. Next: decision 11
(the mutator), then a long JEV run in that world next to a C3 SHUFFLED control
run, followed with `gene_timeline --species prey` and `--species predator`,
and a common-garden test of evolved against founder genomes.

1. Decide G2 handling (decision 2), apply it, and rerun the gate. Cached
   answers make reruns cheap.
2. Run the full E1 suite on the LLM brain → G3 and a larger G1/G2 sample.
3. Run several seeds and longer runs with the JEV brain in the evolution-test
   world, each next to a C3 SHUFFLED control.
4. Write the evolution tooling: the experiment matrix (C1 FULL, C2 NO-MUT, C3
   SHUFFLED, C4 RANDOM-FOUNDERS, C5 RULE-BASED), the common garden, and the
   analysis (allele frequencies, lineages, diversity, gene length) → G4, G5.
5. Write the report and take the go/no-go decision (H5).

If the brain can't be made to read genes (G1/G2) or is too slow (G5), the
fallback is **"LLM as development"**: one LLM call at birth turns the genome
into a fixed behaviour profile (utility weights, thresholds), which runs
cheaply every tick. That is 100–1 000× fewer calls
(`Docs/redesign/02-assessment.md`).

### Phase 1–3 — Unity (from `Docs/redesign/07-open-questions-and-roadmap.md`)

1. **Unity core:** fixed-step lockstep simulation, configuration asset, seeds,
   logging; agents split into perception / decision / executor / metabolism;
   rule-based and neural-network brains; walkability from the terrain.
2. **Prompt genome in Unity:** loci, founder pool, crossover; an inference
   client with batching and caching; blind LLM mutation with logging.
3. **Student-facing tools:** genome browser, allele-frequency plots, lineage
   view, lab handouts with exercises, reference results for the founder pool.

### The lab sequence

Lab 1 runs on the flat world. The terrain and foliage labs then feed their
outputs into the world: walkability and food placement
([02 §7](02-lab1.md#7-toward-the-next-labs-terrain-and-foliage)).

## 6. Parked work

Kept in the repository, tested with fakes, not used:

- **Laya** decision model: backend, probes, input-placement study
  (`backends/laya_backend.py`, `experiments/e0_probe_laya.py`,
  `e0_budget.py`).
- **Distillation pipeline:** held-out mutants, allele-split dataset,
  resumable teacher labelling (`make_mutants.py`, `make_dataset.py`,
  `label_teacher.py`).

Background: `Docs/redesign/05-decision-backend.md`.
