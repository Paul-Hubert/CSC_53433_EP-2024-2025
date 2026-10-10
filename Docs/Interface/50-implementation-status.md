# 50 — Implementation status
Updated: 2026-10-10, session 1 (continuing without checkpoint stops, owner's instruction)

The single source of truth between implementation sessions of
[`Docs/prompts/06-implement-unity-system.md`](../prompts/06-implement-unity-system.md).

## Next step
M12.1: teaching-path stubs (Assets/EvoSim/Exercises/My*.cs.txt), the Exercises menu, the grading suite and the
students' README; then M12.2: the P1 audit, phase 1 (table below), and the owner's final review.
The editor answers again (16:05): EditMode runs go through it with `--async_tests true` (scratchpad evo.sh);
long and model runs still use the batch copy (copyrun.sh).

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
| M12 Teaching path and final audit | | | ⏸ |

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
# EditMode (T0, T1): synchronous
unity command --timeout 330 --result-only run_tests --mode editor \
  --filter EvoSim.Tests.EditMode --filter_type assembly --timeout 300 > Logs/editmode-cli.json
# PlayMode (T2): must be async (entering play mode reloads the domain), then poll
unity command --result-only run_tests --mode playmode \
  --filter EvoSim.Tests.PlayMode --filter_type assembly --async_tests true
unity command --result-only test_status > Logs/playmode-cli.json   # until "status": "completed"
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
- M0 — tests from the command line — the editor is open, so batch mode can't
  run (rule 8); the owner asked to use the `unity` CLI and com.unity.pipeline
  instead. Tests run in the open editor with the commands above.
- M0 — the owner's upgrade to 6000.3.9f1 was uncommitted — committed as
  M0.1 together with the explicit Newtonsoft package, so the lock matches
  the manifest.
- 20 §7 — assemblies — `EvoSim.Testing` is constrained to
  `UNITY_INCLUDE_TESTS` (NUnit for `Stat`), so it never ships in a build;
  Runtime and Http are auto-referenced so student scripts in
  `Assembly-CSharp` can derive from the base classes.
- 22, rule 7 of the prompt — samples — `Assets/EvoSim/Samples/` will get its
  own `EvoSim.Samples` assembly (from M11), so the test assemblies can
  reference the sample modules; 20 §7 doesn't list it.

- RAND-13 — an empty world has no events, so equal hashes for any seed — the events hash chain starts from
  SHA-256 of the seeds: h0 = SHA-256("evosim:seed:worldSeed"), h(i) = SHA-256(h(i-1) ‖ line i).
- RAND-03/04 — which streams the world seed drives — "world" and every "world/<layer>" stream (cover map,
  initial food), so adding a layer never changes another layer's map.
- GENE-01 — duplicate gene labels in a species aren't in the V-catalogue — added V-14 (error).
- SENSE-22 / ACT-05 — who declares "vision" — banded senses declare the trait "vision" (default 20); a species
  without one uses 20 m (WorldQueries.ReferenceVision).
- ACT-10 — reach comparisons — within reach means distance ≤ reach + 1 mm (Units.Epsilon), so an animal that stopped
  at exactly 1 m interacts; the reaches (graze 0.5, strike 1, scavenge 1) are settings of the ActPhase, the
  mate reach of the MatingRule.
- ANIM-30 — when the busy countdown starts — in the tick after the animal became busy (a kill at tick t keeps
  the hunter busy for ticks t+1…t+50; it decides at t+51).
- ANIM-40 — kill cause name — "killed" in events; compatibility mode will write "predator" (OUT-05).
- GENE-22 — "letters" in the allowed characters — ASCII letters only, so "é" fails V-40 (T-GENE-07).
- CORE-03 — Species' default display name — "species" (not "prey") so core code names no species (T-CORE-08).
- 20 §3.6 — action prompt lines — `AnimalAction.DefaultDescription` holds each reference action's line from
  08 §7; the inspector field overrides it when not empty.
- 20 §3.4 — the inspector's "current target" — Animal.TargetId/TargetPoint, set by the act phase (also used by
  the "chased" sense recipe, 22 §13).

- SENSE-22 — per-animal vision in texts — each text states the trait's default vision (one text per token, so
  equal observations give equal texts, SENSE-10); per-animal vision changes the band, not the wording.
- SPEC-20 — threat/prey sense labels — empty label = the species' display name when the set has one species
  ("Predator" for `predator`), so renaming a species changes what the brain reads; else "Predator"/"Prey".
- 15 §2 — the 48 situations — the prototype's own set (prototype/data/observations_v2.jsonl, copied to
  Data/Observations), mapped onto the Unity prey (stamina high, cover none where it had none); the predator's
  48 are energy × prey band × carcass × kin.
- PROMPT-01 — line breaks — the reference texts keep the prototype prompts' hard wraps (JEV was trained on
  them); the 08 §7 example is reproduced word for word (T-PROMPT-01).
- PROMPT-01 — rule order — modules in hierarchy order; the search rule sits on the species' own GameObject (first);
  the carcass line is written by the diet that scavenges, so it comes before the breeding line as in predator_v3.
- T-SENSE-08 — golden file name — Tests/Golden/situations.tsv (tab-separated, easier to review) instead of .json.
- DEC-04 — staggering and per-species periods — an optional DecisionSchedule module.
- MUT-31 — batching rounds — one mutator batch in flight at a time; queued tries (new first tries and redraws)
  go out together when it returns. Results don't depend on batching (tries are drawn at conception).
- MUT-12 — guard order — as the prototype: "invalid" (empty, > maxWords, same as parent ignoring case, forbidden
  characters) before "unchanged" (punctuation only). T-MUT-07 checks against the prototype's own clean().
- REPRO-10 — block cap — the litter is cut to cap − living animals (eggs don't count, REPRO-22).
- 22 — Samples — reference module defaults may name species in tooltips; T-CORE-08 checks Core, Phases, Recording.
- OUT-05 — the prototype's names ("predator" kill cause, "pred_" columns, "<id>s" summary key) — kept in
  Runtime/Compatibility/PrototypeFormat, outside the core folders (T-CORE-08).
- CFG-01 — "range or unit" for numbers — T-CFG-01 accepts Range/Min or a reference value in the tooltip.
- 20 §2 — building scenes while the editor holds an unsaved untitled scene — the builder writes an empty scene file
  and opens it additively (NewScene additive is refused then); the owner's open scene is never touched.
- R-01 — the scripted prey policy — flee a close threat, else eat food in sight, else mate with a ready kin, else
  rest (2 % on the others); hashes pinned from the first run.
- DEC-30 / R-06 — memo memory in long runs — keys stored as 128-bit hashes, identical rows shared, at most
  World.memoCapacity keys (262 144); beyond, queries go to the brain (and its cache) again (40 §2 #11).
- R-07 — per-tick allocations — module look-ups cached per species, species streams cached without building names,
  carcasses pooled, index loops on the act path, spatial cells made once: full world 1.5 ms per tick, no allocation
  in the act phase (Unity's Mono counts no per-thread allocations: R-07 reads the managed heap between two probes).
- Answer cache folder — Library/EvoSim/AnswerCache (gitignored; deleting Library loses it); the file is
  .jsonl per brain ("mutator-<model>" for the mutator), rows stored with round-trip precision.

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

## Rule coverage
Every MUST rule of 01–22 is cited by a passing test (CoverageTests.EveryMustRuleIsCited, from M11.3). Whether each
citing test really checks its rule is item 1 of the P1 audit (M12.2).
