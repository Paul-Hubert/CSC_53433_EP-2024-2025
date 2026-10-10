# 20 — Unity architecture (proposal)

A proposal for implementing the contract in Unity 6000.3, aimed at two readers:
students, who should understand one behaviour by reading one small class and add
one by writing one; and the course staff, who need runs that are fast,
reproducible and testable.

## 1. Principles

The `ARCH` rules bind the reference implementation (MUST for it) and are
recommendations (SHOULD) for any other implementation of the contract.

- **ARCH-01** *The scene is the configuration.* A world is a GameObject tree. Each
  feature is a component (a **module**) placed in it: drag a prefab in to add a
  feature, disable it to switch it off, delete it to remove it.
- **ARCH-02** *One file per feature.* A sense, an action, a gene kind, a stat, a
  brain, a mutation operator, a phase: each is one class deriving from one base
  class, overriding one or two methods.
- **ARCH-03** *Animals are data.* An animal is a plain C# object owned by its
  species. No animal has a MonoBehaviour with `Update`. Phases loop over all
  animals at once. The visible animal is a pooled **view** that only displays.
- **ARCH-04** *The World initialises everything, in a fixed order.* Modules never
  set themselves up in `Awake`, `Start` or `OnEnable` (Unity doesn't guarantee
  their order). The World calls them.
- **ARCH-05** *Modules belong to their nearest owner.* A module under a species
  belongs to the nearest `Species` above it; a gene belongs to the nearest action
  above it (or on its GameObject). A species inside a species is an error.
- **ARCH-06** *Disabled means absent.* An inactive GameObject or a disabled
  component is not a module of the world.
- **ARCH-07** *No statics, no singletons.* Everything is reached from the World,
  a species or a context object. Several worlds can live side by side (tests,
  parallel seeds), and "Enter Play Mode" without a domain reload stays safe.
- **ARCH-08** *Prefabs never reference the scene.* A module prefab shared between
  species refers to world things by role or name ("the threats of my species",
  "the layer named Grass"), resolved when the World initialises. A prefab can't
  hold a reference to a scene object, so this is what makes sharing work.
- **ARCH-09** *Logic is testable without a scene.* Modules take their inputs from
  context objects that tests can build in code ([30](30-tests.md)).
- **ARCH-10** *Version-specific code is fenced.* HTTP, waiting and NavMesh calls
  live in a few files, so a later Unity upgrade touches little. (The owner moves
  the project to 6000.3 before work starts.)
- **ARCH-11** *The reference modules are student code.* Every reference sense,
  action, gene, stat, locomotion, rule, brain and phase derives from the same
  public base classes a student uses, with no internal shortcut. The teaching
  path relies on it: students first reimplement existing components against
  their tests, then invent their own ([22](22-extending-recipes.md)).
- **ARCH-12** *Scale can change later without touching modules' meaning.* Phases
  call modules on lists of animals; storage, spatial queries and views sit
  behind interfaces (§10). Nothing is optimised now.

## 2. The GameObject tree

```
Lab1 World                                [World]  seed, wait mode, act order, decision period
├── Ground                                [FlatGround] | [TerrainGround]+Terrain | [NavMeshGround]
├── Environment
│   ├── Grass                             [FoodGrid]       initial 0.1, regrow 0.0015, no food in cover
│   │                                     [Edible]         grazed, 25 per item
│   ├── Thickets                          [CoverLayer]     20 %, hides prey from their threats
│   ├── Carcasses                         [CarcassSystem]  carcasses as each species' edible component says
│   └── Eggs                              [EggSystem]      no [Edible]: eggs can't be eaten
├── Brains
│   ├── JEV                               [JevBrain]       (world default) localhost, model revision, strict
│   ├── Gemma (optional)                  [OllamaPointsBrain] localhost, gemma4:12b
│   ├── Random                            [RandomBrain]    the null model
│   ├── Answer cache                      [AnswerCache]    folder, read/write
│   └── Mutator                           [MutatorService] host, model, temperature
├── Recording                             [RunRecorder] [LiveStatistics]
├── Phases                                (children run in this order every tick)
│   ├── Sense                             [SensePhase]
│   ├── Ask brains                        [AskBrainsPhase]
│   ├── Choose actions                    [ChooseActionsPhase]
│   ├── Act                               [ActPhase]
│   ├── Breed                             [BreedPhase]
│   ├── Hatch                             [HatchPhase]
│   ├── Deaths                            [DeathPhase]
│   ├── Migration                         [MigrationPhase]
│   ├── Environment                       [EnvironmentPhase]
│   ├── Floor                             [FloorPhase]
│   └── Record                            [RecordPhase]
├── Prey                                  [Species] id and name "prey", brain: JEV
│   ├── Body                              [Body] → prey view prefab
│   ├── Locomotion                        [KinematicLocomotion] walk 1, run 1 m per tick
│   ├── Stats                             [Energy] [Stamina] [Metabolism]
│   ├── Senses                            (situation-text order = this order)
│   │   ├── Energy                        [LevelSense] stat energy, 30 / 70
│   │   ├── Stamina                       [LevelSense] stat stamina, 20 / 40
│   │   ├── Food                          [NearestResourceSense] layer "Grass"
│   │   ├── Predator                      [NearestAnimalSense] set Threats
│   │   ├── Cover                         [NearestCoverSense]
│   │   ├── Animal                        [NearestAnimalSense] set Kin, readiness on
│   │   └── Age                           [AgeSense]
│   ├── Actions                           (probability / prompt / option order = this order)
│   │   ├── eat    (prefab Eat)           [EatAction]  [TextGene]
│   │   ├── flee   (prefab Flee)          [FleeAction] [TextGene]   flee into cover: off
│   │   ├── hide   (prefab Hide)          [HideAction] [TextGene]
│   │   ├── follow (prefab Follow)        [FollowAction] [TextGene]
│   │   ├── rest   (prefab Rest)          [RestAction] [TextGene]
│   │   └── mate   (prefab Mate)          [MateAction] [TextGene]
│   ├── Food                              [Diet] grass   [Edible] struck, 60 per kill, carcass 2 × 30, 100 ticks
│   ├── Life                              [MatingRule] [Litter] [UniformCrossover] [Incubation 0]
│   │                                     [Starvation] [OldAge] [CapRule] [FloorRule]
│   └── Mutation                          [LlmMutation] deck mutate_v4
└── Predator                              [Species] id and name "predator"
    ├── Locomotion                        [KinematicLocomotion] walk 1, run 2 m per tick
    └── ...                               [HuntAction] [Diet: prey (strike), prey carcasses (scavenge)]; no [Edible] ...
```

The sub-GameObjects ("Senses", "Actions", "Life") are only folders: discovery
looks at the whole subtree. Order matters only *within a kind*: the n-th
`AnimalAction` found depth-first is action n, wherever it sits. The inspector of
the species shows the resulting orders ([21](21-editor-tooling.md)).

A sense may also travel inside its action's prefab (a Hide prefab carrying its
Cover sense); it then takes its place in depth-first order, which moves it in
the situation text. A number gene (a stamina gene, scenario S12) would sit under
a `Genes` folder with a `GaussianMutation` beside the LLM mutation; the
reference species have none (owner decision).

## 3. Base classes

All runtime code lives in the namespace `EvoSim`. The sketches below are the
public surface students meet; bodies are abbreviated.

### 3.1 World-level modules

```csharp
/// A component under the World (not under a species): a phase or a service.
public abstract class WorldModule : MonoBehaviour
{
    public World World { get; private set; }
    /// Called by the World in a fixed order, before any animal exists.
    public virtual void Initialize() { }
    /// Report configuration problems (editor, and at the start of a run).
    public virtual void Validate(ValidationReport report) { }
}

/// One step of a tick. The World runs its phases in hierarchy order (TICK-01).
public abstract class TickPhase : WorldModule
{
    /// Work this phase must wait for (brain answers, mutations); null = none (TICK-03).
    public virtual Pending WaitsFor(TickContext t) => null;
    public abstract void Run(TickContext t);
}

/// Something phases and modules use: ground, layers, brains, recorder.
public abstract class WorldService : WorldModule { }
```

### 3.2 The World

```csharp
[DisallowMultipleComponent]
public class World : MonoBehaviour
{
    [SerializeField] int seed = 1234;
    [SerializeField] WaitMode waitMode = WaitMode.Responsive;   // or Freeze (SPACE-14)
    [SerializeField, Min(1)] int ticksPerFixedUpdate = 1;
    [SerializeField, Min(1)] int decisionPeriod = 4;
    [SerializeField, Min(0.01f)] float samplingTemperature = 1f;
    [SerializeField] TextStyle textStyle = TextStyle.V1;
    [SerializeField] Brain defaultBrain;
    [SerializeField] bool startOnPlay = true;

    public int Tick { get; private set; }
    public float SamplingTemperature => samplingTemperature;
    public RandomStreams Random { get; private set; }
    public IReadOnlyList<Species> AllSpecies => species;
    public T Service<T>() where T : WorldService => ...;        // first enabled service of that type
    public T Service<T>(string name) where T : WorldService => ...;

    readonly List<Species> species = new();
    readonly List<TickPhase> phases = new();
    int nextPhase;                                               // where a waiting tick resumes

    void Awake()       { if (startOnPlay) Initialize(); }
    void FixedUpdate() { if (IsRunning) Advance(ticksPerFixedUpdate); }
    void LateUpdate()  { views.Sync(InterpolationFactor); }      // visuals only (SPACE-12)

    /// Discovery and set-up, in a fixed order (§4).
    public void Initialize() { ... }

    /// Runs up to maxTicks ticks. In Responsive mode it returns early when a phase must
    /// wait, and the same tick continues at that phase on a later call (SPACE-13/14).
    public void Advance(int maxTicks)
    {
        for (int done = 0; done < maxTicks;)
        {
            var phase = phases[nextPhase];
            var pending = phase.WaitsFor(context);
            if (pending != null && !pending.IsDone)
            {
                if (waitMode == WaitMode.Responsive) return;     // Unity keeps rendering
                pending.Wait();                                  // Freeze: block until answered
            }
            phase.Run(context);                                  // inside a profiler marker
            if (++nextPhase == phases.Count) { nextPhase = 0; Tick++; done++; }
        }
    }

    /// A new species during a run (SPEC-30): clones a template under this World.
    public Species AddSpecies(Species template, string name, Species parent = null) { ... }
}
```

`FixedUpdate` only *drives* the loop; simulation time is `Tick` (SPACE-10).
With `Freeze`, a brain batch blocks `FixedUpdate` until it is answered, which is
the owner's baseline. With `Responsive` (the default in the editor), the tick
pauses at "Choose actions" and resumes when the answers are in, while Unity keeps
drawing; both give the same events hash (RAND-11), and a test checks it.

### 3.3 Species and species modules

```csharp
/// A kind of animal. Configure it with child components; subclass it only for hooks.
public class Species : MonoBehaviour
{
    [SerializeField] string displayName = "prey";               // what the brain reads (SPEC-01)
    [SerializeField] Brain brain;                               // null = the World's default
    [SerializeField, TextArea(2, 6)] string promptHeader =
        "You decide what a wild animal does next in a simple world.";
    [SerializeField] TextAsset frozenPrompt;                    // optional (PROMPT-04)

    public string Id { get; private set; }
    public World World { get; private set; }
    public IReadOnlyList<AnimalAction> Actions { get; private set; }
    public IReadOnlyList<Gene> Genes { get; private set; }
    public IReadOnlyList<Sense> Senses { get; private set; }
    public IReadOnlyList<Animal> Animals => animals;
    public string Signature { get; private set; }               // SPEC-03

    public T Module<T>() where T : SpeciesModule => ...;         // the one Diet, Litter, CapRule...
    public IEnumerable<T> ModulesOf<T>() where T : SpeciesModule => ...;

    // Hooks for subclasses. Most species never need them.
    protected internal virtual void OnBorn(Animal a) { }
    protected internal virtual void OnDied(Animal a, string cause) { }
}

/// A component under a species: a gene, a sense, an action, a stat, a rule...
public abstract class SpeciesModule : MonoBehaviour
{
    public Species Species { get; private set; }
    public World World => Species.World;

    /// Declare the stats and traits this module needs (before any animal exists).
    public virtual void Declare(SpeciesBuilder b) { }
    /// Look up other modules, layers and species (after every module declared).
    public virtual void Initialize() { }
    public virtual void Validate(ValidationReport report) { }
    /// Lines this module adds to the prompt's rules (PROMPT-01, PROMPT-03).
    public virtual void WritePromptRules(PromptWriter w) { }
}
```

### 3.4 Animals, stats and traits

```csharp
/// One animal: plain data, owned by its species (ANIM-01).
public sealed class Animal
{
    public readonly int Id;
    public readonly Species Species;
    public Vector3 Position, PreviousPosition;     // previous: for interpolating the view
    public float Heading;                          // degrees, for wandering
    public Genome Genome;
    public int Generation, BornTick, Age;
    public int[] Parents;
    public int Action = -1;                        // index into Species.Actions; -1 = none
    public bool Searching, BredThisPeriod, Killed;
    public int KilledBy = -1, BusyTicks;
    public string BusyReason;
    public int Meals, Offspring;
    public Observation LastObservation;            // for the inspector (DEC-21)
    public float[] LastProbabilities;

    public float this[StatId s] { get => stats[s.Index]; set => stats[s.Index] = s.Clamp(value); }
    public float Trait(TraitId t) => traits[t.Index];
    public bool IsBusy => BusyTicks > 0;
    readonly float[] stats, traits;
}
```

A stamina module declares what it needs and reads only that:

```csharp
public class Stamina : SpeciesModule
{
    [SerializeField] float max = 60f, regenPerTick = 2f, regenEnergyCost = 0.3f;
    public StatId Value { get; private set; }
    public TraitId Max { get; private set; }

    public override void Declare(SpeciesBuilder b)
    {
        Max   = b.DeclareTrait("stamina.max", defaultValue: max, min: 1f, max: 1000f);
        Value = b.DeclareStat("stamina", start: StatStart.FullTrait(Max));
    }

    /// How far the animal may move this tick (ANIM-21).
    public float Budget(Animal a) => a[Value];

    /// Called by the metabolism after the animal acted.
    public void Settle(Animal a, float metersMoved, Energy energy)
    {
        if (metersMoved > Units.Epsilon) { a[Value] -= metersMoved; return; }
        if (a[Value] < a.Trait(Max))
        {
            a[Value] = Mathf.Min(a.Trait(Max), a[Value] + regenPerTick);
            a[energy.Value] -= regenEnergyCost;
        }
    }

    public override void WritePromptRules(PromptWriter w) => w.Rule(
        "Every meter moved costs stamina. Standing still brings it back, which costs some " +
        "energy until stamina is full. Without stamina an animal cannot move.");
}
```

A number gene sets `stamina.max` for one animal; the module never knows a gene
exists (ANIM-16).

### 3.5 Senses

```csharp
public abstract class Sense : SpeciesModule
{
    [SerializeField] protected string label = "Food";
    public string Label => label;

    /// Every token this sense can return, "none" included. Fixed before the run (SENSE-01).
    public abstract IReadOnlyList<string> Tokens { get; }
    /// Read one animal at a decision (SENSE-03).
    public abstract int Read(Animal a, SenseContext s);
    /// The text the brain reads for a token (SENSE-10).
    public virtual string Write(int token, TextStyle style) => $"{label}: {Tokens[token]}.";
}

/// For senses that cast rays: ask for rays first, read the results after one batch (SENSE-30).
public abstract class BatchedSense : Sense
{
    public abstract void RequestRays(Animal a, RayBatch rays);
}
```

```csharp
public class NearestAnimalSense : Sense
{
    [SerializeField] AnimalSet targets = AnimalSet.Threats;     // Threats | Prey | Kin | Listed
    [SerializeField] List<Species> listed;
    [SerializeField] Bands bands = Bands.Reference;             // 1, 4, 10 m and the vision trait
    [SerializeField] bool reportReadiness;                      // "…, ready to mate"

    public override IReadOnlyList<string> Tokens => tokens;     // built in Initialize
    public override int Read(Animal a, SenseContext s)
    {
        var nearest = s.NearestAnimal(a, targets, bands.Vision(a));   // hidden and killed excluded
        if (nearest == null) return NoneToken;
        int band = bands.IndexOf(nearest.Distance);
        return reportReadiness && nearest.Distance <= s.PartnerRange
            ? TokenFor(band, s.IsReady(nearest.Animal))
            : TokenFor(band);
    }
    public override string Write(int t, TextStyle style) => ...;  // "Predator: 4-10 m away."
}
```

### 3.6 Actions

```csharp
public abstract class AnimalAction : SpeciesModule
{
    [SerializeField, TextArea] string description = "go to the nearest visible food and eat it";
    public string Name => gameObject.name;                      // "eat"
    public TextGene Gene { get; internal set; }                 // bound by the species (GENE-05)

    /// Every tick while this action is chosen: say where to go, and what to do on arrival.
    public abstract void Act(Animal a, ActContext c);

    /// Directed tests: is this observation relevant for this action (CTRL-10)?
    public virtual bool IsRelevant(ObservationView o) => true;

    public override void WritePromptRules(PromptWriter w) => w.Action(Name, description);
}
```

The whole eat behaviour:

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

Hunting, with carcasses:

```csharp
public class HuntAction : AnimalAction
{
    public override void Act(Animal a, ActContext c)
    {
        var prey    = c.NearestAnimal(a, AnimalSet.Prey);      // not killed, not hidden in cover
        var carcass = c.NearestEntity<Carcass>(a, x => x.EdibleBy(a));
        if (carcass != null && (prey == null || carcass.Distance <= prey.Distance))
        {
            c.RunTo(carcass.Position, stopAt: 1f);
            c.OnArrival(Interaction.Scavenge(carcass.Entity));
            return;
        }
        if (prey == null) { c.Search(); return; }
        c.RunTo(prey.Position, stopAt: 1f);
        c.OnArrival(Interaction.Strike(prey.Animal));
    }
}
```

`ActContext` offers only intents (`WalkTo`, `RunTo`, `RunAwayFrom`, `Stay`,
`Wander`, `Search`) and queries; it cannot move the animal or touch another one
(ACT-06). Custom interactions implement `IInteraction { float Reach; void
Apply(Animal actor, InteractionContext c); }`.

### 3.7 Locomotion

Each species has one locomotion component. An action only gives a direction
(and walk or run, and how far at most); the locomotion decides what happens,
and whatever it does is the tick's result (MOVE-01).

```csharp
/// How a species moves. Inherit it for terrain, NavMesh or physics movement.
public abstract class Locomotion : SpeciesModule
{
    [SerializeField] protected float walkSpeed = 1f, runSpeed = 1f;    // meters per tick
    public TraitId WalkSpeed { get; private set; }
    public TraitId RunSpeed { get; private set; }

    public override void Declare(SpeciesBuilder b)
    {
        WalkSpeed = b.DeclareTrait("speed.walk", walkSpeed, min: 0f, max: 100f);
        RunSpeed  = b.DeclareTrait("speed.run",  runSpeed,  min: 0f, max: 100f);
    }

    /// Move one animal this tick; return the meters actually moved (MOVE-06).
    public abstract float Move(Animal a, Intent intent, float staminaBudget, MoveContext c);

    /// Extra energy this movement costs (slopes, mud). None in the reference.
    public virtual float ExtraCost(Animal a, Vector3 from, Vector3 to) => 0f;

    /// The act phase calls this for a group; override it to move many animals at once (ARCH-12).
    public virtual void MoveAll(IReadOnlyList<Animal> group, IReadOnlyList<Intent> intents,
                                MoveContext c, Span<float> moved)
    {
        for (int i = 0; i < group.Count; i++)
            moved[i] = Move(group[i], intents[i], c.StaminaBudget(group[i]), c);
    }
}

/// The reference: straight lines, no physics (MOVE-04).
public class KinematicLocomotion : Locomotion
{
    public override float Move(Animal a, Intent intent, float stamina, MoveContext c)
    {
        if (intent.IsStay) return 0f;
        float speed = a.Trait(intent.Run ? RunSpeed : WalkSpeed);
        float d = Mathf.Min(speed, stamina, intent.MaxDistance);
        Vector3 reached = c.Ground.SlideTo(a.Position, a.Position + intent.Direction * d);
        if (d > 0f && c.Distance(a.Position, reached) < Units.Epsilon)          // stuck
            reached = c.Ground.SideStep(a.Position, d, c.Stream(Species, "actions"));
        float moved = c.Distance(a.Position, reached);
        a.Position = reached;
        return moved;
    }
}
```

Later subclasses (owner decision: not now): `TerrainLocomotion` (slower and
costlier uphill, no steep slopes), `NavMeshLocomotion` (paths around
obstacles), `PhysicsLocomotion` (a rigid body pushed toward the direction; it
may not go exactly there, which is fine).

### 3.8 Genes

```csharp
public abstract class Gene : SpeciesModule
{
    [SerializeField] protected string label;                    // empty = the action's name
    public string Label => ...;
    public string LocusId => $"{Species.Id}.{Label}";            // GENE-11
    public abstract AlleleKind Kind { get; }
    public abstract IReadOnlyList<AlleleValue> FounderPool { get; }
    /// Turn this gene's allele into what acts: a prompt line or a trait (GENE-03/04).
    public abstract void Express(AlleleValue value, Expression e);
}

public class TextGene : Gene
{
    [SerializeField] AllelePool pool;                           // shared asset, or the lists below
    [SerializeField] string neutral = "No preference.";
    [SerializeField, TextArea] List<string> founders = new();
    [SerializeField] string contrastPro, contrastAnti;
    public override AlleleKind Kind => AlleleKind.Text;
    public override void Express(AlleleValue v, Expression e) => e.PromptGene(Label, v.Text);
}

public class NumberGene : Gene
{
    [SerializeField] string trait = "stamina.max";
    [SerializeField] bool multiplyDefault;                      // false: set the value
    [SerializeField] float min = 20f, max = 120f;               // the gene's own range
    [SerializeField] List<float> founders = new() { 45f, 60f, 75f };
    public float Min => min;
    public float Max => max;
    public override AlleleKind Kind => AlleleKind.Number;
    public override void Express(AlleleValue v, Expression e) =>
        e.SetTrait(trait, v.Number, multiplyDefault);           // clamped to the trait's range
}
```

Founder sentences live in an `AllelePool` ScriptableObject when several species
or prefabs share them; the inspector checks every sentence with the mutation
guards (GENE-22).

### 3.9 Mutation

```csharp
public abstract class MutationOperator : SpeciesModule
{
    [Range(0f, 1f)] public float rate = 0.03f;
    [SerializeField] List<Gene> onlyThese;                      // empty = every gene it accepts
    public abstract bool Accepts(Gene g);
    /// Draw every random choice now (MUT-30), then start the work; the job may finish later.
    public abstract MutationJob Start(Gene gene, Allele parent, RandomStream rng);
}

public class GaussianMutation : MutationOperator
{
    [SerializeField] float sigma = 5f;
    public override bool Accepts(Gene g) => g.Kind == AlleleKind.Number;
    public override MutationJob Start(Gene g, Allele parent, RandomStream rng)
    {
        var gene = (NumberGene)g;
        float v = Mathf.Clamp(parent.Number + sigma * rng.NextGaussian(), gene.Min, gene.Max);
        return MutationJob.Done(v, "gauss");                    // MUT-20
    }
}

public class LlmMutation : MutationOperator
{
    [SerializeField] TextAsset deck;                            // one instruction per line (MUT-10)
    [SerializeField] string contextLine = "The sentence below is a rule that a wild animal follows.";
    [SerializeField] int tries = 5, maxWords = 12;
    List<string> instructions;                                  // read from the deck in Initialize
    public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;
    public override MutationJob Start(Gene g, Allele parent, RandomStream rng)
    {
        var plan = TryPlan.Draw(rng, tries, instructions.Count);  // (instruction, seed) per try
        return World.Service<MutatorService>().Submit(parent.Text, plan, contextLine, instructions,
                                                      MutationGuards.For(maxWords));
    }
}
```

### 3.10 Brains

```csharp
public abstract class Brain : WorldService
{
    public virtual int MaxActions => int.MaxValue;
    public virtual bool AcceptsAttachments => false;
    public abstract string Id { get; }                           // part of memo and cache keys
    /// Answer a batch (DEC-11). Fast brains fill the answer at once; HTTP brains later.
    public abstract BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch);
}
```

| Brain | Class | Notes |
|---|---|---|
| Random | `RandomBrain` | uniform; the null model, and the brain of fast tests |
| Ollama points | `OllamaPointsBrain : HttpBrain` | JSON schema per species, temperature 0, seed, `num_ctx`, `think: false`, parallel requests |
| JEV choice | `JevBrain : HttpBrain` | one `/v1/completions` request with the list of prompts of the batch, `max_tokens` 1, allowed option tokens, log-probabilities, head bias, calibrated temperature |
| Fake | `ScriptedBrain` (tests) | returns given vectors, counts calls |

`HttpBrain` holds the shared plumbing: a `System.Net.Http.HttpClient`, retries
with back-off, time-outs, a cap on parallel requests, strict mode, and the answer
cache. All of its `async` code uses `ConfigureAwait(false)` and never touches
Unity objects, so the `Freeze` mode can block the main thread on it without a
deadlock.

### 3.11 Other modules

| Contract | Module(s) |
|---|---|
| Ground (SPACE-04) | `FlatGround`, `TerrainGround` (heightmap, water level, maximum steepness, tree footprints), `NavMeshGround` |
| Locomotion (MOVE-01), one per species | `KinematicLocomotion` (reference); later `TerrainLocomotion` (slope costs, steepness), `NavMeshLocomotion` (`NavMesh.Raycast`, `NavMesh.CalculatePath` toward far targets, paths cached per target for a few ticks), `PhysicsLocomotion` |
| Resource layers (ENV-01) | `FoodGrid`, `TerrainGrassFood` (the terrain's detail layer is the grid) |
| Cover (ENV-10) | `CoverLayer` (noise patches), `TerrainCover` (from tree instances or a detail layer) |
| Entities (ENV-20) | `EntitySystem<T>`; `CarcassSystem`, `EggSystem` |
| Food (SPEC-10, SPEC-15) | `Edible` on whatever can be eaten, `Diet` on the eater (§3.12); threats derived by the World |
| Stats | `Energy`, `Stamina`, `Metabolism` (costs per tick) |
| Busy (ANIM-30) | `Digestion` (makes eaters busy) |
| Mating, litters, eggs | `MatingRule`, `Litter`, `UniformCrossover`, `Incubation` |
| Death | `Starvation`, `OldAge` (each a `DeathRule`: `string CauseOfDeath(Animal a)`) |
| Population | `CapRule` (migrate or block), `FloorRule` (newcomers), `EggBankFloor` (rejected option, off) |
| Prompt | `PromptWriter` (assembles header, actions, rules, genes, situation, ask), `FrozenPrompt` |
| Memo, cache | `DecisionMemo` (in memory), `AnswerCache` (append-only JSON lines per brain in a cache folder, loaded at start) |
| Spatial queries | `SpatialIndex`: a uniform hash grid per species and per entity kind, rebuilt once per sense and act phase |
| Randomness | `RandomStreams` → `RandomStream` (PCG32 seeded from SHA-256 of seed and name) |
| Recording | `RunRecorder` (files of [13](13-outputs-and-recording.md), Newtonsoft JSON with sorted keys, running SHA-256), `LiveStatistics` (for the editor graphs) |
| Views | `Body` (the view prefab of a species), `AnimalView` (holds the animal id; no `Update`), `ViewPool` |
| Species added during a run | `World.AddSpecies`; a speciation rule deciding when to split (a `TickPhase`) is left for later (owner decision) |
| Controls | settings on the World and on the evolution modules ([15](15-controls-metrics-and-gates.md)) |

### 3.12 Food: edible and diet

Whether something can be eaten is a matter of components on both sides (owner
decision): the eaten thing says *how* it is eaten and *how much* it gives; the
eater says *what* it eats.

```csharp
/// Makes what it sits on edible: a species' animals, a resource layer's items, an entity kind.
public class Edible : MonoBehaviour
{
    public EatMethod method = EatMethod.Graze;      // Graze | Strike | Scavenge
    public float energy = 25f;                       // per item, per kill or per portion
    [Header("Strike only: what a kill leaves")]
    public int carcassPortions = 0;
    public float carcassEnergy = 30f;
    public int carcassTicks = 100;
}

/// What a species eats (SPEC-15). Targets must carry an Edible component (V-24).
public class Diet : SpeciesModule
{
    [Serializable] public class Entry
    {
        public string target = "Grass";              // a layer, a species, "carcass:prey", "egg:prey"...
        public float energyScale = 1f;
    }
    public List<Entry> eats = new();

    public bool Eats(IEdibleTarget t) => ...;        // listed here and the target is edible (SPEC-13)
}
```

Making eggs edible is then two components and no code: an `Edible` on the
`EggSystem` and a diet entry `egg:prey` on the egg eater
([22 §15](22-extending-recipes.md#15-food-make-eggs-edible)).

## 4. Initialisation order

`World.Initialize()` does, in this order, and stops with a validation report at
the first error:

1. **Find species.** `GetComponentsInChildren<Species>()`; a species with another
   species above it is an error (SPEC-05).
2. **Find world modules.** Services and phases under the World but not under a
   species, in hierarchy order. Phases keep that order (TICK-01).
3. **Initialise services**: random streams from the seed, ground, layers,
   entity systems, brains, cache, mutator, recorder.
4. **Collect each species' modules**, keeping only those whose nearest `Species`
   is this one:

   ```csharp
   static List<T> Owned<T>(Species owner) where T : SpeciesModule =>
       owner.GetComponentsInChildren<T>(includeInactive: false)
            .Where(m => m.enabled && m.GetComponentInParent<Species>(true) == owner)
            .ToList();
   ```

5. **Bind**: each text gene to the nearest action above it or on its GameObject;
   free genes stay free. Build the action, gene and sense orders and the
   signature.
6. **Declare** stats and traits (`SpeciesModule.Declare`), then **Initialize**
   every module (look-ups by role or name, ARCH-08).
7. **Derive** the food web (threats) and resolve every `AnimalSet`.
8. **Validate** everything ([21](21-editor-tooling.md)); errors stop here.
9. **Create view pools** from each species' `Body`.
10. **Spawn founders** (POP-04), express their genomes, record `founder` events.

## 5. A tick in code

```csharp
public class ActPhase : TickPhase
{
    [SerializeField] ActOrder order = ActOrder.SpeciesInTurn;   // ACT-30

    public override void Run(TickContext t)
    {
        // SpeciesInTurn and AllMixed give one animal per group; Simultaneous gives one group.
        foreach (var group in t.ActGroups(order))
        {
            foreach (var a in group) t.Plan(a);       // the action's Act → intent and interaction
            foreach (var a in group) t.Move(a);       // the species' locomotion, speed and stamina
            foreach (var a in group) t.Interact(a);   // within reach after moving, diet-checked
            foreach (var a in group) t.Settle(a);     // metabolism, stamina, busy countdown
        }
    }
}
```

```csharp
public class ChooseActionsPhase : TickPhase
{
    public override Pending WaitsFor(TickContext t) => t.Decisions.Pending;   // the brains

    public override void Run(TickContext t)
    {
        foreach (var d in t.Decisions.Due)
        {
            var p = Sampling.Temper(d.Probabilities, t.World.SamplingTemperature);   // DEC-20
            d.Animal.Action = t.Stream(d.Animal.Species, "sampling").Choose(p);       // RAND-03
            d.Animal.Searching = d.Animal.BredThisPeriod = false;                     // DEC-03
        }
    }
}
```

## 6. Views

- A species' `Body` names a prefab (mesh, animator, maybe a collider for
  picking). At initialisation the World fills a pool of inactive instances under
  a hidden container; a new animal takes one, a dead one returns it.
- `AnimalView` holds only the animal id. Nothing on a view has `Update`.
- In `LateUpdate` the World places every view between its animal's previous and
  current position (interpolation by the fraction of the next tick already
  elapsed), turns it toward its heading and sets the height from the ground.
- Above a few thousand animals, `GPU instancing` (`Graphics.RenderMeshInstanced`)
  can replace GameObject views without touching the simulation.
- Clicking a view selects its animal in the Animal inspector window.

## 7. Folders, assemblies, packages

```
Assets/EvoSim/
  Runtime/          EvoSim.Runtime.asmdef        Core/ Phases/ Genes/ Senses/ Actions/ Stats/
                                                 Reproduction/ Population/ Mutation/ Environment/
                                                 Ground/ Locomotion/ Brains/ Recording/ Views/
  Http/             EvoSim.Http.asmdef           HttpBrain, Ollama, JEV, mutator clients (ARCH-10)
  Editor/           EvoSim.Editor.asmdef         inspectors, windows, validation UI (Editor only)
  Testing/          EvoSim.Testing.asmdef        WorldBuilder, ScriptedBrain, FakeMutator, Golden
  Tests/EditMode/   EvoSim.Tests.EditMode.asmdef
  Tests/PlayMode/   EvoSim.Tests.PlayMode.asmdef
  Modules/          prefabs: Eat, Flee, Hide, Follow, Rest, Mate, Hunt, Scavenge, StaminaGene, ...
  Data/             AllelePools, mutation decks (TextAssets), frozen prompts, control sentences
  Scenes/           Lab1_Full, Lab1_Small, HideVsFlee, ThreeSpecies, Terrain_Locomotion, Sandbox
  Scenarios/        ScenarioAssets (overrides, CFG-02)
```

Packages: `com.unity.nuget.newtonsoft-json` (declared explicitly),
`com.unity.ai.navigation` (NavMesh surfaces), `com.unity.test-framework`;
optional `com.unity.collections` and `com.unity.burst` if spatial queries ever
need jobs.

## 8. Notes for Unity 6000.3

- **Waiting.** `Awaitable` exists, but services return `Task`s so that `Freeze`
  can block on them; phases stay synchronous and readable.
- **HTTP.** `HttpClient` runs off the main thread and works in the editor and in
  standalone builds. `UnityWebRequest` would tie completion to the main loop and
  break `Freeze`.
- **NavMesh.** Bake a `NavMeshSurface` on the terrain. Use the static queries
  (`NavMesh.SamplePosition`, `NavMesh.Raycast`, `NavMesh.CalculatePath`), not a
  `NavMeshAgent` per animal (a component per animal, its own update loop, and
  results that depend on frame timing).
- **Raycasts.** `RaycastCommand.ScheduleBatch` with `QueryParameters`, one batch per
  sense phase (SENSE-30).
- **Enter Play Mode without domain reload.** Safe because there are no statics
  (ARCH-07).
- **Profiling.** One `ProfilerMarker` per phase and per brain call.
- **Upgrade.** Everything version-specific sits in `EvoSim.Http`, `NavMeshGround`,
  `NavMeshLocomotion` and the ray batch helper.

## 9. Performance budget

| Item | Target with the random brain (the brain's own time excluded), 340 animals |
|---|---|
| One tick, all phases | < 2 ms (the prototype: 17 ms in Python) |
| Sense phase | spatial index rebuilt once; nearest-food search by expanding rings of cells, stopping at the first ring beyond the best distance |
| Brain batch | one HTTP request per brain per tick where the server accepts a list (vLLM does) |
| Memory | no allocation per animal per tick in the hot loops (pooled lists, arrays reused) |

LLM brains dominate everything else: JEV answers about 12 queries per second, so
the memo, the answer cache and batching matter far more than C# speed.

## 10. Designed to scale (later)

Nothing is optimised now: a few hundred animals and plain C# lists are enough,
and the brain is the bottleneck. The seams below let a later version handle
thousands of animals without changing what any module means (ARCH-12).

| Seam | Now | Later, without touching module meaning |
|---|---|---|
| Batch entry points | phases call `Sense.ReadAll`, `AnimalAction.ActAll`, `Locomotion.MoveAll`, `DeathRule.CheckAll`, whose defaults loop over the single-animal methods | a module overrides the batch method with a Burst job; its single-animal method stays as the readable reference and the test oracle |
| Animal storage | `Animal` objects in a list per species; stats and traits in small arrays reached through `StatId` and `TraitId` | struct-of-arrays storage in `Core`; `Animal` becomes a handle with the same accessors |
| Spatial queries | `ISpatialIndex`: a uniform hash grid rebuilt per phase | a k-d tree, or a Burst-built grid, behind the same interface |
| Raycasts | one `RaycastCommand` batch per sense phase | unchanged |
| Brains | one request per brain per tick, answer cache, memo | several servers per brain, requests split across them |
| Views | pooled GameObjects | instanced rendering (`Graphics.RenderMeshInstanced`) |

The single-animal methods stay the ones students write and read; batch versions
are optional optimisations that must pass the same conformance suites
([30 §4](30-tests.md#4-conformance-suites-for-students-modules)).
