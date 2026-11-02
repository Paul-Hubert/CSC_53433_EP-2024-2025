Implement the **neural prey and predator species** of EvoSim exactly as `Docs/Neuro/PLAN.md` describes them (milestone M16): a vector-allele weight genome, ray senses that stop at the first EvoSim object, five steering actions, a greedy neural brain beside JEV, and the prey-vs-prey and 2×2 survival comparisons. It is a long job over several sessions, so this prompt may be pasted many times: always start with **Where are we**.

## Settings

| Setting | Value |
|---|---|
| Branch | `llm-evolution`. `git pull` before you start and before every push; never force-push, never rebase pushed commits. The plan arrived with merge `0f3efd6`. |
| Unity | 6000.3.9f1. The commands that work on this machine are in `Docs/Interface/50-implementation-status.md` § Machine: the `unity` CLI drives the open editor; batch mode only when the editor is closed, or on the scratchpad copy of the project. |
| New code | `Assets/EvoSim/Neuro/` with its runtime, editor and test assemblies (plan §5). Core changes are only the ones of plan §4 (C1–C8) in `Assets/EvoSim/Runtime`, `Editor`, `Testing` and `Tests`. Scenes and scenarios go in `Assets/EvoSim/Scenes` and `Assets/EvoSim/Scenarios`. |
| Docs | The new chapter `Docs/Interface/16-neural-species.md`; the rule text plan §4 lists for 07, 08, 09, 13, 30 and 31; the site page `Docs/Site/extending/neural-species.md`; the status file. |
| Status file | `Docs/Interface/50-implementation-status.md` (under 200 lines; finished details move to `51-implementation-log.md`). |
| Model budget | M16.1–M16.8: no model calls except the T-NEURO-16 smoke runs (about 10 minutes of JEV each for S32 and S33); tell the owner before them. M16.9: the first measured run, then the other seven only after the owner's go. Cached answers are free. |
| Packages | None new. Burst and Collections (M16.8) only if profiling shows the managed path misses the budget, and only after asking the owner. |

## Context (read every session)

1. `Docs/Interface/50-implementation-status.md`, its **Next step** first.
2. `Docs/Neuro/PLAN.md`, the specification for this work.
   - §1 holds the owner's 29 decisions; they override the rest of the plan and anything older.
   - The other sections: §4 core changes, §5 classes, §6 scenarios and measurements, §7 validation codes, §8 tests, §9 milestones, §13 risks.
3. Before a milestone, read the contract chapters it touches in `Docs/Interface/`: 06 (senses), 07 (actions), 08 (brains), 09 (genes), 10 (mutation), 11 (reproduction), 12 (determinism), 13 (outputs), 20 (architecture), 22 (recipes), 30 (tests), 31 (scenarios). Also read the reference classes they describe.
4. The previous lab's network on `master` (`Assets/02 - Scripts/04 - Crowds and Evolution/`) is the inspiration. Don't port it.

## Rules

1. **The plan is the specification, the contract its frame.** Implement the plan and keep every MUST rule of `Docs/Interface/`. When the code disagrees with the plan (an API that isn't there, a case the plan missed):
   - choose the reading that keeps the owner's §1 decisions and every MUST rule;
   - write it in the status file ("Decisions taken while implementing") and in plan §12, then go on;
   - ask the owner at the next checkpoint.

   Never change an owner decision on your own.
2. **Existing runs don't change.** Every core change is additive. After M16.1, and at every milestone after it:
   - all EditMode tests pass (466 before M16);
   - the R-01 golden hashes are unchanged;
   - the existing scenes validate as before.
3. **Architecture and determinism**: Rules 2, 3, 5 and 6 of `Docs/prompts/06-implement-unity-system.md` apply:
   - no statics; animals are plain data;
   - modules keep no per-animal state (declare stats, as `HungerClock` does);
   - reference modules use only the public API a student has;
   - no `UnityEngine.Random`; every draw from a named stream; no dictionary iteration to make a decision;
   - no allocation per animal per tick in hot loops once a system works;
   - one public class per file, with doc comments citing contract rule ids and plan sections.
4. **Tests come with the code.** Write the T-NEURO tests of plan §8 (their ids in the test descriptions), the conformance-suite changes (C6), and the contract tests the core changes touch. Commit a step only when its tests pass.
5. **Commits**: one step = one commit, titled `EvoSim M16.<n>: <what>`, with the attribution lines your session requires. Push after each milestone (pull first).
6. **Output hygiene**: summaries of at most 20 lines; long output to `Logs/`; read logs with `grep` and `tail`; long runs in the background. Don't poll faster than the work moves.
7. **Unity**: never kill the editor. Before batch mode, check `Temp/UnityLockfile`. If the editor is busy or locked, use the CLI or the scratchpad copy, or ask the owner.
8. **Checkpoints (⏸)**: update the status file, commit, push, then stop and tell the owner "⏸ Checkpoint M16.<n> — <what to look at>". Wait for the answer.

## Where are we (start here each time)

1. `git checkout llm-evolution && git pull`, then `git log --oneline -10` and `git status`.
2. Read the status file. If it has no M16 rows yet, add M16.1–M16.9 to its milestone table, set **Next step** to M16.1, and commit.
3. Run the EditMode tests (status file § Machine) and read the summary. If something that passed before fails, fix it first.
4. Continue from **Next step**.
5. At the end of a session: update the status file (done, next step, decisions, known problems), commit, push, and say "✅ Session done — paste the prompt again to continue."

## Milestones (plan §9; the details are in the plan sections named)

### M16.1 — Core (plan §4, C1–C7)
- **Build:**
  - the `Vector` allele kind;
  - its recording, with the `vectors.jsonl` sidecar;
  - `DecisionQuery.Genome`;
  - `AnimalAction.SelfDirected` and `Brain.ReadsGeneText`;
  - the test-fixture changes (a vector probe gene, brain-suite companions);
  - recombinant alleles with a second parent.
- **Not now:** C8.
- **Docs:** rule text in 07, 08, 09, 13 and 30.
- **Tests:** T-NEURO-01, the updated conformance suites, and every existing test.
- **Done when:** everything passes and R-01 is unchanged. ⏸ (the owner reviews the core diff)

### M16.2 — Genes (plan §5.1–§5.3)
- **Build:** `NeuralLayout`, `NeuralNet`, `NeuralGenomeGene` (256 founders, one hidden layer of 12), `NeuralMutation` (the Gentle defaults), `NeuronCrossover`.
- **Tests:** T-NEURO-02…04.

### M16.3 — Senses (plan §5.4)
- **Build:**
  - `WorldRayCaster`;
  - `RayFanSense` (12 rays over 360°, everything occludes);
  - `InteroceptionSense` (12 inputs);
  - `HungerClock`;
  - `RayFanView`.
- **Tests:** T-NEURO-05, 06.
- **Done when:** the fan is visible in a sandbox scene.

### M16.4 — Brain (plan §5.6)
- **Build:** `NeuralBrain` (Greedy by default, `Sample` as a setting), synchronous path first.
- **Tests:** T-NEURO-07.

### M16.5 — Actions (plan §5.5)
- **Build:** `NeuralAction`, `TurnAction`, `ForwardAction`, `StopAction`; contact interactions; breeding both ways.
- **Tests:** T-NEURO-09…11.

### M16.6 — Templates and pairings (plan §5.7, §6.1)
- **Build:**
  - `NeuroPrey`, `NeuroPredator` and the menu;
  - `MatchedPairs` and the validation codes V-70…V-79;
  - the `Neuro_Lab` scene;
  - S29–S31 and their L0 versions;
  - `NeuroStats` and the inspector panel.
- **Tests:** T-NEURO-12, T-NEURO-15 (S29–S31).
- ⏸ (the owner watches S29 in Play mode)

### M16.7 — Comparison scenarios (plan §6.2–§6.3)
- **Scenes and scenarios:**
  - `Prey_vs_Prey` and `Brains_2x2` on Lab1_Full's map;
  - S33 and S32 with both arms (default rate, matched rate);
  - asexual, with the JEV mating-prompt override.
- **Analysis:** `SurvivalStats`, `PlateauStop`, `SurvivalAnalysis`, `SurvivalReportWindow`.
- **Docs:** chapter 16 and the site page.
- **Tests:** T-NEURO-13, 14, 15 (the L0 versions), and 16 (tell the owner first).
- ⏸ (the owner opens the Survival Report on the L0 and smoke runs)

### M16.8 — Concurrency and scale (plan §5.6, §9)
- **Build:**
  - the async `BrainAnswer.Later` path;
  - pooled attachments;
  - one fan per animal per tick;
  - a profile of S32 at its caps;
  - Burst only under the Packages setting.
- **Tests:** T-NEURO-08.
- **Done when:** the 20 §9 budget holds at 300 neural animals.

### M16.9 — Measured runs (plan §6.3)
- **First run:** S33, default rate, seed 1. Report its cost (seconds per tick, model calls) and its stop tick. ⏸ (the owner's go)
- **Then** the other seven runs:
  - resumable;
  - both seeds of an arm run to the same length;
  - one Survival Report per scenario and arm in `Logs/EvoSim/Reports/`.
- ⏸ (the owner reads the reports)

## Notes from the planning session (checked against 2fdd431)

**Test suites the new classes must pass:**
- The action conformance suite requires an action alone in an empty world to search or stay. C4 (`SelfDirected`) exempts the moving neural actions; `StopAction` passes through `Rests`.
- The mutation-operator suite requires an operator to accept a text or number gene. C6 adds a vector probe gene.
- The brain suite runs every brain class on the reference prey and predator. C6 adds the `[RequiresModule]` companions, and `NeuralLayout` one-hot-encodes token senses for that case.

**Engine behaviour to rely on:**
- `ActContext.Intent` has a private setter: actions plan with `WalkTo`/`RunTo`/`Stay`.
- Actions never set `a.Heading`; `ActPhase.AfterMove` turns the heading toward any non-wandering move.
- `SensePhase.Attachments` skips null attachments, so the brain maps attachments by `NeuralInput.SenseIndex`, not by position.
- `AskBrainsPhase.QueryGenome` implements the shuffled-genes control; C3 must expose that genome, not `a.Genome`.
- `HatchPhase` (mutation entries), `RunRecorder` (`alleles.jsonl`) and `RunOverlay` branch on "text, else number"; C2 adds the vector branch.
- Death events already carry `cause`, `age` and `killer` (`World.Deaths.cs`), and births name the species, so the survival analysis needs no core change.
- `Cover.Hides` uses a hiding-species list; the 2×2 must list both preys (V-79).
- `MatingRule.Sexual` already honours the World's asexual control, and `promptRule` is a serialized field the scenario can override.
- The sampling τ is world-wide; Greedy rows are immune to it.

**Free ids:** validation codes V-70…V-79 (V-61…V-63 are taken); scenarios S29–S33.

## What to report at each checkpoint

Five lines at most:
- what was built;
- the EditMode and PlayMode counts;
- what the owner should open or try in Unity;
- decisions taken or still open;
- the next milestone.
