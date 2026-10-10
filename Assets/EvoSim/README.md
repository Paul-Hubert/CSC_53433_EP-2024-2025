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

## 4. Rewrite a module, or invent one

There is nothing to reach and nothing grades you: change anything, replace any
module, invent senses, actions, genes, stats, phases, brains or mutation
operators, and see what evolves.

A good way in is to rewrite a module that exists, from the smallest
([22 §0](../../Docs/Interface/22-extending-recipes.md#0-the-teaching-path)):
`RestAction`, `LevelSense`, `Starvation`, `EatAction`, `NearestAnimalSense`,
`FleeAction`, `HideAction`, `Stamina`, `Litter`, `UniformCrossover`,
`KinematicLocomotion`, `HuntAction`, `RandomBrain`, `LlmMutation`.

1. Open a scene, then `EvoSim ▸ Exercises ▸ Replace with stub ▸ <module>`. It
   writes `Assets/Student/Exercises/My<Module>.cs` and swaps the module for it
   in the open scenes, keeping its settings.
2. Write the methods that throw, your own way. The doc comments say what the
   reference does; yours can do something else.
3. Press Play and watch. The conformance suite of the module's kind (Test
   Runner, EditMode) checks only that your class fits the system, for example
   that the same seed gives the same run.
4. `EvoSim ▸ Exercises ▸ Restore reference` puts the reference modules back;
   your files stay.

## Worth knowing

- A run repeats exactly with its seed only if every draw comes from the stream
  you are given (`RandomStream`), not `UnityEngine.Random` or the clock.
- The reference modules split the work this way: senses read, actions ask for
  an intent, phases change the world. Your modules may split it differently.
- Keep API keys in environment variables, never in scenes or code: scenes and
  run files get shared.
