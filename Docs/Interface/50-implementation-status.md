# 50 — Implementation status
Updated: 2026-11-03, M16.3 senses for the neural species (before it: M15 overlay actions ⏸)

The single source of truth between implementation sessions of
[`Docs/prompts/06-implement-unity-system.md`](../prompts/06-implement-unity-system.md).

## Next step
M16 (plan `Docs/Neuro/PLAN.md`, prompt `Docs/prompts/08-implement-neural-species.md`): **M16.4 Brain** — `NeuralBrain` (sync,
Greedy/Sample), with T-NEURO-07. M16.3 (ray fan, interoception, hunger clock, fan view) and M16.2 (genes, mutation,
crossover in `Assets/EvoSim/Neuro`) are done; the fan is seen in a sandbox once M16.6 gives the `Neuro_Lab` scene. M16.1 (core C1–C7) is done: vector alleles, `vectors.jsonl`, `DecisionQuery.Genome`,
`AnimalAction.SelfDirected`, `Brain.ReadsGeneText`, the conformance fixtures, recombinant alleles; EditMode see M16.1 row,
R-01 unchanged. The owner's review of the core diff (⏸ M16.1) is still to come.

Before M16: M15 waits for the owner ⏸ (overlay actions, the alarm-calls example; 51 §M15). Since M14.1, docs only (0a322b7,
9f0838f, 334771d, 2ac2368, 2fdd431); the gh-pages site predates the 2026-10-22 doc fixes: rebuild and publish it again.

Before M15, M14 and M13 waited at their checkpoints ⏸; their next-step notes moved to 51 (end).

## Milestones
| M | State | Commit | Notes |
|---|---|---|---|
| M0 Project check and skeleton | done | e6ef6fc, 7536724 | compile clean; EditMode 2/2, PlayMode 1/1 from the CLI |
| M1 Core and the tick loop | done | 2bacd0c | S00 1 000 ticks, same hash in both wait modes; T-TICK-03, T-RAND-04, T-CORE-06/07/10 finish later |
| M2 Space, ground, environment | done | 1aed10e | EggSystem/Egg/MutationJob built here (needed by diets) |
| M3 Animals, metabolism, locomotion, actions, food web | done | 7a6688a | EditMode 83/83; T-ANIM-01/09 (migration), T-MOVE-05, T-SPEC-01/02 come with M5–M6 |
| M4 Senses and observations | done | 143de9a | situation texts pinned from the first run: Tests/Golden/situations.tsv (owner to review) |
| M5 Genes, decisions, prompts | done | 95d47df | prompts pinned: Tests/Golden/prompts.txt; the 08 §7 example matches word for word |
| M6 Reproduction, population, non-LLM mutation, controls | done | de83e06 | EditMode 154/154, PlayMode 11/11; LLM mutation logic done with a fake client (HTTP client in M8) |
| M7 Outputs, configuration, batch runs | done | c5bf389, 75d0f81 | EditMode 162/162, PlayMode 16/16; S03 headless from the open editor; R-01 hashes pinned (owner to review situations.tsv) |
| M8 LLM services | done | ade561f, 8784ba7 | B-01 PASS (JEV, 0.29 s), B-11 PASS (23 prompts blind); S03 jev 50 ticks: Responsive 245 calls, longest Advance 22 ms; Freeze replay same hash, 0 calls |
| M9 Reference content and views | done | a021fc2 | six scenes, module prefabs, bodies, T-SPACE-06; Play-mode look not checked by eye (editor frozen) |
| M10 Editor tooling | done | c61f043 | a world from the menus validates and runs; T-EDIT-01 for every catalogue code; windows not checked by eye |
| M11 Samples, scenarios, verification, CI | done | e5f402d…cde9e9c, M11.5 | samples, conformance, CI, scenarios S00–S27, R-01…R-08, coverage test; EditMode 319 + T3, PlayMode 17/17; gates B-01…B-11 (Machine) |
| M12 Teaching path and final audit | review | bb6b46a, 1eaa571, 66a81e5, 83019ce, M12.5 | 14 stubs, Exercises menu, README (no grading, hooks); P1 phases 1–2; EditMode 433/433 (with soak and pinned hashes), PlayMode 19/19 ⏸ |
| M15 Overlay actions, alarm-calls example | review | d5e5870 (titled M14.2) | ACT-07 overlays: a carrier drawn from the rest of the row, V-19, V-27, T-ACT-11 (OverlayTests); `Assets/Student` worked example with tests, scene and site page; EditMode 466/466 (batch), R-01 unchanged ⏸ |
| M14 Documentation for students | review | 598a755, 0a322b7 | `///` on every type and member (347 files, +10 149 lines, code token-identical); Docs/Site: 41 pages, 50 compiled examples, strict build 0 warnings; EditMode 438/438, PlayMode 23/23 (batch, -nographics); B1–B5 student-readiness fixes (0a322b7); published on gh-pages `/evosim` (334771d) ⏸ |
| M13 Real-time visual test | review | ed16db5, d155e72, 29838b3, 9140625, c9d9e0a, e735804, 5efefe4, 6f3b302, 2328091, M13.10 | scenes watchable (ground, food, cover, markers, overlay, camera, capture); L0 and L2 hashes equal headless = Play = build; then memo of cache hits, player build of the project, staggered reference; EditMode 438/438, PlayMode 23/23; report RT-2026-10-10 ⏸ |
| M16.1 Neural species: core C1–C7 | done | M16.1 | `AlleleKind.Vector`, `vectors.jsonl`, `DecisionQuery.Genome`, `SelfDirected`, `ReadsGeneText`, `VectorProbeGene`, recombinants; T-NEURO-01 (VectorAlleleTests); EditMode 472/472 (batch), 6 new; R-01 unchanged ⏸ |
| M16.2 Genes | done | M16.2 | `NeuralLayout`, `NeuralNet`, `NeuralGenomeGene`, `NeuralMutation`, `NeuronCrossover`; T-NEURO-02…04 (NeuralGeneTests), T-EDIT-02 for EvoSim.Neuro; EditMode 480/480 + 1 |
| M16.3 Senses | done | M16.3 | `WorldRayCaster`, `RayFanSense`, `InteroceptionSense`, `HungerClock`, `RayFanView`; T-NEURO-05, 06 (NeuralSenseTests); EditMode 496/496 with Neuro |
| M16.4 Brain | next | | `NeuralBrain` (sync, Greedy/Sample); T-NEURO-07 |
| M16.5 Actions | | | `NeuralAction`, `TurnAction`, `ForwardAction`, `StopAction`, contacts, breeding; T-NEURO-09…11 |
| M16.6 Templates and pairings | | | `NeuroPrey`/`NeuroPredator`, menu, `MatchedPairs` V-70…V-79, `Neuro_Lab`, S29–S31 (+L0), `NeuroStats`; T-NEURO-12, 15 ⏸ |
| M16.7 Comparison scenarios | | | `Prey_vs_Prey`, `Brains_2x2`, S32/S33 both arms, survival analysis and report, chapter 16, site page; T-NEURO-13…16 ⏸ |
| M16.8 Concurrency and scale | | | async `Later`, pooled attachments, one fan per tick, profile S32; T-NEURO-08 |
| M16.9 Measured runs | | | first S33 run (cost, stop tick) ⏸, then 7 runs with the owner's go ⏸ |

## Machine
- Unity **6000.3.9f1**: `C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe`
  (the only editor installed).
- Packages: `com.unity.nuget.newtonsoft-json` 3.2.2 (declared explicitly),
  `com.unity.ai.navigation` 2.0.16, `com.unity.test-framework` 1.6.0,
  `com.unity.pipeline` 0.8.0-exp.1 (the owner's, for the `unity` CLI).
- `unity` CLI 1.0.0-beta.13 at `%LOCALAPPDATA%\Unity\bin\unity`. It drives the
  **open editor** over HTTP (com.unity.pipeline), so batch mode isn't needed
  while the editor is open. Options for the CLI go *before* the command name,
  the command's own arguments after it.

Commands that work on this machine (Git Bash, from the repository root):

```bash
unity command --result-only editor_status                 # ready / compiling / playMode
unity command --result-only recompile                     # then:
unity command --result-only recompile_status              # completed, failed, errors[]
unity command --result-only console_status                # error and warning counts
# Both modes async, then poll (a synchronous run that outlives the pipeline's 5-minute timer froze the editor);
# --filter takes a test-name part with --filter_type testName (the whole assembly includes the 30-minute T3 soak)
unity command --result-only run_tests --mode editor \
  --filter EvoSim.Tests.EditMode --filter_type assembly --async_tests true --timeout 3600
unity command --result-only run_tests --mode playmode \
  --filter EvoSim.Tests.PlayMode --filter_type assembly --async_tests true
unity command --result-only test_status > Logs/editmode-cli.json   # until "status": "completed"
```

Read results with `grep -c '"Status": "Failed"' Logs/editmode-cli.json` or the
`Summary` block at the top. Pre-existing warnings: 4 CS0162/CS0414 in the
course's lab scripts (`Assets/02 - Scripts`), not ours.

Batch mode (editor closed, CI): the commands of the prompt with
`$U = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"`.
Works on a copy of the project while the editor holds the lock (scratchpad copyrun.sh: sync, build, edit, play, method).
Scenario runs in the open editor (same code as `-executeMethod EvoSim.Batch.RunScenario -scenario S03`):
`unity command --timeout 900 --result-only eval --code 'return EvoSim.Batch.RunScenarioNamed("S03");'`
(optional variant, seed, ticks); files go to `Logs/EvoSim/runs/<scenario>-<variant>-s<seed>-<time>/`.
Rebuild the reference scenes and pools: `unity command --result-only menu --path "EvoSim/Build Reference Scenes"`.

Real-time runs (M13), from the open editor (`R=EvoSim.Editor.RealTimeRuns`; levels L0 random brain + no mutation, L1 no
mutation, L2 everything):
```bash
unity command --result-only eval --code 'return EvoSim.Editor.RealTimeRuns.PreparePlay("HideVsFlee", 1234, 600, 5f, "L2", "RUN");'
unity command --result-only editor_play                    # settings go in through World.Configuring; the scene file is untouched
unity command --result-only eval --code 'return EvoSim.Editor.RealTimeRuns.Status();'     # poll: tick, state, pace, hash, captures
unity command --result-only editor_stop
# headless, stepped from the editor loop (never blocks a CLI call); an empty scene first: WithScene refuses an open scene
unity command --result-only eval --code 'return EvoSim.Editor.RealTimeRuns.HeadlessStart("HideVsFlee", 1234, 600, "L2", "RUN-headless");'
unity command --result-only eval --code 'return EvoSim.Editor.RealTimeRuns.HeadlessStatus();'   # or Logs/EvoSim/realtime-headless.txt
unity command --result-only eval --code 'return EvoSim.Editor.RealTimeRuns.Behaviour("HideVsFlee", 1234, 600, "L2");'
# editor closed: Unity.exe -batchmode -nographics -projectPath . -executeMethod EvoSim.Editor.RealTimeRuns.FromCommandLine
#   -do headless|behaviour -scene HideVsFlee -seed 1234 -ticks 1368 -level L2 [-run NAME] -quit  (→ Logs/EvoSim/realtime-batch.txt)
```
Player build (Windows 64, Mono, six scenes, HideVsFlee first): EvoSim ▸ Build ▸ Visual Player, or from the CLI
`eval 'return EvoSim.Editor.VisualPlayerBuild.BuildLater();'` (result in Logs/EvoSim/visual-player-build.txt); a build
makes Unity rewrite `ProjectSettings/UnityConnectSettings.asset` (restore it, or commit Unity's version once). Batch, or
a copy of Assets/EvoSim + Packages + ProjectSettings on a short path (`subst W:`): `-executeMethod
EvoSim.Editor.VisualPlayerBuild.BuildFromCommandLine -out <repo>/Builds/EvoSim/<date>`. Scripts only:
`VisualPlayerBuild.CompilePlayerScriptsLater()` (Logs/EvoSim/player-scripts.txt). Then
`EvoSim.exe -scene HideVsFlee -seed 1234 -ticks 600 [-minutes 10] -speed 20 [-brain Random -noMutation] [-stagger] -capture 10 -run NAME
-window 1920x1080 -quitAtEnd -logFile p.log` (`-screen-fullscreen 0` alone stays fullscreen). Run files, captures and the
answer cache land next to the exe (`Logs/EvoSim/…`, `Library/EvoSim/AnswerCache`); copy the editor's cache there to replay.
Ollama for L2: `OLLAMA_MAX_LOADED_MODELS=1 OLLAMA_KEEP_ALIVE=2h ollama serve`, gemma4:26b warmed with num_gpu 0 (9 GB RAM).

Servers (2026-10-10): JEV (vLLM container promptevo-jev, localhost:8000, model jev-decision) and Ollama 0.35.1
(localhost:11434: the mutator gemma4:26b 001e5dafc3c7, the judge gemma4:12b 6114515d63c1, qwen3.5:0.8b de63045f2975). Gates, on the copy:
`-executeMethod EvoSim.Integrity.RunIntegrity -checks B-02,B-03 -brain JEV [-cpu] [-judge gemma4:12b]`.
Results (reference scene, reports in Logs/EvoSim/Reports): B-01 pass (JEV 0.29 s); B-02 pass (sign 1.00, mean ΔP
0.281); B-03 pass (MI_G founders/random: prey 0.057/0.006, predator 0.062/0.004); B-04 pass (locality 0.135,
ρ 0.69, 33 edits); B-05 7 % of top answers change with reversed options; B-06 pass (463 of 1 024 tokens); B-07
pass (gemma4:12b on CPU, every sum 100); B-08 pass (P(flee) wolf 0.42, shadow 0.035); B-09 JEV rows differ by up
to 0.04 (reported); B-10 FAIL, gemma4:26b: 72.5 % usable after one mutation < 80 %, 62.5 % after ten, the founders themselves 92.5 % (qwen3.5:0.8b: 67.5 % / 17.5 %) (40 §2 #13); B-11 pass. About 5 500 calls.

## Decisions taken while implementing
M0–M15: in [51](51-implementation-log.md) (M12–M15 at its end). Since then:
- M16 (2026-11-03) — prompt 08 was only on `origin/ccr-6660386e-d1ubni` (147e31c); cherry-picked onto llm-evolution (be097e3).
- C1 — a vector's canonical form is `vec[<n>]#` + 16 hex of SHA-256 over each value as the int64 round(v·10^d), little-endian
  (no text formatting in the hash); `ToString` keeps 8 hex. `Vector` is a `ReadOnlySpan<float>` over a private copy.
- C2 — `vectors.jsonl` is appended at each `alleles.jsonl` write (rewriting it would copy megabytes every 5 000 ticks), and
  exists only once a vector allele does, so runs without vector genes write exactly the files they did.
- C3 — `DecisionQuery.Genome` is a new optional constructor argument; the ask-brains phase and the prompt preview pass it.
  Ask-the-Brain builds its queries from sentences without a genome, so it passes none (a network brain will fail there,
  DEC-40, rather than invent a row).
- C5/C6 — `ScriptedBrain.ReadsText` (test fixture) switches `ReadsGeneText`; the brain suite adds a brain's
  `[RequiresModule]` species modules to both species and passes each animal's genome.
- M16.2 — the test assembly is `EvoSim.Tests.Neuro` (plan: `EvoSim.Neuro.Tests`): the conformance suites scan every
  `EvoSim.*` assembly except `EvoSim.Testing*`/`EvoSim.Tests*`, so a test probe in `EvoSim.Neuro.Tests` would become a
  conformance case.
- M16.2 — founders come from `RandomStream(founderSeed, "neuro/founders/<layout hash>")` (PCG32, the project's stream)
  instead of a private SplitMix64: same property (independent of the run seed, GENE-21). Xavier draws weights only;
  biases start at 0. Weights are stored neuron by neuron (bias, then incoming weights), so a neuron's slice is contiguous.
- M16.2 — V-71: the founder pool survives `Initialize` (only the layout is rebuilt), so a pool drawn for an older layout
  is reported with the fix "Rebuild founders" instead of being replaced silently; Inspector edits rebuild it.
- M16.2 — `NeuralMutation`: 3 draws per weight (choice, added float, replacement) plus the fallback's index and float, so
  the count never depends on the outcome (MUT-30); add and replace are exclusive per weight; the Gentle rate 0.5 is set in
  the constructor (the base class keeps 0.03 for other operators).
- M16.3 — a ray's distance to an animal, entity or item is where its centre projects on the ray (food 3 m ahead reads 3 m,
  0.85 at 20 m), hit when the ray passes within 0.5 m (items: through their cell). Cover counts only where a ray enters it
  from outside, so an animal in a thicket is not blinded by its own cell. Tie order = the `RayHitKind` values: Threat,
  Prey, ReadyKin, Kin, OtherAnimal, Carcass, Entity, Food, Item, Cover, Water, Obstacle. V-76 checks the fan's settings.
- M16.3 — `HungerClock` is a `DeathRule` that never kills (the thirst-recipe hook). The sense conformance suite gives a sense
  with attachments a scripted brain that accepts them (V-51), an additive fixture change like C6.

## Disagreements between the brief and Docs/Interface
- 20 §3.9 names the operator method `MutationOperator.Start(gene, parent, rng)`; on a MonoBehaviour Unity takes
  `Start` for its message and logs "Script error: Start() can not take parameters" for every operator. Renamed
  `StartMutation` (templates, samples, tests). The contract's text should follow.
Brief vs Interface: none found so far. Chapter B of the brief (the Claude Doc, tabs A–G) matches 20; it adds the run loop's
seven states (Idle, Running, Waiting, Blocked, Paused, Stepping, Stopped), now `RunState`.
`Docs/Interface/design.md` (chapter A) and `design.pdf` are the owner's, committed in 2ac2368 (2026-10-21) as the v1 brief
of 2026-10-10; a note at the top of `design.md` says what changed since.

## Known problems
- 2026-10-10 13:54–16:05 — the editor froze after the pipeline's 5-minute timer cancelled a synchronous EditMode run;
  it recovered by itself (same process). Lesson kept: in the editor, run EditMode with `--async_tests true` and poll.
- 2026-10-10 evening — the machine stopped the Ollama server and a B-10 run for low memory (the editor and both models
  loaded). Rerun with the editor closed and one model loaded at a time (gemma4:26b is about 19 GB).
- GPU shared: the JEV container (restarted by Docker Desktop's restart policy) and another Python process of the
  owner's; the mutator runs on the CPU in Ollama (started for the checks).
- Files that aren't EvoSim's, left alone, then committed as they were in 2ac2368 (2026-10-21):
  `Assets/MobileDependencyResolver/`, two `.cs.meta` in `Assets/02 - Scripts/`, `Docs/Interface/design.md`, `design.pdf`.
- M13: in the editor a capture needs a drawn Game view (hidden, captures are given up after 5 s); Unity warns on Play that
  the Input Manager is deprecated (`activeInputHandler: 0`, the owner's setting).
- M14: `ViewPlayTests.ViewsNeverChangeTheRun` fails in batch mode with a graphics device (start and end captures 1 < 2: the
  test skips its capture checks only without a device). It passes in the editor (M13) and in batch with `-nographics`.
- M13: a `BuildPipeline.BuildPlayer` inside the project made Unity rewrite `ProjectSettings/UnityConnectSettings.asset`
  (format upgrade, `m_Enabled: 1`); restored from git. Build players from the copy only.
- 2026-11-03 — `ExerciseTests.StubsCompile` failed once in batch `-nographics` before any M16 change (`AssemblyBuilder.Build()`
  returned false: "the build started"), then passed on the next runs: flaky in batch, not an M16 regression.

## Rule coverage
Every MUST rule of 01–22 is cited by a test that names it (CoverageTests, corrected in phase 2). The weak and
untested parts phase 1 listed have tests now (51, rows 13–26), except ANIM-31's "logged" (40 §2 #15 rewords it).
Small gaps left: MUT-30 with answers arriving out of order, TICK-02, a shallow PhaseConformance.
