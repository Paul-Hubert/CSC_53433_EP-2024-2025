# 22 — Extending: recipes for students

Every extension is one class deriving from one base class, placed as a
component in the right part of the tree. Each recipe ends with a checklist: what
the validator expects ([21](21-editor-tooling.md)) and which test to copy from
the templates ([30](30-tests.md)).

The running example: **water and thirst**. The terrain lab adds lakes; animals
get thirsty, sense water, can choose to drink, and a gene says when.

## 1. A stat: thirst

```csharp
public class Thirst : SpeciesModule
{
    [SerializeField] float perTick = 0.5f, deadly = 100f;
    public StatId Value { get; private set; }
    public float PerTick => perTick;
    public float Deadly => deadly;

    public override void Declare(SpeciesBuilder b) =>
        Value = b.DeclareStat("thirst", start: StatStart.Fixed(0f), min: 0f, max: deadly);

    public override void WritePromptRules(PromptWriter w) =>
        w.Rule($"Thirst grows every step; an animal dies of thirst at {deadly}. Drinking resets it.");
}

/// Thirst grows by itself: a small death rule does both jobs.
public class DiesOfThirst : DeathRule
{
    Thirst thirst;
    public override void Initialize() => thirst = Species.Module<Thirst>();
    public override string CauseOfDeath(Animal a)
    {
        a[thirst.Value] += thirst.PerTick;                    // ages with the animal, in the death phase
        return a[thirst.Value] >= thirst.Deadly ? "thirst" : null;
    }
}
```

Checklist: the cause name appears in `death` events and the statistics
(`deaths_thirst`); copy *T-ANIM stat template*.

## 2. A sense: thirst level and nearest water

```csharp
// Thirst as low / medium / high: the existing LevelSense, no code.
//   Add [LevelSense] stat "thirst", thresholds 30 / 70, label "Thirst",
//   V2 words: "I am not thirsty." / "I am thirsty." / "I am parched."

public class NearestWaterSense : Sense
{
    [SerializeField] Bands bands = Bands.Reference;
    Ground ground;
    public override IReadOnlyList<string> Tokens => bands.TokensWithHere;   // here, adjacent, ..., none
    public override void Initialize() => ground = World.Service<Ground>();
    public override int Read(Animal a, SenseContext s)
    {
        var water = ground.NearestWater(a.Position, bands.Vision(a));      // a query the terrain ground offers
        return water == null ? bands.None : bands.IndexOf(water.Value.Distance, here: 0.5f);
    }
    public override string Write(int t, TextStyle style) => $"Water: {bands.Describe(t, style)}.";
    public override void DeclareKeywords(KeywordTable k)
    {
        k.Condition(@"water is (close|near)|by the water", this, "here", "adjacent", "close");
        k.Condition(@"no water|far from water", this, "none", "far");
    }
}
```

Checklist: observation space grows (×3 for thirst, ×6 for water) and the species
inspector shows the new size; copy *T-SENSE template* (tokens, text, the
"nothing there" case, hidden things).

## 3. An action: drink

```csharp
public class DrinkAction : AnimalAction
{
    Thirst thirst;
    public override void Initialize() => thirst = Species.Module<Thirst>();

    public override void Act(Animal a, ActContext c)
    {
        var water = c.Ground.NearestWater(a.Position, c.Vision(a));
        if (water == null) { c.Search(); return; }
        c.WalkTo(water.Value.Shore, stopAt: 0f);
        c.OnArrival(new Drink(thirst));
    }

    public override float KeywordScore(ObservationView o) =>
        o.Token("Thirst") switch { "high" => 3f, "medium" => 1f, _ => -1f };
    public override string KeywordPattern => @"\b(drink|water|thirst|thirsty)\b";
    public override bool IsRelevant(ObservationView o) => o.Token("Water") != "none";

    sealed class Drink : IInteraction
    {
        readonly Thirst thirst;
        public Drink(Thirst t) => thirst = t;
        public float Reach => 0.5f;
        public void Apply(Animal a, InteractionContext c) => a[thirst.Value] = 0f;
    }
}
```

Then, on the action's GameObject, a `TextGene` with founders such as "Drink
whenever you pass water.", "Drink only when very thirsty.", the neutral "No
preference.", and a contrast pair "Always drink, whatever happens." / "Never
drink unless parched.". Set the action's description: *go to the nearest visible
water and drink*. Save the GameObject as a prefab in `Modules/` to share it.

Checklist: the action shows in the species' action table and the prompt preview;
the gene passes the guards; the signature changes (accept it); copy *T-ACT
template* (search when nothing is in sight, reach, intent never moves the animal
itself).

## 4. A number gene

No code: add a `NumberGene` under the species, set its trait (`stamina.max`),
range and founder values, and make sure a `GaussianMutation` accepts it. To keep
the gene interesting, give the trait a cost: e.g. a `Metabolism` setting
"base cost per tick += 0.01 × stamina.max". Validator V-47 reminds you.

## 5. A free text gene (a temperament)

Add a `TextGene` that is not under an action and tick *free*; give it a label
("temperament") and founders ("Be bold.", "Be careful.", "No preference.").
It appears in the prompt's genes block and every brain reads it; the keyword
brain ignores it unless its words match an action.

## 6. A resource layer: berries

```csharp
public class BerryBushes : ResourceLayer
{
    [SerializeField] float regrowPerBushPerTick = 0.01f;
    [SerializeField] int berriesPerBush = 5;
    // Bushes are the terrain's tree instances of one prototype; each holds up to N berries.
    public override ResourceItem Nearest(Vector3 p, float radius) => ...;
    public override bool Consume(ResourceItem item) => ...;
    public override void Tick(RandomStream rng) => ...;      // regrowth, in the environment phase
}
```

Then add a diet entry `graze BerryBushes +15` to the species that eat berries, and
a `NearestResourceSense` with layer "Berries" if the brain should know about
them. Checklist: copy *T-ENV layer template* (consume once, regrow in its own
phase with its own stream).

## 7. A brain

```csharp
public class TinyNetBrain : Brain
{
    [SerializeField] int hidden = 8;
    public override string Id => $"tinynet-{hidden}";
    public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
    {
        var rows = new float[batch.Count][];
        for (int i = 0; i < batch.Count; i++)
            rows[i] = Softmax(Forward(Encode(batch[i].Observation), WeightsFrom(batch[i].Genes)));
        return BrainAnswer.Now(rows);                        // answered at once, nothing to wait for
    }
}
```

An HTTP brain derives from `HttpBrain` and only builds the request and reads the
answer. Checklist: copy *T-DEC brain template* (row length, sums to 1, order,
same query same answer, failure handling).

## 8. A mutation operator: the intensity ladder

```csharp
public class IntensityLadder : MutationOperator
{
    static readonly string[] Ladder = { "never", "rarely", "sometimes", "often", "always" };
    public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;
    public override MutationJob Start(Gene g, Allele parent, RandomStream rng)
    {
        var words = parent.Text.Split(' ');
        int i = Array.FindIndex(words, w => Ladder.Contains(w.ToLowerInvariant()));
        if (i < 0) return MutationJob.Failed("no intensity word");
        int k = Array.IndexOf(Ladder, words[i].ToLowerInvariant());
        int step = rng.NextBool() ? 1 : -1;
        words[i] = Ladder[Mathf.Clamp(k + step, 0, Ladder.Length - 1)];
        return MutationJob.Done(string.Join(" ", words), $"ladder{step:+0;-0}");
    }
}
```

Put it under the species' `Mutation` object, with `onlyThese` naming the genes
it applies to. Checklist: copy *T-MUT template* (blind: it receives only the gene;
deterministic with a seed; guards still apply).

## 9. A tick phase: seasons

```csharp
public class SeasonsPhase : TickPhase
{
    [SerializeField] int yearLength = 2000;
    [SerializeField] float winterRegrowFactor = 0.2f;
    FoodGrid food;
    public override void Initialize() => food = World.Service<FoodGrid>("Grass");
    public override void Run(TickContext t)
    {
        bool winter = t.Tick % yearLength >= yearLength / 2;
        food.RegrowFactor = winter ? winterRegrowFactor : 1f;
    }
}
```

Place it among the `Phases` children before *Environment*. Checklist: V-08 checks
the order; copy *T-TICK phase template*.

## 10. A species

Duplicate the Rabbit, rename it ("Deer"), change its display name, body,
parameters and founder sentences; put a diet entry on the Wolf ("strike Deer
+80"). The food web window shows the new edges and the threats update by
themselves. A cannibal species simply lists itself in its diet.

## 11. Hide instead of flee into cover

The reference prey has both: a `FleeAction` (with *flee into cover* off) and a
`HideAction` with its `NearestCoverSense` child and its gene. Remove the hide
prefab to get a prey that can only run; turn *flee into cover* on to get the
prototype's behaviour. Scenario S06 compares the three ([31](31-scenarios.md)).

## 12. Act order, decision timing, speciation

- Act order: a setting of the `ActPhase` (species in turn, all mixed,
  simultaneous).
- Decision period and staggering: settings of the World and, per species, of an
  optional `DecisionSchedule` module.
- Speciation: add a `SpeciationRule` phase. A simple one: every 500 ticks, if the
  animals of a species split into two groups whose genome keys share fewer than
  half their alleles, the smaller group becomes a new species with
  `World.AddSpecies` (it inherits relations, SPEC-31).
