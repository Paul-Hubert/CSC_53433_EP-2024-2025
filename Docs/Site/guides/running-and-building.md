---
uid: guide-running-and-building
---

# Running and building

A world can run in four ways: in Play mode in the editor, headless from the editor, in batch mode with the editor
closed, or in a Windows player build. All four give the same events hash for the same scene, seed and brain answers
(see [Determinism and replay](determinism.md)). This page says when to use each and how.

| Way | Use it to | Speed | Shows the world |
|---|---|---|---|
| Play mode | watch, inspect, debug a module | as you set it | yes |
| Headless in the editor | get a run's numbers quickly while the editor is open | as fast as the brain allows | no |
| Batch mode | CI, scenarios, replays, long checks with the editor closed | as fast as the brain allows | no |
| Player build | long watched runs, demos, runs on another machine | as you set it | yes |

## The World's run settings

These fields of the <xref:EvoSim.World> decide how and how long a run goes. Every way of running reads them.

| Field | What it does | Reference |
|---|---|---|
| **Seed** | the run's seed: every random stream derives from it (RAND-02) | 1234 |
| **Wait Mode** | *Responsive*: while the brain answers, Unity keeps drawing and the tick resumes later. *Freeze*: the main thread blocks until the answers are in. Both give the same run (SPACE-14) | Responsive |
| **Run Speed** | *PerFixedUpdate*: a fixed number of ticks per physics step. *Fast*: as many ticks as fit in a frame budget. *RealTime*: a fixed number of ticks per second | PerFixedUpdate |
| **Ticks Per Fixed Update**, **Fast Budget Ms**, **Real Time Ticks Per Second** | the number that goes with each speed | 1, 10 ms, 5 |
| **Start On Play** | initialise and run when Play mode starts | on |
| **Tick Limit**, **Wall Clock Minutes** | stop after this many ticks, or minutes of real time (0 = never) | 0 |
| **Stop File** | stop at the next tick when this file exists | empty |
| **Stop On Extinction** | stop when a species has no animal left | off |
| **No Mutation** | switch mutation off (a control run) | off |

A brain answer that takes time never changes the result: the World waits for it, whatever the speed. The speed only
says how fast ticks go when nothing is waited for.

## Play mode

Open a scene and press Play ([Getting started](getting-started.md)). The run views draw the ground, the animals and the
panel; see [Views and real time](views-and-real-time.md) for the camera keys. To watch at a steady pace, set **Run
Speed** to *RealTime* and **Real Time Ticks Per Second** to 5 or 20.

## Run levels

Three levels name the usual combinations of brain and mutation:

| Level | Brain | Mutation | Servers needed |
|---|---|---|---|
| L0 | the random brain | off | none |
| L1 | the scene's brain (JEV) | off | JEV |
| L2 | the scene's brain (JEV) | the LLM mutator | JEV and Ollama |

`EvoSim.Editor.RealTimeRuns.SetLevel(world, "L0")` sets a world to a level. In batch mode, `-level` exists only for
`RealTimeRuns.FromCommandLine` (below). The player has **no** `-level` argument, and it ignores an argument it doesn't
know without a word: `EvoSim.exe -level L0` runs the scene as it is, which is L2, calling JEV and Ollama. Give the player
the equivalent arguments instead (`RealTimeRuns.LevelArgs`):

| Level | Player arguments |
|---|---|
| L0 | `-brain Random -noMutation` |
| L1 | `-noMutation` |
| L2 | none |

## Headless, from the editor

`EvoSim.Editor.RealTimeRuns` runs a scene without drawing it, in *Freeze* mode, and writes the run's files:

```csharp
// From an editor script, e.g. a menu item of your own: scene, seed, ticks, level, run name.
string line = EvoSim.Editor.RealTimeRuns.Headless("HideVsFlee", 1234, 600, "L0", "my-run");
// → "HideVsFlee L0 seed 1234: 600 ticks, hash 27c52857…, brain Random, brain calls 0, …, folder C:\…\Logs\EvoSim\runs\my-run"
```

`Headless` blocks the editor until the run ends. For a long run with model calls, `HeadlessStart` starts the same run
and advances it from the editor's update loop, one tick at a time for up to 50 ms per editor update, so the editor stays
usable; `HeadlessStatus` gives its progress, then its result line, which also goes to
`Logs/EvoSim/realtime-headless.txt`. One stepped run goes at a time. In both cases the scene must not be open in the
editor: the run opens the scene file itself, additively, and closes it without saving.

To run a scenario asset the same way, use the *Scenario Runner* window ([Editor tooling](editor-tooling.md#scenario-runner))
or `EvoSim.Batch.RunScenarioNamed("S03")`, which returns one summary line per seed.

## Batch mode

With the editor closed, Unity runs a static method and quits. From the repository root:

```powershell
$U = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
& $U -batchmode -nographics -projectPath . -executeMethod EvoSim.Batch.RunScenario -scenario S03 -seed 7 -ticks 2000 -quit
```

| Method | Arguments | What it does |
|---|---|---|
| `EvoSim.Batch.RunScenario` | `-scenario S03 [-variant name] [-seed n] [-ticks n] [-responsive]` | runs one scenario asset (S00–S27), every seed of it unless `-seed` is given; one run folder per seed, `Logs/EvoSim/runs/<scenario>-<variant>-s<seed>-<time>/`, with `run_info.json`, `events.jsonl`, `stats.csv`, `alleles.jsonl`, `final_population.json` and `summary.json` |
| `EvoSim.Batch.RunRegression` | | runs the EditMode tests of category T3 (pinned hashes, equivalences, scene checks); totals in `Logs/EvoSim/Reports/regression-<time>.txt` |
| `EvoSim.Batch.RunScenarios` | `-tier T2\|T3\|T4 [-ticks n]` | runs every scenario variant of a tier, each seed twice, and checks both runs give the same hash; `Logs/EvoSim/Reports/scenarios-<tier>-<time>.txt` |
| `EvoSim.Batch.RunIntegrity` | `[-checks B-02,B-03] [-scene path] [-brain JEV] [-cpu] [-judge gemma4:12b]` | the brain and mutator checks B-01–B-11 (32 §2); without `-checks`, all but B-07 and B-10. One JSON and one Markdown report per check |
| `EvoSim.Editor.RealTimeRuns.FromCommandLine` | `-do headless\|behaviour -scene HideVsFlee -seed 1234 -ticks 600 -level L2 [-run NAME] -quit` | a headless run, or the behaviour probe (hiding, kills, food, babies); the result line is appended to `Logs/EvoSim/realtime-batch.txt` |
| `EvoSim.Editor.VisualPlayerBuild.BuildFromCommandLine` | `[-scenes A,B] [-out folder]` | builds the Windows player (default: the six scenes, into `Builds/EvoSim/<yyyy-MM-dd>`) |

Reports go to `Logs/EvoSim/Reports`, run folders to `Logs/EvoSim/runs`. The `Batch` methods and `BuildFromCommandLine`
exit Unity themselves: 0 on success, 1 when a run, a test, the build or a blocking check (B-01) failed, and 2 for
`RunScenario` and `RunIntegrity` on an error such as an unknown scenario. `FromCommandLine` does not exit by itself:
pass `-quit`. Its exit code is then 0 whatever happened, so read its result line.

> [!NOTE]
> Batch mode cannot open a project that an open editor holds: close the editor first.

## Player builds

The player is not in the repository: `Builds/` is ignored by git, so build it yourself the first time.
*EvoSim ▸ Build ▸ Visual Player* builds a Windows 64-bit player of the six reference scenes, HideVsFlee first, into
`Builds/EvoSim/<yyyy-MM-dd>/EvoSim.exe`, and shows the result in a dialog. The build never edits the project's Build
Settings: it passes its own scene list. It needs the Mono scripting backend for Standalone, and stops with a message
rather than change Player settings. From the open editor, `EvoSim.Editor.VisualPlayerBuild.BuildLater()` starts the same
build and writes its result to `Logs/EvoSim/visual-player-build.txt`.

The player takes its settings from the command line. They are applied to the World before it starts:

```powershell
EvoSim.exe -scene HideVsFlee -seed 1234 -ticks 600 -speed 20 -capture 10 -run demo -window 1920x1080 -quitAtEnd
```

| Argument | Meaning |
|---|---|
| `-scene NAME` | the scene to load (default: the first one, HideVsFlee) |
| `-seed N` | the run's seed |
| `-ticks N`, `-minutes M` | stop after N ticks, or M minutes of wall-clock time |
| `-speed X` | X ticks per second (RealTime), or `fast`, or `fixed` (PerFixedUpdate) |
| `-wait responsive\|freeze` | the wait mode |
| `-brain NAME` | the default brain, by GameObject name or brain id (`-brain Random`) |
| `-noMutation` | switch mutation off |
| `-stagger` | every species decides staggered (DEC-04) |
| `-capture S` | a screenshot every S seconds (`0`: start and end only, `off`: none) |
| `-cache FOLDER` | the answer cache to read and write |
| `-run NAME` | the run folder's name |
| `-window WxH` | a window of that size instead of full screen |
| `-quitAtEnd` | quit when the run stops, and log the events hash |

Run files, captures and the answer cache land next to the exe: `Logs/EvoSim/…` and `Library/EvoSim/AnswerCache`. To
replay a live run without calling the model again, give the player the cache the live run wrote (`-cache`). To replay
a player run headless in the editor, see [Replay a player run in the editor](determinism.md#replay-a-player-run-in-the-editor).
