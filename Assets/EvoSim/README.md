# EvoSim — evolving animals in Unity

Animals whose behaviour is written in English: each gene is a short sentence, a
language model reads the genes with what the animal senses and picks an action,
and the sentences mutate and cross over from one generation to the next. The
contract is in [`Docs/Interface`](../../Docs/Interface/) (start with
[22 — Extending](../../Docs/Interface/22-extending-recipes.md)).

## 1. Open a scene and run it

1. Open `Assets/EvoSim/Scenes/Lab1_Small.unity` (48 m, prey and predators).
   The other scenes: `Lab1_Full`, `HideVsFlee`, `ThreeSpecies`,
   `Terrain_Locomotion`, `Sandbox`.
2. The scenes ask the JEV brain (a model server at `localhost:8000`). Without a
   server, select the `World` object and set **Default Brain** to `Brains/Random`;
   everything else works the same.
3. Press **Play**. The play-mode gate stops you if the world has validation
   errors; each message names the object to fix.

## 2. Inspect

- Select the `World`: tick, state, populations, the validation panel.
- Select a species: its modules in order (senses, actions, genes, stats, rules)
  and the prompt its animals send (`PromptPreview`).
- `Window ▸ EvoSim ▸` *Animal Inspector* (click an animal), *Ecology Monitor*,
  *Event Log*, *Food Web*, *Gene Pool*, *Brain Health*, *Ask the Brain* (try a
  gene on a situation), *Scenario Runner* (the scenarios S00–S28).
- Runs write their files to `Logs/EvoSim/runs/`.

## 3. Extend

- `GameObject ▸ EvoSim ▸ New World`, `New Species`, `Add Action / Sense / Gene`
  build worlds from the menus.
- `Assets ▸ Create ▸ EvoSim ▸ Script ▸` writes a new sense, action, gene kind,
  stat, phase, brain or mutation operator into `Assets/Student`, with a test.
  The recipes are in [22](../../Docs/Interface/22-extending-recipes.md); the
  samples in `Assets/EvoSim/Samples/` (thirst, water, seasons, slopes…) are the
  recipes, written out.
- Every module you write is checked by the conformance suite of its kind
  ([30 §4](../../Docs/Interface/30-tests.md)): `Window ▸ General ▸ Test Runner`,
  EditMode.

## 4. The teaching path

First reimplement what exists, smallest first
([22 §0](../../Docs/Interface/22-extending-recipes.md#0-the-teaching-path)):
`RestAction`, `LevelSense`, `Starvation`, `EatAction`, `NearestAnimalSense`,
`FleeAction`, `HideAction`, `Stamina`, `Litter`, `UniformCrossover`,
`KinematicLocomotion`, `HuntAction`, `RandomBrain`, `LlmMutation`.

1. Open a scene, then `EvoSim ▸ Exercises ▸ Replace with stub ▸ <module>`. It
   writes `Assets/Student/Exercises/My<Module>.cs` and swaps the module for it
   in the open scenes, keeping its settings.
2. Fill in every method that throws. The doc comments say what each must do
   and cite the rules.
3. Run the EditMode tests: `TeachingPathGradingTests` runs the Lab scene with
   your class instead of the reference. A deterministic module must give the
   same run as the reference (the same events with the same seed); crossover,
   litter and mutation are checked by their properties. The conformance suite
   of the module's kind runs on your class too.
4. `EvoSim ▸ Exercises ▸ Restore reference` puts the reference modules back;
   your files stay.

Then invent: new senses, actions, genes, phases or brains, each with its tests.

## Rules worth knowing

- Never use `UnityEngine.Random` or the clock: draw from the stream you are
  given (`RandomStream`), so a run repeats exactly with its seed.
- Senses read, actions ask for an intent; only the phases change the world.
- API keys only from environment variables, never in scenes or code.
