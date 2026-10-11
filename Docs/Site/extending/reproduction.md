---
uid: extend-reproduction
---

# Reproduction and population

Six modules in a species' `Life` group decide how it breeds and how many of its animals the world keeps: who mates, how
many babies a mating makes, how the parents' genes mix, how long eggs incubate, and the population's cap and floor. Most
changes are settings; override a member when the rule itself must change. How these modules work together is explained
in [Reproduction and population](../guides/reproduction-and-population.md).

The phases find each module by its type (`s.Module<Litter>()`), so a species has one of each, and a replacement derives
from the reference class: `MyLitter : Litter`, `MyRule : MatingRule`. Crossovers derive from the abstract
<xref:EvoSim.Crossover>.

| Module | Override | Called |
|---|---|---|
| <xref:EvoSim.Crossover> | [Cross](xref:EvoSim.Crossover.Cross(EvoSim.Genome,EvoSim.Genome,EvoSim.RandomStream)) (must), [Copy](xref:EvoSim.Crossover.Copy(EvoSim.Genome)) | breed phase, once per baby of two parents (`Cross`) or one parent (`Copy`) |
| <xref:EvoSim.Litter> | [Size](xref:EvoSim.Litter.Size(EvoSim.RandomStream,System.Single[])), [ChildEnergy](xref:EvoSim.Litter.ChildEnergy) | breed phase, once per conception; hatch phase, for each baby's energy |
| <xref:EvoSim.MatingRule> | [IsReady](xref:EvoSim.MatingRule.IsReady(EvoSim.Animal)), [IsAdult](xref:EvoSim.MatingRule.IsAdult(EvoSim.Animal)), [Sexual](xref:EvoSim.MatingRule.Sexual) | breed phase, for seekers and partners; kin senses, to report readiness |
| <xref:EvoSim.Incubation> | [Ticks](xref:EvoSim.Incubation.Ticks) | breed phase, once per conception |
| <xref:EvoSim.CapRule> | [Cap](xref:EvoSim.CapRule.Cap) | migration phase every tick; breed phase in Block mode |
| <xref:EvoSim.FloorRule> | settings only (**Floor**) | floor phase every tick |

## A crossover

The reference <xref:EvoSim.UniformCrossover> takes each locus from either parent with probability ½ (GENE-30). This one
cuts the genome once: loci before a random cut come from one parent, the others from the other.

```csharp
using EvoSim;

public class OnePointCrossover : Crossover
{
    public override Allele[] Cross(Genome a, Genome b, RandomStream rng)
    {
        CheckSpecies(a, b);                            // genomes never cross species (SPEC-04)
        bool aFirst = rng.NextBool();                  // which parent gives the loci before the cut
        int cut = rng.Range(0, a.Count + 1);
        var first = aFirst ? a : b;
        var second = aFirst ? b : a;
        var child = new Allele[a.Count];
        for (int i = 0; i < child.Length; i++) child[i] = i < cut ? first[i] : second[i];
        return child;
    }
}
```

Replace the `UniformCrossover` component of the species' `Life` group with yours.

> [!NOTE]
> Neighbouring loci now travel together, like linked genes on a chromosome. This leaves the contract's independent loci
> (GENE-30): fine for an experiment, but it is no longer the reference.

## A litter

Override [Size](xref:EvoSim.Litter.Size(EvoSim.RandomStream,System.Single[])) to change how many babies a mating
makes. The reference draws k in [**Min**, **Max**] and cuts it to what the parents can pay, never below **Min**
(REPRO-10). This one makes small litters unless both parents are rich:

```csharp
using EvoSim;
using UnityEngine;

public class CarefulLitter : Litter
{
    [SerializeField, Min(0f), Tooltip("Energy every parent needs for a full litter, in points.")]
    float richEnergy = 80f;

    public override int Size(RandomStream rng, float[] parentEnergies)
    {
        int k = base.Size(rng, parentEnergies);        // always draw: the stream stays the same
        foreach (var e in parentEnergies) if (e < richEnergy) return Min;
        return k;
    }
}
```

Each parent then pays **Child Energy** / (number of parents) × k (REPRO-11). The cap's Block mode may cut the litter
further.

## Who mates

Most changes to the <xref:EvoSim.MatingRule> are settings: **Mate Energy** (50 points), **Maturity** (150 ticks, a trait
a number gene can set), **Reach** (1 m), **Sexual**. Override a predicate to change who is ready:

```csharp
using EvoSim;

public class AgeLimitedMating : MatingRule
{
    public override bool IsReady(Animal a) => base.IsReady(a) && a.Age < 1200;   // old animals stop breeding
}
```

The brain reads the rule in the prompt: when you change who mates, change **Prompt Rule** to say so.

## Incubation, caps and floors

- **Incubation.** Set **Ticks** above 0 and babies hatch later; the mutator's answers usually arrive meanwhile, so the
  world doesn't wait (MUT-33). Override `Ticks` to compute it, without random draws.
- **Cap.** Choose **Mode** Migrate (reference: random animals leave above the cap) or Block (no conception at the cap).
  Override `Cap` to compute it, without random draws.
- **Floor.** **Floor** 0 means no newcomers. To bring newcomers another way, replace the `FloorPhase` with your own
  [tick phase](world-modules.md).

## Eggs as food

In the reference nothing eats eggs. Eggs exist only while they incubate, so:

1. give the species that lays them an **Incubation** above 0, e.g. 20 ticks;
2. add an <xref:EvoSim.Edible> to the World's `Environment ▸ Eggs` object (method *graze*: an egg is taken whole);
3. add a diet entry `egg:prey` to the egg eater's <xref:EvoSim.Diet>, and give it a way to find eggs: an action pointed
   at eggs, and a sense if its brain should know about them.

An eaten egg never hatches and is recorded as an `egg_lost` event with the reason `eaten` (REPRO-23). The details are in
[Food, cover, entities and ground](environment.md#who-eats-what).

## Rules your module keeps

- **Draw only from the stream you are given** (RAND-03): `Size` gets the species' `litter` stream, `Cross` its `mutation`
  stream. Draw the same number of times whatever the parents, as `CarefulLitter` does by calling `base.Size` first.
- **A crossover returns a new array**: one allele per locus, in locus order, each taken from that same locus of a parent
  (GENE-30), after `CheckSpecies`. It never changes the parents' genomes. It is called once per baby, so siblings differ
  (GENE-32).
- **Readiness only reads.** `IsReady`, `IsAdult` and `Sexual` change nothing and draw nothing: kin senses call `IsReady`
  too, while the world is being sensed.
- **Computed settings are deterministic.** `Ticks`, `Cap` and `ChildEnergy` depend only on the settings and the world
  state, never on the clock or `UnityEngine.Random`.

## Test it

These modules have no conformance suite. Copy the tests in
`Assets/EvoSim/Tests/EditMode/Reproduction/ReproductionTests.cs`: T-REPRO-05 checks litter sizes and the energy cut,
T-GENE-08 runs 10 000 crossovers and checks that each locus comes from the first parent half the time. See
[Testing your module](testing-your-module.md).

## Read next

- Rewrite the reference `Litter` and `UniformCrossover` from their stubs (*EvoSim ▸ Exercises ▸ Replace with stub ▸
  9 Litter*, *10 UniformCrossover*): [the teaching path](exercises.md).
- What a baby inherits and how it mutates: [Genes](genes.md), [Mutation](mutation.md).
- The contract: [11 — Reproduction and population](../spec/11-reproduction-and-population.md).
