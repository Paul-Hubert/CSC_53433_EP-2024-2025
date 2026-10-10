# Real-time visual test: watch the scenes run, in Play mode and in a build

Paste it into a Claude Code session at the root of the Unity project; fill in the parts in <angle brackets> first.
It follows `06-implement-unity-system.md` (M0–M12 done): read `Docs/Interface/50-implementation-status.md` first.

---

Run the EvoSim reference scenes **in real time, with everything on** (brain, mutator, views), watch them, and
prove that what the screen shows is the run: the same events hash as a headless run with the same seed, smooth
motion, sensible behaviour, complete output files. Do it twice: in **Play mode** in the editor, then in a
**Windows build**. Make the scene watchable first if it isn't (today it mostly isn't, see Context).

## Settings

| Setting | Value |
|---|---|
| Branch | `llm-evolution`. Pull before you start and before every push; never force-push. |
| Unity | 6000.3.9f1, `C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe`; Windows build support is installed. |
| Scenes | <Lab1_Small, then Lab1_Full; add HideVsFlee, ThreeSpecies, Terrain_Locomotion if time allows> |
| Seeds and length | <seed 1234>; <600> ticks at level L0, <300> at L2; plus one run of at least 10 minutes for memory and pacing. |
| Levels | **L0** offline: random brain, no mutation (needs nothing). **L1**: JEV brain, no mutation. **L2** full: JEV brain and the gemma4:26b mutator. Go as far as the servers allow: <L0 / L1 / L2>. |
| Servers | JEV: `docker compose -f prototype/docker/compose.yaml up -d` (localhost:8000, GPU). Ollama (localhost:11434): mutator `gemma4:26b` on the CPU, about 19 GB of RAM. The machine has stopped processes for low memory before: close what isn't needed, keep one Ollama model loaded, check free memory before L2. |
| Model budget | Anything over 2 000 new model calls only after telling the owner the estimate (decisions per tick × ticks, minus memo hits; cached answers are free). |
| New code | `Assets/EvoSim/` only. Never edit `ProjectSettings/` (no Build Settings scene list, no Player Settings): build with `BuildPipeline.BuildPlayer` and an explicit scene list from a script in `Assets/EvoSim/Editor/`. |
| Outputs | Builds in `Builds/EvoSim/<date>/`, captures in `Logs/EvoSim/captures/<run>/`, reports in `Logs/EvoSim/Reports/` (all gitignored). |
| Status file | `Docs/Interface/50-implementation-status.md`: the results, the decisions and the commands that work go there; details in `51`. |

## Context (what exists today)

- **World run controls**: Play, Pause, Step; `RunSpeed` PerFixedUpdate, Fast, or RealTime (`realTimeTicksPerSecond`,
  default 5); `WaitMode` Freeze or Responsive (views keep moving while the tick waits for the brain); `StartOnPlay`
  (on); `TickLimit`, `WallClockMinutes`. `Stop` writes the run files; leaving Play stops the run (`OnDisable`).
- **Views**: each species' `Body` makes a pool of capsules (or its prefab), placed in `LateUpdate` and interpolated
  between the two tick positions. `AnimalView.OnShow(animal)` is the hook for colour or animation. Views only read
  (SPACE-12): no colliders, Ignore Raycast layer.
- **Not drawn today**: the flat-ground scenes have no ground mesh, and food items, cover, carcasses and eggs are
  invisible (only `Terrain_Locomotion` has a terrain and a water plane). The camera is fixed above the world at 60°.
  The editor windows (`Window ▸ EvoSim ▸` Ecology Monitor, Animal Inspector…) exist only in the editor, and a build
  has no overlay at all.
- **Headless reference**: `EvoSim.Batch.HashOf(scenePath, seed, ticks, configure)` and `Batch.RunScenarioNamed`; R-01
  pins hashes for S03.
- **Driving the open editor**: the `unity` CLI (commands in 50 §Machine): `eval`, `menu`, `editor_status`. Run
  anything long asynchronously and poll: a synchronous command that outlived the pipeline's 5-minute timer once
  froze the editor.
- **Run files**: `Logs/EvoSim/runs/<run>/` (events.jsonl, stats.csv, alleles.jsonl, final_population.json,
  summary.json, run_info.json).

## Rules

1. **The run is the truth; the picture only shows it.** Everything you add to see the world (ground, food, cover,
   overlay, camera, capture) only reads it: it never changes the World, draws no random numbers, and has no collider
   a sense ray could hit (SPACE-12, RAND-11). Prove it: the same seed and tick count give the same events hash with
   the new views on and off.
2. **Determinism**: no `UnityEngine.Random`, no unseeded `System.Random`, no time as randomness, no decision taken
   by iterating a hash set or dictionary (RAND-01…05).
3. **Never kill Unity.** If the editor is open (`Temp/UnityLockfile`), drive it with the CLI; run batch commands
   (builds, headless hashes) on a copy of the project, or after the owner closes the editor.
4. **Tests come with the code**: EditMode or PlayMode tests for what you add, using `EvoSim.Testing`, described like
   the others; the full suites stay green.
5. **No secrets** in code, scenes, captures, logs or the build: keys only from environment variables (OUT-04).
6. **A difference is a finding, not something to smooth over.** If a Play-mode or build hash differs from the
   headless one, find the first tick and event where they differ (the method of prompt P3), report it with its
   rule id, and don't re-pin anything.
7. Summaries of at most 20 lines; long output to `Logs/`; jobs over 2 minutes in the background.
8. **Commits**: one step = one commit, `EvoSim M13.<step>: <what>` (or the next milestone number in 50), with the
   session's attribution lines; push at the end (pull first). Ask before installing any tool (ffmpeg, Pillow…).

## Steps

0. **Where are we.** `git pull`; read 50; check whether the editor is open, which servers answer (`/health`,
   `/api/tags`) and how much memory is free. Choose the highest level you can reach and say which.
1. **Make the scene watchable**, adding only what is missing, as views:
   - a ground surface sized to the `FlatGround`;
   - food items and cover cells (instanced meshes, or a texture rewritten only when the layer changed), carcasses
     and eggs as small markers;
   - an overlay that also works in a build: tick, state ("running", "waiting for JEV: n queries, s"), ticks per
     second achieved against the target, FPS, population per species, births, deaths, mutations ok and failed;
   - a camera that frames the world at start, with pan, zoom and orbit; click an animal to follow it and show its
     action, stats and genes (read only);
   - a capture component: a PNG every <N ticks or seconds> to `Logs/EvoSim/captures/<run>/`, and a contact sheet
     made in Unity. Make a GIF or video only if ffmpeg is already installed.
   - Use the existing seams before new ones: Body prefabs and `AnimalView.OnShow` (for example colour by action or
     energy), world-module hooks. Keep each piece small and readable: students will copy it.
   - Tests: the views-change-nothing hash test (T-SPACE-06) also covers the new components; the overlay's numbers
     equal the World's counters.
2. **Headless reference.** For each scene and seed, `Batch.HashOf` in Freeze mode with the chosen tick count:
   record the hash and keep the run folder.
3. **Play mode, real time.** Open the scene; set RealTime at 5 ticks per second (then 20), Responsive for L1 and
   L2, the seed, and the tick limit; enter Play through the CLI and poll. Capture the start, the middle and the end,
   and open the captures to look at them. At the limit, compare the hash with step 2 (it must match) and check the
   run folder. Then leave Play once in the middle of a run: the files must still be complete (RAND-20, OUT-03). The
   editor slows down when unfocused: note the window state with each measurement.
4. **Windows build.** Add `EvoSim ▸ Build ▸ Visual Player` (also callable with `-executeMethod`): Windows 64, Mono,
   the chosen scenes, into `Builds/EvoSim/<date>/`. The player takes `-scene`, `-seed`, `-ticks`, `-speed`,
   `-brain`, `-capture` and `-quitAtEnd`, and runs windowed (not `-batchmode -nographics`: it must draw). Start it
   with `-logFile` (else the log is `%USERPROFILE%\AppData\LocalLow\DefaultCompany\test\Player.log`). Check where the
   run files and the answer cache (`Library/EvoSim/AnswerCache` in the editor) land in a build and make that
   sensible (next to the build) without changing the editor's paths. Compare the hash with step 2: same machine and
   Mono, it should match (RAND-12); if not, rule 6.
5. **What to check while watching.** For each check give evidence: a capture's file name, a number, or a log line.
   - Animals move smoothly between ticks, face where they go, stay on the ground (heights) and inside the world.
   - Each view is at its animal's position (at the tick the views show).
   - Predators chase, strike and kill; carcasses appear and are eaten; food is eaten and grows back; prey hide in
     cover and the hunter loses them.
   - Babies appear next to a parent; the dead disappear on their death tick; population curves look like the
     headless stats.csv.
   - Pacing: achieved ticks per second close to the target; at least 30 FPS with 340 animals (Lab1_Full). In
     Responsive mode the views keep moving while the brain answers, and no tick runs without its answers.
   - At L2: mutations happen (overlay, events) and the mutator's latency never freezes the frame.
   - No errors or warnings in the console or Player.log; memory flat over 10 minutes (pools, carcasses, captures).
   - Readable at 1920 × 1080 and in a small window.
6. **Report** in `Logs/EvoSim/Reports/RT-<date>.md`: levels, scenes, seeds and ticks; the three hashes (headless,
   Play mode, build) and whether they match; ticks per second, FPS, model calls and latencies; each check with its
   evidence; problems found (rule id, evidence, severity bug / risk / look, proposed fix); 6–12 representative
   captures and the contact sheet. Put the short version and the commands that worked in 50, details in 51.
7. **Checkpoint ⏸.** Commit, push, then tell the owner what to look at, for example "⏸ Open Lab1_Full and press
   Play (L2 needs JEV and Ollama), or run `Builds/EvoSim/<date>/EvoSim.exe -scene Lab1_Full -speed 20`", with the
   report's path. Wait for the answer.

Stop and ask the owner if making the scene watchable would need a change outside `Assets/EvoSim/`, if a server
or the memory isn't enough for the level asked, or if the call estimate goes over the budget.
