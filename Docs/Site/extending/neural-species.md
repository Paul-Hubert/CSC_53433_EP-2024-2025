---
uid: extend-neural-species
---

# A neural species

EvoSim's brains need not read text. The `EvoSim.Neuro` assembly adds a species whose genome is the weights of a small
neural network, evolved by mutation and crossover like any other gene. Use it as it is, beside the JEV species, or as a
worked example of a brain that reads numbers: every part of it is an ordinary extension point.

## Adding one

1. Open a scene with a World that has a JEV `prey` or `predator`.
2. **GameObject ▸ EvoSim ▸ New Neural Species ▸ Prey** (or **Predator**). The new species copies the selected JEV
   species' body and life settings and gets its own senses, actions, genome and brain.
3. Add the new prey to the diets of the predators that should eat it. Validation warns (V-79) until you do.

The scenes `Neuro_Lab`, `Neuro_Terrain_Small` and `Neuro_Terrain_Large` are ready-made examples with neural species only (no JEV, no LLM, no model server); `Brains_2x2` and `Prey_vs_Prey` put them beside JEV species; select a neural animal in Play mode
and open **Window ▸ EvoSim ▸ Animal Inspector**: its **Network** section shows the inputs and the outputs.

## The parts, and the extension point each uses

| Part | Class | Extension point |
|---|---|---|
| The weights | `NeuralGenomeGene` | a [gene](genes.md) with vector alleles |
| Weight mutation | `NeuralMutation` | a [mutation operator](mutation.md) |
| Neuron crossover | `NeuronCrossover` | a [crossover](reproduction.md) |
| Ray fan, interoception | `RayFanSense`, `InteroceptionSense` | [senses](sense.md) with attachments (float arrays) |
| Turning, walking, stopping | `TurnAction`, `ForwardAction`, `StopAction` | self-directed [actions](action.md) |
| The forward pass | `NeuralBrain` | a [brain](brain.md) that reads attachments and the genome, not text |
| Meal clock, loop statistics | `HungerClock`, `NeuroStats` | [death rules](stats-and-death.md) that never kill, [stats columns](recording.md) |
| Survival columns, stop rule | `SurvivalStats`, `PlateauStop` | a [world service and a phase](world-modules.md) |
| The fan on screen | `RayFanView` | a [view](views.md) |
| The Network section | `NeuralInspectorPanel` | an `AnimalInspectorPanel` of the [editor](editor.md) |

## Comparing brains

`Window ▸ EvoSim ▸ Survival Report` reads the runs of S32 or S33 (one folder per seed) and shows how long animals of
each species live, what kills them, and the hazard ratio of each neural species against its JEV match. The method is
in chapter 16 of the interface documents.
