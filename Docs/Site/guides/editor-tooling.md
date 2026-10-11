---
uid: guide-editor-tooling
---

# Editor tooling

The Unity editor is where you meet EvoSim. Its tools have three jobs: show the configuration (orders, prompts, food
web), catch mistakes before Play, and make a running world readable (who does what, which genes spread). They all
live in the `EvoSim.Editor` assembly, under `Assets/EvoSim/Editor`; the simulation never depends on them (EDIT-03).
Most of them work on the configuration in Edit mode and on the running world in Play mode (EDIT-05). You find them in
the inspectors of the World and its species, under *Window ▸ EvoSim*, in the Scene view's **EvoSim** overlay, and in
the menus *GameObject ▸ EvoSim*, *Assets ▸ Create ▸ EvoSim* and *EvoSim*.

## Validation

One validation engine checks a world everywhere: in the inspectors, when you press Play, when the World initialises,
and in the tests (EDIT-01). It is <xref:EvoSim.World.Prepare(EvoSim.ValidationReport)>: the World finds its species and
modules, sets them up, and asks every module to check itself in its `Validate` method. Each problem becomes a
<xref:EvoSim.ValidationMessage> with:

- an **id** from the catalogue of 21 §2, such as V-04 (a species with no action) or V-30 (band edges out of order);
- a **severity**: *Error* stops the run from starting; *Warning* and *Info* never do;
- the **object at fault**, a module or a GameObject;
- a sentence that names the rule, and sometimes a **fix**.

### In the inspectors

The World and Species inspectors end with a validation list, drawn by <xref:EvoSim.Editor.ValidationPanel>. Each message
is a box coloured by severity that reads `V-04  Species 'prey' has no action (CORE-04).` Next to it:

- **Select** selects the object at fault in the Hierarchy and highlights it.
- A **fix button**, when the check offers one, labelled with what it does: *Add Rest*, *Rename*, *Move it up*,
  *Clamp*, *Add a text gene*, *Mark free*… Once the fix is applied, the list is checked again at once.

An empty list says *Validation is green.*

### The play-mode gate

When you press Play, <xref:EvoSim.Editor.PlayModeGate> validates every enabled World of the open scenes before Play
mode starts. If one has an error, Play is cancelled and the Console shows one line per error:

```text
EvoSim: Play blocked — V-04 Error [Lab1 World/Prey]: Species 'prey' has no action (CORE-04).
```

Click the line to select the object at fault. To use a fix button, select the World and press *Validate*. Warnings
and information never block Play: read them in the inspector.

## The World inspector

Select the World. Above its fields, a line gives its state: *Not initialised* before Play, then the run state and the
tick (*Running — tick 1204*). While the brains answer it adds what the world waits for, such as *waiting for jev: 12
queries, 1.4 s*, and once the run has stopped, why.

| Part | What it does |
|---|---|
| **Play**, **Pause**, **Step**, **Run N**, **Stop** | drive a running world (Play mode). *Step* runs one tick; *Run N* runs the number of ticks in the field next to it (100 by default) |
| the World's fields | seed, wait mode, run speed, decision period, default brain, stop conditions… ([Running and building](running-and-building.md)) |
| **Phases (in this order every tick)** | the children of the `Phases` object, numbered. A toggle switches a phase on or off; ▲ moves it up one place. Both work in Edit mode only and can be undone |
| **Services** | every world service below the World, by service name and class; *(off)* marks a disabled one |
| **Validation** | *Validate* in Edit mode; in Play mode *Show the last report*, the report of the World's initialisation |

## The Species inspector

Select a species. Below its own fields, press **Show the computed tables** (in Edit mode it validates the world to
compute them; in Play mode they show the running world). Then you see the **observation space** (how many different
situations the senses can describe), the **signature**, and these tables:

| Table | One line per… |
|---|---|
| Actions | action in order, with the gene that steers it, or *no gene (V-05)* |
| Senses, in situation-text order | sense: its label, its tokens, and the V1 and V2 text of its first token |
| Genes, in locus order | gene: locus id, kind, number of founders, mutation operator and rate |
| Stats and traits | stat or trait, the module that declares it; for a trait its default, range and whether it has a cost |
| Food | what the species eats (method, energy), whether it is edible itself, and the threats derived from the food web |
| Population | initial size, floor, cap and its rule, and the living count in Play mode |

### Prompt preview

The **Prompt preview** foldout shows the exact text a brain receives, built by the same code as the *Ask brains* phase
(<xref:EvoSim.Editor.PromptPreview>, T-PROMPT-05):

1. Pick a **Brain** among the World's brains, a **Founder genome** (the n-th founder sentence of each gene) and a **Text
   style** (V1 or V2).
2. Pick one token per sense, or press **Random situation**.
3. Press **Build**. The prompt appears with *About N tokens* and the brain's limit; **Copy** puts it on the clipboard.

For JEV the preview is its whole input text. The estimate counts about one token per four characters.

## The Scene view overlay

In Play mode, the Scene view draws over the running world. The **EvoSim** overlay (shown by default; the Scene view's
*Overlays* menu hides it) has the toggles:

| Toggle | Draws |
|---|---|
| Food items | a green circle on each food cell that holds an item |
| Cover | an outline on each cover cell |
| Carcasses, Eggs | a brown circle under each carcass, a yellow one under each egg |
| Action letters | the first letter of each animal's action above it, in the action's colour |
| Show *species* | one per species: hides or shows that species' letters |

Each action keeps the same colour in every window, set by its place in the species' action order. Select an animal's
body to see its vision circle and a line to its target. The toggles are kept in your editor preferences.

## Windows

| *Window ▸ EvoSim ▸* | Works in | Shows |
|---|---|---|
| **Animal Inspector** | Play | the animal whose body you click in the Scene view: age, generation, parents, stats, traits, each gene's sentence, current action (*searching* when nothing is in sight), busy reason, target, last situation, last probabilities as bars |
| **Ask the Brain** | Edit, Play | the brains' answers for a genome and a situation you choose |
| **Brain Health** | Edit, Play | each brain and mutator client: host, *Test connection* (*reachable* or V-60), tries, failures, latency in seconds; the answer cache's size and hits |
| **Ecology Monitor** | Play | population of each species over time; births, deaths by cause, decisions, memo hits, action shares |
| **Event Log** | Play | the run's events as they are recorded, filtered by kind, species or animal |
| **Food Web** | Edit, Play | who eats whom as a graph, the derived threats, the food-web messages (V-20, V-22, V-24, V-25) |
| **Gene Pool** | Play | for each locus, the share of each allele over time, the most common alleles, the lineage of one |
| **Scenario Runner** | Edit | the scenario assets: run their seeds headless and list the results |

### Ask the Brain

The place to see what a sentence does to the brain (labs C and I). In Edit mode press *Prepare the world* first.

1. Pick a species. Each text gene has a field: type a sentence, or press **pro**, **anti** or **neutral** to load the
   gene's contrast sentences. **Founders** puts back the first founders; **Control sentences** puts the World's
   control (random) sentences instead.
2. Pick one token per sense: the situation text appears below.
3. Tick one or more brains and press **Ask**. Each answer arrives as one line: the brain, a label, and P(action) for
   every action.

**Directed test (pro, anti)** asks twice, with the chosen gene's pro sentence and then its anti sentence: compare the
two lines for that gene's action. This is what the integrity check B-02 measures on every gene. Asking registers
nothing in the world, so it never changes a run.

### Gene Pool

Keep it open while the world runs: it samples the living animals every 20 ticks, so its chart starts when you open it.
A text gene shows its allele shares over time, the 12 most common alleles, and the lineage of the one you click back to
its founder. A number gene shows a histogram of the living animals' values.

### Scenario Runner

Pick a scenario asset and a variant, edit the seeds and the length (*-1* = the scenario's), and press **Run**. Each seed
runs headless, like `EvoSim.Batch.RunScenario` ([Running and building](running-and-building.md)): one line per run with
its ticks, events hash, model calls and run folder. **Run each seed twice and compare the hashes** checks that runs
repeat. Close the scenario's scene first: a scene open in the editor cannot be run.

## Building scenes

| Menu | What it does |
|---|---|
| *GameObject ▸ EvoSim ▸ New World* | a world of 48 m with the full tree: `Ground`, `Environment` (grass, thickets, carcasses, eggs), `Brains` (JEV, Random, Ollama points, answer cache, mutator), `Recording` and the eleven `Phases`. The random brain is the default, so it runs without a server |
| *GameObject ▸ EvoSim ▸ New Species* | with a World selected: a species with locomotion, energy, stamina, metabolism, a diet of grass, mating, litter, crossover, starvation, old age, cap, floor and staggered decisions. It has no sense and no action yet: V-04 asks for one |
| *GameObject ▸ EvoSim ▸ Add Action ▸* Eat, Flee, Hide, Follow, Rest, Mate, Hunt | with a species selected: the action with its text gene, under `Actions` |
| *GameObject ▸ EvoSim ▸ Add Sense ▸* Energy, Stamina, Food, Predator, Prey, Cover, Animal, Carcass, Age | the sense, under `Senses`; its object's name is its label |
| *GameObject ▸ EvoSim ▸ Add Gene ▸ Stamina gene* | a number gene for maximum stamina, under `Genes` |

The *Add* menus copy prefabs from `Assets/EvoSim/Modules`. A new world has no run views, so Play mode shows little:
copy the `Run views` object from a reference scene and add a <xref:EvoSim.WorldCamera> to your camera, or start from a
copy of `Sandbox`.

Three menus are for course staff, when the reference itself changes:

| Menu | What it does |
|---|---|
| *EvoSim ▸ Build Reference Scenes* | rebuilds the six reference scenes, allele pools, module prefabs and scenario assets from code (<xref:EvoSim.Editor.ReferenceScenes>), overwriting any edit made in them |
| *EvoSim ▸ Reference ▸ Update Reference Scenes* | brings the saved scenes to the reference decision period and staggered decisions, without rebuilding them |
| *EvoSim ▸ Views ▸ Add Run Views to Reference Scenes* | adds what is missing of the run views and rebuilds the two placeholder bodies ([Views and real time](views-and-real-time.md)) |

## Script templates

In the Project window, *Assets ▸ Create ▸ EvoSim ▸ Script ▸* asks for a class name and writes two files:
`Assets/Student/<Name>.cs` and its test `Assets/Student/Tests/<Name>Tests.cs`. Each compiles as it is.

| Template | Derives from | The script starts with |
|---|---|---|
| Sense | <xref:EvoSim.Sense> | three tokens (*none*, *near*, *far*), `Read` and `Write` |
| Action | <xref:EvoSim.AnimalAction> | a description line and an `Act` that searches |
| Gene kind | <xref:EvoSim.Gene> | a number gene that sets a trait from its founder values |
| Stat | <xref:EvoSim.SpeciesModule> | a stat declared with a start and a maximum |
| Phase | <xref:EvoSim.TickPhase> | an empty `Run` |
| Brain | <xref:EvoSim.Brain> | uniform probabilities |
| Mutation operator | <xref:EvoSim.MutationOperator> | a number mutation of ±1 |

The test derives from <xref:EvoSim.Testing.WorldFixture> and builds a small world in code that uses your class. The
first template also creates two assemblies: `Student` (it sees EvoSim, its HTTP brains and the samples) and
`Student.Tests` (editor only). *Assets ▸ Create ▸ EvoSim* also has **Allele Pool**, **Scenario** and **Mutation Deck**
(a copy of the reference deck to edit).

## Exercises and the player

*EvoSim ▸ Exercises ▸ Replace with stub ▸ 1 RestAction … 14 LlmMutation* replaces a reference module, in every open
scene, by an empty class of yours with the same settings; *EvoSim ▸ Exercises ▸ Restore reference* puts the reference
back. See [The teaching path](../extending/exercises.md).

*EvoSim ▸ Build ▸ Visual Player* builds a Windows player that shows runs of the six reference scenes, into
`Builds/EvoSim/<yyyy-MM-dd>/EvoSim.exe`. See [Running and building](running-and-building.md#player-builds).

## The main types

| Type | Role |
|---|---|
| <xref:EvoSim.Editor.WorldInspector>, <xref:EvoSim.Editor.SpeciesInspector> | the two custom inspectors |
| <xref:EvoSim.Editor.ValidationPanel> | the validation list with *Select* and the fix buttons |
| <xref:EvoSim.Editor.PlayModeGate> | validates the open worlds before Play |
| <xref:EvoSim.Editor.PromptPreview> | the exact prompt for a genome and a situation |
| <xref:EvoSim.Editor.EvoSimOverlay>, <xref:EvoSim.Editor.SceneDrawing> | the Scene view overlay and what it draws |
| <xref:EvoSim.Editor.EditorWorlds> | the worlds of the open scenes, the selected world and animal, the action colours |
| <xref:EvoSim.Editor.WorldFactory> | *New World*, *New Species*, the *Add* menus |
| <xref:EvoSim.Editor.ScriptTemplates>, <xref:EvoSim.Editor.Exercises> | script templates, the teaching path |
| <xref:EvoSim.Editor.ReferenceScenes>, <xref:EvoSim.Editor.RunViews> | the reference content and its views |

## Go further

- [The editor](../extending/editor.md): your own window or inspector, validation messages with fixes, your own *Add*
  menu.
- [Running and building](running-and-building.md): headless runs, batch mode, the player.
- [Views and real time](views-and-real-time.md): the run views, the camera, captures.
- [21 — Editor tooling](../spec/21-editor-tooling.md): the validation catalogue and the editor's rules.
