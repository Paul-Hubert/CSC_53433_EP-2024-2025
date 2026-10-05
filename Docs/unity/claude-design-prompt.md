# Claude Design prompt — Genetic Agents Unity architecture

Copy everything inside the fence below into Claude Design. It is
self-contained: Claude Design does not need the repo. The names match the
docs in this folder exactly, so the design and the docs stay in sync.
After the first render, iterate in the same conversation, for example:
"move TemperatureDecorator inside MemoDecorator and show what changes", or
"add a Weather species to frames 3, 4 and 10".

````text
# Brief

Design an EDITABLE, multi-frame architecture board for "Genetic Agents", a Unity framework for a
Master 2 course. Students evolve animals whose GENOME IS A LIST OF SHORT NATURAL-LANGUAGE GENES
(e.g. "Eat whenever food is close."). Each decision, an LLM (Ollama, local or cloud) reads the genes
plus a text description of what the animal perceives and returns a probability for each action.
Reproduction mixes parents' genes per locus; mutation rewrites a gene with word operators or an LLM.
Selection is implicit: animals that eat, avoid predators and mate leave offspring.

The board explains the ARCHITECTURE: its layers, Unity components, ScriptableObject assets,
pluggable strategies, the tick pipeline, the decision and evolution flows, the editor tooling, the
design patterns, and how the design absorbs big changes. Audience: students (readability first) and
the course owner (who will edit the board to change the design). Status: proposal, not implemented.

# What I need from the output

- One canvas with 11 frames (artboards), laid out left→right, top→bottom, each titled
  "NN — Title", 1600×1000 each, plus a small legend frame.
- Every box, chip, arrow and label must be a SEPARATE, EDITABLE element with live text (no
  flattened images, no text baked into pictures). Reuse a small set of components (below) so a
  change to one card style updates everywhere.
- Use the EXACT names given in this brief. Do not invent classes, rename things or add features.
  If something seems missing, add a yellow "question" sticky note instead of inventing it.
- Light and dark theme variants of the colour tokens.
- Interactive prototype behaviour if supported: hover a node → tooltip with its responsibility, its
  pattern(s) and its file path; a toggle "Highlight student extension points" that dims everything
  except the extension points; a filter by pattern (chips in the legend); clicking a node
  highlights the same node in every other frame.

# Visual language (design tokens)

Node categories, each with one colour token, one shape and one icon:
| Category                    | Token        | Shape                       | Examples |
| Unity component (per agent) | comp/teal    | rounded card, left accent bar | AgentRoot, FoodSense, EatAction |
| ScriptableObject asset      | asset/amber  | card with folded corner      | SpeciesDefinition, FounderPool |
| Pluggable strategy          | strat/violet | pill with a dropdown caret ▾ | UniformCrossover, OllamaBackend, TemperatureDecorator |
| Simulation system           | sys/blue     | wide rectangle in a swimlane | DecisionSystem, BirthSystem |
| Core type (plain C#)        | core/slate   | plain rectangle, monospace   | Genome, Observation, ActionDistribution |
| Adapter / external          | ext/green    | hexagon                      | TerrainWorld, Ollama server, ML-Agents policy |
| Editor tool                 | editor/rose  | dashed-border card           | Simulation Doctor, Brain Debugger |
| Event                       | event/grey   | small flag                   | AgentBorn, GeneMutated |
Arrow types (distinct line styles, labelled in the legend):
  owns/contains (solid), references (thin solid, hollow arrow), calls (solid, filled arrow),
  wraps (double line — decorators), publishes/subscribes (dotted), discovered by reflection
  (dash-dot, magnifier icon).
Badges: "extension point" (puzzle-piece icon), "async/await barrier" (hourglass), "RNG stream"
(dice chip with the stream name), "pattern" chips (small rounded tags, e.g. [Decorator]).
Typography: a clean sans-serif for titles and labels, monospace for class names. Generous spacing;
max ~12 words of body text per node; details go in tooltips.

# Frames and their exact content

## 00 — Legend
Category swatches, arrow styles, badges, and the 22 pattern chips (see frame 08).

## 01 — Layers (overview)
Horizontal stacked bands, top to bottom:
1. PRESENTATION: AgentView · AnimatorView · labels   (note: "read state, listen to events")
2. AUTHORING (assets): SimulationProfile · SpeciesDefinition · FounderPool · prefabs ·
   PromptTemplate · PhraseBook · ExperimentDefinition
3. SIMULATION: SimulationRunner ("the only Update()") → Simulation → systems[] by phase →
   agents' components; DecisionScheduler · Population · AgentFactory · RunRecorder
4. DOMAIN CORE (plain C#): Genome · Allele · AlleleRegistry · Observation · DecisionRequest ·
   ActionDistribution · RngStreams · EventBus · ValidationReport · interfaces
5. ADAPTERS: IWorld → GridWorld | TerrainWorld(CustomTerrain); AgentLocomotion → Grid | Kinematic |
   Rigidbody | MLAgents; IDecisionBackend → Ollama HTTP | Rules | NeuralNet | ONNX
A vertical side panel on the right: EDITOR (separate assembly, never shipped): validation ·
inspectors · windows · gizmos.
Bottom strip: "The five placement rules" as 5 cards:
  per-agent state/capability → Component · shared data → ScriptableObject asset ·
  swappable rule → [SerializeReference] class picked from a dropdown · world-wide per-tick process →
  ISimulationSystem · checks/visualisation/management → Editor assembly.

## 02 — Assemblies & dependency rule
Nodes: GeneticAgents.Core · GeneticAgents.Runtime · GeneticAgents.LLM · GeneticAgents.Editor ·
StudentWork · StudentWork.Editor · GeneticAgents.Tests.
Edges ("references"): Runtime→Core, LLM→Core, Editor→Core/Runtime/LLM, StudentWork→Core/Runtime/LLM,
StudentWork.Editor→Editor+StudentWork, Tests→all. Core references nothing of ours.
A red crossed-out edge Framework ✕→ StudentWork with the note "never referenced — student types are
discovered by interface (TypeCache)". Side table: what each assembly contains (one line each).

## 03 — Agent anatomy (the prefab)
A tall "prefab" card titled "Herbivore (prefab)" listing components top to bottom, grouped:
AgentRoot · Metabolism · Brain · GridLocomotion
Senses/: EnergySense · AgeSense · FoodSense · PredatorSense · NeighbourSense
Actions/: EatAction · FleeAction · FollowAction · WanderAction (fallback) · RestAction · MateAction ·
AttackAction
Model/: AgentView · AnimatorView
Each sense shows the observation field it emits as a chip ("food = none|far|near|here").
Each action shows a "locus" link to a LocusDefinition asset (eat, flee, …) on the right side.
Base-class strip at the bottom: AgentComponent (OnSpawned, OnDespawned, Validate) ← AgentSense,
AgentAction, AgentLocomotion, Metabolism, Brain. Optional hooks as small sockets on the card:
IAgentTick · IDeathCheck · IPhenotypeReceiver · IActionOverride.
A Bridge illustration: AgentAction ("what": MoveToward / MoveAwayFrom / Wander) ⇄ AgentLocomotion
("how") with four interchangeable bodies: GridLocomotion · KinematicLocomotion ·
RigidbodyLocomotion · MLAgentsLocomotion.
Callout: "No Update() in simulation components — systems call them in a fixed order."

## 04 — Asset graph
ExperimentDefinition (seeds[], ticks, conditions[] → modifiers[]) → SimulationProfile
(simulationSeed, worldSeed, statsEveryTicks) → WorldDefinition (GridWorldDefinition |
TerrainWorldDefinition), SpeciesRelations, species[] → SpeciesDefinition ×N, systems[] (strategy
list), sinks[] (JsonlEventSink, CsvStatsSink).
SpeciesDefinition → prefab (AgentRoot), GenomeSchema → LocusDefinition ×N (kinds: Action,
Temperament, Free, Trait), FounderPool (version, frozen; per locus: neutral + alleles), PromptTemplate
(placeholders {actions} {genes} {temperament} {situation} {ask}), renderer ▾ (TerseRenderer |
PhraseRenderer → PhraseBook), decision ▾ (decorator chain), canMate ▾, crossover ▾, mutation
(MutationPipeline), development ▾, population ▾.
Side table "Experiment conditions as modifier stacks": C1 FULL (none) · C2 NO-MUT SetMutationRate(0) ·
C3 SHUFFLED WrapDecision(GenomeShuffleDecorator, outermost) · C4 RANDOM-FOUNDERS
UseFounderPool(control) · C5 RULE-BASED ReplaceTerminalBackend(RuleBasedBackend) · C7 ASEXUAL
SetCrossover(CloneParent) + SetMating(Solo).

## 05 — Tick pipeline
Six horizontal swimlanes = phases, in order: World · Decide · Act · Interact · Lifecycle · Record.
Systems in lanes (with order numbers and RNG stream chips):
  World: FoodRegrowthSystem [food] (+ dashed slot "SeasonsSystem — student example")
  Decide: DecisionSystem [sampling] with an hourglass badge "await: LLM calls; tick waits (lockstep)"
  Act: ActionSystem [actions] ("species by actOrder; random permutation; fallback if unavailable;
       locomotion; metabolism cost"), PhysicsStepSystem (optional, dashed)
  Interact: MatingSystem → BirthSystem [mutation] (hourglass: "async if LLM mutates") →
            PredationSystem [predators]
  Lifecycle: AgentTickSystem → DeathSystem → ImmigrationSystem [agents]
  Record: StatsSystem
Above the lanes: SimulationRunner → Simulation.StepAsync() loop. Below: EventBus with dotted arrows to
RunRecorder, Population Dashboard, Genome Browser, views. Callout "Observers never change the
simulation". Small timing toggle: DecisionSystem.timing = Lockstep (default) | Realtime (VR/demos).

## 06 — Decision flow
Left→right: Senses (component chips) → ObservationBuilder → Observation ("energy=low|food=near|…",
CanonicalKey) → two branches: IObservationRenderer (TerseRenderer / PhraseRenderer) → text, and
ObservationEncoder → one-hot floats → DecisionRequest (AgentId, GenomeKey, Genes, Phenotype,
Observation, ActionSet) → DecisionScheduler → the DECORATOR CHAIN drawn as NESTED boxes:
  TraceDecorator ⊃ TemperatureDecorator (τ = 1.0) ⊃ MemoDecorator ⊃ PersistentCacheDecorator ⊃
  FallbackDecorator ⊃ { primary: RateLimitDecorator(2) ⊃ OllamaBackend (model, mode: Points |
  Logprobs | Table | KSample) ; fallback: RuleBasedBackend }
→ ActionDistribution[] (Probabilities, Source, Heads) → "sample with Rng('sampling')" → Brain.Current.
An external hexagon "Ollama server (local or cloud) — key only from an env var" attached to
OllamaBackend. A palette of other terminal backends: RandomBackend · RuleBasedBackend ·
UtilityBackend · NeuralNetBackend · OnnxBackend · ScriptedChaseBackend; other decorators:
GenomeShuffleDecorator (must be OUTERMOST — red rule badge "ShuffleOutsideMemoRule") ·
AvailabilityMaskDecorator · EpsilonExploreDecorator (student example, dashed).
Bottom band: "Option A — LLM as brain (per decision)" vs "Option B — LLM at birth: LlmDevelopment →
Phenotype → UtilityBackend", showing that switching is two dropdowns.

## 07 — Evolution flow
MatingSystem with a specification tree: canMate = All( IsAdult, EnergyAbove 50, ChoseAction "mate",
Not( BredThisTick ) ) and its sentence "adult AND energy > 50 AND chose mate AND NOT bred this tick".
→ birth queue → BirthSystem: crossover ▾ (UniformCrossover | OnePointCrossover | LocusBlockCrossover |
CloneParent | DiploidCrossover) → MutationPipeline (perLocusRate 0.03; weighted operators:
IntensityOperator · NegateOperator · ConditionSwapOperator · SynonymOperator ·
FounderReintroduceOperator · LlmRewriteOperator · GaussianOperator; then guards in order:
CleanTextGuard → MaxWordsGuard → CharsetGuard → ChangedGuard; fallback operator) → development ▾
(NoDevelopment | TraitDevelopment | LlmDevelopment) → Phenotype → AgentFactory.Spawn → AgentBorn event.
Side: AlleleRegistry as a shared table (Flyweight) — genomes hold ids like "eat:12"; ImmigrationSystem
refills from FounderPool below the floor; population ▾ (FloorAndCapPolicy init 30 / floor 10 / cap 60 |
HallOfFamePolicy | FixedCountPolicy); DeathSystem (starvation, old age, predator, attacked).
Banner: "Selection is implicit — fitness is measured, not used."

## 08 — Pattern map
Grid of 22 pattern cards (name, one-line problem, where): Component · Template Method · Type Object ·
Prototype · Strategy · Plugin discovery · Decorator · Null Object · Pipeline/Chain of Responsibility ·
Specification · Bridge · Adapter · Facade · Flyweight/Registry · Command · Observer (event bus) ·
Memento/Snapshot · Systems pipeline · Context Object · Factory + Object Pool · Humble Object · State.
Below, "Chords" (how patterns combine) as rows of chips with the files a student writes:
  New behaviour = Component + Template Method + Type Object → 1 action class, 1 locus asset, alleles
  New perception = Component + Pipeline → 1 sense class (+ phrases)
  New algorithm = Strategy + Plugin discovery + Null Object → 1 class
  Cross-cutting concern = Decorator over Strategy → 1 decorator
  New rule from parts = Specification + Strategy → 0–1 class
  New experiment condition = Prototype + Command → 0–1 modifier
  New body = Bridge + Adapter → 1 locomotion
  New world = Adapter + Facade → 1 IWorld + 1 WorldDefinition
  New world rule = Systems pipeline + Context Object → 1 system
  New view/analysis = Observer + Memento → 1 subscriber or window
  New safety check = Plugin discovery + Chain → 1 ValidationRule
A small "anti-patterns avoided" column: GameManager singleton, Animal inheritance tree,
enum ActionType + switch, boolean flags, Update() per agent, UnityEvent wiring, FindObjectOfType,
fixed Observation struct.

## 09 — Editor tooling
Validation flow: IValidatable.Validate (local, on components/assets) + ValidationRule<T> (cross-object,
discovered by attribute) → ValidationReport (Error / Warning / Info, message, why, one-click fix) →
four consumers: Inspector help boxes · Pre-Play hook ("errors block Play") · Simulation Doctor window ·
CLI/batch (exit code 2). List 8 example rules as chips: LociMatchActionsRule,
FounderPoolCoversSchemaRule, PromptPlaceholdersRule, DecisionChainRule, ShuffleOutsideMemoRule,
ObservationSpaceRule, NoSecretsInAssetsRule, LocomotionMatchesWorldRule.
Windows as cards with a mini wireframe each: Simulation Doctor · Brain Debugger (observation, prompt,
answer, distribution bars, source, What-if) · Genome Browser (allele table, frequency areas, lineage
tree) · Gene Playground (heatmap p(action|obs,genome), ΔP, MI_G, MI_O, gates) · Population Dashboard ·
Experiment Runner · New Feature Wizard.
Inspector tools: SubclassSelectorDrawer (dropdown of implementations) · DecisionChainDrawer ·
ConditionDrawer · GeneTextDrawer (word-count bar) · SpeciesDefinitionEditor (genome contract table) ·
FounderPoolEditor. Scene view: Overlay toolbar (play/pause/step/speed) and layers Walkability · Food ·
Perception · Intent · Lineage colours · Threats; note "[DrawGizmo] drawers live in the Editor
assembly; runtime only exposes DebugInfo". Script templates menu: Sense · Action · Locomotion ·
Decision Backend · Decision Decorator · Mutation Operator · Gene Guard · Agent Condition ·
Simulation System · Profile Modifier · Validation Rule · Gizmo Drawer · Editor Window.

## 10 — Change scenarios
A heat-map matrix: rows = scenarios, columns = seams S1 Capabilities · S2 Content · S3 Decision chain ·
S4 Evolution rules · S5 Tick pipeline · S6 Body & world · S7 Tooling · Framework edit.
Cells: ● change, ○ optional, empty untouched; last column "none" for every row.
Rows (● = change, ○ = optional; every unlisted seam is untouched; Framework edit = none for all):
 1 Predators that evolve (co-evolution) — ● S2 S3 S4 · ○ S1 S7
 2 Continuous terrain from the terrain/foliage labs — ● S2 S6 · ○ S7
 3 Physics bodies driven by a DRL controller — ● S5 S6 · ○ S1 S7
 4 VR interaction with the animals — ● S1 S2 S5 · ○ S6 S7
 5 Option B: LLM at birth — ● S3 S4
 6 Numeric genomes / neural-network brain — ● S2 S3 S4
 7 Agents that communicate — ● S1 S2 · ○ S5 S6
 8 Explicit-fitness generational GA — ● S5 · ○ S4
 9 Distilled on-device model (ONNX) — ● S3
10 Memory within a lifetime — ● S1 · ○ S2 S3
11 Several decision heads (direction, speed) — ● S1 S3 (uses the reserved Heads seam)
12 Another LLM provider — ● S3 · ○ S4
Featured diagram: hierarchical control for scenario 3 — genes + observation →(LLM, ~1 Hz)→ "flee" →
FleeAction.Tick → Locomotion.MoveAwayFrom (goal) → MLAgentsLocomotion → ONNX policy (ML-Agents,
inference only) → ArticulationBody joint targets; PhysicsStepSystem runs k substeps
(RequestDecision → Academy.EnvironmentStep → Physics.Simulate); Metabolism receives
EffortThisTick = Σ|τ·ω|·dt. Caption: "Same genomes on a grid token or a ragdoll."

## 11 — Student cheat-sheet
The five placement rules (big), then 11 recipe cards (R1 Hydration state, R2 ThirstSense/WaterSense,
R3 DrinkAction + drink locus, R4 SeasonsSystem, R5 HungerFirstBackend, R6 EpsilonExploreDecorator,
R7 DropConditionOperator, R8 NearWater condition, R9 DrinkNeedsWaterRule, R10 MutationFeedWindow,
R11 SetRenderer modifier), each showing: files to add, where they go (StudentWork/), and
"framework edits: 0". Footer: "After each change, open the Simulation Doctor."

# Tone and quality bar
Clean, technical, calm. Prefer whitespace and alignment over decoration. No stock illustrations or
emojis except the defined icons. Every frame must be readable at 100% zoom on a laptop screen. Keep
node text short; put detail in tooltips. Mark the whole board "Proposal — not implemented yet".
````

## Tips for editing the result

- Ask for one frame at a time when changing the design ("frame 06 only: …"),
  so the other frames keep their layout.
- If you rename a class on the board, rename it in these docs too (search
  the `Docs/unity/` folder); the names are meant to match one-to-one.
- To explore an alternative, ask Claude Design to duplicate a frame as
  "06b — alternative chain" instead of overwriting the original.
