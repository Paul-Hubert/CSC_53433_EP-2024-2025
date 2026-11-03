# Plan — neural prey and predators beside JEV, and a 2×2 brain comparison (M16)

Status: **plan, not started.** Written 2026-11-02 against `llm-evolution` at 2fdd431;
revised the same day with the owner's answers (§1, 29 decisions). Nothing open (§12).

## 0. In one paragraph

Bring back the previous lab's animal: a small neural network that sees through a fan of rays,
whose weights are its genes, and that only steers. Make it a proper EvoSim brain + gene
system in a separate assembly (`EvoSim.Neuro`), with **two species templates, a neural prey
and a neural predator**. Their bodies match the JEV reference prey and predator, so the only
difference is the controller. Each species keeps its own brain (DEC-13), so neural and JEV
species share a world: the network answers locally while JEV's HTTP batch is in flight. The
neural animal has **five actions** (turn left, turn right, walk, run, stop). **Each ray
reports the first EvoSim object it hits**. The **weights are one long vector allele** whose
length follows the observation and action sizes. **Mutation is its own class of random float
changes**. Breeding is **sexual or asexual**, a setting. Scenarios cover every pairing of
prey and predator brains. The big one puts **two prey species and two predator species
(JEV and neural for each role) in one world, a 2×2 design that compares survival rates**.
A prey-vs-prey world does the same without predators. Those two are measured with 2 seeds
each, from random neural founders, until a plateau rule says the run is long enough.
Everything is C#, deterministic, and kept within the EvoSim contract. The contract needs eight
small additions to core (§4), and none of them changes an existing run.

## 1. Owner decisions (2026-11-02)

| # | Question | Answer | Where |
|---|---|---|---|
| 1 | How to store the weights | **One allele of a new `Vector` kind**: one long `float[]` per animal, one gene component (never a GameObject per weight). Its length is derived from the observation size and the action count | D2, §5.1 |
| 2 | Actions | **5 actions: `left`, `right`, `walk`, `run`, `stop`.** Turning has a single speed; walk and run go straight; stop stays | D1, §5.5 |
| 3 | Decision and tick rate | **Tick speed is a variable in a component**, never hard-coded. Each species' decision period is its `DecisionSchedule.period` (neural default 1, staggering optional). The World's run speed (Fast / RealTime ticks per second) stays the World's setting. Results never depend on the run speed (SPACE-11) | D4, §6 |
| 4 | Breeding | **Both sexual and asexual**, a setting of the species' `MatingRule`; sexual babies get a per-neuron crossover | D5, §5.3 |
| 5 | What a ray hits | **The first EvoSim object** along it: an animal, a resource item, an entity (carcass, egg, any `EntitySystem`), cover, or the ground's edge, water or non-walkable cells. No Physics colliders | D3, §5.4 |
| 6 | Roles | **Prey and predators both, with JEV or neural brains**: a neural prey template and a neural predator template; scenarios for every pairing | D7, §6 |
| 7 | Mutation | **A separate class, random float mutations** (`NeuralMutation`), apart from the LLM and Gaussian operators | §5.2 |
| 8 | Turning | **The defaults are fine, and they must be tunable**: turn angle and turn speed are Inspector settings of the turn actions | §5.5 |
| 9 | The big scenario | **Two prey species and two predator species, JEV and neural for each role (2×2), in one world, to compare survival rates** | D8, §6.2 |
| 10 | Neural founders in the comparisons | **Random networks** (the naive start); no pre-evolved pool | §6.2 |
| 11 | Decision rate in the comparisons | **Both**: the neural default (every tick) as the main arm, and the matched-rate arm (period 4, staggered, like JEV) as a variant | §6.2 |
| 12 | Measured runs | **2 seeds, run long enough, only for the prey-vs-prey and 2×2 scenarios**; the other pairings are functional checks | §6.3 |
| 13 | Prey vs prey | **Only prey**: JEV prey and neural prey, no predators (S33) | §6.1 |
| 14 | Mating in the measured runs | **Asexual for both species, for now**; sexual runs later | §6.3 |
| 15 | A "same interface" arm (the neural brain on JEV's senses and actions) | **Never** | §6.2, §11 |
| 16 | Breeding as a choice for the neural species | **No**: it breeds whenever it is ready (every action mates), as in the old lab | D5, §6.3 |
| 17 | World of the measured runs | **Lab1_Full**: per species prey 136 initial / cap 270, predators 28 / cap 68 | §6.2 |
| 18 | Floor in the measured runs | **On**, and the newcomers each species needed are counted as a result | §6.2 |
| 19 | Grass with two prey species | **×2 regrowth** | §6.2 |
| 20 | Headline survival measure | **The death hazard ratio** (Kaplan–Meier survival by age), JEV vs neural within each role, in the late windows | §6.2 |
| 21 | Policy of the neural brain | **Greedy**: the action with the highest output, like the old net | D1, §5.6 |
| 22 | Occlusion | **Everything occludes**: cover, animals, food and obstacles all stop a ray | §5.4 |
| 23 | Extra inputs | **In-cover flag, previous action, ticks since last meal** | §5.4 |
| 24 | Network size | **One hidden layer of 12** | §5.1 |
| 25 | Mutation defaults | **Gentle**: rate 0.5; 5 % of weights get a random float in ±0.5 added; 0.5 % are replaced | §5.2 |
| 26 | Founder pool | **256 random networks** | §5.1 |
| 27 | Ray fan | **12 rays over 360°**, 20 m | §5.4 |
| 28 | Stop rule | **The plateau rule**: ≥ 50 generations, ±10 % over three 2 000-tick windows, cap 60 000 ticks | §6.3 |
| 29 | Survival analysis | **C# in the editor**: a Survival Report window, no Python | §6.2 |

## 2. Where it comes from: the previous lab

| Previous lab (`Assets/02 - Scripts/04 - Crowds and Evolution`, on `master`) | Here |
|---|---|
| 5 eyes, ray-marched over the grass grid, distance only (food) | 12 rays all round, each stopped by the **first EvoSim object**, reporting distance and kind |
| Net 5 → 5 → 1, sigmoid everywhere, weights U(−10, 10) (saturated: bang-bang steering) | Net in → 12 hidden (tanh) → 5 outputs, the highest one wins (greedy, like the old net); Xavier init (the old ±10 init kept as a setting) |
| One output: turn in [−10°, +10°] per frame; constant speed from `CapsuleAutoController` | `left`/`right` turn at one speed, `walk`/`run` straight, `stop`; turn angle and speed tunable |
| Genome = weight matrices, copied with per-weight swap (1 %) / nudge (1 %) | One `Vector` allele per animal, given a lineage; random float mutations; per-neuron crossover in sexual mode |
| Grazers only; eat a grass cell → +energy and an offspring at once | Neural prey and neural predators; graze or strike on contact (ACT-10); breed when ready, alone or with a partner |
| Per frame, `UnityEngine.Random`, eyes off-centre (−25…+15°), `Random.value` can hit index = size | Per tick, named streams, symmetric fan, no global random (RAND-01) |

## 3. Fitting the EvoSim contract: eight decisions

The contract has hard rules that the old design breaks: a brain only returns
probabilities over the species' actions (CORE-06, DEC-11); a sense returns a token from a
finite list (SENSE-01); alleles are text or numbers (GENE-02). Each decision below keeps the
rule and gets the old behaviour back.

**D1 — Five discrete actions, the highest output wins.** The actions are `left`, `right`,
`walk`, `run` and `stop`, in that order. The network has one output per action. With the
default Greedy policy (owner) the row it returns is a one-hot on the highest output (ties to
the lower index), a valid DEC-11 row, so the draw always picks that action. A `Sample` policy
(softmax) stays available as a setting. `left`/`right` turn by a set angle per tick (20° by
default) while moving at one set speed (walk by default). `walk`/`run` keep the heading.
`stop` stays still and rests. With a decision every tick the animal steers continuously, as
the old net did each frame.

**D2 — The weights are one `Vector` allele whose length follows the layout.** GENE-07
already allows "the weights of a small neural network". A single `NeuralGenomeGene` holds
the whole network as one `float[]` allele and carries the architecture. The length is
computed, not typed in: inputs = the sum of the species' sense widths, outputs = the action
count. For one hidden layer of h, length = (in + 1)·h + (h + 1)·out. A layout change (a sense,
a ray kind, an action) makes validation report that the founders no longer fit (V-71) and
offer to rebuild them. The core gains `AlleleKind.Vector` (C1).

**D3 — A ray stops at the first EvoSim object.** Animals, food and carcasses are data, not
colliders, so no Physics ray is used. A `WorldRayCaster` marches each ray through the world
model and stops at the first object (§5.4). The sense returns a small token (for the
situation text and the inspector) and a float-vector attachment the network reads (SENSE-40).
The attachment has no cache key, so these queries are never memoised (SENSE-41), which is
correct because no two animals share weights.

**D4 — Rates are component variables, and the network runs beside JEV.**
- A neural species decides on its `DecisionSchedule`: period 1 by default, any period, staggered or not.
- The World's run speed sets how many ticks per second or per frame.
- `NeuralBrain.Ask` copies its inputs and returns `BrainAnswer.Later`, then computes on a
  worker task. Choose-actions waits for every brain (TICK-03), so the neural work hides
  inside JEV's round-trip.
- The network is pure arithmetic on copied arrays and draws no random number, so threading
  can't change a row (DEC-12) or the events hash (RAND-11).

**D5 — Eating and striking on contact; breeding sexual or asexual.**
- No action targets anything. Each tick, after planning its move, every neural action plans
  at most one contact interaction from the species' diet (ACT-03): strike > scavenge > graze,
  on the candidate nearest to where the step ends. ACT-10 applies it only within reach.
- The neural prey eats the grass it walks onto. The neural predator strikes the prey it runs
  into, gets busy digesting, and leaves a carcass, exactly as the JEV predator does.
- Every neural action has `Mates => true` by default (a per-action setting):
  - **asexual** (REPRO-04): a ready animal breeds alone;
  - **sexual** (REPRO-02): it breeds on contact with a ready animal of its species, and the
    baby's network comes from `NeuronCrossover` (§5.3).

**D6 — Moving actions are "self-directed": the search rule doesn't apply.** ACT-04
assumes a target; `left`, `right`, `walk` and `run` have none. A new flag,
`AnimalAction.SelfDirected`, exempts them, the way `Rests` exempts rest (C4).

**D7 — Neural prey and neural predator templates, matched to the JEV species.** Each
template copies every non-controller setting of its JEV counterpart in the reference
scenes. That covers locomotion speeds, stamina, energy, metabolism, maturity, old age,
mating rule, litter, edible energy, diet, cap, floor and initial population. Only the
controller differs: senses, actions, genes, mutation, crossover, brain and decision
schedule. A `MatchedPairs` world module names the pairs, and validation lists any other
difference (V-78).

**D8 — The 2×2 comparison is one world with four species.** `prey` (JEV), `prey-nn`,
`predator` (JEV), `predator-nn`. Both predators eat both preys, and cover hides both preys
from both predators. JEV keeps its species ids, senses and prompts, so its answer caches stay
valid. Survival is read from the events every run already writes (birth, and death with
cause, age and killer), so the comparison needs no core change (§6.2).

## 4. Core changes (EvoSim.Runtime), all additive

Every existing scene keeps its events hash: the R-01 golden and the 466 EditMode tests stay
green, and that is the exit criterion of M16.1.

| # | Change | Files | Rule text to add |
|---|---|---|---|
| C1 | `AlleleKind.Vector`; `AlleleValue.OfVector(float[])` (copied), `.Vector` (read-only), `Canonical(d)` = the length plus SHA-256 (16 hex) of the values rounded to *d* decimals, `ToString()` = `vec[1229]#ab12cd34`; `Equals` through `Canonical` | `Genes/AlleleKind.cs`, `Genes/AlleleValue.cs` | GENE-07 becomes MUST-if-used: vector alleles compare at the gene's decimals |
| C2 | Recording a vector: birth-event mutation entries and `alleles.jsonl` write `vector_sha` and `length`, never the values; values go to a sidecar `vectors.jsonl` (`{id, values}`, written at the same flushes). `RunOverlay` and the Gene Pool window show `ToString()` | `Phases/HatchPhase.cs`, `Recording/RunRecorder.cs`, `Views/RunOverlay.cs`, `Editor/GenePoolWindow.cs` | 13: the new sidecar file |
| C3 | `DecisionQuery.Genome`: the genome the brain reads (`QueryGenome`, so the shuffled-genes control still works); null for tool-built queries. A brain reads only genes with `ReadByBrain` from it, so the brain-visible genome key (DEC-31) stays exact | `Brains/DecisionQuery.cs`, `Phases/AskBrainsPhase.cs`, `Editor/AskTheBrainWindow.cs`, `Editor/PromptPreview.cs` | DEC-10 gains "the brain-visible genome" |
| C4 | `AnimalAction.SelfDirected` (virtual, false). The action conformance suite accepts a self-directed action that moves alone in an empty world and is not searching | `Actions/AnimalAction.cs`, `Tests/EditMode/Conformance/ConformanceTests.cs` | ACT-04: "…except self-directed actions (steering), which have no target" |
| C5 | `Brain.ReadsGeneText` (virtual, true); V-05 ("action has no text gene") is skipped when the species' brain says false | `Brains/Brain.cs`, `Core/World.cs` | — |
| C6 | Test fixtures: the T-MUT suite adds a vector probe gene, so an operator may accept text, number **or vector** genes; the brain suite adds each brain's `[RequiresModule]` companions to the species, as the action suite does | `Testing/` (new `VectorProbeGene`), `ConformanceTests.cs`, `DecisionTests.cs` | 30 §4 |
| C7 | Recombinant alleles: `Allele.SecondParentId` (nullable) and the origin `recombinant`, so a crossover that builds a new value records both parents; `alleles.jsonl` writes `parent2_id` only when set | `Genes/Allele.cs`, `Genes/AlleleRegistry.cs`, `Recording/RunRecorder.cs` | GENE-12 gains `recombinant` and the second parent; GENE-33 extends to vector genes |
| C8 | *(M16.8, only if profiling asks)* `SpatialIndex.ForEachWithin(p, r, among, visitor)`, the ring walk of `Nearest` without the "best" cut-off | `Core/SpatialIndex.cs` | — |

None of these changes names a species, sense or action (CORE-03). The rays need no core
change: `World.ServicesOf<T>()` already lists every `ResourceLayer`, `Cover` and `EntityKind`
(`EntityCount`, `EntityAt`, `Kind`). Survival needs none either: death events already carry
`cause`, `age` and `killer`.

## 5. The `EvoSim.Neuro` assembly

```
Assets/EvoSim/Neuro/
  EvoSim.Neuro.asmdef            → EvoSim.Runtime (no UnityEditor: the T-EDIT-02 check covers it)
  Genes/        NeuralGenomeGene.cs   NeuralInit.cs
  Mutation/     NeuralMutation.cs                     Reproduction/ NeuronCrossover.cs
  Net/          NeuralLayout.cs       NeuralNet.cs    Brains/       NeuralBrain.cs
  Senses/       RayFanSense.cs  RayHitKind.cs  WorldRayCaster.cs  InteroceptionSense.cs  NeuralInput.cs  INeuralSense.cs
                HungerClock.cs (stats for "ticks since last meal", updated each tick)
  Actions/      NeuralAction.cs (base: contacts, Mates, SelfDirected)
                TurnAction.cs (left | right)   ForwardAction.cs (walk | run)   StopAction.cs
  Comparison/   MatchedPairs.cs (WorldModule: pairs + V-78)   SurvivalStats.cs (WorldModule, IStatsColumns)
                PlateauStop.cs (WorldModule: the "long enough" stop rule of §6.3)
                SurvivalAnalysis.cs (pure C#: Kaplan–Meier, hazard ratios, kill matrix, windows)
  Recording/    NeuroStats.cs (IStatsColumns: action shares, speed, food per meter)
  Views/        RayFanView.cs (AnimalView: draws the fan, each ray coloured by what it hit)
  Editor/       EvoSim.Neuro.Editor.asmdef: NeuralSpeciesMenu.cs (prey / predator, matched to a
                JEV species), NeuralGenomeGeneEditor.cs (layout table, vector length),
                AnimalInspector panel (inputs, outputs), SurvivalReportWindow.cs (§6.2)
  Tests/        EvoSim.Neuro.Tests.asmdef (EditMode)
  Modules/      NeuroPrey.prefab, NeuroPredator.prefab, NeuroPreyBody.prefab, NeuroPredatorBody.prefab
Assets/EvoSim/Scenes/     Neuro_Lab.unity, Prey_vs_Prey.unity, Brains_2x2.unity
Assets/EvoSim/Scenarios/  S29–S33 (§6)
```

One tick, for one neural animal (reference phases, unchanged):

```
Sense ── RayFanSense.ReadAll (token)   InteroceptionSense (token)
Ask brains ── attachments: RayFan float[12 rays × kinds], Interoception float[12]
          ── DecisionQuery{Species, Genome, Observation, Attachments}
          ── NeuralBrain.Ask → BrainAnswer.Later → worker: encode → forward → one-hot argmax(5)
          (JEV's batch for the JEV species is already in flight)
Choose actions ── waits for both ── draws from <species>/sampling (DEC-20)
Act ── left/right: heading ± turn, turn speed │ walk/run: straight │ stop: stay, rests
    ── one contact: strike > scavenge > graze (from the diet)
    ── KinematicLocomotion moves; the act phase turns the heading toward the move
Breed ── asexual: Copy │ sexual: NeuronCrossover ── NeuralMutation at conception (synchronous)
Hatch ── no mutator to wait for
```

### 5.1 `NeuralLayout` and `NeuralGenomeGene : Gene` (one locus, one long vector)

- **Layout** (`NeuralLayout.For(species)`, rebuilt at `Initialize`, hashed):
  - inputs, in sense order: a sense implementing `INeuralSense` contributes its attachment
    width (`RayFanSense`: rays × kinds; `InteroceptionSense`: 12). Any other sense contributes
    a one-hot of its tokens (the energy `LevelSense` costs 3). This exists only because the
    brain conformance suite runs every brain on the reference species (C6); no scenario
    uses it (owner: never a same-interface arm);
  - outputs: one logit per action of the species, in action order;
  - hidden: the gene's `hidden` setting (default one layer of 12);
  - **vector length** = Σ over layers of (fan-in + 1) × fan-out.
- `Kind => AlleleKind.Vector`, `ReadByBrain => true` (so the weights are part of the
  brain-visible genome key), `Decimals => 4`.
- Settings: `hidden` (default `[12]`, owner), `activation` (tanh | sigmoid | relu), `init`
  (Xavier | OldLab ±10), `founders` (default 256, owner), `founderSeed`, `clamp` (|w| ≤ 10).
- **Founder pool** (GENE-20/21): `founders` random vectors from a private SplitMix64 seeded
  by `founderSeed` and the layout hash, never by the run seed. Every run therefore starts
  from the same random nets (the owner's naive start), and newcomers below the floor are
  drawn from the same pool. There is no neutral allele. Loading evolved networks as founders
  (a `NeuralFounderPool` asset) is left for later (§9).
- `Check`: length equals the layout's, every value finite and within ±clamp.
- `Express`: sets no traits; for the genes block it writes one summary line
  (`- brain: "vec[1229]#ab12cd34"`). The brain reads the values from `q.Genome` (C3).

**Sizes with the default templates** (12 rays, interoception 12, 12 hidden, 5 actions):

| Template | Ray kinds | Inputs | Vector length |
|---|---|---|---|
| prey, asexual (the measured runs) | Food, Threat, Kin, OtherAnimal, Cover, Carcass, Obstacle (7) | 12×7 + 12 = 96 | (96+1)·12 + (12+1)·5 = **1 229** (4.9 KB) |
| prey, sexual | + ReadyKin (8) | 12×8 + 12 = 108 | **1 373** |
| predator, asexual (the measured runs) | Prey, Carcass, Kin, OtherAnimal, Cover, Threat, Obstacle (7) | 96 | **1 229** |
| predator, sexual | + ReadyKin (8) | 108 | **1 373** |

The founder pool of 256 networks is about 1.3 MB per neural species, registered once per run.

### 5.2 `NeuralMutation : MutationOperator`, its own class of random float mutations

- Separate from `LlmMutation` (text) and `GaussianMutation` (number):
  `Accepts(g) => g.Kind == AlleleKind.Vector`, so MUT-02 gives every vector gene this
  operator and no other.
- Defaults are the owner's "Gentle" set.
- `Rate` (MUT-01) = probability that a baby's network mutates (default 0.5). When it does,
  each weight independently:
  - with `pAdd` (default 0.05) gets a **random float added**, U(−`addStrength`, +`addStrength`)
    (default 0.5), or N(0, `addStrength`) with `gaussian` on;
  - with `pReplace` (default 0.005) is **replaced by a random float**, U(−`replaceStrength`,
    +`replaceStrength`) (default: the gene's init scale);
  - is then clamped to ±clamp.

  If no weight changed, one index is drawn and gets a random float added, so a mutation never
  returns its parent (MUT-04 would count that as a failure). Setting rate 1, pAdd 0.01 ×
  0.5 and pReplace 0.01 × 10 reproduces the old lab.
- Every draw comes from the stream it is given, inside `StartMutation`, as many draws
  whatever happens (MUT-30). It finishes at once (`MutationJob.Done`, operator `nn-add`,
  `nn-replace` or `nn-both`); there is no mutator service, so eggs never wait (MUT-32).

### 5.3 `NeuronCrossover : Crossover` (sexual mode)

- For each neuron of each layer (hidden and output), the baby takes that neuron's whole slice
  (bias and incoming weights) from parent A or B with probability ½, drawn from the species'
  mutation stream (GENE-30 at neuron granularity; GENE-32: every baby its own draws).
- When the result equals one parent, that parent's allele is reused. Otherwise
  `World.Alleles.Register(gene, value, "recombinant", parentA, …, secondParent: parentB)` (C7)
  runs at conception, in conception order, so ids are deterministic. `NeuralMutation` then
  applies to the recombinant as to any inherited allele.
- Other loci (number genes, if any) are crossed as `UniformCrossover` does.
- Asexual babies use `Copy` (GENE-31): the parent's allele, then mutation.

### 5.4 `RayFanSense : Sense, INeuralSense` and `WorldRayCaster`

- **Fan.** `rays` (default 12), `fov` (default 360°), `range` (the `vision` trait, else 20 m).
  - At 360° the rays are evenly spaced: ray *i* at `i·360°/rays` from the heading. Ray 0 looks
    straight ahead, and with an even count one looks straight back. That gives all-round
    vision like JEV's senses (owner).
  - Below 360° the fan is symmetric: ray *i* at `−fov/2 + i·fov/(rays−1)` (one ray: straight
    ahead). This fixes the old −25…+15° bug.
  - Rays start at the body radius (0.5 m), so they never hit the animal itself or the item it
    stands on ("on food" is an interoception input).
- **What stops a ray: the first EvoSim object** (owner: everything occludes). Along the ray,
  the nearest of:

  | Object | Found through | Kind channel |
  |---|---|---|
  | an animal, living, not killed, not hidden from the viewer (SENSE-06) | the species' animal lists (v1), `ForEachWithin` (C8) later; ray–circle test, body radius 0.5 m | `Kin` / `ReadyKin` (sexual) / `Prey` (a species it strikes) / `Threat` (a species that eats it) / `OtherAnimal`, from the food web, so in the 2×2 world both predators are `Threat` to both preys |
  | a resource item | every `ResourceLayer`: DDA over `FoodGrid` cells (exact, 1 m), a 0.25 m march with `WithinReach` for other layers | `Food` (a layer in its diet); other layers → `Item` (off by default) |
  | an entity | every `EntityKind` (`EntityAt`, not `UsedUp`); ray–circle test, radius 0.5 m | `Carcass`; other kinds → `Entity` (off by default) |
  | a cover cell | every `Cover` (`InCover` while marching; DDA over `CoverLayer`) | `Cover` |
  | the ground | `Ground.Inside` / `IsWater` / `IsWalkable` while marching | `Obstacle` (edge, water, non-walkable; `Water` can be split out) |

  The ray stops there; nothing behind it is seen. At equal distance the lower kind value
  wins, then the lower id (SPACE-08). The enabled kinds set the input width. A disabled kind
  still stops the ray but writes nothing.
- **Encoding** (attachment `NeuralInput{SenseIndex, Values}`): per ray, one channel per
  enabled kind holding the proximity `1 − d/range` in the channel of the kind it hit,
  0 elsewhere. Optional `MLAgentsLayout`: one-hot plus hit fraction, as `RayPerceptionSensor3D`
  writes it (§10).
- **Token:** `none` + one per enabled kind, the kind of the nearest hit over the whole fan;
  text "Rays: threat 4-10 meters ahead." (V1/V2). `AttachmentKey => null` (SENSE-41).
- Pure function of the start-of-tick state, no random draws (SENSE-03). `ReadAll` and `Read`
  give the same tokens (SENSE-31).

`InteroceptionSense : Sense, INeuralSense`: the animal's own state, 12 inputs with 5 actions.
Token = the energy level (low/medium/high).

| Input | Width | Value |
|---|---|---|
| energy | 1 | energy / max |
| stamina | 1 | stamina / max |
| age | 1 | min(age / maturity, 1) |
| ready to mate | 1 | 0 / 1 |
| on food | 1 | 1 when a grazeable item (prey) or an edible carcass (predator) is within reach |
| **in cover** (owner) | 1 | 1 when a cover service hides it from at least one threat species (`World.Queries.IsHiddenFrom`); for a species with no threat, 1 when it stands in cover |
| **previous action** (owner) | 5 (the action count) | one-hot of `a.Action`, the action it did last tick; all zeros before its first decision. Read from the animal's record, so no hidden state (CORE-05) |
| **ticks since last meal** (owner) | 1 | min(ticks / `hungerHorizon`, 1), `hungerHorizon` = 200 by default |

"Ticks since last meal" needs per-animal state, so `HungerClock` (a species module) declares
two stats (CORE-05): `neuro.mealsSeen` and `neuro.sinceMeal`. Each tick, in the death phase
(the hook the thirst recipe uses, 22 §1, returning no cause of death), it resets `sinceMeal`
to 0 when `a.Meals` grew and adds 1 otherwise. `InteroceptionSense` has
`[RequiresModule(typeof(HungerClock))]`.

### 5.5 The five actions: `NeuralAction` and its subclasses

`NeuralAction : AnimalAction` (abstract) holds what they share:
- `SelfDirected => true` (C4) for the moving ones; `Mates => mates` (a setting, default true on every action); `Hunts =>` contacts include
  Strike; `IsRelevant => true`.
- `contacts` (Graze | Strike | Scavenge; the templates set Graze for prey and Strike | Scavenge
  for predators; checked against the diet, V-75). After the intent, one interaction on the
  candidate nearest to the step's end (ACT-03), found through `ActContext`/`World.Queries`
  (ACT-05).
- Plans only through `ActContext` (ACT-06): it never sets the heading or the position. The act
  phase turns the heading toward the move. A blocked step leaves the heading unchanged, and the
  kinematic locomotion's side step (MOVE-04) unsticks it.

| Action | Class | Intent | Inspector settings (defaults) |
|---|---|---|---|
| `left` | `TurnAction` | `c.WalkTo(p + HeadingDirection(a.Heading − angle)·range)`, or `c.RunTo` at run speed | `angle` (20°), `speed` (Walk \| Run \| Slow, default Walk), `slowFraction` (0.5) |
| `right` | `TurnAction` | same with + angle (headings run clockwise from +z) | same |
| `walk` | `ForwardAction` | `c.WalkTo(p + HeadingDirection(a.Heading)·range)` | — |
| `run` | `ForwardAction` | `c.RunTo(…)`: `speed.run`, costs stamina | — |
| `stop` | `StopAction` | `c.Stay()` | `rests` (true: rest cost, stamina recovers); still grazes or strikes what is in reach |

The template builds the order `left, right, walk, run, stop`. With the defaults, a
walk-speed prey (1 m/tick) turns on a radius of about 2.9 m (1 / 0.349 rad).

### 5.6 `NeuralBrain : Brain` (a world service)

- `Id => "neural-greedy"` (or `$"neural-sample-{tau:0.##}"`), `Memoizable => false`, `useCache` off,
  `AcceptsAttachments => true`, `CanMixSpecies => true`, `ReadsGeneText => false` (C5),
  `MaxActions` unlimited, `MaxPromptTokens => 0`, not strict.
- `[RequiresModule(typeof(NeuralGenomeGene))]`: validation offers to add it, and the brain
  conformance suite adds it (C6).
- **Ask:** on the main thread, encode each query into a pooled `float[]` (one-hot tokens and
  attachments, in layout order) and take the weights from `q.Genome[gene.Locus].Value.Vector`.
  Then `BrainAnswer.Later(n)` and `Task.Run`: forward pass, policy, `SetRow`/`Fail`,
  `Complete`. Below `syncBelow` queries (default 32) it computes inline (`BrainAnswer.Now`).
- **Policy:** **`Greedy` (default, owner)** gives a one-hot of the argmax, ties to the lower
  index. The draw from the sampling stream then always returns that action, and the World's τ
  can't change a one-hot row. `Sample` (a setting) gives softmax with the brain's own τ, and
  the World's τ (DEC-20) applies on top of it.
- **Steering loops:** a greedy, deterministic policy can lock into a loop (circling, or
  walking into a wall). The previous-action input (§5.4) and mutation are what break such
  loops. `NeuroStats` counts the share of animals that did the same action for 50 ticks in a
  row, so loops show up in the stats.
- **Failures:** no genome, wrong length or a NaN gives `Fail(i, reason)`, never a made-up row
  (DEC-40).
- `NeuralNet` is allocation-free (`Span<float>`). It is the test oracle for the Burst path.

### 5.7 Templates (`GameObject ▸ EvoSim ▸ New Neural Species ▸ Prey | Predator`)

The menu asks for the JEV species to match (default: the scene's `prey` or `predator`),
copies its body and life settings, adds the neural controller and registers the pair in
`MatchedPairs`.

| Part | `NeuroPrey` (id `prey-nn`) | `NeuroPredator` (id `predator-nn`) |
|---|---|---|
| Copied from the JEV species | `Energy`, `Stamina`, `Metabolism`, `Starvation`, `OldAge`, `KinematicLocomotion` (walk/run speeds), vision, maturity, `MatingRule` (energy, **sexual toggle**), `Litter`, `Incubation`, edible energy, cap / floor / initial, cap rule | the same, plus the diet's kill chance and digestion |
| Diet | graze Grass (same energy scale) | strike every prey species the JEV predator strikes, same energy; scavenge carcasses |
| Senses | `RayFanSense` (prey kinds, §5.1), `InteroceptionSense` | `RayFanSense` (predator kinds), `InteroceptionSense` |
| Actions | `left`, `right`, `walk`, `run`, `stop`; contacts Graze | same; contacts Strike, Scavenge |
| Genes, mutation, crossover | `NeuralGenomeGene`, `NeuralMutation`, `NeuronCrossover` | same |
| Brain, schedule | `NeuralBrain` (added under the World's services if missing); `DecisionSchedule` period 1 (a component variable) | same |

Other species' diets don't change by themselves: the 2×2 scenario adds the neural prey to the
JEV predator's diet and to the cover's hiding species (§6.2).

## 6. Scenarios

### 6.1 Every pairing of prey and predator brains

| Scenario | Prey brain | Predator brain | Server | Tier |
|---|---|---|---|---|
| S02/S03 Lab1 (existing) | JEV | JEV | JEV | T4 |
| **S29 NeuroLab** | neural | neural | none | **T2 (CI)** |
| **S30 JevPreyNeuroPredator** | JEV | neural | JEV | T4 |
| **S31 NeuroPreyJevPredator** | neural | JEV | JEV | T4 |
| **S32 Brains2x2** (§6.2) | JEV **and** neural | JEV **and** neural | JEV | T4, **measured** |
| **S33 PreyVsPrey** (§6.2) | JEV **and** neural | none | JEV | T4, **measured** |

- S33 is the 2×2 without its predators (owner: only prey). The two prey species share the
  grass and nothing hunts them, so it isolates foraging and breeding from escaping.
- S29–S31 are functional checks (the L0 versions in CI, a 10-minute T4 smoke run with JEV).
  Only S32 and S33 get the measured runs of §6.3 (owner).
- Every scenario has an asexual and a sexual version (the World's control "C7 ASEXUAL" of 15,
  switches every species at once). The measured runs use asexual for now (§6.3).
- Maps: the functional scenarios (S29–S31 and the L0 versions) use Lab1_Small's map, which is
  cheap. The measured S32 and S33 use **Lab1_Full** (owner), whose populations are large
  enough for a neural predator species to evolve.
- Each has an L0 variant where JEV is replaced by `RandomBrain` or `ScriptedBrain`, so CI
  covers every pairing without a server.
- **Rates:** decision periods are the species' `DecisionSchedule` values (JEV reference: 4,
  staggered; neural default: 1) and can be overridden per scenario. The World's run speed is
  the World's setting: Fast without JEV, and in lockstep with JEV each tick lasts as long as
  JEV's batch (SPACE-13). The neural work runs on a worker inside that wait: in S32 at the
  caps, about 340 neural animals × (12 rays + a 1 229-weight forward pass), a few ms.
- **What adding neural species does to JEV species:** their signatures, senses and prompts
  don't change, so their answer caches stay valid. Their streams are per species (RAND-03).
  Run hashes differ, and comparisons are made within a scenario.

### 6.2 S32 Brains2x2 and S33 PreyVsPrey: survival rates in one world

**Design.**

| | JEV brain | Neural brain |
|---|---|---|
| **Prey** | `prey` (unchanged reference) | `prey-nn` (`NeuroPrey`, matched) |
| **Predator** | `predator` (unchanged reference) | `predator-nn` (`NeuroPredator`, matched) |

- Food web: both predators strike both preys (same energy); both scavenge carcasses; both
  preys graze the same Grass. `Cover` hides both preys from both predators (its hiding-species
  list names both, V-79).
- Fairness: matched body and life settings (V-78), equal initial populations and caps per role
  (Lab1_Full's, owner: prey 136 / cap 270 / floor 10, predators 28 / cap 68 / floor 3, per
  species), and grass regrowth ×2 because two species graze (owner, a scenario setting). Same
  map for every run (World seed, RAND-04); only the run seed varies.
- S33 is the same world without both predators: `prey` and `prey-nn`, the same Grass at ×2,
  the same caps. The neural prey keeps the same ray kinds as in S32 (its `Threat` channel
  stays at 0), so the two scenarios' neural prey have the same layout.
- **Neural founders are random networks** (owner): every run starts the neural species from
  the gene's seeded random pool, and newcomers below the floor come from it too. JEV founders
  are curated sentences, so the comparison includes how fast evolution gets a random network
  to survive. The survival measures are therefore reported per time window (§6.3), not only
  over the whole run.
- **Arms** (scenario overrides; both measured, owner):
  - *Default rate* (main): neural species decide every tick (their `DecisionSchedule` period 1).
  - *Matched rate* (variant): neural species on period 4, staggered, like JEV. This separates
    "better controller" from "decides 4× more often".
  - There is no same-interface arm (owner: never). The comparison is between whole
    controllers: JEV with its senses, sentences and six actions, against the network with its
    rays and five steering actions.
- **Floor: on** (owner). Newcomers (random networks for the neural species) keep every species
  alive, and the number each species needed is a result (POP-05). No extinction variant.

**Survival measures, per species** (from `events.jsonl`; migration and the run's end are
censoring, not death):

| Measure | Definition |
|---|---|
| **Death hazard ratio (headline, owner)** | per role, neural vs JEV: the ratio of death rates (deaths per animal-tick, all causes but migration) in the late windows, with its 95 % interval; below 1 means the neural species survives better |
| Survival curve | Kaplan–Meier over age (deaths = killed, starvation, old age), with Greenwood bands; median lifespan |
| Cause-specific hazards | deaths by cause per 1 000 animal-ticks alive, and their neural/JEV ratios |
| Juvenile survival | share of animals born that reach maturity |
| Reproductive success | offspring per animal over its life; share that ever breed |
| Persistence | newcomers needed per 1 000 ticks |
| Population | mean and range over time; share of ticks at the cap |
| **Kill matrix** (S32) | kills by killer species × victim species (2×2), per 1 000 predator-ticks and per 1 000 prey-ticks |
| Predator success (S32) | kills and scavenged portions per predator-tick; starvation hazard |

"Late windows" are the last three 2 000-tick windows, the ones the plateau rule (§6.3)
checked. Every measure is also given per window, so the bootstrap from random networks shows.

**The analysis is C# in the editor** (owner):
- `SurvivalStats` (world module, `IStatsColumns`) writes live columns to `stats.csv`: alive,
  births, deaths by cause, and kills by the killer's species, for each species. At the end it
  writes `survival.json`.
- `SurvivalAnalysis` (pure C#, in `EvoSim.Neuro`, unit-tested) reads a run's `events.jsonl`. It
  rebuilds each animal's life: birth or arrival, death with cause, age and killer; migration
  and the run's end count as censoring. From these it computes:
  - Kaplan–Meier curves with Greenwood intervals;
  - death-rate ratios as the hazard ratios, with a 95 % interval from the log rate ratio
    (SE = √(1/d₁ + 1/d₂), d = deaths in each species;
    a constant-hazard model per window, no regression library);
  - the kill matrix and every per-window measure.
- `SurvivalReportWindow` (`Window ▸ EvoSim ▸ Survival Report`, `EvoSim.Neuro.Editor`) opens
  one or more run folders. It shows:
  - the curves and the per-window plots, drawn with the editor's own drawing (no package);
  - the kill matrix, and the headline table per seed and pooled.

  It also writes the markdown report and CSV tables. The brain effect within each role is read
  from the rows and columns of the 2×2. The interaction (for example, the neural prey doing
  better against the neural predator than against JEV) is read from the kill matrix.
- With 2 seeds there is no between-seed error bar. Each measure is reported per seed and
  pooled. An effect counts only when it points the same way in both seeds and its pooled
  interval excludes zero.
- Report: one page per scenario and arm, `Logs/EvoSim/Reports/<S32|S33>-<arm>-<date>.md`,
  with the settings that differed between arms.

### 6.3 Measured runs: 2 seeds, long enough

| | S33 PreyVsPrey | S32 Brains2x2 |
|---|---|---|
| Map and populations | Lab1_Full, 2 prey species | Lab1_Full, 2 prey + 2 predator species |
| Arms | default rate, matched rate | default rate, matched rate |
| Seeds | 2 (run seeds 1 and 2; same World seed, so the same map) | 2 |
| Mating | **asexual for every species** (the World's control "C7 ASEXUAL" of 15) | same |
| Runs | 2 arms × 2 seeds = 4 | 4 |

**Asexual for both, now; sexual later** (owner). Two things change for the JEV species, and
the scenario sets both:
- **Their prompt.** The mating rule's line ("Breeding needs only one of the two to choose
  mate… as soon as it reaches a ready partner") would be false. The scenario overrides it with
  "An adult that chooses mate breeds alone as soon as it is ready." A prompt that matches the
  world is required (PROMPT-03 in spirit). It gives the JEV species new prompt ids, so these
  runs start with an empty answer cache; the first run's cost measure includes that.
- **Their choice.** A JEV animal still breeds only when its brain picks `mate` and it is ready
  (REPRO-04). A neural animal has no `mate` action: every one of its actions has
  `Mates => true`, so it breeds whenever it is ready, as the old lab's animals did (owner:
  kept). This difference is part of the controllers being compared, and the report states it.
  The per-action `mates` setting (§5.5) stays available for later experiments.

**"Long enough" is a stop rule, not a guess** (owner: kept as proposed; `PlateauStop`, a world
module that requests a
clean stop at a tick boundary, RAND-20). A run stops at the first window boundary where both
of these hold:
1. **at least 50 generations** of every neural species (its mean generation over the window);
2. **a plateau**: over the last three 2 000-tick windows, every neural species' per-window
   measures (population mean, starvation hazard, predation hazard in S32, births per animal)
   stay within ±10 % of their mean.

There is a hard cap (default 60 000 ticks) for a species that never settles; the report then
says so. Both seeds of an arm
then run to the **larger** of their stop ticks, rounded up to 5 000, so the two seeds cover
the same length. That costs one resumed run (RAND-21: the shorter seed replays from its
caches and continues).

**Cost, to measure on the first run.** JEV sets the tick rate. In S33 one JEV species decides
(the Lab1_Full prey load, up to 270 animals on a staggered period of 4); in S32 two do (prey
and predators, up to 338). The asexual prompt override also starts the answer cache empty.
Runs are resumable (stop file, answer caches),
so a long run can be split over several sessions. The first S33 run measures seconds per tick
and the stop tick, and the owner confirms the remaining runs before they start.

## 7. Validation (new codes V-70…V-79)

| Code | Severity | Check |
|---|---|---|
| V-70 | error | a species with `NeuralBrain` has no `NeuralGenomeGene` (fix: add one) |
| V-71 | error | founder vectors don't match the layout's length (a sense, ray kind, action or hidden size changed): fix "rebuild founders" |
| V-72 | error | an `INeuralSense`'s attachment width changed since the layout was built |
| V-73 | warning | sexual mating rule without `NeuronCrossover`: babies take one parent's whole network |
| V-74 | info | a neural species decides less often than every tick (it steers late); expected in the matched-rate arm |
| V-75 | warning | a contact the diet can't use (Strike without a striking diet entry…) |
| V-76 | error | ray fan: rays < 1, fov ∉ (0, 360], range ≤ 0, no kind enabled |
| V-77 | info | a neural species on the `Sample` policy in a World whose sampling τ ≠ 1: τ tempers its rows too (the default Greedy policy is immune) |
| V-78 | warning | a matched pair differs in a body or life setting (lists each one: speed, stamina, metabolism, maturity, mating, litter, diet energy, cap…) |
| V-79 | warning | a matched pair is treated differently by the world: a predator's diet or a cover's hiding species names one of the pair but not the other |

## 8. Tests (EditMode unless marked)

| Id | Test |
|---|---|
| T-NEURO-01 | vectors equal at 4 decimals → one allele; differ in the 3rd → two; canonical stable across runs; R-01 and every existing golden unchanged (C1, C2) |
| T-NEURO-02 | gene conformance (T-GENE-01) for `NeuralGenomeGene`; vector length = the layout formula for 1–3 senses, 1–8 actions, 0–2 hidden layers; the random founder pool is identical in two worlds with different run seeds (GENE-21); adding an action → V-71 |
| T-NEURO-03 | the T-MUT suite passes for `NeuralMutation`: same stream → same mutant; the mutant differs from its parent; `Check` passes; clamped; over 10 000 mutations the share of added and replaced weights matches pAdd and pReplace within 4 s.e. |
| T-NEURO-04 | `NeuronCrossover`: each neuron slice comes whole from one parent; same stream → same baby; a recombinant is registered once with both parents (C7); a child equal to a parent reuses its allele |
| T-NEURO-05 | 360° fan: 12 rays at 0°, 30° … 330° from the heading, none doubled at ±180°; partial fans symmetric for 1, 2, 5, 7 rays; food 3 m ahead → ray 0 Food proximity 0.85; a kin 2 m ahead hides food 3 m behind it (first object); a prey in cover → the ray stops at the cover cell; killed → passes through; the world edge → Obstacle; equal distance → kind order; in a 4-species world both predators read as Threat to both preys |
| T-NEURO-06 | `ReadAll` = `Read` over 500 animals (SENSE-31, T-SENSE-09); interoception: in-cover flag set only where a cover hides the animal from a threat, previous action one-hot (zeros before the first decision), ticks since last meal resets on a graze, strike or scavenge and grows by 1 per tick, clamped at `hungerHorizon` |
| T-NEURO-07 | brain conformance (T-DEC-03) with the companions (C6), on the reference prey and on both neural templates; rows sum to 1; Greedy is a one-hot on the largest output (ties → lower index) and the drawn action is always it, whatever the World's τ; a query without a genome → `Fail` |
| T-NEURO-08 | async: `Later` with a slow `FakeHttpTransport` JEV in the same world → the tick completes and neural rows equal the synchronous ones; Freeze vs Responsive and Fast vs RealTime → the same hash (SPACE-11, SPACE-14) |
| T-NEURO-09 | actions: action conformance with `SelfDirected` (C4); `left` for 9 ticks → heading −20°/tick at walk speed; angle 35° and speed Run → −35°/tick at run speed; `walk`/`run` keep the heading; `stop` doesn't move, charges the rest cost, recovers stamina |
| T-NEURO-10 | contacts: grass 0.8 m ahead → the neural prey walks and grazes the same tick; a prey 0.8 m ahead → the neural predator strikes, gets busy, leaves a carcass; `stop` on food → grazes; at most one interaction per tick (ACT-03) |
| T-NEURO-11 | breeding: asexual → a ready animal breeds alone and pays the litter; sexual → only on contact with a ready kin, the baby's network is a neuron mix; in both, mutation with probability `Rate` |
| T-NEURO-12 | templates: `NeuroPrey`/`NeuroPredator` built against the reference species → V-78 silent; change the neural prey's run speed → V-78 names it; the JEV predator's diet without `prey-nn` → V-79 |
| T-NEURO-13 | `SurvivalStats` on a scripted 4-species run: deaths by cause and kills by killer species equal the events; migrated animals are censored, not dead; the kill matrix sums to the kills. `SurvivalAnalysis` on hand-made event logs: Kaplan–Meier and Greenwood values equal a worked textbook example; the rate ratio and its interval equal hand-computed values; censoring at the run's end and at migration |
| T-NEURO-14 | `PlateauStop` on scripted measures: no stop before 50 generations; stops at the first window boundary of a ±10 % plateau over three windows; stops at the cap otherwise; the run's outputs are complete (RAND-20) |
| T-NEURO-15 (T2) | S29 asexual and sexual for 5 000 ticks, and the L0 versions of S30–S33: the same seed twice → the same hash; evolved neural prey eat more grass per animal-tick than the random founder pool under the no-mutation control (measured first, then pinned as a gate) |
| T-NEURO-16 (T4) | S32 and S33 with JEV: 10 minutes each, never stall on the neural brain, JEV cache hit rate within ±5 points of Lab1 alone; `survival.json` written; a stopped run resumes and gives the hash of an unbroken one |

## 9. Milestones (M16, after M15)

| Step | Delivers | Done when |
|---|---|---|
| **M16.1** Core | C1–C7 with their tests; spec text in 07, 08, 09, 13, 30 | 466 + new EditMode tests green; R-01 unchanged |
| **M16.2** Genes | `NeuralLayout`, `NeuralNet`, `NeuralGenomeGene`, `NeuralMutation`, `NeuronCrossover` | T-NEURO-01…04 |
| **M16.3** Senses | `WorldRayCaster`, `RayFanSense`, `InteroceptionSense`, `HungerClock`, `RayFanView` | T-NEURO-05, 06; the fan visible in a sandbox |
| **M16.4** Brain | `NeuralBrain` (sync), Sample/Greedy | T-NEURO-07 |
| **M16.5** Actions | `NeuralAction`, `TurnAction`, `ForwardAction`, `StopAction`, contacts, mating both ways | T-NEURO-09…11 |
| **M16.6** Templates and pairings | `NeuroPrey`, `NeuroPredator`, the menu, `MatchedPairs` (V-78, V-79), `Neuro_Lab` scene, S29–S31 and their L0 versions, `NeuroStats`, inspector panel | T-NEURO-12, T-NEURO-15 (S29–S31); the owner watches S29 |
| **M16.7** Comparison scenarios | `Prey_vs_Prey` and `Brains_2x2` scenes on Lab1_Full's map, S33 and S32 with both arms, `SurvivalStats`, `PlateauStop`, `survival.json`, `SurvivalAnalysis`, `SurvivalReportWindow` and its markdown report, docs chapter `Docs/Interface/16-neural-species.md` and site page `extending/neural-species.md` | T-NEURO-13, 14, 15 (S32/S33 L0), 16 |
| **M16.8** Concurrency and scale | async `Later` path, pooled attachments, one fan per animal per tick (a per-tick buffer in a world service), optional Burst `IJobParallelFor` forward pass and ray march behind `ReadAll` (same results as the managed oracle), C8 if needed. Before the measured runs, so they don't pay for slow code | T-NEURO-08; the 20 §9 budget at 300 neural animals |
| **M16.9** Measured runs | §6.3: the first S33 run (default rate, seed 1) measures the cost and the stop tick; with the owner's go, the other 7 runs; one Survival Report per scenario and arm | 8 runs done; reports written |
| *Later* | a `NeuralFounderPool` asset (networks exported from a run, used as founders); §10 | — |

## 10. Later: a DRL brain on the same species

The neural species is shaped like an ML-Agents agent: a ray-perception observation, one
discrete action branch of 5, and a reward that could be energy gained. A PPO policy trained in
ML-Agents (ONNX, run with Unity's inference engine) can become one more `Brain` for the same
templates (`OnnxBrain`, `Memoizable => false`). That would make a third column in the 2×2:
LLM, evolved and trained. Out of scope for M16. The `MLAgentsLayout` ray encoding (§5.4) keeps
the door open.

## 11. Alternatives considered

| Alternative | Why not |
|---|---|
| Weights as a text allele (base64) | No core change, but kilobytes of text in birth events, `alleles.jsonl`, the overlay and prompt previews. The species-word check can find species names inside base64. Every decision parses text |
| One `NumberGene` (and GameObject) per weight | Rejected by the owner: hundreds of components and registry loci per species |
| A turn × speed grid of actions with factorised heads | Superseded by the owner's 5 actions: turning has one speed |
| Continuous outputs (turn angle, speed) through a new brain → action channel | Breaks CORE-06/DEC-11 and the memo/cache model. Discrete actions with a decision every tick give the same behaviour |
| Physics raycasts | Animals, food and carcasses aren't colliders; rays hit EvoSim objects only (owner) |
| Mutating weights with `GaussianMutation` | It is the number-gene operator (one value, a range); the owner wants a separate class for networks |
| Comparing brains in separate worlds only (S30, S31) | Kept, but separate worlds don't share food or predators. The 2×2 puts all four under the same conditions, and its kill matrix shows who eats whom |
| A same-interface arm (the neural brain on JEV's senses and actions) | Rejected by the owner: never |

## 12. Still open

Nothing. The only gate left is in M16.9: the first measured run reports its cost and stop
tick, and the owner gives the go for the other seven.

**Readings taken while implementing** (also in the status file; for the owner at the next checkpoint):
- M16.1 C1: the vector hash is SHA-256 over each value as the int64 round(v·10^d), little-endian.
- M16.1 C2: `vectors.jsonl` is appended at each `alleles.jsonl` write, and exists only in runs with a vector gene.
- M16.1 C3: Ask-the-Brain builds queries from sentences without a genome; a network fails there (DEC-40).

## 13. Risks

- **Fairness of the 2×2:**
  - The JEV and neural species differ in their interface as well as in their brain. Whole
    controllers are compared (owner). The matched-rate arm takes out the decision rate; the
    report lists the remaining differences:
    - senses: JEV's see through cover and animals, the neural rays don't (owner: everything
      occludes);
    - action choice: JEV samples, the network is greedy (owner);
    - breeding: JEV picks `mate`, the network breeds whenever it is ready (owner).
  - JEV's founders are curated sentences and the neural founders random networks (owner),
    so early windows measure the bootstrap more than the brain. The plateau rule (§6.3) and
    the per-window report keep the two apart: compare the late windows.
  - Newcomers below the floor bring random networks back into the neural species and slow its
    evolution. Their count is reported as a result (POP-05), not hidden.
- **Two seeds** show whether an effect repeats, not how large its spread is. Intervals come
  from individual animals within runs, and an effect must agree in both seeds (§6.2).
- **Run time:** JEV sets the tick rate. On Lab1_Full, S32 has two JEV species at reference
  sizes, the heaviest JEV load of any scene so far. The plateau rule may ask for tens of
  thousands of ticks. The first S33 run measures the cost before the other seven start.
- **Memory:** every allele keeps its vector for the run's lineage, about 4.9 KB × 10 000
  mutants ≈ 49 MB per neural species, plus 1.3 MB of founders. Lab1_Full's larger populations
  make more mutants. Mitigation: float16 storage, or drop the values of extinct alleles after
  they reach `vectors.jsonl`.
- **Greedy loops:** a deterministic policy can circle or push into a wall for many ticks. The
  previous-action input and mutation should break it; `NeuroStats` measures it (§5.6).
- **Determinism across code paths:** the managed and Burst forward passes may differ in the last
  bit. Pick one path per run; the Burst job uses `FloatMode.Strict` and is checked against the
  managed oracle (RAND-12 limits hash comparisons to one machine).
- **The world-wide τ** would temper neural rows under the `Sample` policy (V-77). The default
  Greedy policy is immune; keep τ = 1 in mixed worlds anyway, as JEV's reference.
- **Hidden assumptions:** windows or tools that treat "not text" as "number" (Gene Pool,
  Ask-the-Brain, integrity gates) need a vector branch. They are listed in C2/C3; the M16.1
  tests read every window's text for vector genes.
