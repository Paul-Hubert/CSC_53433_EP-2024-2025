# 50 — Implementation status
Updated: 2026-10-10, session 1 (continuing without checkpoint stops, owner's instruction)

The single source of truth between implementation sessions of
[`Docs/prompts/06-implement-unity-system.md`](../prompts/06-implement-unity-system.md).

## Next step
M4.1: senses (Sense, BatchedSense, Bands, LevelSense, AgeSense, NearestResourceSense, NearestAnimalSense with
readiness, NearestCoverSense), V1/V2 texts in meters, observations and their space size; tests T-SENSE-01…10.

## Milestones
| M | State | Commit | Notes |
|---|---|---|---|
| M0 Project check and skeleton | done | e6ef6fc, 7536724 | compile clean; EditMode 2/2, PlayMode 1/1 from the CLI |
| M1 Core and the tick loop | done | 2bacd0c | S00 1 000 ticks, same hash in both wait modes; T-TICK-03, T-RAND-04, T-CORE-06/07/10 finish later |
| M2 Space, ground, environment | done | 1aed10e | EggSystem/Egg/MutationJob built here (needed by diets) |
| M3 Animals, metabolism, locomotion, actions, food web | done | 7a6688a | EditMode 83/83; T-ANIM-01/09 (migration), T-MOVE-05, T-SPEC-01/02 come with M5–M6 |
| M4 Senses and observations | next | | |
| M5 Genes, decisions, prompts | | | |
| M6 Reproduction, population, non-LLM mutation, controls | | | |
| M7 Outputs, configuration, batch runs | | | ⏸ |
| M8 LLM services | | | ⏸; model calls allowed from here |
| M9 Reference content and views | | | ⏸ |
| M10 Editor tooling | | | ⏸ |
| M11 Samples, scenarios, verification, CI | | | ⏸ |
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

## Disagreements between the brief and Docs/Interface
None found so far. Chapter B of the brief (the Claude Doc, tabs A–G) matches 20; it adds the run loop's
seven states (Idle, Running, Waiting, Blocked, Paused, Stepping, Stopped), now `RunState`.
`Docs/Interface/design.md` (chapter A) and `design.pdf` stay untracked (the owner's).

## Known problems
- Untracked files that aren't EvoSim's and were left alone:
  `Assets/MobileDependencyResolver/` (+ `.meta`), two `.cs.meta` files in
  `Assets/02 - Scripts/`, `Docs/Interface/design.md`, `design.pdf`.

## Rule coverage
Covered by passing tests so far: EDIT-03; CORE-02/03/05/09 (part); SPEC-02/03/04/05/10/11/12/13/15;
ARCH-05/06; GENE-05; TICK-01/02/03/07; RAND-01/02/03/04/05/10/11/13/20; SPACE-01…04/06/07/08/10/11/13/14;
ENV-01…04/10/11/12/20/22/23; ANIM-10/11/15/16/17/20/21/22/30/31/35/36/40/41/42/43; ACT-01…06/10…13/20/30/31/32;
MOVE-01…07; REPRO-23 (part).
Still without a test: everything from M4 on, and the parts listed in the milestone notes.
