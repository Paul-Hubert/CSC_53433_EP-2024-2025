Design a **full architecture brief** for the Unity version of the teaching lab "Genes Are Prompts": a long-form technical document with UML diagrams, a design-pattern catalogue, worked examples of custom setups and how each is done, the editor tooling, and the verification plan. It is the reference that the course staff build from and that students read when they extend the system.

The one idea to get across: **the system is a tree of small, replaceable components. A student who understands one base class can read, reimplement or invent one behaviour without touching the rest.**

Nothing is built yet. Label the brief **"Proposed architecture, v1 (2026-10-10)"** everywhere. If you can read the repository, the source of truth is `Docs/Interface/` (README first, then 01–40); where it and this message disagree, the documents win. Otherwise use only the facts in this message. Where a diagram needs a detail that isn't given, write "to be decided" in it; don't invent classes, methods or numbers.

## Audience and goal
- Main audience: the course staff who will implement it in Unity 6000.3, and Master 2 students (AI and computer graphics) who will extend it. They know C#, Unity's GameObject/component model and what an LLM is; they may not know UML well or evolutionary algorithms.
- Second audience: the course owner, checking that every decision taken is reflected.
- After the brief, a reader should be able to: (1) draw the GameObject tree of a world from memory; (2) say which base class to subclass for a new sense, action, gene, stat, locomotion, rule, brain, mutation operator or tick phase, and which methods to override; (3) follow one tick and one birth step by step; (4) explain how determinism and the answer cache work; (5) set up any of the custom setups below; (6) know which tests and validators will judge their work.

## Format
- **The brief**: a multi-page document (A4 portrait, or a scrolling web page with a sticky table of contents), chapters A–G below, page numbers, running chapter titles.
- **The diagrams**: every diagram also exported as a standalone 16:9 frame (1920 × 1080) so it can be projected; body text at least 24 px at that size.
- **One poster frame** (A1 landscape or a large canvas): the whole system on one sheet, from the GameObject tree on the left, through one tick in the middle, to the outputs and tests on the right.
- **Notation**: UML 2.5 for class, sequence, state, activity and component diagrams. Abstract classes and methods in italics; `«interface»`, `«service»`, `«species module»`, `«world module»`, `«data»`, `«editor»`, `«test only»` stereotypes; composition, aggregation and dependency arrows used correctly; multiplicities shown. Every diagram has a legend and a one-line caption saying what to look at.
- **Code**: C# snippets in a monospace font with syntax colouring, at most 15 lines each, taken from the facts below. Comments in the snippets cite rule ids (e.g. `// MOVE-04`).
- **Rule ids**: the contract numbers its rules (`SENSE-01`, `MOVE-04`, …). Wherever a diagram or example shows a rule at work, tag it with its id in a small pill.

## Visual direction
- Concept: an engineer's field notebook. Clean, quiet, generous white space; diagrams carry the explanation, text labels them.
- One colour family per layer, used consistently in every diagram and on the poster: **world modules** (phases and services), **species modules** (genes, senses, actions, locomotion, rules), **data** (animal records, genomes, observations, queries), **external** (the JEV and Ollama servers, files on disk), **editor-only**, **test-only**. Things that are planned but not in v1 (NavMesh and physics locomotion, speciation, camera senses, real time) are drawn with a dashed outline and the tag "later".
- Use shape and pattern as well as colour so it works in greyscale and for colour-blind readers.
- No robots, glowing brains, neon gradients or stock "AI" imagery. The prey and predator can be simple, friendly top-down shapes.
- No university logos or branding.

## Chapters and frames

### A. Orientation
1. **Title.** "Genes Are Prompts in Unity — architecture brief". Subtitle: "a tree of replaceable components". The version label.
2. **The loop.** Genes (sentences, or numbers) → senses → observation and situation text → brain (batched, cached) → probabilities → an action is drawn → action gives an intent → the species' locomotion moves the animal → interactions (graze, strike, scavenge) → metabolism → deaths → mating → eggs → mutation → births. No fitness function anywhere: animals that eat, escape and mate leave more children.
3. **Decisions that shape the design.** A table from the decisions list below, grouped into: space and movement, species and food, genes and mutation, brains, the tick, teaching, infrastructure.
4. **How to read this brief.** Contract (what must hold, numbered rules) → architecture (how it is built) → patterns → custom setups → editor → verification. A map of `Docs/Interface/` 01–40.

### B. The architecture
5. **The GameObject tree** of the Lab 1 world, drawn as an annotated hierarchy with the component badges (tree below). Callouts: "folders are only folders", "order within a kind is meaningful", "disabled = absent", "prefabs refer to world things by role or name".
6. **Discovery and ownership.** Four mini-trees, each marked valid or invalid: a gene under an action (bound to it); a free gene; a disabled module (absent); a species inside a species (error V-01). Show `World.Initialize`'s ten steps as a numbered strip.
7. **UML class diagram: the core.** `World`, `WorldModule`, `TickPhase`, `WorldService`, `Species`, `SpeciesModule`, `Animal`, `Genome`, `Allele`, `AlleleRegistry`, `StatId`, `TraitId`, `Observation`, `DecisionQuery`, `Pending`, `TickContext`, `RandomStreams`.
8. **UML class diagram: species modules.** `Gene` (`TextGene`, `NumberGene`); `Sense` (`BatchedSense`, `LevelSense`, `AgeSense`, `NearestResourceSense`, `NearestAnimalSense`, `NearestCoverSense`); `AnimalAction` (`EatAction`, `FleeAction`, `HideAction`, `FollowAction`, `RestAction`, `MateAction`, `HuntAction`); `Locomotion` (`KinematicLocomotion`; dashed: `TerrainLocomotion`, `NavMeshLocomotion`, `PhysicsLocomotion`); `Energy`, `Stamina`, `Metabolism`, `Digestion`; `Diet`; `Edible`; `MatingRule`, `Litter`, `UniformCrossover`, `Incubation`; `DeathRule` (`Starvation`, `OldAge`); `CapRule`, `FloorRule`; `MutationOperator` (`LlmMutation`, `GaussianMutation`); `Body`.
9. **UML class diagram: services.** `Ground` (`FlatGround`, `TerrainGround`, `NavMeshGround`); `ResourceLayer` (`FoodGrid`, `TerrainGrassFood`); `CoverLayer`; `EntitySystem<T>` (`CarcassSystem`, `EggSystem`); `Brain` (`RandomBrain`, `HttpBrain` → `JevBrain`, `OllamaPointsBrain`; `ScriptedBrain` «test only»); `DecisionMemo`, `AnswerCache`, `MutatorService`, `RunRecorder`, `LiveStatistics`, `SpatialIndex`.
10. **UML component diagram.** Assemblies `EvoSim.Runtime`, `EvoSim.Http`, `EvoSim.Editor`, `EvoSim.Testing`, `EvoSim.Tests.EditMode`, `EvoSim.Tests.PlayMode` and their dependencies (Runtime never depends on Editor); external: the JEV server (vLLM, `localhost:8000`), Ollama for the mutator (`localhost:11434`), the answer-cache files (JSON lines), the run output folder. Everything runs on the owner's computer.
11. **UML activity diagram: one tick.** The eleven phases in order, the "waits for" markers on Choose actions and Hatch, and the act phase expanded: for each group (from the act order) plan → move → interact → settle.
12. **UML sequence diagram: a decision tick.** SensePhase picks who decides, builds spatial indexes, reads senses (one raycast batch) → AskBrainsPhase checks the memo and sends one batch per brain → ChooseActionsPhase reports `Pending` until the answers are in → answers stored in memo and cache → actions drawn with the sampling stream. Show a memo hit and a miss.
13. **UML state machine: the World's run loop.** States Idle, Running, Waiting (Responsive mode: Unity keeps rendering, the tick resumes at the same phase), Blocked (Freeze mode: the main thread waits), Paused, Stepping, Stopped. Note: both wait modes give the same events hash.
14. **UML sequence diagram: a birth.** BreedPhase finds a mate seeker and a ready partner in reach → `Litter` draws 2–4 and cuts it to what the parents can pay → per baby: `UniformCrossover`, then each locus rolls the mutation rate and the operator draws instruction and seed for every try **at conception** → `MutatorService` sends all first tries in one batch, then a redraw round for rejected answers → eggs → HatchPhase waits until each egg's genome is complete → the animal appears → a `birth` event. Incubation is 0 by default (the birth waits in the same tick); a few ticks hide the mutator's latency.
15. **UML state machine: an animal's life.** Egg → newborn (no action) → deciding → acting (searching when nothing is in sight) → busy (digesting, no decision, no movement) → back to deciding → dead by starvation, old age, killed or migrated.
16. **Prompt anatomy.** The default prey prompt (below) annotated: which component wrote each line (species header, action descriptions, search rule, locomotion speeds, stamina, mating rule, genes block, situation from the senses, the answer instruction hard-coded in the brain), and where the numbers come from (placeholders filled from component settings). Then the same content as a JEV request.
17. **Food web and edibility.** Two sides: an `Edible` component on whatever can be eaten (how: graze, strike, scavenge; how much energy; what a kill leaves), a `Diet` on the eater (what it eats, energy scale). Draw the Lab 1 web (grass → prey → predator, prey carcasses → predator) and a matrix "can X eat Y?" with the reasons (no Edible → never). Threats are derived: whoever's diet strikes you.
18. **Intents and locomotion.** Action → `Intent` (direction, walk or run, maximum distance) → `Locomotion.Move` → actual displacement → metabolism and stamina. Show the kinematic reference (straight line, slide along obstacles, random sidestep when stuck) and, dashed, a physics locomotion that doesn't follow the direction exactly ("whatever it does is the tick's result").
19. **Determinism.** Named random streams per purpose and species (table below); the events hash; replay from the answer cache; "compared on one machine only".
20. **Data and scale.** Animals as plain data owned by their species; modules as shared logic; the batch entry points (`ReadAll`, `ActAll`, `MoveAll`, `CheckAll`) and the other seams that let a later version scale (table below). "Nothing is optimised now."

### C. Design patterns
One frame per pattern, each with: the intent in one sentence, where it appears here, a mini UML diagram, a code snippet from the facts, why it helps students, and its pitfalls in this system.
21. **Composition over inheritance** (the GameObject tree is the configuration; a species is assembled from modules).
22. **Template Method** (base classes with one or two methods to override: `Sense.Read`/`Write`, `AnimalAction.Act`, `Locomotion.Move`, `DeathRule.CauseOfDeath`).
23. **Strategy** (brains, locomotion, mutation operators, act order, ground).
24. **Pipeline** (the ordered tick phases; `WaitsFor` as the only coupling to slow services).
25. **Data-oriented design and Flyweight** (one module instance per species shared by all its animals; animals as records).
26. **Object Pool** (views of animals).
27. **Registry and interning** (`AlleleRegistry`: one allele per locus and text; ids `prey.eat:3`; lineage by parent ids).
28. **Builder** (`SpeciesBuilder.DeclareStat` / `DeclareTrait`; `WorldBuilder` in tests).
29. **Service Locator** (`World.Service<T>()`), with its cost and why it is accepted (prefabs can't reference scene objects).
30. **Observer** (the recorder and the live statistics receive the same events).
31. **Memoization and cache-aside** (`DecisionMemo` in memory, `AnswerCache` on disk; failed answers never stored).
32. **Command-like intents** (an action produces a value object; the locomotion executes it).
33. **Specification** (an interaction needs both: the target is `Edible` and the eater's `Diet` lists it; animal sets "threats", "prey", "kin" resolved from the food web).
34. **Null Object** (the random brain as the null model; a disabled module is simply absent).

### D. Custom setups, and how they are done
One spread per setup: **goal**, **before** tree, **steps** (numbered, each one a component added, removed or set, or one class written), **after** tree, **what the validator says** along the way, **the test to copy**, **what to expect** when it runs. Use the setups below.
35. The Lab 1 reference world from an empty scene.
36. Hide versus flee: three prey variants.
37. A cannibal species, and a sense that tells it whether a threat is chasing it.
38. A food chain of three species.
39. Edible eggs and an egg eater.
40. A stamina number gene, with and without a cost.
41. Water and thirst: a stat, a sense, an action and a gene.
42. A predator that can't climb steep slopes.
43. A new brain (a student's rule-based brain).
44. A new mutation operator (the intensity ladder).
45. Seasons: a new tick phase.
46. A species added during a run.
47. The teaching path: replace a reference module with a stub and let its tests grade the student's version.

### E. Editor tooling
48. **Inspector mockups**: the World (state, tick, run controls, phase list), a Species (tables of actions, senses, genes, stats and traits, food; observation-space size; signature; prompt preview).
49. **Windows**: ecology monitor, gene pool (allele frequencies, lineage as text diffs), animal inspector (genes, last situation, last probabilities as bars), ask the brain, food web graph, event log, scenario runner, brain health.
50. **Validation panel** with real messages: V-01 species inside a species, V-13 no locomotion, V-24 a diet target that isn't edible, V-40 a founder sentence with 13 words, V-50 too many situations, V-61 a key in a serialized field.
51. **Scene view**: gizmos for the selected animal (vision and band rings, target line coloured by action, reach) and the overlay toggles.

### F. Verification
52. **Test tiers** T0–T5 as a pyramid, with what runs where and when (everything on the owner's computer).
53. **Conformance suites**: every module of a kind is tested the moment it exists; they also grade the teaching path.
54. **Scenario matrix** S00–S28 as a grid: what varies, which brain, which tier.
55. **Integrity checks** B-01 to B-11 and the gates G1–G5, with today's status.
56. **The CI pipeline** on the owner's machine as a self-hosted runner, and the coding-agent audit.

### G. Appendix
57. Rule-id index (prefixes and their documents). 58. Glossary. 59. Decisions log. 60. Open and deferred questions.

## Facts to use

### Decisions (owner, 2026-10-09 and 2026-10-10)
- **Space**: continuous positions on a flat plane or a Unity Terrain; Euclidean distance on the horizontal plane; straight-line movement; texts the brain reads say "meters". Grids exist only inside layers (food cells, cover cells, spatial index).
- **Speed and movement**: each species has one `Locomotion` component; walk and run speeds are its traits. Kinematic by default, no rigid bodies. An action only gives a direction (walk or run, how far at most); whatever the locomotion does is the tick's result. Terrain, NavMesh and physics locomotion come later as subclasses.
- **Values**: the Python prototype's values, one cell read as one meter, unchanged.
- **Species**: any number; not hard-coded; named `prey` and `predator` in the reference. A species inside a species is an error. One `Species` class, subclassable, but composition is the main way.
- **Food**: edibility by components: an `Edible` on what can be eaten, a `Diet` on the eater. Eggs have no `Edible` (not edible until a student adds one). Cannibals list their own species and flee from their own kind.
- **Hide and flee**: separate actions, genes and senses (cover).
- **Genes**: an interface: text genes go into the prompt; number genes set a trait. One text gene per action by default; free genes allowed. The neutral allele and the founder pool are set on the gene component. No number genes in the reference species.
- **Prompts**: header, per-action lines and rule lines are inspector fields with placeholders for numbers; the answer instruction is hard-coded in each brain.
- **Observations**: wording in each sense's code, labels and thresholds in the inspector; distances only for now (angles later).
- **Brains**: JEV is the default, running locally on the owner's computer, as is the mutator. The keyword brain of the prototype is **not** part of the Unity system (fast tests use the random brain and a test-only scripted brain).
- **Tick**: an ordered list of phase components on the World; species act in turn by default (all mixed and simultaneous are options; simultaneous gives contested items to a random winner); breeding after the act phase; no incubation by default.
- **Waiting**: lockstep; a decision never uses old observations. Freeze (main thread blocks) or Responsive (the tick pauses, Unity keeps rendering), same results. Not real time.
- **Determinism**: per machine only. Answer cache: local JSON-lines files.
- **Teaching**: reference modules are written exactly like student modules; students first reimplement existing components against their tests, then invent their own.
- **Infrastructure**: Unity 6000.3; Newtonsoft JSON; Unity Test Framework; CI and everything else on the owner's computer.

### The GameObject tree (Lab 1 world)
```
Lab1 World                [World]  seed 1234, wait mode, decision period 4
├── Ground                [FlatGround]
├── Environment
│   ├── Grass             [FoodGrid] initial 0.1, regrow 0.0015, no food in cover   [Edible] grazed, 25 per item
│   ├── Thickets          [CoverLayer] 20 % of the ground, hides prey from their threats
│   ├── Carcasses         [CarcassSystem]
│   └── Eggs              [EggSystem] (no Edible)
├── Brains                [JevBrain] (default)  [OllamaPointsBrain] (optional)  [RandomBrain]  [AnswerCache]  [MutatorService]
├── Recording             [RunRecorder] [LiveStatistics]
├── Phases                Sense, Ask brains, Choose actions, Act (species in turn), Breed, Hatch, Deaths, Migration, Environment, Floor, Record
├── Prey                  [Species] "prey", brain JEV
│   ├── Body              [Body] view prefab
│   ├── Locomotion        [KinematicLocomotion] walk 1, run 1 m per tick
│   ├── Stats             [Energy] [Stamina] [Metabolism]
│   ├── Senses            Energy [LevelSense 30/70] · Stamina [LevelSense 20/40] · Food [NearestResourceSense "Grass"]
│   │                     · Predator [NearestAnimalSense Threats] · Cover [NearestCoverSense] · Animal [NearestAnimalSense Kin, readiness] · Age [AgeSense]
│   ├── Actions           eat, flee (flee into cover: off), hide, follow, rest, mate — each [XAction] + [TextGene]
│   ├── Food              [Diet] grass   [Edible] struck, 60 per kill, leaves a carcass of 2 portions × 30, rots in 100 ticks
│   ├── Life              [MatingRule] [Litter 2–4] [UniformCrossover] [Incubation 0] [Starvation] [OldAge] [CapRule 270, migrate] [FloorRule 10]
│   └── Mutation          [LlmMutation] deck v4, rate 0.03, 5 tries, 12 words max
└── Predator              [Species] "predator"
    ├── Locomotion        [KinematicLocomotion] walk 1, run 2 m per tick
    ├── Actions           hunt, follow, rest, mate
    └── Food              [Diet] prey (strike, kill chance 0.5), prey carcasses (scavenge); no [Edible]
```

### Base classes (C#, namespace `EvoSim`)
```csharp
public abstract class WorldModule : MonoBehaviour { World World; virtual void Initialize(); virtual void Validate(ValidationReport r); }
public abstract class TickPhase : WorldModule { virtual Pending WaitsFor(TickContext t) => null; abstract void Run(TickContext t); }
public abstract class WorldService : WorldModule { }
public class Species : MonoBehaviour { string Id; IReadOnlyList<AnimalAction> Actions; IReadOnlyList<Gene> Genes;
    IReadOnlyList<Sense> Senses; IReadOnlyList<Animal> Animals; string Signature; T Module<T>();
    protected internal virtual void OnBorn(Animal a); protected internal virtual void OnDied(Animal a, string cause); }
public abstract class SpeciesModule : MonoBehaviour { Species Species; virtual void Declare(SpeciesBuilder b);
    virtual void Initialize(); virtual void Validate(ValidationReport r); virtual void WritePromptRules(PromptWriter w); }
public sealed class Animal { int Id; Species Species; Vector3 Position; float Heading; Genome Genome; int Generation, Age;
    int Action; bool Searching, BredThisPeriod, Killed; int BusyTicks; float this[StatId s]; float Trait(TraitId t); }
public abstract class Sense : SpeciesModule { string Label; abstract IReadOnlyList<string> Tokens;
    abstract int Read(Animal a, SenseContext s); virtual string Write(int token, TextStyle style); }
public abstract class AnimalAction : SpeciesModule { string Name; TextGene Gene;
    abstract void Act(Animal a, ActContext c); virtual bool IsRelevant(ObservationView o); }
public abstract class Locomotion : SpeciesModule { TraitId WalkSpeed, RunSpeed;
    abstract float Move(Animal a, Intent intent, float staminaBudget, MoveContext c); virtual float ExtraCost(Animal a, Vector3 from, Vector3 to); }
public abstract class Gene : SpeciesModule { string Label; string LocusId; abstract AlleleKind Kind;
    abstract IReadOnlyList<AlleleValue> FounderPool; abstract void Express(AlleleValue v, Expression e); }
public abstract class MutationOperator : SpeciesModule { float rate = 0.03f; abstract bool Accepts(Gene g);
    abstract MutationJob Start(Gene g, Allele parent, RandomStream rng); }
public abstract class Brain : WorldService { abstract string Id; virtual int MaxActions; virtual bool AcceptsAttachments;
    abstract BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch); }
public class Edible : MonoBehaviour { EatMethod method; float energy; int carcassPortions; float carcassEnergy; int carcassTicks; }
public class Diet : SpeciesModule { List<Entry> eats; /* Entry: target ("Grass", "prey", "carcass:prey", "egg:prey"), energyScale */ }
```
`ActContext` offers only intents (`WalkTo(target, stopAt)`, `RunTo`, `RunAwayFrom`, `Stay`, `Wander`, `Search`) and queries (`NearestResource`, `NearestAnimal(set)`, `NearestEntity<T>`); `OnArrival(Interaction.Graze | Strike | Scavenge | custom)`.

### The eat action, whole
```csharp
public class EatAction : AnimalAction
{
    [SerializeField] string layerName = "";                     // empty = every layer the diet grazes
    public override void Act(Animal a, ActContext c)
    {
        var food = c.NearestResource(a, layerName);             // within vision, diet-checked
        if (food == null) { c.Search(); return; }               // ACT-04
        c.WalkTo(food.Position, stopAt: 0f);                    // MOVE-04
        c.OnArrival(Interaction.Graze(food));                   // ACT-10
    }
}
```

### The tick (in order)
1 Sense (who decides: every 4 ticks all non-busy animals, otherwise animals without an action) · 2 Ask brains (memo, one batch per brain) · 3 Choose actions (waits for the brains; draws with the sampling stream) · 4 Act (per group: plan, move, interact, settle; busy animals count down) · 5 Breed (litters, eggs, mutation requests) · 6 Hatch (waits for the mutator) · 7 Deaths (killed removed, age +1, starvation, old age) · 8 Migration (above the cap, random older animals leave) · 9 Environment (food regrows, carcasses rot or move) · 10 Floor (newcomers below the floor) · 11 Record (stats every 100 ticks).

### Numbers (the prototype's, kept)
Prey: 136 at start, floor 10, cap 270, energy 60 at start and 100 max, costs 0.7 per tick, 0.5 per meter, 0.2 resting, stamina 60 (2 back per still tick, 0.3 energy while it recovers), maturity 150, max age 1 500, mate at 50 energy, 40 energy per baby shared by the parents, litters of 2–4. Predator: 28 at start, floor 3, cap 68, stamina 30, run 2 m per tick, kill chance 0.5, 50 ticks of digestion after a kill, 25 after a carcass portion. Vision 20 m; distance bands within 1 m, 1–4, 4–10, 10–20 m, none. World 192 × 192 m. Observation space: 29 160 prey situations, 4 050 predator situations.

### The default prey prompt (answer instruction of the points brain at the end)
```text
You decide what a wild animal does next in a simple world.

Actions:
- eat: go to the nearest visible food and eat it
- flee: run away from the nearest predator
- hide: go to the nearest cover and stay in it; predators can't see or catch an animal in cover
- follow: move toward the nearest other animal
- rest: stay still to catch your breath and save energy
- mate: walk to the nearest ready partner in sight and breed with it
If the chosen action has nothing to act on in sight (no food, predator, cover,
animal or ready partner), the animal searches the surroundings instead.
Animals move 1 meter per step. Predators run 2 meters per step when they hunt,
but they have 30 stamina against an animal's 60, so a long chase tires them first.
Every meter moved costs stamina. Standing still brings it back, which costs some
energy until stamina is full. Without stamina an animal cannot move.
Breeding needs only one of the two to choose mate: an adult that chooses mate
breeds as soon as it reaches a ready partner, whatever the partner is doing.

This animal's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
- eat: "Eat whenever food is close."
- flee: "Run from any predator you see."
- hide: "Hide when a predator is close."
- follow: "Stay close to other animals."
- rest: "Rest when you are tired."
- mate: "Look for a partner when energy is high."

Situation: Energy: low. Stamina: high. Food: 1-4 meters away. Predator: 4-10 meters away. Cover: 1-4 meters away. Animal: none within 20 meters. Age: adult.

Distribute 100 points across the actions according to how likely this animal is to choose each.
```
The JEV request wraps the same text without the last line: `[kind] choice`, `[state] …`, `[question] Which action does this animal take now?`, `[options]` A) eat … F) mate, `[decision]:`; the options always follow the species' action order.

### Random streams
`world` (terrain, cover, initial food; may have its own seed), one per resource layer, `act-order`, and per species: `founders`, `sampling`, `actions` (wandering, sidesteps, strike rolls), `litter`, `mutation` (crossover and mutation choices), `migration`. Adding a species never shifts another species' draws.

### Scale seams (later)
| Seam | Now | Later |
|---|---|---|
| Batch entry points | `ReadAll`, `ActAll`, `MoveAll`, `CheckAll` loop over the single-animal methods | Burst jobs override the batch method |
| Animal storage | objects in a list per species | struct-of-arrays; `Animal` becomes a handle |
| Spatial queries | a hash grid rebuilt per phase | a k-d tree behind the same interface |
| Views | pooled GameObjects | instanced rendering |

### Validators to show (subset)
V-01 a species inside a species (error) · V-05 an action without a gene (warning) · V-12 the species' signature changed (warning, "accept") · V-13 a species without exactly one locomotion (error) · V-21 an animal set that resolves to nothing (warning) · V-24 a diet target with no Edible (error) · V-40 a founder sentence over 12 words or with forbidden characters (error) · V-50 more than 100 000 situations (warning) · V-61 a key in a serialized field (error).

### Tests and CI (subset)
Tiers: T0 validation and static checks; T1 EditMode unit tests and conformance suites; T2 PlayMode runs with the random or scripted brain; T3 regression (pinned events hashes, soak, performance under 2 ms per tick for 340 animals, brain time excluded); T4 JEV and the mutator locally (integrity checks B-01–B-11, behavioural scenarios, reference ranges); T5 a coding-agent audit. Every MUST rule (193 of them) has at least one test. Gates: G1 semantics passes; G2 information fails for current models and stays as a tracked gate; G3 locality passes on JEV; G4 not measured; G5 throughput fails (JEV ≈ 12 decisions per second).

### Open and deferred
Open: the founder pools, including the draft hide sentences ("Hide when a predator is close.", "Stay in cover when danger is near.", "Hide only when you are tired.", "Leave cover to find food when hungry."). Deferred: speciation rules, names of species created at run time, camera senses, a real-time mode, terrain/NavMesh/physics locomotion, angles in observations, scaling, a shared class-wide answer cache.

## Deliverables checklist
- Chapters A–G as one document, with a table of contents and the version label.
- Every diagram as a 16:9 frame too, numbered as in the list above.
- The poster.
- A last page "What changed from the Python prototype": grid → continuous space; a fixed two-species ecology → a food web of components; sentence genes → text and number genes; a keyword brain → none (JEV by default); hide inside flee → its own action; prompts in files → inspector fields.
