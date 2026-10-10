# 50 — Implementation status
Updated: 2026-10-10, session 1

The single source of truth between implementation sessions of
[`Docs/prompts/06-implement-unity-system.md`](../prompts/06-implement-unity-system.md).

## Next step
⏸ Waiting for the owner's answer at checkpoint M0 (packages and layout).
Then **M1.1**: read 01, 02, 12, 20 §1–§5, 04 §1 and chapter B of the brief
(`Docs/Interface/design.pdf`, or the Claude Doc), then build `RandomStreams`
(PCG32 seeded from SHA-256 of seed and name) with T-RAND tests, and go on with
`World`, `WorldModule`, `TickPhase`, `Species`, discovery and the run loop.

## Milestones
| M | State | Commit | Notes |
|---|---|---|---|
| M0 Project check and skeleton | done, ⏸ checkpoint | e6ef6fc, 7536724 | compile clean; EditMode 2/2, PlayMode 1/1 from the CLI |
| M1 Core and the tick loop | next | | |
| M2 Space, ground, environment | | | |
| M3 Animals, metabolism, locomotion, actions, food web | | | |
| M4 Senses and observations | | | |
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

## Disagreements between the brief and Docs/Interface
None found yet. The brief is in the working tree, untracked:
`Docs/Interface/design.md` (chapter A only) and `Docs/Interface/design.pdf`
(about 8 pages); the full brief is also the Claude Doc with tabs A–G. To read
before M1.

## Known problems
- Untracked files that aren't EvoSim's and were left alone:
  `Assets/MobileDependencyResolver/` (+ `.meta`), two `.cs.meta` files in
  `Assets/02 - Scripts/`, `Docs/Interface/design.md`, `design.pdf`.

## Rule coverage
Covered by a passing test: EDIT-03 (T-EDIT-02).
Every other MUST rule: not yet implemented (M1–M11).
