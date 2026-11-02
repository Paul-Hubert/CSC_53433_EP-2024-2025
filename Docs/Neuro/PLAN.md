# Plan — a neural steering species beside JEV (M16)

Status: **plan, not started.** Written 2026-11-02 against `llm-evolution` at 2fdd431;
revised the same day with the owner's answers (§1). Still open: §12.

## 0. In one paragraph

Bring back the previous lab's animal: a small neural network that sees through a fan of rays,
whose weights are its genes, and that only steers. Make it a proper EvoSim species, with
its own brain, gene kind, senses and actions in a separate assembly (`EvoSim.Neuro`). It
lives in the same world as the JEV species. Each species keeps its own brain (DEC-13), so
the neural species answers locally in microseconds while JEV's HTTP batch for the other
species is in flight. It has **five actions** (turn left, turn right, walk straight, run
straight, stop). **Each ray reports the first EvoSim object it hits** and that object's kind.
The **weights are one long vector allele** whose length follows the observation and action
sizes. It breeds **sexually or asexually**, a setting of the species. Everything is C#,
deterministic, and kept within the EvoSim contract. The contract needs eight small additions
to core (§4), and none of them changes an existing run.

## 1. Owner decisions (2026-11-02)

| # | Question | Answer | Where in this plan |
|---|---|---|---|
| 1 | How to store the weights | **One allele of a new `Vector` kind**: one long `float[]` per animal, one gene component (never a GameObject per weight). Its length is derived from the observation size and the action count | D2, §5.1 |
| 2 | Actions | **5 actions: `left`, `right`, `walk`, `run`, `stop`.** Turning has a single speed; walk and run go straight; stop stays | D1, §5.5 |
| 3 | Decision rate | **Tick speed is adaptive.** Read here as: the neural species decides **every tick**, and the World's tick rate adapts. It runs Fast (as many ticks per frame as fit in the budget) when no slow brain is in the world, and goes at JEV's pace in lockstep when JEV is (SPACE-13). Nothing depends on wall-clock time (SPACE-11) | D4, §6 |
| 4 | Breeding | **Both sexual and asexual**, a setting of the species' `MatingRule`; sexual babies get a per-neuron crossover | D5, §5.3 |
| 5 | What a ray hits | **The first EvoSim object** along it: an animal, a resource item, an entity (carcass, egg, any `EntitySystem`), cover, or the ground's edge, water or non-walkable cells. No Physics colliders | D3, §5.4 |

## 2. Where it comes from: the previous lab

| Previous lab (`Assets/02 - Scripts/04 - Crowds and Evolution`, on `master`) | Here |
|---|---|
| 5 eyes, ray-marched over the grass grid, distance only (food) | N rays, symmetric fan, each stopped by the **first EvoSim object**, reporting distance and kind |
| Net 5 → 5 → 1, sigmoid everywhere, weights U(−10, 10) (saturated: bang-bang steering) | Net in → hidden (tanh) → softmax over the 5 actions; Xavier init (the old ±10 init kept as a preset) |
| One output: turn in [−10°, +10°] per frame; constant speed from `CapsuleAutoController` | `left`/`right` turn at one speed, `walk`/`run` straight, `stop`; chosen every tick; speed costs stamina and energy |
| Genome = weight matrices, copied with per-weight swap (1 %) / nudge (1 %) | One `Vector` allele per animal: registered, given a lineage, mutated with the same swap/nudge rule (preset), crossed per neuron in sexual mode |
| Eat a grass cell → +energy and an offspring at once; die below 0 | Graze on contact (ACT-10); breed when ready, alone or with a partner (REPRO-02/04); the reference metabolism, death, cap and floor |
| Per frame, `UnityEngine.Random`, eyes off-centre (−25…+15°), `Random.value` can hit index = size | Per tick, named streams, symmetric fan, no global random (RAND-01) |

## 3. Fitting the EvoSim contract: six decisions

The contract has hard rules that the old design breaks: a brain only returns
probabilities over the species' actions (CORE-06, DEC-11); a sense returns a token from a
finite list (SENSE-01); alleles are text or numbers (GENE-02). Each decision below keeps the
rule and gets the old behaviour back.

**D1 — Five discrete actions, one softmax.**
The species' actions are `left`, `right`, `walk`, `run` and `stop`, in that order. The
network's output layer has one logit per action, so the output size **is** the action count.
The row the brain returns is their softmax, a valid DEC-11 row. `left` and `right` turn by a
fixed angle per tick (20° by default) while moving at one speed (walk). `walk` and `run` keep
the heading. `stop` stays still and counts as resting, so stamina recovers at the rest cost.
With a decision every tick the animal steers continuously, as the old net did each frame.

**D2 — The weights are one `Vector` allele, and their length follows the layout.**
GENE-07 already allows "the weights of a small neural network". A single `NeuralGenomeGene`
holds the whole network as one `float[]` allele and also carries the architecture (hidden
sizes, activation, initialisation, founder count). The vector's length is computed, not
typed in: inputs = the sum of the species' sense widths (§5.1), outputs = the species' action
count, so length = (in + 1)·h + (h + 1)·out for one hidden layer of h. Adding a sense, a ray
kind or an action changes the layout. Validation then says the founders no longer fit (V-71)
and offers to rebuild them. The core gains `AlleleKind.Vector` (C1); the registry, lineage,
genome keys and recording then work as they do for the other kinds.

**D3 — A ray stops at the first EvoSim object, and gives a token plus a vector attachment.**
Animals, food and carcasses are data, not colliders, so Physics rays can't see them, and none
are used. A `WorldRayCaster` marches each ray through the world model and stops at the first
object it meets (§5.4): an animal, a resource item, an entity, a cover cell, or a ground
boundary. The sense returns a small token (the kind of the nearest hit over the whole fan, for
the situation text and the inspector) and an attachment (SENSE-40): the float vector the
network reads. The attachment has no cache key, so these queries are never memoised
(SENSE-41), which is correct because no two animals share weights.

**D4 — Concurrent with JEV, decisions every tick, the tick rate adaptive.**
The neural species names `NeuralBrain` as its own brain; the World default stays JEV. Its
`DecisionSchedule` has period 1. Ask-brains sends one batch per brain (DEC-15).
`NeuralBrain.Ask` copies its inputs and returns `BrainAnswer.Later`, then computes the rows
on a worker task. Choose-actions waits for both, JEV's HTTP batch and the network batch
(TICK-03), so the neural work hides inside JEV's round-trip. The tick rate is whatever the
slowest brain allows: Fast mode without JEV, JEV's pace with it. Results never depend on it
(SPACE-11). The network is pure arithmetic on copied arrays and draws no random number, so
threading can't change a row (DEC-12) or the events hash (RAND-11).

**D5 — Eating and striking on contact; breeding sexual or asexual.**
No action targets anything. Each tick, after planning its move, every neural action plans at
most one contact interaction from the species' diet (ACT-03): strike > scavenge > graze,
picking the candidate nearest to where this tick's step ends. ACT-10 applies it only if the
animal is within reach after moving, so it eats what it runs into, and `stop` on food eats
it. Every neural action has `Mates => true`, so a ready animal breeds in the breed phase:
- **asexual** (`MatingRule` sexual off, REPRO-04, the old lab's regime): alone, as soon as it
  is ready;
- **sexual** (REPRO-02): when it touches a ready animal of its species. The baby's network
  comes from `NeuronCrossover` (§5.3): each neuron's incoming weights and bias come whole
  from one parent.

**D6 — Moving actions are "self-directed": the search rule doesn't apply.**
ACT-04 ("nothing to act on → search") assumes a target. `left`, `right`, `walk` and `run`
have none: the move *is* the behaviour. A new flag, `AnimalAction.SelfDirected`, exempts
them, the way `Rests` exempts rest (C4); `stop` rests and needs no flag. A neural action never
calls `Search` and is never counted as searching.

## 4. Core changes (EvoSim.Runtime), all additive

Every existing scene keeps its events hash: the R-01 golden and the 466 EditMode tests stay
green, and that is the exit criterion of M16.1.

| # | Change | Files | Rule text to add |
|---|---|---|---|
| C1 | `AlleleKind.Vector`; `AlleleValue.OfVector(float[])` (copied), `.Vector` (read-only), `Canonical(d)` = the length plus SHA-256 (16 hex) of the values rounded to *d* decimals, `ToString()` = `vec[809]#ab12cd34`; `Equals` through `Canonical` | `Genes/AlleleKind.cs`, `Genes/AlleleValue.cs` | GENE-07 becomes MUST-if-used: vector alleles compare at the gene's decimals |
| C2 | Recording a vector: birth-event mutation entries and `alleles.jsonl` write `vector_sha` and `length`, never the values; values go to a sidecar `vectors.jsonl` (`{id, values}`, written at the same flushes). `RunOverlay` and the Gene Pool window show `ToString()` | `Phases/HatchPhase.cs`, `Recording/RunRecorder.cs`, `Views/RunOverlay.cs`, `Editor/GenePoolWindow.cs` | 13: the new sidecar file |
| C3 | `DecisionQuery.Genome`: the genome the brain reads (`QueryGenome`, so the shuffled-genes control still works); null for tool-built queries. Rule: a brain reads only genes with `ReadByBrain` from it, so the brain-visible genome key (DEC-31) stays exact | `Brains/DecisionQuery.cs`, `Phases/AskBrainsPhase.cs`, `Editor/AskTheBrainWindow.cs`, `Editor/PromptPreview.cs` | DEC-10 gains "the brain-visible genome" |
| C4 | `AnimalAction.SelfDirected` (virtual, false). The action conformance suite accepts a self-directed action that moves alone in an empty world and is not searching | `Actions/AnimalAction.cs`, `Tests/EditMode/Conformance/ConformanceTests.cs` | ACT-04: "…except self-directed actions (steering), which have no target" |
| C5 | `Brain.ReadsGeneText` (virtual, true); V-05 ("action has no text gene") is skipped when the species' brain says false | `Brains/Brain.cs`, `Core/World.cs` | — |
| C6 | Test fixtures: the T-MUT suite adds a vector probe gene, so an operator may accept text, number **or vector** genes; the brain suite adds each brain's `[RequiresModule]` companions to the species, as the action suite does | `Testing/` (new `VectorProbeGene`), `ConformanceTests.cs`, `DecisionTests.cs` | 30 §4 |
| C7 | Recombinant alleles: `Allele.SecondParentId` (nullable) and the origin `recombinant`, so a crossover that builds a new value (GENE-33) records both parents; `alleles.jsonl` writes `parent2_id` only when set | `Genes/Allele.cs`, `Genes/AlleleRegistry.cs`, `Recording/RunRecorder.cs` | GENE-12 gains `recombinant` and the second parent; GENE-33 extends to vector genes |
| C8 | *(M16.7, only if profiling asks)* `SpatialIndex.ForEachWithin(p, r, among, visitor)`, the ring walk of `Nearest` without the "best" cut-off | `Core/SpatialIndex.cs` | — |

None of these changes names a species, sense or action (CORE-03). C1–C3 are what GENE-07
and SENSE-40 already promise. The rays need no core change: `World.ServicesOf<T>()` already
lists every `ResourceLayer`, `Cover` and `EntityKind` (`EntityCount`, `EntityAt`, `Kind`).

## 5. The `EvoSim.Neuro` assembly

```
Assets/EvoSim/Neuro/
  EvoSim.Neuro.asmdef            → EvoSim.Runtime (no UnityEditor: the T-EDIT-02 check covers it)
  Genes/        NeuralGenomeGene.cs   NeuralInit.cs
  Reproduction/ NeuronCrossover.cs                Mutation/  NeuralMutation.cs
  Net/          NeuralLayout.cs       NeuralNet.cs   Brains/    NeuralBrain.cs
  Senses/       RayFanSense.cs  RayHitKind.cs  WorldRayCaster.cs  InteroceptionSense.cs  NeuralInput.cs  INeuralSense.cs
  Actions/      NeuralAction.cs (base: contacts, Mates, SelfDirected)
                TurnAction.cs (left | right)  ForwardAction.cs (walk | run)  StopAction.cs
  Recording/    NeuroStats.cs (IStatsColumns)
  Views/        RayFanView.cs (AnimalView: draws the fan, each ray coloured by what it hit)
  Editor/       EvoSim.Neuro.Editor.asmdef: NeuralSpeciesMenu.cs, NeuralGenomeGeneEditor.cs
                (layout table, vector length), AnimalInspector panel (inputs, action probabilities)
  Tests/        EvoSim.Neuro.Tests.asmdef (EditMode)
  Modules/      NeuroGrazer.prefab, NeuroBody.prefab
Assets/EvoSim/Scenes/     Neuro_Sandbox.unity, Neuro_WithJEV.unity
Assets/EvoSim/Scenarios/  S29_NeuroAlone (asexual / sexual), S30_NeuroWithJEV, S31_NeuroAsPrey
```

One tick, for one neural animal (reference phases, unchanged):

```
Sense ── RayFanSense.ReadAll (token)   InteroceptionSense (token)
Ask brains ── attachments: RayFan float[7×8], Interoception float[5]
          ── DecisionQuery{Species, Genome, Observation, Attachments}
          ── NeuralBrain.Ask → BrainAnswer.Later → worker: encode → forward → softmax(5)
          (JEV's batch for the other species is already in flight)
Choose actions ── waits for both ── draws from <species>/sampling (DEC-20)
Act ── left/right: heading ± 20°, walk speed │ walk/run: straight │ stop: stay, rests
    ── one contact: strike > scavenge > graze
    ── KinematicLocomotion moves; the act phase turns the heading toward the move
Breed ── asexual: Copy │ sexual: NeuronCrossover ── NeuralMutation at conception (synchronous)
Hatch ── no mutator to wait for
```

### 5.1 `NeuralLayout` and `NeuralGenomeGene : Gene` (one locus, one long vector)

- **Layout** (`NeuralLayout.For(species)`, rebuilt at `Initialize`, hashed):
  - inputs, in sense order: a sense implementing `INeuralSense` contributes its attachment
    width (`RayFanSense`: rays × kinds; `InteroceptionSense`: 5). Any other sense contributes
    a one-hot of its tokens (the energy `LevelSense` costs 3), so the same brain can also
    drive an ordinary species;
  - outputs: one logit per action of the species, in action order;
  - hidden: the gene's `hidden` setting (default one layer of 12);
  - **vector length** = Σ over layers of (fan-in + 1) × fan-out.
- `Kind => AlleleKind.Vector`, `ReadByBrain => true` (so the weights are part of the
  brain-visible genome key), `Decimals => 4`.
- Settings: `hidden` (default `[12]`), `activation` (tanh | sigmoid | relu), `init`
  (Xavier | OldLab ±10), `founders` (default 64), `founderSeed`, `clamp` (|w| ≤ 10).
- **Founder pool** (GENE-20/21): `founders` vectors of the layout's length, drawn from a
  private SplitMix64 seeded by `founderSeed` and the layout hash, never by the run seed.
  Every run therefore starts from the same nets; a changed layout gives new founders. There
  is no neutral allele.
- `Check`: length equals the layout's, every value finite and within ±clamp.
- `Express`: sets no traits; for the genes block it writes one summary line
  (`- brain: "vec[809]#ab12cd34"`) so the prompt preview and Ask-the-Brain stay readable.
  The brain reads the values from `q.Genome` (C3), never by parsing text.

**Sizes of the default animal:**

| Mode | Inputs | Vector length (12 hidden) |
|---|---|---|
| asexual | 7 rays × 8 kinds + 5 = 61 | (61 + 1)·12 + (12 + 1)·5 = **809** (3.2 KB) |
| sexual | 7 rays × 9 kinds (`ReadyKin` added) + 5 = 68 | (68 + 1)·12 + (12 + 1)·5 = **893** (3.6 KB) |

### 5.2 `NeuralMutation : MutationOperator`

- `Accepts(g) => g.Kind == AlleleKind.Vector`.
- `Rate` (MUT-01) = probability that a baby's network mutates (default 0.5). When it does,
  each weight is **reset** with `pReset` (U(−s, s), *s* = the gene's init scale) or **nudged**
  with `pNudge` (+N(0, σ)), clamped to ±clamp. If no weight changed, one index is drawn and
  nudged, so a mutation never comes back equal to its parent (MUT-04 counts that as a
  failure).
- Every draw comes from the stream it is given, inside `StartMutation`, as many draws
  whatever happens (MUT-30). It finishes at once (`MutationJob.Done`, operator `nn-reset`,
  `nn-nudge` or `nn-mix`); there is no mutator service, so eggs never wait (MUT-32).
- Presets: *Gentle* (pNudge 0.05, σ 0.2, pReset 0.005) and *Old lab* (rate 1, swap 0.01 ×
  strength 10, eps 0.01 × strength 0.5, uniform nudges).

### 5.3 `NeuronCrossover : Crossover` (sexual mode)

- For each neuron of each layer (hidden and output), the baby takes that neuron's whole slice
  (its bias and incoming weights) from parent A or B with probability ½, drawn from the
  species' mutation stream (GENE-30 at neuron granularity, GENE-32: every baby its own draws).
  A neuron's weights were selected together, so they stay together.
- When the result equals one parent, that parent's allele is reused. Otherwise
  `World.Alleles.Register(gene, value, "recombinant", parentA, …, secondParent: parentB)`
  (C7) runs at conception. The registration order follows the conception order, so ids are
  deterministic. Mutation (§5.2) then applies to the recombinant as to any inherited allele.
- Other loci (number genes such as vision, if any) are crossed as `UniformCrossover` does.
- Asexual babies use `Copy` (GENE-31): the parent's allele, then mutation.

### 5.4 `RayFanSense : Sense, INeuralSense` and `WorldRayCaster`

- **Fan.** `rays` (7), `fov` (120°), `range` (the `vision` trait, else 20 m), symmetric:
  ray *i* at `−fov/2 + i·fov/(rays−1)` from the heading (one ray: straight ahead). This
  fixes the old −25…+15° bug. An optional rear ray. Rays start at the body radius (0.5 m),
  so they never hit the animal itself or the item it stands on: "standing on food" is an
  interoception input.
- **What stops a ray: the first EvoSim object.** Along the ray, the nearest of:

  | Object | Found through | Kind channel (default) |
  |---|---|---|
  | an animal, living, not killed, not hidden from the viewer (SENSE-06) | the species' animal lists (v1), `ForEachWithin` (C8) later; ray–circle test, body radius 0.5 m | `Kin` / `ReadyKin` (sexual) / `Prey` (a species it strikes) / `Threat` (a species that eats it) / `OtherAnimal`, from the food web |
  | a resource item | every `ResourceLayer`: DDA over `FoodGrid` cells (exact, 1 m), a 0.25 m march with `WithinReach` for other layers | `Food` (a layer in its diet); other layers → `Item` (off by default) |
  | an entity | every `EntityKind` (`EntityAt`, not `UsedUp`); ray–circle test, radius 0.5 m | `Carcass` (one it may eat from); other kinds → `Entity` (off by default) |
  | a cover cell | every `Cover` (`InCover` while marching; DDA over `CoverLayer`) | `Cover` |
  | the ground | `Ground.Inside` / `IsWater` / `IsWalkable` while marching | `Obstacle` (edge, water and non-walkable merged; `Water` can be split out) |

  The ray stops there; nothing behind it is seen. At equal distance the lower kind value
  wins, then the lower id (SPACE-08). The kinds and their channels are fixed at
  `Initialize` (they set the input width), and a disabled kind still stops the ray but
  writes nothing.
- **Encoding** (attachment `NeuralInput{SenseIndex, Values}`): per ray, one channel per
  enabled kind holding the proximity `1 − d/range` in the channel of the kind it hit,
  0 elsewhere (0 = nothing within range, so no bias is needed). Optional `MLAgentsLayout`
  writes one-hot plus hit fraction (1 = no hit), as `RayPerceptionSensor3D` does, for §10.
- **Token:** `none` + one per enabled kind, the kind of the nearest hit over the whole fan;
  text "Rays: food 2-4 meters ahead." (V1/V2). Small, so SENSE-05 stays quiet.
  `AttachmentKey => null`: the query is never memoised (SENSE-41).
- Pure function of the start-of-tick state, no random draws (SENSE-03). `ReadAll` and
  `Read` give the same tokens (SENSE-31).

`InteroceptionSense : Sense, INeuralSense`: attachment = energy/max, stamina/max,
min(age/maturity, 1), ready to mate (0/1), standing on food (0/1). Token = the energy level
(low/medium/high), so the situation text says something.

### 5.5 The five actions: `NeuralAction` and its subclasses

`NeuralAction : AnimalAction` (abstract) holds what they share:
- `SelfDirected => true` (C4) for the moving ones, `Mates => true`, `Hunts =>` contacts include
  Strike, `IsRelevant => true`.
- `contacts` (Graze | Strike | Scavenge, checked against the diet: V-75): after the intent,
  one interaction on the candidate nearest to the step's end point (ACT-03, ACT-05: only
  through `ActContext` and `World.Queries`).
- Plans only through `ActContext` (ACT-06): it never sets the heading or position. The act
  phase turns the heading toward the move. A step that doesn't move (a wall) leaves the
  heading unchanged, and the kinematic locomotion's side step (MOVE-04) unsticks it.

| Action | Class | Intent | Notes |
|---|---|---|---|
| `left` | `TurnAction` (`turn = −20°`) | `c.WalkTo(a.Position + HeadingDirection(a.Heading − 20°)·range)` | single speed: walk (setting `speed`, walk by default) |
| `right` | `TurnAction` (`turn = +20°`) | same with +20° | headings run clockwise from +z |
| `walk` | `ForwardAction` (`run = false`) | `c.WalkTo(a.Position + HeadingDirection(a.Heading)·range)` | straight |
| `run` | `ForwardAction` (`run = true`) | `c.RunTo(…)` | straight; uses `speed.run` and stamina |
| `stop` | `StopAction` | `c.Stay()` | `Rests => true` (rest cost, stamina recovers); still grazes or strikes what is in reach |

The species' action order is `left, right, walk, run, stop` (the template builds it).
The network doesn't care about the order, but the inspector and the stats columns read the
actions by name.

### 5.6 `NeuralBrain : Brain` (a world service)

- `Id => $"neural-{policy}-{tau:0.##}"`, `Memoizable => false`, `useCache` off,
  `AcceptsAttachments => true`, `CanMixSpecies => true`, `ReadsGeneText => false` (C5),
  `MaxActions` unlimited, `MaxPromptTokens => 0`, not strict.
- `[RequiresModule(typeof(NeuralGenomeGene))]`: validation offers to add it, and the brain
  conformance suite adds it (C6).
- **Ask:** on the main thread, encode each query into a pooled `float[]` (one-hot tokens and
  attachments, in layout order) and take the weights from `q.Genome[gene.Locus].Value.Vector`.
  Then `BrainAnswer.Later(n)` and `Task.Run`: forward pass, softmax over the actions,
  `SetRow`/`Fail`, `Complete`. Below `syncBelow` queries (default 32) it computes inline and
  returns `BrainAnswer.Now`.
- **Policy:** `Sample` gives softmax with the brain's own τ. `Greedy` gives a one-hot of the
  argmax (ties go to the lower index), the old net's deterministic behaviour. The World's τ
  (DEC-20) applies on top and is world-wide, so keep it at 1 in mixed worlds.
- **Failures:** no genome (a tool-built query), wrong length, or a NaN gives `Fail(i, reason)`,
  never a made-up row; non-strict → uniform, counted (DEC-40).
- `NeuralNet` is allocation-free: `Span<float>` layers, weights read in place. It is the
  test oracle for the Burst path (M16.7).

### 5.7 The species template (`GameObject ▸ EvoSim ▸ New Neural Species`)

`NeuroGrazer`: `NeuroBody`, `Energy`/`Stamina`/`Metabolism`/`Starvation`/`OldAge` (reference),
`KinematicLocomotion`, `DecisionSchedule` (period 1, not staggered), Senses (`RayFanSense`,
`InteroceptionSense`), Actions (`left`, `right`, `walk`, `run`, `stop`), Genes
(`NeuralGenomeGene`), Mutation (`NeuralMutation`), `NeuronCrossover`, `Diet` (graze Grass),
`MatingRule` (**Sexual** toggle; the menu asks), `Litter` (1–2), cap 120 / floor 10 /
initial 40, brain = `NeuralBrain` (added under the World's services if missing). The menu
accepts the new signature (V-12). Switching sexual ↔ asexual adds or removes the `ReadyKin`
ray kind, which changes the layout: V-71 offers to rebuild the founders.

## 6. Side by side with JEV

- **Scene `Neuro_WithJEV`** = `Lab1_Small` (JEV prey and predators, unchanged) plus
  `NeuroGrazer` on the same Grass. Scenarios:
  - **S29 NeuroAlone** (no server, T2), asexual and sexual variants: the neural species
    alone on Lab1_Small's map. CI can run it.
  - **S30 NeuroWithJEV** (T4, JEV server): the neural species competes for grass.
  - **S31 NeuroAsPrey**: the JEV predator's diet also lists `NeuroGrazer` (strike +80).
    Its "Prey" sense covers it automatically, because the senses are species-agnostic.
  - S29/S30 with JEV replaced by `RandomBrain` or `ScriptedBrain` give the L0 versions.
- **What it does to the JEV species:** their signatures, senses and prompts don't change,
  so their answer caches stay valid. A situation seen before replays without a model call.
  Their streams are per species (RAND-03), so their draws don't shift. The events hash of
  the run changes, of course, and comparisons are made between runs of the same scenario.
- **Adaptive tick speed:** the neural species decides every tick, so its behaviour is the
  same whatever the tick rate (SPACE-11). Alone (S29) the World runs Fast, as many ticks per
  frame as fit in the budget. With JEV in lockstep (SPACE-13) a tick lasts as long as JEV's
  batch, which is about 2.8 s for 330 animals deciding together. The staggered reference
  schedule spreads them so that about a quarter decide each tick. The neural work, about
  150 animals × (rays + an 809-weight forward pass) ≈ 1 ms of managed code, runs on a
  worker inside that wait. Targets: the neural species adds no measurable time to a JEV
  tick, and stays under 2 ms per tick in S29 at 300 animals (20 §9).
- **What to measure:** population per species, grass eaten per species, deaths by cause,
  the neural species' action shares (left/right/walk/run/stop), mean speed and food per meter
  (`NeuroStats` columns), recombinant vs mutant alleles in sexual runs, and whether a species
  needs newcomers (POP-05).

## 7. Validation (new codes V-70…V-79)

| Code | Severity | Check |
|---|---|---|
| V-70 | error | a species with `NeuralBrain` has no `NeuralGenomeGene` (fix: add one) |
| V-71 | error | founder vectors don't match the layout's length (a sense, ray kind, action or hidden size changed): fix "rebuild founders" |
| V-72 | error | an `INeuralSense`'s attachment width changed since the layout was built |
| V-73 | warning | sexual mating rule without `NeuronCrossover`: babies take one parent's whole network |
| V-74 | warning | a self-directed species decides less often than every tick (it steers late) |
| V-75 | warning | a contact the diet can't use (Strike without a striking diet entry…) |
| V-76 | error | ray fan: rays < 1, fov ∉ (0, 360], range ≤ 0, no kind enabled |
| V-77 | info | the World's sampling τ ≠ 1 also tempers the neural species' rows |

## 8. Tests (EditMode unless marked)

| Id | Test |
|---|---|
| T-NEURO-01 | vectors equal at 4 decimals → one allele; differ in the 3rd → two; canonical stable across runs; R-01 and every existing golden unchanged (C1, C2) |
| T-NEURO-02 | gene conformance (T-GENE-01) for `NeuralGenomeGene`; vector length = the layout formula for 1–3 senses, 1–8 actions, 0–2 hidden layers; the founder pool is identical in two worlds with different run seeds (GENE-21); adding an action → V-71 |
| T-NEURO-03 | the T-MUT suite passes for `NeuralMutation`: same stream → same mutant; the mutant differs from its parent; `Check` passes; clamped |
| T-NEURO-04 | `NeuronCrossover`: each neuron slice comes whole from one parent; same stream → same baby; a recombinant is registered once with both parents (C7); a child equal to a parent reuses its allele |
| T-NEURO-05 | fan angles symmetric for 1, 2, 5, 7 rays; food 3 m ahead → centre ray Food proximity 0.85; a kin 2 m ahead hides food 3 m behind it (first object); a prey in cover from the viewer → the ray stops at the cover cell; killed → passes through; the world edge → Obstacle; equal distance → kind order |
| T-NEURO-06 | `ReadAll` = `Read` over 500 animals (SENSE-31, T-SENSE-09) |
| T-NEURO-07 | brain conformance (T-DEC-03) with the companions (C6), on the reference prey (its own actions) and the neural species (5 actions); rows sum to 1; Greedy is one-hot; a query without a genome → `Fail` |
| T-NEURO-08 | async: `Later` with a slow `FakeHttpTransport` JEV in the same world → the tick completes and neural rows equal the synchronous ones; Freeze vs Responsive and Fast vs RealTime → the same hash (SPACE-11, SPACE-14) |
| T-NEURO-09 | actions: action conformance with `SelfDirected` (C4); `left` for 9 ticks → heading −20°/tick at walk speed, circle radius ≈ walk / 0.349 rad; `walk`/`run` keep the heading at their speeds; `stop` doesn't move, charges the rest cost, recovers stamina |
| T-NEURO-10 | contacts: grass 0.8 m ahead → walks and grazes the same tick; `stop` on food → grazes; strike only with a striking diet; at most one interaction per tick (ACT-03) |
| T-NEURO-11 | breeding: asexual → a ready animal breeds alone and pays the litter; sexual → only on contact with a ready kin, the baby's network is a neuron mix; in both, mutation with probability `Rate` |
| T-NEURO-12 (T2) | S29 asexual and sexual for 5 000 ticks, the same seed twice → the same hash; mean grass per animal-tick of evolved nets ≥ that of the founder pool under the no-mutation control (measured first, then pinned as a gate) |
| T-NEURO-13 (T4) | S30 with JEV: runs 10 minutes, never stalls on the neural brain, JEV cache hit rate within ±5 points of the same run without the neural species |

## 9. Milestones (M16, after M15)

| Step | Delivers | Done when |
|---|---|---|
| **M16.1** Core | C1–C7 with their tests; spec text in 07, 08, 09, 13, 30 | 466 + new EditMode tests green; R-01 unchanged |
| **M16.2** Genes | `NeuralLayout`, `NeuralNet`, `NeuralGenomeGene`, `NeuralMutation`, `NeuronCrossover` | T-NEURO-01…04 |
| **M16.3** Senses | `WorldRayCaster`, `RayFanSense`, `InteroceptionSense`, `RayFanView` | T-NEURO-05, 06; the fan visible in Sandbox |
| **M16.4** Brain | `NeuralBrain` (sync), Sample/Greedy | T-NEURO-07; also drives the reference prey |
| **M16.5** Actions | `NeuralAction`, `TurnAction`, `ForwardAction`, `StopAction`, contacts, mating both ways | T-NEURO-09…11 |
| **M16.6** Species and scenes | template menu, `Neuro_Sandbox`, `Neuro_WithJEV`, S29–S31, `NeuroStats`, inspector panel, docs chapter `Docs/Interface/16-neural-species.md`, site page `extending/neural-species.md` | T-NEURO-12; the owner watches S29 and S30 |
| **M16.7** Concurrency and scale | async `Later` path, pooled attachments, one fan per animal per tick (a per-tick buffer in a world service), optional Burst `IJobParallelFor` forward pass and ray march behind `ReadAll` (same results as the managed oracle), C8 if needed | T-NEURO-08, 13; the 20 §9 budget at 300 neural animals |
| **M16.8** *(optional)* | export/import champion networks as a founder-pool asset; §10 | — |

## 10. Later: a DRL brain on the same species

The species is shaped like an ML-Agents agent: a ray-perception observation, one discrete
action branch of 5, and a reward that could be energy gained. A PPO policy trained in
ML-Agents (exported to ONNX, run with Unity's inference engine) can become one more `Brain`
for this species (`OnnxBrain`, `Memoizable => false`). Evolved and trained policies could
then be compared on the same world, or trained nets seeded into the founder pool. Out of
scope for M16. The `MLAgentsLayout` ray encoding (§5.4) keeps the door open.

## 11. Alternatives considered

| Alternative | Why not |
|---|---|
| Weights as a text allele (base64) | No core change, but kilobytes of text in birth events, `alleles.jsonl`, the overlay and prompt previews. The species-word check (World.Checks) can find species names inside base64. Every decision parses text |
| One `NumberGene` (and GameObject) per weight | Rejected by the owner: 809 components and registry loci per species |
| A turn × speed grid of actions with factorised heads | Superseded by the owner's 5 actions: turning has one speed |
| Continuous outputs (turn angle, speed) through a new brain → action channel | Breaks CORE-06/DEC-11 and the memo/cache model. Discrete actions with a decision every tick give the same behaviour |
| Physics raycasts | Animals, food and carcasses aren't colliders; rays hit EvoSim objects only (owner) |
| Per-layer loci, so `UniformCrossover` mixes layers | Crossover would mix layers that were never selected together; `NeuronCrossover` on one locus keeps each neuron whole |

## 12. Still open

1. **Tick speed:** confirm the reading in §1 #3, that the neural species decides every tick
   and the World's tick rate adapts. The other reading is a decision period that adapts per
   animal, for example every tick while something is within a few meters and every 4 ticks
   otherwise. That would be an `AdaptiveDecisionSchedule : DecisionSchedule` reading the
   last observation (deterministic, DEC-04), one extra class in M16.5.
2. **Role in the JEV world:** competitor for grass (S30), prey of JEV predators (S31), or
   both as separate scenarios (recommended).
3. **Turn rate and turn speed:** 20° per tick at walk speed (recommended; about a 3 m
   turning radius).

## 13. Risks

- **Memory:** every allele keeps its vector for the run's lineage, about 3.2–3.6 KB × 10 000
  mutants and recombinants ≈ 36 MB. Mitigation: float16 storage, or drop the values of
  extinct alleles after they reach `vectors.jsonl`.
- **Determinism across code paths:** the managed and Burst forward passes may differ in the
  last bit. Pick one path per run; the Burst job uses `FloatMode.Strict` and is checked
  against the managed oracle (RAND-12 already limits hash comparisons to one machine).
- **Ecology balance:** a species that decides every tick may outcompete the JEV prey for
  grass. Tune its metabolism (energy per meter, run stamina) and cap before reading results.
- **The world-wide τ** also tempers neural rows (V-77). Keep τ = 1 in mixed worlds.
- **Hidden assumptions:** windows or tools that treat "not text" as "number" (Gene Pool,
  Ask-the-Brain, integrity gates) need a vector branch. They are listed in C2/C3; the
  M16.1 tests read every window's text for vector genes.
