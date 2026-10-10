Implement the **Unity version of the evolving-animals system** ("Genes Are Prompts") in this repository, exactly as `Docs/Interface/` describes it: the contract (01–15), the architecture (20), the editor tooling (21), the student recipes (22), and the tests, scenarios and CI (30–32). It is a long job over many sessions, so this prompt may be pasted many times: always start with **Where are we**.

The result is a Unity package of small, readable, replaceable components under `Assets/EvoSim/`, the reference scenes, the editor tools, and the test suites, with every MUST rule of the contract covered by a passing test.

## Settings

| Setting | Value |
|---|---|
| Branch | `llm-evolution`. Pull before you start and before every push; never force-push, never rebase pushed commits. |
| Unity | 6000.3 (the owner upgrades the project before the first session). Path: `C:\Program Files\Unity\Hub\Editor\<6000.3 version>\Editor\Unity.exe`; find the exact folder in the first session and write it in the status file. |
| New code | `Assets/EvoSim/` only (layout in `Docs/Interface/20-unity-architecture.md` §7). |
| Status file | `Docs/Interface/50-implementation-status.md`: the single source of truth between sessions. Create it in the first session. |
| Unity logs and test results | `Logs/` (gitignored); read them with `grep`/`tail`, never whole. |
| LLM servers (local) | JEV on vLLM at `http://localhost:8000` (`docker compose -f prototype/docker/compose.yaml up -d`, see `prototype/docker/README.md`); Ollama at `http://localhost:11434` for the mutator (`qwen3.5:0.8b`) and the optional gemma brain. |
| Model budget | none before milestone M8. From M8: smoke tests freely; anything over 2 000 new model calls (a scenario run, a gate) only after telling the owner the estimate. Cached answers are free. |

## Context

- **Read first, every session**: `Docs/Interface/50-implementation-status.md` (once it exists), then `Docs/Interface/README.md`, including its three tables of owner decisions. They override anything older.
- **The specification**: `Docs/Interface/`, read in this order before writing code for a milestone: 01 (concepts), 20 (architecture), then the contract documents the milestone names, then 30 (the tests to write). Rules are numbered (`SENSE-01`, `MOVE-04`, …): MUST is mandatory and tested, SHOULD is the reference behaviour, MAY is optional. 40 lists what is still open or deferred.
- **The architecture brief**: the owner made an illustrated brief with Claude Design from `Docs/prompts/05-claude-design-architecture-brief.md` (UML, design patterns, worked custom setups). If it is in the repository (look under `Docs/` for an architecture brief, as HTML, PDF or images), read it and implement what it shows. Where it and `Docs/Interface/` disagree, `Docs/Interface/` wins; record each disagreement in the status file.
- **Background**: `Docs/prompt-genome/` (the Python prototype's documentation) and `prototype/` (the code). The prototype is a behavioural reference only: don't port it line by line. Unity differs on purpose (continuous space, components, a food web, no keyword brain, hide as its own action); the README's decision tables list every difference.
- **Existing Unity content**: the course's other labs live in `Assets/` (terrain brushes, IK, scenes). Don't modify, move or delete anything outside `Assets/EvoSim/`, `Packages/manifest.json` (packages only), `Docs/Interface/50-implementation-status.md`, `Docs/Interface/40-open-questions.md` and `.github/workflows/`.

## Rules

1. **The contract is the specification.** Implement the rules, not your own idea of them. When a document is ambiguous or two documents disagree, choose the reading that keeps every MUST rule, write the decision in the status file and in `40-open-questions.md` §2 ("still open"), and go on; ask the owner at the next checkpoint. Never silently drop a rule.
2. **Architecture rules** (`20` §1, ARCH-01…12) are mandatory:
   - the scene is the configuration; one feature = one class deriving from one base class;
   - animals are plain data, never MonoBehaviours with `Update`; views only display;
   - the World initialises everything in a fixed order; no module sets itself up in `Awake`/`Start`/`OnEnable`;
   - modules belong to their nearest owner; disabled means absent; a species inside a species is an error;
   - no statics, no singletons; several worlds can coexist;
   - prefabs never reference scene objects (look-ups by role or name);
   - reference modules use only the public API a student has: no `internal` shortcuts for them;
   - version-specific code (HTTP, waiting, NavMesh, raycast batches) fenced in its few files.
3. **Determinism**: no `UnityEngine.Random`, no unseeded `System.Random`, no time or GUID as randomness, no iteration over hash sets or dictionaries to make a decision; every draw from a named stream (`12` RAND-01…05).
4. **Tests come with the code.** Each step writes the tests that `30-tests.md` lists for it (same ids, `T-AREA-NN`, rule ids in the test description), runs them, and commits only when they pass. A MUST rule without a passing test is not done. Use the helpers of `EvoSim.Testing` (`WorldBuilder`, `ScriptedBrain`, `FakeMutator`, `Place`, `Stat`, `Golden`) and build them as you need them.
5. **Readable code for students.** One public class per file, named like the class. A reference module (a sense, an action, a stat, a locomotion, a rule) fits on one screen where possible. XML doc comments of one or two lines; comments cite rule ids (`// MOVE-04`). No LINQ in per-tick loops. No allocation per animal per tick in hot loops once a system works (R-07).
6. **No secrets** anywhere in code, scenes, prefabs, assets or logs (OUT-04): keys only from environment variables.
7. **Scope.** Implement everything the documents describe as v1. Things marked *later* (speciation rules, camera senses, real time, terrain/NavMesh/physics locomotion, Burst scaling, a shared class cache) get only the hooks the documents describe (base classes, interfaces, rules), not implementations. The student recipes of `22` (thirst, the water sense, drink, the chased sense, slope locomotion, the intensity ladder, seasons, edible eggs) are built as samples in `Assets/EvoSim/Samples/`, because scenarios S16, S17, S28 and the tests need them.
8. **Unity in batch mode** cannot open a project that the editor has open. Before a batch command, check for a lock (`Temp/UnityLockfile`); if the editor is open, ask the owner to close it or to run the step themselves, never kill Unity.
9. **Output hygiene**: summaries of at most 20 lines; long output to `Logs/`; read logs with `grep -n "error\|warning CS\|Failed"` and `tail`. Jobs over 2 minutes run in the background with a log; no polling loops.
10. **Commits**: one step = one commit, message `EvoSim M<n>.<step>: <what>`, with the attribution lines the session requires. Push after each milestone (pull first).
11. **Checkpoints** (⏸): update the status file, commit, push, then stop and tell the owner: "⏸ Checkpoint M<n> — <what to look at in Unity>". Wait for the answer.

## Where are we (start here each time)

1. `git pull`, `git log --oneline -10`, `git status`.
2. Read `Docs/Interface/50-implementation-status.md`. No file yet → milestone M0.
3. Run the fast tiers (T0–T1, EditMode) in batch mode and read the result summary. If something that passed before fails, fix it first.
4. Continue from the status file's **Next step**. If the last session ended mid-step, the status file says where.
5. At the end of a session: update the status file (done, next step, open questions, known problems, the exact commands that work on this machine), commit, push, and say "✅ Session done — paste the prompt again to continue."

## Batch-mode commands (Windows, PowerShell; adapt the Unity path)

```powershell
$U = "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
# compile only
& $U -batchmode -nographics -projectPath . -quit -logFile Logs/compile.log
# EditMode tests (T0, T1)
& $U -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/editmode.log
# PlayMode tests (T2)
& $U -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log
# a scenario or a regression entry point (from M7)
& $U -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunScenario -scenario S03 -quit -logFile Logs/s03.log
```

Count failures with `grep -c 'result="Failed"' Logs/editmode.xml`; compile errors with `grep -n "error CS" Logs/compile.log`.

## Milestones

Each milestone lists what to read, what to build, which tests must pass, and when it is done. Inside a milestone, split the work into steps of at most a few hundred lines each, one commit per step. The class names come from `20` (and the brief); keep them.

### M0 — Project check and skeleton
- Read: README, 20 §7–§8, 30 §1–§2.
- Check `ProjectSettings/ProjectVersion.txt`: if it isn't 6000.3, stop and ask the owner.
- Check `Packages/manifest.json` and add what is missing, choosing versions that this Unity version accepts: `com.unity.nuget.newtonsoft-json`, `com.unity.ai.navigation`, `com.unity.test-framework`. Don't add Burst or Collections. Compile and read the log.
- Create the folders and assembly definitions of 20 §7 (`EvoSim.Runtime`, `EvoSim.Http`, `EvoSim.Editor`, `EvoSim.Testing`, `EvoSim.Tests.EditMode`, `EvoSim.Tests.PlayMode`) with the references the architecture allows (Runtime never references Editor).
- One trivial EditMode and one PlayMode test that pass in batch mode.
- Create the status file with: the Unity path, the commands that work, the milestone plan, the next step.
- Done when: compile clean, both tests pass from the command line. ⏸ (packages and layout)

### M1 — Core and the tick loop
- Read: 01, 02, 12, 20 §1–§5, 04 §1.
- Build: `World`, `WorldModule`, `TickPhase`, `WorldService`, `Species`, `SpeciesModule`; discovery and ownership (nearest owner, disabled = absent, nesting error, orders, signature); `SpeciesBuilder` with stats and traits; `Animal`; `RandomStreams` (PCG32 seeded from SHA-256 of seed and name); `Pending`, the run loop with `Freeze` and `Responsive` wait modes and the run controls; the validation engine (`ValidationReport`, messages with object, severity, fix) used by `Initialize`; a minimal event recorder with the running SHA-256 hash.
- Tests: T-CORE-01…10, T-SPACE-05…07, T-TICK-01…03, T-RAND-01…05 (as far as the pieces exist).
- Done when: an empty world (S00) runs 1 000 ticks in both wait modes with identical hashes.

### M2 — Space, ground and environment
- Read: 02, 03, 04 §2, 20 §3.11–§3.12.
- Build: the distance function, `FlatGround` (and `TerrainGround` for heights, water level, steepness), `SpatialIndex`, `ResourceLayer`/`FoodGrid`, `CoverLayer`, `EntitySystem<T>`, `CarcassSystem`, `EggSystem`, `Edible`, the environment phase.
- Tests: T-SPACE-01…04, T-ENV-01…08.

### M3 — Animals, metabolism, locomotion, actions, food web
- Read: 05, 07, 04, 20 §3.4, §3.6, §3.7, §3.12.
- Build: `Energy`, `Stamina`, `Metabolism`, `Digestion` (busy), `Starvation`, `OldAge`, the death phase; `Intent`, `ActContext`, `Locomotion`/`KinematicLocomotion`; the act phase with the three act orders; `Diet`, threats derived from the food web, interactions (graze, strike, scavenge); the reference actions eat, flee (with the off-by-default *flee into cover* option), hide, follow, rest, mate, hunt, and the search fallback.
- Tests: T-ANIM-01…10, T-ACT-01…10, T-MOVE-01…07, T-SPEC-01…05, T-SPEC-08 (with eggs from M2).

### M4 — Senses and observations
- Read: 06, 20 §3.5.
- Build: `Sense`, `BatchedSense` (the ray-batch helper may stay minimal until a sense needs it), `LevelSense`, `AgeSense`, `NearestResourceSense`, `NearestAnimalSense` (with readiness), `NearestCoverSense`; bands; V1 and V2 texts in meters; the observation tuple and its space size.
- Tests: T-SENSE-01…10 (T-SENSE-08's snapshot is pinned once the texts are reviewed at the M7 checkpoint).

### M5 — Genes, decisions, prompts
- Read: 08, 09, 20 §3.8, §3.10.
- Build: `Gene`, `TextGene`, `NumberGene`, `AllelePool`, `AlleleRegistry`, genome keys (full and brain-visible), founder sampling, expression into traits and the prompt; `Brain`, `RandomBrain`, `ScriptedBrain` (in `EvoSim.Testing` only), `DecisionMemo`, `AnswerCache` (local JSON-lines files), `DecisionQuery`, the Sense / Ask brains / Choose actions phases, sampling with temperature, strict and non-strict failure handling; `PromptWriter` with inspector fields, placeholders and the brain's hard-coded answer instruction; frozen prompts.
- Tests: T-GENE-01…11, T-DEC-01…09 and T-DEC-11 (failures with a failing `ScriptedBrain`), T-PROMPT-01…04 and T-PROMPT-06.

### M6 — Reproduction, population, non-LLM mutation, controls
- Read: 10, 11, 15 §1, 20 §3.9.
- Build: readiness, `MatingRule` (one partner's choice is enough, once per period), `Litter`, `UniformCrossover`, `Incubation` and eggs, the breed and hatch phases, `CapRule` (migrate, block), the migration phase, `FloorRule` and newcomers, founders at tick 0, `World.AddSpecies`; `MutationOperator`, `MutationJob` (draws at conception), `GaussianMutation`; the controls C2, C3, C4, C7 and NULL.
- Tests: T-REPRO-01…08, T-POP-01…04, T-SPEC-06…07, T-MUT-01…05, T-MUT-09…12 (with `FakeMutator`), T-TICK-04, T-CTRL-01…02.

### M7 — Outputs, configuration, batch runs
- Read: 13, 14, 12 §4, 30 §5–§6, 31 §1.
- Build: the recorder's files (events, stats, alleles, final population, summary, run info) with the compatibility mode; `LiveStatistics`; `ScenarioAsset` with path overrides; `EvoSim.Batch` entry points (run a scenario, run the regression, run the integrity checks later); stop file and clean stop; resume by replay; the per-tick invariant checker for test worlds.
- Tests: T-OUT-01…03, T-CFG-01…03, T-RAND-06, R-01 (pin the hashes), R-04, R-05.
- Done when: S03 runs headless with the random brain, writes every file, and replays to the same hash. ⏸ (the owner opens a run's files; reviews the V1/V2 situation texts before T-SENSE-08 is pinned)

### M8 — LLM services
- Read: 08 §5–§7, 10 §2 and §4, 32 §2, `prototype/docker/README.md`, `prototype/promptevo/backends/jev_backend.py` and `prototype/promptevo/llm/openai_client.py` (request formats only).
- Build in `EvoSim.Http`: `HttpBrain` (`HttpClient`, `ConfigureAwait(false)` everywhere, retries with back-off, time-outs, a cap on parallel requests, strict mode, the answer cache), `JevBrain` (one `/v1/completions` request per batch with the list of prompts, `max_tokens` 1, allowed option tokens, log-probabilities, head bias and calibrated temperature from the model's `adapter_vllm/decision_head.json` and `calibration.json` at the pinned revision, stored as assets), `OllamaPointsBrain` (JSON schema, temperature 0, seed, context length, thinking off); `MutatorService` and `LlmMutation` (deck v4 as a TextAsset, the context line, cleaning, guards, redraw rounds, cache). Test everything with fake transports first.
- Tests: T-DEC-09 and T-DEC-10 with fake transports, T-MUT-06…08, then B-01 (smoke) and B-11 (blindness) against the real local servers.
- Done when: a 50-tick S03 with JEV for both species runs in both wait modes, replays from the cache with zero calls, and the editor stays usable in Responsive mode. ⏸ (the owner watches a JEV run in the editor)

### M9 — Reference content and views
- Read: 04 §5, 09 §3, 20 §2 and §6, 31.
- Build: the module prefabs in `Modules/` (Eat, Flee, Hide, Follow, Rest, Mate, Hunt, …); `AllelePool` assets with the prototype's founder pools (`prototype/data/*founder_pool_v2.json`), the draft hide sentences, contrast pairs and control sentences; the scenes `Lab1_Full`, `Lab1_Small`, `HideVsFlee`, `ThreeSpecies`, `Terrain_Locomotion`, `Sandbox`; `Body`, `AnimalView`, `ViewPool`, interpolation in `LateUpdate`; simple placeholder bodies for prey and predator.
- Done when: `Lab1_Full` validates green and runs in Play mode with visible animals. ⏸ (the owner plays the scene)

### M10 — Editor tooling
- Read: 21 completely, 22 §0.
- Build: every validator V-01…V-62 with its fix buttons; the play-mode gate on errors; the World and Species inspectors (tables, observation space, signature, prompt preview); the drawers; the windows (ecology monitor, gene pool, animal inspector, ask the brain, food web, event log, scenario runner, brain health); gizmos and the scene-view overlay; the menus (New World, New Species, Add Action/Sense/Gene, Exercises ▸ Replace with stub / Restore reference) and the script templates.
- Tests: T-EDIT-01…03, T-PROMPT-05.
- Done when: a new world built only from the menus validates and runs. ⏸ (the owner tries the tools)

### M11 — Samples, scenarios, verification, CI
- Read: 22, 30 §4–§6, 31, 32.
- Build: the samples of rule 7; the conformance suites of 30 §4 (they run on every module of each kind found, samples included); the scenario assets S00–S28 and their checks; the regressions R-01…R-08; the integrity checks B-01…B-11 with their reports; `.github/workflows/evosim.yml` for the owner's self-hosted runner as in 32 §4; save the agent prompts P1–P6 of 32 §3 as `Docs/prompts/07-…` files.
- Run: T0–T3 fully; T4 scenarios and gates only after telling the owner the estimated model calls and time.
- Done when: T0–T3 green, the T4 reports written, every MUST rule cited by a passing test (write the coverage check as an EditMode test that reads `30-tests.md` and the test attributes). ⏸ (the owner reviews the reports and the CI run)

### M12 — Teaching path and final audit
- Read: 22 §0, 32 §3 P1.
- Build: stub classes for the teaching path's reimplementation exercises, so that *Replace with stub* gives each reference module an empty student class whose tests fail until written; a short `Assets/EvoSim/README.md` for students (open a scene, run, inspect, extend; links to `Docs/Interface/22`).
- Then run the audit of 32 §3 P1 on your own work, phase 1 only, and put its table in the status file.
- Done when: the owner has the audit table. ⏸ (final review)

## The status file

Keep `Docs/Interface/50-implementation-status.md` under 200 lines:

```markdown
# 50 — Implementation status
Updated: <date>, session <n>
## Next step
M<n>.<step>: <exactly what to do next>
## Milestones
| M | State | Commit | Notes |
## Machine
Unity path, Unity version, package versions, test commands that work, server checks.
## Decisions taken while implementing
<rule id> — <ambiguity> — <choice> — <asked the owner? answer>
## Disagreements between the brief and Docs/Interface
## Known problems
## Rule coverage
<MUST rules without a passing test, by milestone>
```

When it grows past 200 lines, move finished milestones' details to `Docs/Interface/51-implementation-log.md`.

## What to report at each checkpoint

Five lines at most: what was built; the test counts (passed / failed / skipped) per tier; what the owner should open or try in Unity, and where; open questions; the next milestone.
