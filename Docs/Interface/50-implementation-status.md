# 50 — Implementation status
Updated: 2026-10-10, session 1 (continuing without checkpoint stops, owner's instruction)

The single source of truth between implementation sessions of
[`Docs/prompts/06-implement-unity-system.md`](../prompts/06-implement-unity-system.md).

## Next step
M12 waits for the owner (final review ⏸): the P1 audit table below; phase 2 fixes only the rows the owner approves.
Open owner decisions: 40 §2 #1, #2 (golden texts), #13 (B-10). Owner 2026-10-10: no grading (40 §2 #14), a better mutator on the CPU, fix every audit row.
EditMode runs go through the editor with `--async_tests true` (scratchpad evo.sh); long runs on the batch copy.

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
| M12 Teaching path and final audit | review | bb6b46a, M12.2 | 14 stubs, Exercises menu, README (no grading); P1 phase 1 below; EditMode 336/336 (non-T3), PlayMode 17/17 ⏸ |

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

Servers (2026-10-10): JEV (vLLM container promptevo-jev, localhost:8000, model jev-decision) and Ollama 0.35.1
(localhost:11434: qwen3.5:0.8b de63045f2975, gemma4:12b 6114515d63c1). Gates, on the copy:
`-executeMethod EvoSim.Integrity.RunIntegrity -checks B-02,B-03 -brain JEV [-cpu] [-judge gemma4:12b]`.
Results (reference scene, reports in Logs/EvoSim/Reports): B-01 pass (JEV 0.29 s); B-02 pass (sign 1.00, mean ΔP
0.281); B-03 pass (MI_G founders/random: prey 0.057/0.006, predator 0.062/0.004); B-04 pass (locality 0.135,
ρ 0.69, 33 edits); B-05 7 % of top answers change with reversed options; B-06 pass (463 of 1 024 tokens); B-07
pass (gemma4:12b on CPU, every sum 100); B-08 pass (P(flee) wolf 0.42, shadow 0.035); B-09 JEV rows differ by up
to 0.04 (reported); B-10 FAIL, 68 % usable after one mutation < 80 % (40 §2 #13); B-11 pass. About 4 500 calls.

## Decisions taken while implementing
M0–M11: in [51](51-implementation-log.md). Since then:
- 32 §2 B-02 — the 48 reference situations have no cover, so a hide gene could never matter — the hide pair's
  relevant situations get cover "close" (the Unity prey's own sense).
- 32 §2 B-08 — run on the reference scene with the predator renamed (wolf / shadow) through a field override.
- 32 §2 B-10 — the judge's {topic} per slot: the prototype's table (gene_timeline.TOPIC), "hide" = "when to hide in cover".
- 22 §0 — stubs are templates (`Exercises/My*.cs.txt`), not compiled in the package (the conformance suites would
  run them and fail); the menu writes them to Assets/Student. Stamina/Litter stubs derive from the reference (40 §2 #10);
  no grading: there is no criterion to reach (owner 2026-10-10, 40 §2 #14); TeachingPathGradingTests removed.

## Disagreements between the brief and Docs/Interface
- 20 §3.9 names the operator method `MutationOperator.Start(gene, parent, rng)`; on a MonoBehaviour Unity takes
  `Start` for its message and logs "Script error: Start() can not take parameters" for every operator. Renamed
  `StartMutation` (templates, samples, tests). The contract's text should follow.
Brief vs Interface: none found so far. Chapter B of the brief (the Claude Doc, tabs A–G) matches 20; it adds the run loop's
seven states (Idle, Running, Waiting, Blocked, Paused, Stepping, Stopped), now `RunState`.
`Docs/Interface/design.md` (chapter A) and `design.pdf` stay untracked (the owner's).

## Known problems
- 2026-10-10 13:54–16:05 — the editor froze after the pipeline's 5-minute timer cancelled a synchronous EditMode run;
  it recovered by itself (same process). Lesson kept: in the editor, run EditMode with `--async_tests true` and poll.
- GPU shared: the JEV container (restarted by Docker Desktop's restart policy) and another Python process of the
  owner's; the mutator runs on the CPU in Ollama (started for the checks).
- Untracked files that aren't EvoSim's and were left alone: `Assets/MobileDependencyResolver/`, two `.cs.meta`
  in `Assets/02 - Scripts/`, `Docs/Interface/design.md`, `design.pdf`.

## P1 audit, phase 1 (2026-10-10)
Four read-only audits; nothing changed. Severity: bug (proved), risk (could go wrong / weak test), doc. Details in 51.

| # | Rule | Finding | Evidence | Sev. | Proposed fix |
|---|---|---|---|---|---|
| 1 | ARCH-06, DEC-13, V-61 | A disabled, inactive or other World's brain is still used when pointed to: never initialized, validated (V-20/V-61) or in run_info | Species.cs:40, AskBrainsPhase.cs:95 | bug | V-10 unless the brain is an enabled service of this World |
| 2 | V-35 | Override `World/decisionPeriod=0` bypasses the setter's clamp: divide by zero at tick 0 | FieldPath.cs:27, World.cs:88, SensePhase.cs:29 | bug | Validate in Prepare + fixture |
| 3 | SPEC-30, CORE-08 | AddSpecies ignores its discovery report, validates nothing, keeps a duplicate display name | World.Species.cs:17-27 | bug | Unique name, validate the clone, stop on errors |
| 4 | RAND-20, TICK-04 | Strict mutator failure stops inside Hatch after Act/Breed: no Deaths/Record that tick | MutatorService.cs:128 | bug (strict) | Stop at the next boundary + test |
| 5 | MUT-03/05 | Gaussian at its range edge clamps to the parent value, counted as a success `{parent:X, child:X}` | GaussianMutation.cs:18, HatchPhase.cs:46 | bug | Result = parent → "unchanged" |
| 6 | 13 §5 | `backend_s` always 0: BrainMilliseconds never added to | SpeciesCounters.cs:16, RunRecorder.cs:306 | bug | Time each batch per species |
| 7 | SPEC-03 | Signature computed before the food web: an empty threat label hashes "Predator", later "Wolf" → false V-12 | Species.cs:183, World.cs:361 | bug (low) | Sign after DeriveRelations; reset maps |
| 8 | ARCH-12 | `Locomotion.MoveAll` never called; `ReadAll`/`ActAll` absent: the batch seam of 20 §10 is dead | Locomotion.cs:41, ActPhase.cs:147 | bug (low) | Call the batch methods or drop the seam |
| 9 | GENE-13 | Genome keys hash numbers at 4 decimals, the registry at the gene's precision (decimals 1) | Genome.cs:44, AlleleRegistry.cs:27 | bug (non-default) | Canonical(gene.Decimals) |
| 10 | 13 §2, OUT-05, V-36 | No stats.csv before statsEvery; compat `failures` overwritten by a species; V-36 checks only gene-bound traits | RunRecorder.cs:123,338; SpeciesBuilder.cs:51 | bug (minor) | Header in Begin; key order; range check in DeclareTrait |
| 11 | coverage gate | Citation regex credits test ids ("T-DEC-02" → DEC-02, 56 rules); the MUST regex misses TICK-04's form | CoverageTests.cs:28,45 | bug (test) | `(?<![A-Z]-)`; accept text after (MUST) |
| 12 | MUT-20 (S13) | S13 filters origin "mutation", mutants are "mutant": its checks assert nothing | ScenarioTests.cs:156,167 | bug (test) | Filter "mutant", assert non-empty |
| 13 | OUT-04 (T-OUT-03) | Sets OLLAMA_API_KEY that nothing reads; logs not scanned: can't fail | OutputPlayTests.cs:99-114 | bug (test) | Fake Ollama client on that variable; scan logs |
| 14 | EDIT-01 | Module prefabs never validated (StaminaGene, Senses/Carcass in no scene) | EditorToolTests.cs:36-44 | bug (test) | Prepare each prefab in a species |
| 15 | DEC-02 | No real test that a busy animal never decides (removing `\|\| a.IsBusy` passes) | SensePhase.cs:28, DecisionTests.cs:26 | risk | Busy across a decision tick |
| 16 | RAND-20, OUT-03 | Leaving Play doesn't Stop: runs < 5 000 ticks get no alleles.jsonl / final_population / summary | RunRecorder.cs:219-241 | risk (high) | Stop on World.OnDisable, or stream alleles |
| 17 | ARCH-06, GENE-05 | Disabling only an action leaves its gene in the genome and prompt (V-06 warning) | Ownership.cs:30 | risk | Gene of a disabled action is absent |
| 18 | EDIT-01 | MutatorService without a client: silent, every mutation fails | MutatorService.cs:47,91 | risk | Warning when a rate > 0 |
| 19 | ACT-04, ACT-31, ACT-13 | Conformance accepts Stay for Search; sequential visibility untested; eggs in act phase unseen | ConformanceTests.cs:137, ActPhase.cs:158 | risk | Stronger asserts (51) |
| 20 | SPEC-10/13 | An added species' strikes use the parent's Edible | Diet.cs:95, InteractionResolver.cs:57 | risk | Resolve from the struck species |
| 21 | ARCH-05, ACT-01, RAND-02 | Nested World undetected; duplicate sense labels / layer names / id = other name not validated | World.cs:303,349 | risk | New V-errors |
| 22 | OUT-04, V-61 | Key check misses gsk_/ghp_/AIza/hex and `?key=`; run_info leaves `-serial`, `-auth`, Bearer unmasked | HttpChecks.cs:9, RunInfo.cs:61 | risk | Wider patterns; all serialized strings |
| 23 | RAND-11, SPACE-12 | Views keep colliders on layer 2: a ray mask with it ties senses to rendering | ViewPool.cs:45, RayBatch.cs:53 | risk | No colliders on views; mask layer 2 |
| 24 | ACT-20, CORE-06 | Genome/LastSituation public; T-ACT-09 scans Runtime/Actions only; contextLine not checked single-line | Animal.cs:13, ActionTests.cs:233 | risk | Widen the scan; validate |
| 25 | EDIT-01/02, 21 §2 | Severities differ (V-20 warnings, V-02), codes reused; no severity asserts; 8 fix buttons of 25 | World.cs:324, ValidationCatalogueTests.cs:123 | risk | Own codes, assert (code, severity) |
| 26 | weak tests | ANIM-17, ANIM-35, SENSE-04, CORE-05, REPRO-03/24, GENE-03/06, RAND-05, V-60 TestConnection | 51 | risk | Strengthen (51) |
| 27 | SPEC-30, ACT-05, ARCH-07 | stats.csv columns fixed at row 1; mate ignores partnerRange < vision; mutable static lists in samples | RunRecorder.cs:123, WorldQueries.cs:74 | risk (low) | 51 |
| 28 | OUT-02, RAND-03, SPEC-05, GENE-12 | species_created has no id; one act-order stream; asymmetric nesting rule; llm#n is 0-based | SimEvent.cs:24, ActPhase.cs:57 | doc | Reword the contract |
| 29 | citations | ANIM-31, SPACE-04/06, SENSE-10…13, ENV-12, ACT-02, SPACE-13 cited by the wrong tests | 51 | doc | Fix descriptions |

Clean: no global or unseeded randomness, no deciding iteration over hash sets, ties by id, async answers applied by
index, Freeze = Responsive path, animals as data, MUT-11 prompt, TICK-04 and ACT-30/31/32 orders, no secret in the
repository, every genome of 8 real runs rebuilt from the events (no run with a mutation yet).

## Rule coverage
Every MUST rule of 01–22 is cited (CoverageTests, but see audit row 11). No real test: DEC-02. Weak only: ACT-04,
ACT-13, ACT-31, ANIM-17, ANIM-30 (half), ANIM-31, ANIM-35, CORE-05, SENSE-04, RAND-05, REPRO-03, GENE-03. Untested
parts: OUT-04 (logs), EDIT-01 (prefabs), EDIT-02 (fix buttons), REPRO-20, REPRO-24, GENE-32, MUT-13.
