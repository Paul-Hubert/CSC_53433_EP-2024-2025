---
uid: extend-brain
---

# A brain

A brain turns each deciding animal's query into one probability per action. Derive from <xref:EvoSim.Brain> to decide
in C#: with rules, a small network, or a model you run yourself. For a model behind a web API, derive from
<xref:EvoSim.HttpBrain> instead: see [An HTTP brain or mutator server](http-brains.md). To change *when* animals decide,
you need no brain: see [Decision timing](#decision-timing). How decisions, the memo and the cache work is in
[Decisions and brains](../guides/brains-and-decisions.md).

## The base class

| Member | | What it does, and when it is called |
|---|---|---|
| [Id](xref:EvoSim.Brain.Id) | must implement | a short name ("random", "keyword-v1"), part of memo and cache keys and the name of its cache file |
| [Ask](xref:EvoSim.Brain.Ask(System.Collections.Generic.IReadOnlyList{EvoSim.DecisionQuery})) | must implement | answers a batch of queries with a <xref:EvoSim.BrainAnswer>; called by the ask-brains phase once per tick and batch, on the main thread |
| [Memoizable](xref:EvoSim.Brain.Memoizable) | may override | false for a brain with memory: every query then reaches it ([below](#brains-with-memory)) |
| [MaxActions](xref:EvoSim.Brain.MaxActions) | may override | the most actions a species may have with this brain (DEC-14, V-11); no limit by default |
| [MaxPromptTokens](xref:EvoSim.Brain.MaxPromptTokens) | may override | the longest prompt it takes, in estimated tokens (V-11); 0 = no limit |
| [AcceptsAttachments](xref:EvoSim.Brain.AcceptsAttachments) | may override | true if it reads attachments such as images (V-51); false by default |
| [CanMixSpecies](xref:EvoSim.Brain.CanMixSpecies) | may override | false to get one batch per species (DEC-13); true by default |
| [AnswerInstruction](xref:EvoSim.Brain.AnswerInstruction) | may override | the line that ends the prompt, `{ask}` (PROMPT-07); empty by default |
| [ModelIdentity](xref:EvoSim.Brain.ModelIdentity), [Mode](xref:EvoSim.Brain.Mode) | may override | the model with its revision, and the brain's mode: parts of the cache key (DEC-33) |
| [Initialize](xref:EvoSim.WorldModule.Initialize), [Validate](xref:EvoSim.WorldModule.Validate(EvoSim.ValidationReport)) | may override | set up before the run; report configuration problems |

Two helpers make rows: [Uniform](xref:EvoSim.Brain.Uniform(System.Int32)) and
[Normalize](xref:EvoSim.Brain.Normalize(System.Double[])), which turns non-negative weights into a valid row. What a
query holds is listed in [Decisions and brains](../guides/brains-and-decisions.md#the-query).

## A complete example

A small rule-based brain, in the spirit of the prototype's keyword brain (08 §6). Each action starts with weight 1; its
gene's wording pushes it up or down, and fleeing gets more likely when a predator is in sight.

```csharp
using System.Collections.Generic;
using EvoSim;

public class KeywordBrain : Brain
{
    public override string Id => "keyword-v1";

    public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
    {
        var rows = new float[batch.Count][];
        for (int i = 0; i < batch.Count; i++)
        {
            var q = batch[i];
            string threat = new ObservationView(q.Species, q.Observation).Token("Predator");
            var weights = new double[q.Species.Actions.Count];
            for (int k = 0; k < weights.Length; k++)
            {
                string action = q.Species.Actions[k].Name;
                weights[k] = Push(SentenceOf(q, action));
                if (action == "flee" && threat != null && threat != "none") weights[k] *= 4.0;
            }
            rows[i] = Normalize(weights);                    // weights ≥ 0 → a row that sums to 1
        }
        return BrainAnswer.Now(rows);                        // answered at once: nothing to wait for
    }

    // An action's gene is labelled with the action's name (PROMPT-02).
    static string SentenceOf(DecisionQuery q, string label)
    {
        foreach (var g in q.Genes) if (g.Key == label) return g.Value.ToLowerInvariant();
        return "";
    }

    static double Push(string gene) =>
        gene.Contains("never") || gene.Contains("avoid") ? 0.2 :
        gene.Contains("always") || gene.Contains("whenever") ? 3.0 : 1.0;
}
```

**Where to put it.**

1. *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Brain* writes `Assets/Student/<Name>.cs` (a uniform brain) and its test.
2. Add the component on a child of the World's `Brains` object: a world service, enabled, not under a species (V-10).
3. Select it as the World's **Default Brain**, or as one species' **Brain**. Validation checks your limits against every
   species that uses it.

## Answering now or later

<xref:EvoSim.BrainAnswer> has two forms. [Now](xref:EvoSim.BrainAnswer.Now(System.Single[][])) takes every row at
once. [Later](xref:EvoSim.BrainAnswer.Later(System.Int32)) returns an empty answer that you fill from another thread
with [SetRow](xref:EvoSim.BrainAnswer.SetRow(System.Int32,System.Single[])) or
[Fail](xref:EvoSim.BrainAnswer.Fail(System.Int32,System.String)), then close with
[Complete](xref:EvoSim.BrainAnswer.Complete). The choose-actions phase waits until then. This is the pattern
<xref:EvoSim.HttpBrain> follows:

```csharp
public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
{
    var answer = BrainAnswer.Later(batch.Count);
    var prompts = new string[batch.Count];
    var actions = new int[batch.Count];
    for (int i = 0; i < batch.Count; i++)                    // main thread: read what you need now
    {
        prompts[i] = batch[i].Prompt(this);
        actions[i] = batch[i].Species.Actions.Count;
    }
    Task.Run(() =>
    {
        try { for (int i = 0; i < prompts.Length; i++) answer.SetRow(i, Score(prompts[i], actions[i])); }
        catch (Exception e) { for (int i = 0; i < prompts.Length; i++) if (answer.Row(i) == null) answer.Fail(i, e.Message); }
        finally { answer.Complete(); }                       // always, exactly once
    });
    return answer;
}

static float[] Score(string prompt, int actions) => Uniform(actions);   // your model here
```

Read every Unity object (species, action names, prompts) in `Ask`, on the main thread; the background code touches only
plain data.

## Brains with memory

The memo assumes that the same query always gets the same row (DEC-12). A brain that remembers or learns breaks that
assumption: override [Memoizable](xref:EvoSim.Brain.Memoizable) to return false. Its queries are then never memoised,
shared within a tick or cached, and each carries the animal's id in
[AnimalId](xref:EvoSim.DecisionQuery.AnimalId). The test brain <xref:EvoSim.Testing.MemoryBrain> does this:

```csharp
public override string Id => "memory";
public override bool Memoizable => false;    // every deciding animal's query reaches Ask, with its AnimalId
```

## Strict mode and the cache

**Strict** stops the run cleanly when a query fails or a row is malformed; off, the animal draws from a uniform row and
the failure is counted (DEC-40). **Use Cache** stores good rows in the World's <xref:EvoSim.AnswerCache> for later runs
(DEC-33). Both are off by default, and on for HTTP brains. The cache trusts your keys: change
[Id](xref:EvoSim.Brain.Id) (`keyword-v2`) whenever the answers change, and put every setting that changes an answer in
[ModelIdentity](xref:EvoSim.Brain.ModelIdentity) or [Mode](xref:EvoSim.Brain.Mode). Otherwise a later run replays old
answers.

## Rules your brain keeps

1. **One valid row per query** (DEC-11): in query order; one entry per action of that query's species, in the species'
   action order; every entry ≥ 0; the sum 1 ± 10⁻⁶. `Normalize` and `Uniform` make such rows.
2. **The same query gives the same row** (DEC-12): no random draws, no clock, no state, unless `Memoizable` is false.
3. **Read only the queries.** A brain never moves an animal or changes the world (CORE-06).
4. **Fail rather than invent**: mark a query you can't answer with `Fail`. A failure is never stored (DEC-34).
5. **Complete a later answer exactly once**, also when everything failed. The tick waits for it.
6. **State your limits** (DEC-14): if a species can't fit, validation says so before the run instead of a failure during it.

## Test it

The brain conformance suite, `DecisionTests.BrainConformance`, asks each brain of the EvoSim assemblies 20 queries twice,
and checks every row and that both answers are equal. A brain in `Assets/Student` is not in its list: add the same two
checks to the test written with your script.

```csharp
[Test]
public void RowsAreValidAndRepeat()
{
    var w = New().Ecology(prey: s => s.Population(0), predator: s => s.Population(0))
                 .ReferencePhases().DefaultBrain<KeywordBrain>().Build();
    var prey = w.FindSpecies("prey");
    var a = Place.Animal(prey, Place.At(10, 10));
    w.Space.Rebuild();
    var o = prey.Observe(a, new SenseContext(w));
    var q = new DecisionQuery(prey, DecisionQuery.BrainGenes(prey, a.Genome), o, prey.Describe(o, TextStyle.V1),
                              TextStyle.V1, a.Genome.BrainKey);
    var first = w.DefaultBrain.Ask(new[] { q });
    first.Pending.Wait();
    var again = w.DefaultBrain.Ask(new[] { q });
    again.Pending.Wait();
    Assert.IsNull(BrainAnswer.Check(first.Row(0), prey.Actions.Count), "a valid row");
    CollectionAssert.AreEqual(first.Row(0), again.Row(0), "the same query, the same row");
}
```

The test class derives from <xref:EvoSim.Testing.WorldFixture>. To test a phase or an action against chosen answers, use
the test brain <xref:EvoSim.Testing.ScriptedBrain>. More in [Testing your module](testing-your-module.md).

## Decision timing

When animals decide is set per species by a <xref:EvoSim.DecisionSchedule>, with no code. Add one under the species (the
reference species have one in their `Life` group):

| Field | Reference | Effect |
|---|---|---|
| **Period** | 0 | ticks between two decisions of this species; 0 = the World's **Decision Period** (4) |
| **Staggered** | on | an animal decides when `(tick + id) % period == 0`, so the decisions spread over the period |

Without a schedule, a species decides all together every World period. Both settings change behaviour (DEC-04). Whatever
the schedule says, a busy animal never decides (DEC-02), and an animal without an action decides on the next tick
(DEC-01).

For other timing, derive from `DecisionSchedule` and override
[IsDue](xref:EvoSim.DecisionSchedule.IsDue(EvoSim.Animal,System.Int32,EvoSim.World)). The sense phase calls it at the
start of every tick for each living, not busy animal. It must depend only on the tick and the animal's state, and draw no
random number, so a seed still repeats the run. Keep one schedule per species: replace the reference one rather than
adding a second.

```csharp
[RequiresModule(typeof(Energy))]
public class HungryDecideOften : DecisionSchedule
{
    Energy energy;
    public override void Initialize() => energy = Species.Module<Energy>();

    // Hungry animals decide every 2 ticks; the others follow the usual schedule.
    public override bool IsDue(Animal a, int tick, World w) =>
        a[energy.Value] < 30f ? tick % 2 == 0 : base.IsDue(a, tick, w);
}
```

## Read next

- Prompt hooks: a text brain adds only its `AnswerInstruction`; rule lines come from `WritePromptRules` on species modules
  and world services ([How the prompt is written](../guides/senses-and-prompts.md#how-the-prompt-is-written)).
- <xref:EvoSim.RandomBrain>, the null model; rewrite it from its stub (exercise 13): [the teaching path](exercises.md).
- <xref:EvoSim.Testing.MemoryBrain> and <xref:EvoSim.Testing.ScriptedBrain>, the test brains.
- <xref:EvoSim.HttpBrain>, <xref:EvoSim.JevBrain>, <xref:EvoSim.OllamaPointsBrain>: [An HTTP brain or mutator
  server](http-brains.md).
- [Decisions and brains](../guides/brains-and-decisions.md) and [Senses and prompts](../guides/senses-and-prompts.md).
- Specification: [08 — Decisions, brains and prompts](../spec/08-decisions-brains-and-prompts.md).
