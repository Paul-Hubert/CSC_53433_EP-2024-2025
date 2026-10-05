# 10 — Extension recipes

Step-by-step recipes for the changes students make most. They build one
running example — **thirst and drinking** — plus a few stand-alone
extensions. Every file goes in `Assets/StudentWork/`; no framework file is
edited. After each recipe, open the **Simulation Doctor**: it lists what is
still missing.

| Recipe | You add | Patterns ([08](08-patterns-catalogue.md)) |
|---|---|---|
| R1 New agent state | `Hydration` component | Component, Template Method |
| R2 New sense | `ThirstSense`, `WaterSense` | Component, Pipeline |
| R3 New action + locus | `DrinkAction`, `drink` locus, alleles | Component, Type Object |
| R4 New world rule | `SeasonsSystem` (+ `SeasonSense`) | Systems pipeline |
| R5 New terminal backend | `HungerFirstBackend` | Strategy |
| R6 New decorator | `EpsilonExploreDecorator` | Decorator |
| R7 New mutation operator | `DropConditionOperator` | Strategy, Pipeline |
| R8 New mating condition | `NearWater` | Specification |
| R9 New validation rule | `DrinkNeedsWaterRule` | Plugin discovery |
| R10 New visualiser | `MutationFeedWindow` | Observer |
| R11 New experiment condition | `SetRenderer` modifier | Prototype + Command |

## Optional component hooks

Components opt into simulation services by implementing small interfaces
(called by systems, in id order, once per tick):

| Interface | Called by | Use |
|---|---|---|
| `IAgentTick` | `AgentTickSystem` (Lifecycle) | per-tick state change (thirst grows) |
| `IDeathCheck` | `DeathSystem` | return a `DeathCause` (e.g. `new DeathCause("dehydration")`) or null |
| `IPhenotypeReceiver` | `AgentFactory` at birth | read evolved trait values |

## R1 — New agent state: `Hydration`

```csharp
[AddComponentMenu("Genetic Agents/State/Hydration")]
public sealed class Hydration : AgentComponent, IAgentTick, IDeathCheck
{
    [SerializeField, Min(0)] float maxWater = 100, startWater = 70, lossPerTick = 0.4f;
    public float Water { get; private set; }
    public float Fraction => Water / maxWater;

    public override void OnSpawned(AgentRoot root) { base.OnSpawned(root); Water = startWater; }
    public void Drink(float amount) => Water = Mathf.Min(maxWater, Water + amount);
    public void OnTick(SimContext ctx) => Water -= lossPerTick;
    public DeathCause? CheckDeath() => Water <= 0 ? new DeathCause("dehydration") : null;
}
```

## R2 — New senses: `ThirstSense` and `WaterSense`

```csharp
[AddComponentMenu("Genetic Agents/Senses/Thirst Sense")]
public sealed class ThirstSense : AgentSense
{
    [SerializeField, Range(0, 1)] float thirstyBelow = 0.3f, quenchedAbove = 0.7f;

    public override IEnumerable<FieldSpec> Fields => new[] { new FieldSpec("thirst", "high", "medium", "low") };

    public override void Sense(in SenseContext ctx, ObservationBuilder obs)
    {
        float w = ctx.Self.GetCapability<Hydration>().Fraction;
        obs.Add("thirst", w < thirstyBelow ? "high" : w > quenchedAbove ? "low" : "medium");
    }

    public override void Validate(ValidationReport r)
    {
        if (!GetComponentInParent<AgentRoot>()?.GetComponentInChildren<Hydration>())
            r.Error(this, "ThirstSense needs a Hydration component on the same agent.",
                    fix: ("Add Hydration", () => GetComponentInParent<AgentRoot>().gameObject.AddComponent<Hydration>()));
    }
}
```

`WaterSense` is the same shape, asking `ctx.World.NearestWater(...)` and
emitting `water = none | far | near | here`. Then:

1. Add both components under the prefab's `Senses/` child.
2. Add phrases to the `PhraseBook` if the species uses `PhraseRenderer`
   (the Doctor lists the missing `(field, value)` pairs).
3. Check the observation-space estimate in the species inspector: it
   multiplied by 3 × 4 = 12. That is a real cost for the LLM cache; it is
   worth discussing in the report.

## R3 — New action and its locus: `DrinkAction`

```csharp
[AddComponentMenu("Genetic Agents/Actions/Drink Action")]
public sealed class DrinkAction : AgentAction
{
    [SerializeField, Min(0)] float vision = 12, waterPerTick = 20;
    Vector3? target;

    public override bool IsAvailable(in ActionContext ctx) =>
        ctx.World.NearestWater(ctx.Self.Position, vision) != null;

    public override void Tick(in ActionContext ctx)
    {
        var water = ctx.World.NearestWater(ctx.Self.Position, vision);
        target = water?.Position;
        if (water is { Distance: <= 1 }) ctx.Self.GetCapability<Hydration>().Drink(waterPerTick);
        else if (target is Vector3 t) ctx.Locomotion.MoveToward(t);
    }

    public override ActionDebugInfo DebugInfo => new(target, "drink");
}
```

1. **Create ▸ Genetic Agents ▸ Locus**: `id = drink`, kind `Action`,
   role "When to drink.", `maxWords = 12`. Add it to the `GenomeSchema`
   (position = gene order).
2. Add `DrinkAction` under `Actions/`, set its locus, and write the prompt
   line: *"drink: go to the nearest water and drink"*. The prompt's
   `{actions}` list and the JSON answer schema now include `drink`.
3. In the `FounderPool`, give `drink` a neutral allele and 3–5 founder
   alleles ("Drink whenever water is close.", "Only drink when very
   thirsty."). The pool's version changes (it was frozen → `v2`).
4. Optional: add keywords to the rule-based `RuleBook`
   (`drink|water|thirst`) so the CPU backend can express the gene too.

The **New Feature Wizard** does steps 1–3 for you, leaving the alleles
for you to write.

## R4 — New world rule: `SeasonsSystem`

```csharp
[Serializable, Category("World")]
public sealed class SeasonsSystem : SimulationSystem
{
    [Min(1)] public int ticksPerSeason = 2000;
    [Range(0, 2)] public float winterFoodMultiplier = 0.3f;

    public override SimPhase Phase => SimPhase.World;
    public override int Order => -10;                      // before FoodRegrowthSystem

    protected override void Tick(SimContext ctx)
    {
        bool winter = ctx.Tick / ticksPerSeason % 2 == 1;
        ctx.World.Food.RegrowMultiplier = winter ? winterFoodMultiplier : 1f;
        if (ctx.Tick % ticksPerSeason == 0)
            ctx.Events.Publish(new SeasonChanged(ctx.Tick, winter ? "winter" : "summer"));
    }
}
```

Add it to the profile's `systems` list. To make seasons **evolvable**, add
a `SeasonSense` (R2 pattern): genes like *"Store energy before winter"*
now have something to react to.

## R5 — New terminal backend: `HungerFirstBackend`

A baseline that ignores genes entirely — the "genes don't matter" control.

```csharp
[Serializable, DisplayName("Baseline / Hunger first"), Category("Baselines")]
public sealed class HungerFirstBackend : IDecisionBackend
{
    public string Name => "hunger_first";

    public Task<ActionDistribution[]> DecideAsync(IReadOnlyList<DecisionRequest> batch,
                                                  DecisionContext ctx, CancellationToken ct)
    {
        var results = batch.Select(r =>
        {
            string choice = r.Observation["predator"] == "near" ? "flee"
                          : r.Observation["food"] is "near" or "here" ? "eat" : "wander";
            return ActionDistribution.OneHot(r.Actions, choice, smoothing: 0.02f);
        }).ToArray();
        return Task.FromResult(results);
    }
}
```

It appears in the species' decision dropdown under *Baselines*. Wrap it
in the same decorators as the LLM to compare fairly.

## R6 — New decorator: `EpsilonExploreDecorator`

```csharp
[Serializable, DisplayName("Explore (ε-uniform mix)"), Category("Decorators")]
public sealed class EpsilonExploreDecorator : DecisionDecorator
{
    [Range(0, 1)] public float epsilon = 0.05f;

    public override async Task<ActionDistribution[]> DecideAsync(IReadOnlyList<DecisionRequest> batch,
                                                                 DecisionContext ctx, CancellationToken ct)
    {
        var inner = await this.inner.DecideAsync(batch, ctx, ct);
        return inner.Select(d => d.MixWithUniform(epsilon)).ToArray();
    }
}
```

Insert it anywhere in the chain from the inspector. Question for the
report: does it matter whether it sits inside or outside the memo?

## R7 — New mutation operator: `DropConditionOperator`

```csharp
[Serializable, DisplayName("Drop condition (generalise)"), Category("Word operators")]
public sealed class DropConditionOperator : IMutationOperator
{
    static readonly Regex Condition = new(@"\s+(when|unless|if|only when)\b[^.]*", RegexOptions.IgnoreCase);
    public string Id => "drop_condition";

    public bool CanMutate(Allele allele, LocusDefinition locus) =>
        locus.kind != LocusKind.Trait && Condition.IsMatch(allele.Text);

    public Task<string> MutateAsync(MutationRequest r, CancellationToken ct) =>
        Task.FromResult(Condition.Replace(r.Parent.Text, "", 1));   // guards clean and check it
}
```

Add it to the species' `MutationPipeline.operators` with a weight. The
pipeline's counters and the Genome Browser's "operator" column show how
often its mutants establish.

## R8 — New mating condition: `NearWater`

```csharp
[Serializable, Category("Place")]
public sealed class NearWater : IAgentCondition
{
    [Min(0)] public float radius = 3;
    public bool IsMet(AgentRoot a, SimContext ctx) => ctx.World.NearestWater(a.Position, radius) != null;
    public string Describe() => $"near water (≤ {radius})";
}
```

Compose it in the inspector: `canMate = All(IsAdult, EnergyAbove 50, ChoseAction mate, NearWater 3)`.

## R9 — New validation rule

```csharp
// StudentWork/Editor/DrinkNeedsWaterRule.cs
[ValidationRule("Student")]
public sealed class DrinkNeedsWaterRule : ValidationRule<SimulationProfile>
{
    protected override void Check(SimulationProfile p, ValidationReport r)
    {
        bool drinks = p.species.Any(s => s.prefab.GetComponentInChildren<DrinkAction>(true));
        if (drinks && !p.world.HasWater)
            r.Error(p.world, "A species can drink but this world has no water.",
                    why: "The drink gene could never be expressed, so selection could not act on it.");
    }
}
```

## R10 — New visualiser: `MutationFeedWindow`

```csharp
public sealed class MutationFeedWindow : EditorWindow
{
    readonly List<string> lines = new();

    [MenuItem("Window/Genetic Agents/Student/Mutation Feed")]
    static void Open() => GetWindow<MutationFeedWindow>("Mutation Feed");

    void OnEnable()  => GeneticAgentsEditor.SimulationStarted += Hook;
    void OnDisable() => GeneticAgentsEditor.SimulationStarted -= Hook;

    void Hook(Simulation sim) => sim.Context.Events.Subscribe<GeneMutated>(e =>
    {
        lines.Insert(0, $"t={e.Tick} {e.Locus} [{e.Operator}] {e.ParentText} → {e.ChildText}");
        if (lines.Count > 200) lines.RemoveAt(lines.Count - 1);
        Repaint();
    });

    void OnGUI() { foreach (var l in lines) EditorGUILayout.LabelField(l); }
}
```

Read-only by construction: it only subscribes.

## R11 — New experiment condition

```csharp
[Serializable, Category("Brain")]
public sealed class SetRenderer : IProfileModifier
{
    [SerializeReference, SubclassSelector] public IObservationRenderer renderer = new PhraseRenderer();
    public void Apply(SimulationProfile p) { foreach (var s in p.species) s.renderer = renderer; }
}
```

Add a condition "C1_V2" with this modifier to the experiment: same run,
first-person observations. Compare allele frequencies in the Genome Browser.

## How many files per feature?

| Feature | Runtime files | Assets | Editor files (optional) | Framework edits |
|---|---|---|---|---|
| Sense | 1 | phrases | 1 gizmo | 0 |
| Action + gene | 1 | 1 locus, alleles | 1 gizmo | 0 |
| Agent state | 1 | — | — | 0 |
| World rule | 1 | — | — | 0 |
| Backend / decorator | 1 | — | — | 0 |
| Mutation operator / guard / condition | 1 | — | — | 0 |
| Experiment condition | 0–1 | condition entry | — | 0 |
| Validation rule / window | — | — | 1 | 0 |
