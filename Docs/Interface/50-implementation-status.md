# 50 — Implementation status
Updated: 2026-10-10, session 1 (continuing without checkpoint stops, owner's instruction)

The single source of truth between implementation sessions of
[`Docs/prompts/06-implement-unity-system.md`](../prompts/06-implement-unity-system.md).

## Next step
M11.2: scenario assets S00–S28 with their checks (exact facts through Batch.RunScenarios; directional ones as T3/T4
tests), regressions R-02…R-08, integrity checks B-02…B-10, the coverage EditMode test (every MUST rule of
30-tests.md cited by a passing test). Then M12 (teaching-path stubs and the Exercises menu, README, P1 audit).
The editor is still frozen (Known problems): tests run in batch mode on a copy (scratchpad copyrun.sh: sync,
build, edit, play, method); generated assets and new .meta files are copied back before committing.

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
| M11 Samples, scenarios, verification, CI | in progress | e5f402d | samples, conformance suites, CI entry points, workflow, prompts; EditMode 275/275, PlayMode 17/17 |
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
**Not run yet on this machine** (the editor was open the whole session).
Scenario runs in the open editor (same code as `-executeMethod EvoSim.Batch.RunScenario -scenario S03`):
`unity command --timeout 900 --result-only eval --code 'return EvoSim.Batch.RunScenarioNamed("S03");'`
(optional variant, seed, ticks); files go to `Logs/EvoSim/runs/<scenario>-<variant>-s<seed>-<time>/`.
Rebuild the reference scenes and pools: `unity command --result-only menu --path "EvoSim/Build Reference Scenes"`.

LLM servers: not checked yet (not needed before M8).

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
- 2026-10-10 13:54 — the Unity editor froze (main thread at 100 %, no log output) after the pipeline's own
  5-minute test timer cancelled a synchronous EditMode run that was still going; every CLI command times out.
  It needs a restart by the owner (never kill Unity). Meanwhile everything runs in batch mode on a copy of the
  project in the scratchpad. Lesson: in the editor, run EditMode with `--async_tests true` and poll.
- GPU shared: the JEV container (restarted by Docker Desktop's restart policy) and another Python process of the
  owner's; the mutator runs on the CPU in Ollama (started for the checks).
- Untracked files that aren't EvoSim's and were left alone: `Assets/MobileDependencyResolver/`, two `.cs.meta`
  in `Assets/02 - Scripts/`, `Docs/Interface/design.md`, `design.pdf`.

## Rule coverage
Covered by passing tests so far: EDIT-03; CORE-02/03/05/09 (part); SPEC-02/03/04/05/10/11/12/13/15;
ARCH-05/06; GENE-05; TICK-01/02/03/07; RAND-01/02/03/04/05/10/11/13/20; SPACE-01…04/06/07/08/10/11/13/14;
ENV-01…04/10/11/12/20/22/23; ANIM-10/11/15/16/17/20/21/22/30/31/35/36/40/41/42/43; ACT-01…06/10…13/20/30/31/32;
MOVE-01…07; REPRO-23 (part).
M4–M6 added: SENSE-01…06/10…13/20/21/30/31/41; DEC-01…03/10…15/20/21/30…34/40; PROMPT-01…05/07; GENE-01…06/10…13/20…24/30…32;
MUT-01…05/10…14/20/22/30…33; REPRO-01…05/10…13/20…24; POP-01…04; SPEC-01/06/30…32; CTRL-01/02; CORE-04/06/07/09.
M7 added: OUT-01…05, CFG-01…04, RAND-06/20/21, EDIT-01 (part: every scene validates).
M8–M11.1 added: DEC-41, DEC-42 (client), PROMPT-06, EDIT-01/02 (catalogue, play-mode gate), SPACE-12 (views),
MUT-21 (ladder sample), OUT-04 (keys only from the environment, V-61).
Still without a test: RAND-22, ENV-21 (moving entities, S18 sample), SENSE-40 camera (S24 sample); the full list
comes from the coverage test (M11.2).
