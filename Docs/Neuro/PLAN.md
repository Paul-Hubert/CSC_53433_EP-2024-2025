# Plan — a neural steering species beside JEV (M16)

Status: **plan, not started.** Written 2026-11-02 against `llm-evolution` at 2fdd431.
Owner decisions needed: §11.

## 0. In one paragraph

Bring back the previous lab's animal: a small neural network that sees through a fan of rays,
whose weights are its genes, and that only steers. Make it a proper EvoSim species, with
its own brain, gene kind, senses and actions in a separate assembly (`EvoSim.Neuro`). It
lives in the same world as the JEV species and eats the same grass, and the JEV predators
can hunt it. Each species keeps its own brain (DEC-13), so the neural species answers
locally in microseconds while JEV's HTTP batch for the other species is in flight. The new
species gets **turn left / straight / turn right** actions with a **speed** choice on top,
and **ray observations that say what each ray hit** (food, threat, prey, kin, carcass,
cover, obstacle…). Everything is C#, deterministic, and kept within the EvoSim contract.
The contract needs seven small additions to core (§3), and none of them changes an
existing run.

## 1. Where it comes from: the previous lab

| Previous lab (`Assets/02 - Scripts/04 - Crowds and Evolution`, on `master`) | Here |
|---|---|
| 5 eyes, ray-marched over the grass grid, distance only (food) | N rays, symmetric fan, ray-marched over the world model, **distance and hit kind** per ray |
| Net 5 → 5 → 1, sigmoid everywhere, weights U(−10, 10) (saturated: bang-bang steering) | Net in → hidden (tanh) → two softmax heads; Xavier init (the old ±10 init is kept as a preset) |
| One output: turn in [−10°, +10°] per frame; constant speed from `CapsuleAutoController` | Turn bins × speed levels, chosen every tick; speed costs stamina and energy |
| Genome = weight matrices, copied with per-weight swap (1 %) / nudge (1 %) | A `Vector` allele per animal: registered, given a lineage, mutated by an operator with the same swap/nudge rule (preset) |
| Eat a grass cell → +energy and an offspring at once; die below 0 | Graze on contact (ACT-10); asexual breeding when ready (REPRO-04); the reference metabolism, death, cap and floor |
| Per frame, `UnityEngine.Random`, eyes off-centre (−25…+15°), `Random.value` can hit index = size | Per tick, named streams, symmetric fan, no global random (RAND-01) |

## 2. Fitting the EvoSim contract: six decisions

The contract has hard rules that the old design breaks: a brain only returns
probabilities over the species' actions (CORE-06, DEC-11); a sense returns a token from a
finite list (SENSE-01); alleles are text or numbers (GENE-02). Each decision below keeps the
rule and gets the old behaviour back.

**D1 — Steering is a grid of discrete actions, and the brain has factorised heads.**
The species' actions are a grid `turn × speed`: by default `{left, straight, right} ×
{slow, walk, run}`, 9 `SteerAction`s named `left-slow` … `right-run`. The network has two
softmax heads, *steer* (one entry per turn bin) and *speed* (one per speed level). The row
the brain returns is their outer product, `p(turn, speed) = p_steer(turn) · p_speed(speed)`,
laid out in the species' action order. That is a valid DEC-11 row (≥ 0, sums to 1). It is
the same factorisation as ML-Agents' discrete action *branches*, so a PPO policy can
later use the same species (§10). With a decision every tick (`DecisionSchedule` period 1)
the animal steers continuously, as the old net did each frame. The bins are settings: 2 turn
bins (left/right only, as in the old lab), 5 for finer control, 1 speed level for constant
speed.

**D2 — Weights are a new allele kind, `Vector`, held by one gene.**
GENE-07 already allows "the weights of a small neural network". A `NeuralGenomeGene` (one
locus) holds the whole network as one `float[]` allele and also carries the architecture
(hidden sizes, activation, initialisation, founder count). The core gains `AlleleKind.Vector`
(C1). The registry, lineage, crossover, genome keys and recording then work as they do for
the other kinds. Text-encoding the weights would avoid the core change, but it would put
kilobytes of base64 into prompts, birth events and the species-word check (§12).

**D3 — Rays read the world model, and give a token plus a vector attachment.**
Animals, food and carcasses are data, not colliders, so Physics rays can't see them. A
`WorldRayCaster` marches each ray through the world model: grid layers (food, cover) by
exact cell traversal (DDA), ground (non-walkable, water, edge) by sampling, and animals and
carcasses by analytic ray–circle tests. Physics colliders (trees, rocks) are optional, through
the existing `RayBatch` (the sense is a `BatchedSense`). The sense returns a small token (the
kind of the nearest hit over all rays, for the situation text and the inspector) and an
attachment (SENSE-40): the float vector the network reads. The attachment has no cache key,
so these queries are never memoised (SENSE-41), which is correct because no two animals share
weights.

**D4 — Concurrent with JEV: one brain per species, answered off the main thread.**
The neural species names `NeuralBrain` as its own brain; the World default stays JEV.
Ask-brains sends one batch per brain (DEC-15). `NeuralBrain.Ask` copies its inputs and returns
`BrainAnswer.Later`, then computes the rows on a worker task. Choose-actions waits for both,
JEV's HTTP batch and the network batch (TICK-03), so the neural work hides inside JEV's
round-trip. The network is pure arithmetic on copied arrays and draws no random number, so
threading can't change a row (DEC-12) or the events hash (RAND-11).

**D5 — Eating, striking and breeding happen on contact.**
`SteerAction` never targets anything. Each tick it plans its move and, from the species'
diet, at most one contact interaction (ACT-03): strike > scavenge > graze, picking the
candidate nearest to where this tick's step ends. ACT-10 applies it only if the animal is
within reach after moving, so it eats what it runs into, as the old animal did.
`SteerAction.Mates` is on, so a ready animal breeds in the breed phase. With the mating rule
asexual (REPRO-04, the old lab's regime) a ready animal breeds alone. With it sexual, two
ready animals in contact breed (one partner's choice is enough, REPRO-02).

**D6 — Steering is "self-directed": the search rule doesn't apply.**
ACT-04 ("nothing to act on → search") assumes a target. A steer action has none: its move
*is* the behaviour. A new flag, `AnimalAction.SelfDirected`, exempts it, the way `Rests`
exempts rest (C4). A self-directed action never calls `Search` and is never counted as
searching.

## 3. Core changes (EvoSim.Runtime), all additive

Every existing scene keeps its events hash: the R-01 golden and the 466 EditMode tests stay
green, and that is the exit criterion of M16.1.

| # | Change | Files | Rule text to add |
|---|---|---|---|
| C1 | `AlleleKind.Vector`; `AlleleValue.OfVector(float[])`, `.Vector` (read-only), `Canonical(d)` = SHA-256 of the values rounded to *d* decimals (16 hex) plus the length, `ToString()` = `vec[810]#ab12cd34`; `Equals` through `Canonical` | `Genes/AlleleKind.cs`, `Genes/AlleleValue.cs` | GENE-07 becomes MUST-if-used: vector alleles compare at the gene's decimals |
| C2 | Recording a vector: birth-event mutation entries and `alleles.jsonl` write `vector_sha` and `length`, never the values; values go to a sidecar `vectors.jsonl` (`{id, values}`, written at the same flushes). `RunOverlay` and the Gene Pool window show `ToString()` | `Phases/HatchPhase.cs`, `Recording/RunRecorder.cs`, `Views/RunOverlay.cs`, `Editor/GenePoolWindow.cs` | 13: the new sidecar file |
| C3 | `DecisionQuery.Genome`: the genome the brain reads (`QueryGenome`, so the shuffled-genes control still works); null for tool-built queries. Rule: a brain reads only genes with `ReadByBrain` from it, so the brain-visible genome key (DEC-31) stays exact | `Brains/DecisionQuery.cs`, `Phases/AskBrainsPhase.cs`, `Editor/AskTheBrainWindow.cs`, `Editor/PromptPreview.cs` | DEC-10 gains "the brain-visible genome" |
| C4 | `AnimalAction.SelfDirected` (virtual, false). The action conformance suite accepts a self-directed action that moves alone in an empty world and is not searching | `Actions/AnimalAction.cs`, `Tests/EditMode/Conformance/ConformanceTests.cs` | ACT-04: "…except self-directed actions (steering), which have no target" |
| C5 | `Brain.ReadsGeneText` (virtual, true); V-05 ("action has no text gene") is skipped when the species' brain says false | `Brains/Brain.cs`, `Core/World.cs` | — |
| C6 | Test fixtures: the T-MUT suite adds a vector probe gene, so an operator may accept text, number **or vector** genes; the brain suite adds each brain's `[RequiresModule]` companions to the species, as the action suite does | `Testing/` (new `VectorProbeGene`), `ConformanceTests.cs`, `DecisionTests.cs` | 30 §4 |
| C7 | *(M16.7, only if profiling asks)* `SpatialIndex.ForEachWithin(p, r, among, visitor)`, the ring walk of `Nearest` without the "best" cut-off | `Core/SpatialIndex.cs` | — |

None of these changes names a species, sense or action (CORE-03). C1–C3 are what GENE-07
and SENSE-40 already promise.

## 4. The `EvoSim.Neuro` assembly

```
Assets/EvoSim/Neuro/
  EvoSim.Neuro.asmdef            → EvoSim.Runtime (no UnityEditor: the T-EDIT-02 check covers it)
  Genes/   NeuralGenomeGene.cs   NeuralInit.cs           Mutation/  NeuralMutation.cs
  Net/     NeuralLayout.cs       NeuralNet.cs            Brains/    NeuralBrain.cs
  Senses/  RayFanSense.cs        RayHitKind.cs   WorldRayCaster.cs   InteroceptionSense.cs   NeuralInput.cs
  Actions/ SteerAction.cs        SpeedLevel.cs           Recording/ NeuroStats.cs (IStatsColumns)
  Views/   RayFanView.cs (AnimalView: draws the fan, coloured by hit kind)
  Editor/  EvoSim.Neuro.Editor.asmdef: NeuralSpeciesMenu.cs, SteeringGridBuilder.cs,
           NeuralGenomeGeneEditor.cs (layout table), AnimalInspector panel (inputs, heads)
  Tests/   EvoSim.Neuro.Tests.asmdef (EditMode)
  Modules/ NeuroGrazer.prefab, NeuroBody.prefab
Assets/EvoSim/Scenes/     Neuro_Sandbox.unity, Neuro_WithJEV.unity
Assets/EvoSim/Scenarios/  S29_NeuroAlone, S30_NeuroWithJEV, S31_NeuroAsPrey
```

One tick, for one neural animal (reference phases, unchanged):

```
Sense ── RayFanSense.ReadAll (token)  ─┐
         InteroceptionSense (token)    │
Ask brains ── attachments: RayFan float[56], Interoception float[4]
          ── DecisionQuery{Species, Genome, Observation, Attachments}
          ── NeuralBrain.Ask → BrainAnswer.Later → worker: encode → forward → heads → outer product
          (JEV's batch for the other species is already in flight)
Choose actions ── waits for both ── draws from <species>/sampling (DEC-20)
Act ── SteerAction: intent = heading ± turn, walk/run/slow; contact: strike > scavenge > graze
    ── KinematicLocomotion moves; the act phase turns the heading toward the move
Breed (asexual if set) ── Hatch: NeuralMutation already done at conception (synchronous)
```

## 5. Components

### 5.1 `NeuralGenomeGene : Gene` (one locus, the whole network)

- `Kind => AlleleKind.Vector`, `ReadByBrain => true` (so the weights are part of the
  brain-visible genome key), `Decimals => 4`.
- Settings: `hidden` (default `[12]`), `activation` (tanh | sigmoid | relu), `init`
  (Xavier | OldLab ±10), `founders` (default 64), `founderSeed`, `clamp` (|w| ≤ 10).
- **Layout.** `NeuralLayout.For(species)` sets the input width from the species' senses in
  sense order. A sense that implements `INeuralSense` contributes its attachment width;
  any other sense contributes a one-hot of its tokens (so the energy `LevelSense` costs 3
  inputs). The output is factorised when every action is a `SteerAction` (§5.5), otherwise
  joint (one logit per action). That lets the same brain drive the reference prey too, as
  recipe 22 §7 sketches.
- **Founder pool** (GENE-20/21): `founders` vectors drawn from a private SplitMix64
  seeded by `founderSeed` and the layout hash, never by the run seed. Every run therefore
  starts from the same nets. There is no neutral allele.
- `Check`: right length for the current layout, every value finite and within ±clamp.
  A mismatch is V-71 before the run (§7).
- `Express`: sets no traits; for the genes block it writes one summary line
  (`- brain: "vec[810]#ab12cd34"`) so the prompt preview and Ask-the-Brain stay readable.
  The brain reads the values from `q.Genome` (C3), never by parsing text.

Size of the default animal: inputs 7 rays × 8 kinds + 4 interoception = 60;
(60+1)×12 + (12+1)×(3+3) = **810 weights** = 3.2 KB per allele.

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
- Crossover: v1 keeps `UniformCrossover`. With one locus, a sexual baby takes one parent's
  whole network (GENE-30). A per-neuron blend (`NeuronCrossover`) is M16.8.

### 5.3 `RayFanSense : BatchedSense, INeuralSense` and `WorldRayCaster`

- **Fan.** `rays` (7), `fov` (120°), `range` (the `vision` trait, else 20 m), symmetric:
  ray *i* at `−fov/2 + i·fov/(rays−1)` from the heading (one ray: straight ahead). This
  fixes the old −25…+15° bug. An optional rear ray.
- **Kinds** (`RayHitKind`, a `[Flags]` mask; channel order fixed by the enum), resolved
  for the viewer from the food web: `Food` (a resource layer in its diet), `Threat`
  (a species that eats it), `Prey` (a species it strikes), `Kin`, `OtherAnimal`,
  `Carcass` (one it may eat from), `Cover`, `Obstacle` (non-walkable ground, water,
  the world's edge, a Physics collider when `usePhysics` is on). `Water` can be split out.
- **Per ray:** the first hit along the ray. At equal distance the lower enum value wins,
  then the lower id (SPACE-08). Hidden, killed and gone animals and the viewer itself are
  skipped through `World.Queries` (SENSE-06, ACT-05).
- **How it marches:** DDA over `FoodGrid.Grid`/`HasItem` and `CoverLayer` cells (exact,
  cell 1 m); a fixed step (0.25 m) for other `ResourceLayer`s (`WithinReach`) and for
  `Ground.IsWalkable`/`IsWater`/`Inside`; ray–circle tests for animals (body radius 0.5 m)
  and carcasses, against candidates within range. v1 scans the species lists (a few hundred
  animals: about 0.3 ms per tick); C7 replaces the scan if profiling asks.
  `RequestRays` adds Physics rays only when `usePhysics` is on.
- **Encoding** (attachment, `NeuralInput{SenseIndex, Values}`): per ray, one channel per
  enabled kind holding the proximity `1 − d/range` in the channel of the kind it hit,
  0 elsewhere (0 = nothing seen, so no bias is needed). Optional `MLAgentsLayout` writes
  one-hot plus hit fraction (1 = no hit), as `RayPerceptionSensor3D` does, for §10.
- **Token:** `none` + one per enabled kind, the kind of the nearest hit over the whole
  fan; text "Rays: food 2-4 meters ahead." (V1/V2). Small, so SENSE-05 stays quiet.
  `AttachmentKey => null`: the query is never memoised (SENSE-41).
- Pure function of the start-of-tick state, no random draws (SENSE-03). `ReadAll` and
  `Read` give the same tokens (SENSE-31).

`InteroceptionSense : Sense, INeuralSense`: attachment = energy/max, stamina/max,
min(age/maturity, 1), ready-to-mate (0/1). Token = the energy level (low/medium/high), so
the situation text says something.

### 5.4 `NeuralBrain : Brain` (a world service)

- `Id => $"neural-{policy}-{tau:0.##}"`, `Memoizable => false`, `useCache` off,
  `AcceptsAttachments => true`, `CanMixSpecies => true`, `ReadsGeneText => false` (C5),
  `MaxActions` unlimited, `MaxPromptTokens => 0`, not strict.
- `[RequiresModule(typeof(NeuralGenomeGene))]`: validation offers to add it, and the brain
  conformance suite adds it (C6).
- **Ask:** on the main thread, encode each query into a pooled `float[]` (one-hot tokens and
  attachments, in layout order) and take the weights from `q.Genome[gene.Locus].Value.Vector`.
  Then `BrainAnswer.Later(n)` and `Task.Run`: forward pass, heads, row, `SetRow`/`Fail`,
  `Complete`. Below `syncBelow` queries (default 32) it computes inline and returns
  `BrainAnswer.Now`.
- **Policy:** `Sample` gives softmax with the brain's own τ per head. `Greedy` gives the argmax
  of each head as a one-hot (ties go to the lower index), the old net's deterministic
  behaviour. The World's τ (DEC-20) applies on top and is world-wide, so keep it at 1 in mixed
  worlds; the neural species' temperature lives in the brain.
- **Failures:** no genome (a tool-built query), wrong length, or a NaN gives `Fail(i, reason)`,
  never a made-up row; non-strict → uniform, counted (DEC-40).
- `NeuralNet` is allocation-free: `Span<float>` layers, weights read in place. It is the
  test oracle for the Burst path (M16.7).

### 5.5 `SteerAction : AnimalAction`

- Settings: `turn` (signed degrees per tick: −20 left, 0, +20 right; headings run clockwise
  from +z), `speed` (`Slow` | `Walk` | `Run`), `slowFraction` (0.5), `contacts` (Graze |
  Strike | Scavenge, checked against the diet: V-75).
- `SelfDirected => true` (C4), `Mates => mates` (default true), `Hunts => contacts has
  Strike`, `Rests => false`, `IsRelevant => true`.
- **Act** (only through `ActContext`, ACT-06): `dir = HeadingDirection(a.Heading + turn)`.
  Walk → `c.WalkTo(a.Position + dir·range)`; Run → `c.RunTo(…)`; Slow → `c.WalkTo(a.Position +
  dir·slowFraction·walk)` (Intent.Toward caps the step, MOVE-04). The act phase then turns
  the heading toward the move. A step that doesn't move (a wall) leaves the heading
  unchanged, and the kinematic locomotion's side step (MOVE-04) unsticks it. Then one
  contact interaction, the nearest candidate to `a.Position + dir·step`. It never calls
  `Search`/`Wander`.
- `SteeringGridBuilder` (editor) builds the grid of `SteerAction` GameObjects from turn bins
  and speed levels, names them, and orders them turn-major, which `NeuralLayout`'s
  factorised heads rely on (V-73 checks it).

### 5.6 The species template (`GameObject ▸ EvoSim ▸ New Neural Species`)

`NeuroGrazer`: `NeuroBody`, `Energy`/`Stamina`/`Metabolism`/`Starvation`/`OldAge` (reference),
`KinematicLocomotion`, `DecisionSchedule` (period 1, not staggered), Senses (`RayFanSense`,
`InteroceptionSense`), Actions (3 × 3 steering grid), Genes (`NeuralGenomeGene`), Mutation
(`NeuralMutation`), `Diet` (graze Grass), `MatingRule` (asexual), `Litter` (1–2), cap 120 /
floor 10 / initial 40, brain = `NeuralBrain` (added under the World's services if missing).
The menu accepts the new signature (V-12).

## 6. Side by side with JEV

- **Scene `Neuro_WithJEV`** = `Lab1_Small` (JEV prey and predators, unchanged) plus
  `NeuroGrazer` on the same Grass. Scenarios:
  - **S29 NeuroAlone** (no server, T2): the neural species alone on Lab1_Small's map.
    CI can run it.
  - **S30 NeuroWithJEV** (T4, JEV server): the neural species competes for grass.
  - **S31 NeuroAsPrey**: the JEV predator's diet also lists `NeuroGrazer` (strike +80).
    Its "Prey" sense covers it automatically, because the senses are species-agnostic.
  - S29/S30 with JEV replaced by `RandomBrain` or `ScriptedBrain` give the L0 versions.
- **What it does to the JEV species:** their signatures, senses and prompts don't change,
  so their answer caches stay valid. A situation seen before replays without a model call.
  Their streams are per species (RAND-03), so their draws don't shift. The events hash of
  the run changes, of course, and comparisons are made between runs of the same scenario.
- **Throughput:** JEV answers about 12 queries/s and sets the tick rate. The neural species
  decides every tick: about 150 animals × (rays + 810-weight forward pass) ≈ 1 ms of
  managed work, on a worker inside JEV's wait. Target: the neural species adds no measurable
  time to a JEV tick, and under 2 ms per tick in S29 at 300 animals (20 §9).
- **What to measure:** population per species, grass eaten per species, deaths by cause,
  the neural species' mean speed, turn shares and food per meter (`NeuroStats` columns),
  and whether a species needs newcomers (POP-05).

## 7. Validation (new codes V-70…V-79)

| Code | Severity | Check |
|---|---|---|
| V-70 | error | a species with `NeuralBrain` has no `NeuralGenomeGene` (fix: add one) |
| V-71 | error | founder vectors don't match the layout (senses or actions changed): fix "rebuild founders" |
| V-72 | error | an `INeuralSense`'s attachment width changed since the layout was built |
| V-73 | error | the steering grid is incomplete, duplicated or not turn-major |
| V-74 | warning | a self-directed species decides less often than every tick (it steers late) |
| V-75 | warning | a contact the diet can't use (Strike without a striking diet entry…) |
| V-76 | error | ray fan: rays < 1, fov ∉ (0, 360], range ≤ 0, no kind enabled |
| V-77 | info | the World's sampling τ ≠ 1 also tempers the neural species' rows |

## 8. Tests (EditMode unless marked)

| Id | Test |
|---|---|
| T-NEURO-01 | vectors equal at 4 decimals → one allele; differ in the 3rd → two; canonical stable across runs; R-01 and every existing golden unchanged (C1, C2) |
| T-NEURO-02 | gene conformance (T-GENE-01) for `NeuralGenomeGene`; the founder pool is identical in two worlds with different run seeds (GENE-21) |
| T-NEURO-03 | the T-MUT suite passes for `NeuralMutation`: same stream → same mutant; the mutant differs from its parent; `Check` passes; clamped |
| T-NEURO-04 | fan angles symmetric for 1, 2, 5, 7 rays; food 3 m ahead → centre ray Food proximity 0.85; prey in cover from the viewer → not seen; killed → not seen; nearest wins; equal distance → enum order |
| T-NEURO-05 | `ReadAll` = `Read` over 500 animals, with and without Physics rays (SENSE-31, T-SENSE-09) |
| T-NEURO-06 | brain conformance (T-DEC-03) with the companions (C6), on the reference prey (joint) and the steering species (factorised); rows sum to 1; Greedy is one-hot; a query without a genome → `Fail` |
| T-NEURO-07 | factorised row = product of marginals; the marginals recovered from 100 000 draws match within 4 s.e. (DEC-20) |
| T-NEURO-08 | async: `Later` with a slow `FakeHttpTransport` JEV in the same world → the tick completes and neural rows equal the synchronous ones; Freeze vs other wait modes → the same hash (RAND-11) |
| T-NEURO-09 | action conformance with `SelfDirected` (C4): only intents, moves alone, never searching; left-walk for 9 ticks → heading −20°/tick, circle radius ≈ walk / 0.349 rad |
| T-NEURO-10 | contacts: grass 0.8 m ahead → walks and grazes the same tick; strike only with a striking diet; at most one interaction per tick (ACT-03) |
| T-NEURO-11 | asexual breeding when ready; litter cost paid; the baby's network is mutated with probability `Rate` |
| T-NEURO-12 (T2) | S29 for 5 000 ticks, the same seed twice → the same hash; mean grass per animal-tick of evolved nets ≥ that of the founder pool under the no-mutation control (measured first, then pinned as a gate) |
| T-NEURO-13 (T4) | S30 with JEV: runs 10 minutes, never stalls on the neural brain, JEV cache hit rate within ±5 points of the same run without the neural species |

## 9. Milestones (M16, after M15)

| Step | Delivers | Done when |
|---|---|---|
| **M16.1** Core | C1–C6 with their tests; spec text in 07, 08, 09, 13, 30 | 466 + new EditMode tests green; R-01 unchanged |
| **M16.2** Genes | `NeuralLayout`, `NeuralNet`, `NeuralGenomeGene`, `NeuralMutation` | T-NEURO-01…03 |
| **M16.3** Senses | `WorldRayCaster`, `RayFanSense`, `InteroceptionSense`, `RayFanView` | T-NEURO-04, 05; fan visible in Sandbox |
| **M16.4** Brain | `NeuralBrain` (sync), Sample/Greedy, joint and factorised | T-NEURO-06, 07; drives the reference prey |
| **M16.5** Actions | `SteerAction`, grid builder, contacts, mating | T-NEURO-09…11 |
| **M16.6** Species and scenes | template menu, `Neuro_Sandbox`, `Neuro_WithJEV`, S29–S31, `NeuroStats`, inspector panel, docs chapter `Docs/Interface/16-neural-species.md`, site page `extending/neural-species.md` | T-NEURO-12; the owner watches S29 and S30 |
| **M16.7** Concurrency and scale | async `Later` path, pooled attachments, one fan per animal per tick (a per-tick buffer in a world service), optional Burst `IJobParallelFor` forward pass and ray march behind `ReadAll` (same results as the managed oracle), C7 if needed | T-NEURO-08, 13; the 20 §9 budget at 300 neural animals |
| **M16.8** *(optional)* | sexual mode with `NeuronCrossover`; export/import champion networks as a founder-pool asset; §10 | — |

## 10. Later: a DRL brain on the same species

The species is shaped like an ML-Agents agent: a ray-perception observation, two discrete
action branches (steer, speed), and a reward that could be energy gained. A PPO policy
trained in ML-Agents (exported to ONNX, run with Unity's inference engine) can become one
more `Brain` for this species (`OnnxBrain`, `Memoizable => false`). Evolved and trained
policies could then be compared on the same world, or trained nets seeded into the founder
pool. Out of scope for M16. The `MLAgentsLayout` ray encoding (§5.3) keeps the door open.

## 11. Owner decisions

1. **Allele kind:** a core `Vector` kind (C1–C3, recommended) or text-encoded weights
   (no core change, see §12)?
2. **Steering bins:** left/straight/right (recommended) or left/right only like the
   old lab; speed levels slow/walk/run (recommended) or walk/run.
3. **Decision period** of the neural species: 1 (recommended; it steers like the old net) or
   the World's 4 staggered.
4. **Breeding:** asexual when ready (recommended for v1, the old lab's regime) or sexual on
   contact.
5. **Role in the JEV world:** competitor for grass (S30), prey of JEV predators (S31), or
   both as separate scenarios (recommended).
6. **Physics colliders** in the rays (trees): off by default?

## 12. Alternatives considered

| Alternative | Why not (now) |
|---|---|
| Weights as a text allele (base64) | No core change, but kilobytes of text in birth events, `alleles.jsonl`, the overlay and prompt previews. The species-word check (World.Checks) can find species names inside base64. Every decision parses text |
| One `NumberGene` per weight | 810 gene components per species, 810 registry loci, prompt and recording noise |
| Continuous outputs (turn angle, speed) through a new brain → action channel | Breaks CORE-06/DEC-11 and the memo/cache model. Discrete bins with a decision every tick give the same behaviour |
| Reading the brain's row (`Animal.LastProbabilities`) in the action as a continuous control | Misuses an inspector field; couples action to brain |
| Physics raycasts only | Animals, food and carcasses aren't colliders; they would need a GameObject per animal (against ARCH-03) |
| Per-layer loci | Crossover mixes layers that were never trained together; one locus plus a per-neuron crossover later is cleaner |

## 13. Risks

- **Memory:** every allele keeps its vector for the run's lineage, about 3.2 KB × 10 000
  mutants ≈ 32 MB. Mitigation: float16 storage, or drop the values of extinct alleles after
  they reach `vectors.jsonl`.
- **Determinism across code paths:** the managed and Burst forward passes may differ in the
  last bit. Pick one path per run; the Burst job uses `FloatMode.Strict` and is checked
  against the managed oracle (RAND-12 already limits hash comparisons to one machine).
- **Ecology balance:** a species that decides every tick may outcompete the JEV prey for
  grass. Tune its metabolism (energy per meter, run stamina) and cap before reading results.
- **The world-wide τ** also tempers neural rows (V-77). Keep τ = 1 in mixed worlds.
- **Hidden assumptions:** windows or tools that treat "not text" as "number" (Gene Pool,
  Ask-the-Brain, integrity gates) need a vector branch. They are listed in C2/C3; the
  M16.1 tests read every window's text for vector genes.
