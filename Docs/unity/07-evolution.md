# 07 — Evolution

Reproduction, inheritance, variation and population control, each a small
pluggable rule on the species, run by systems in a fixed order.

```text
MatingSystem                       BirthSystem                                          Population
 canMate(a) && canMate(b)    ┌──► crossover(a.genome, b.genome)  ─► Genome child ─┐
 && both chose "mate"        │    mutation pipeline (per locus, p_mut)              │
 && within reach   ──► birth queue  ├ pick operator (weighted)                       ├─► AgentFactory.Spawn
                             │    ├ operator.MutateAsync(text)   (LLM: cached)      │     + AgentBorn event
                             │    └ guards: clean → max words → charset → changed   │
                             └──► development(genome) ─► Phenotype ─────────────────┘
ImmigrationSystem: below the species floor → founders from FounderPool (immigrant = true)
DeathSystem: starvation | old age | predator → AgentDied → pool
```

## Genetics core (plain C#, `GeneticAgents.Core.Genetics`)

| Type | Responsibility | Prototype |
|---|---|---|
| `Allele` | immutable gene text (+ optional numeric `Values` for trait loci), locus id, origin, parent allele, operator, model, seed | `genome.Allele` |
| `AlleleRegistry` | one shared table per run; same (locus, text) → same allele id `eat:12` (Flyweight) | `AlleleRegistry` |
| `Genome` | immutable array of allele ids in schema order; `Replace(locus, id)` returns a new genome | `Genome` |
| `GenomeKey` | stable hash of gene **texts**, run-independent (cache key) | `genome_key` |

Genomes never hold strings, only ids into the registry, so a population of
thousands shares a few hundred allele texts, and allele frequencies are a
count over ids.

## Mating conditions (Specification pattern)

```csharp
public interface IAgentCondition { bool IsMet(AgentRoot agent, SimContext ctx); string Describe(); }
```

Conditions are small `[SerializeReference]` objects that compose:

```text
canMate = All
           ├ IsAdult
           ├ EnergyAbove   50
           ├ ChoseAction   "mate"
           └ Not
              └ BredThisTick
```

`Describe()` prints `adult AND energy > 50 AND chose mate AND NOT bred this
tick` in the inspector and the Brain Debugger, so the rule is readable
without opening code. Students add `InSameHerd`, `SeasonIs("spring")`,
`HasTerritory` as one-class conditions. `MatingSystem` pairs candidates
within `reachCells` (prototype: Chebyshev ≤ 1), parents pay
`childEnergy / parents` each.

## Crossover

```csharp
public interface ICrossover { Genome Cross(Genome a, Genome b, GenomeSchema schema, IRandom rng); }
```

| Strategy | Rule | Use |
|---|---|---|
| `UniformCrossover` | each locus 50/50 from either parent | default (prototype) |
| `OnePointCrossover` | prefix from A, suffix from B | compare linkage effects |
| `LocusBlockCrossover` | action loci together, temperament together | "gene linkage" exercise |
| `CloneParent` | copy one parent | asexual control C7 |
| `DiploidCrossover` + `DominanceDevelopment` | keep two alleles, express one | advanced: dominance |

## Mutation pipeline (operators + guards)

```csharp
[Serializable]
public sealed class MutationPipeline
{
    [Range(0, 1)] public float perLocusRate = 0.03f;                              // prototype p_mut
    public List<WeightedOperator> operators;                                     // operator + weight
    [SerializeReference, SubclassSelector] public List<IGeneGuard> guards;        // run in order
    [Min(0)] public int retries = 3;                                              // LLM rewrites
    [SerializeReference, SubclassSelector] public IMutationOperator fallback = new IntensityOperator();
}

public interface IMutationOperator
{
    string Id { get; }
    bool CanMutate(Allele allele, LocusDefinition locus);             // text vs trait loci
    Task<string> MutateAsync(MutationRequest r, CancellationToken ct); // null = no change possible
}

public interface IGeneGuard { GuardResult Check(string candidate, Allele parent, LocusDefinition locus); }
```

| Operator | LLM | Effect (prototype `mutation.py`) |
|---|---|---|
| `IntensityOperator` | no | moves along never ↔ rarely ↔ sometimes ↔ often ↔ always |
| `NegateOperator` | no | always ↔ never, seek ↔ avoid, run from ↔ stand up to … |
| `ConditionSwapOperator` | no | "when hungry" → "when threatened"; adds a condition if none |
| `SynonymOperator` | no | close ↔ near, explore ↔ roam … (lexicon is a `WordList` asset) |
| `FounderReintroduceOperator` | no | replace with another founder allele of the locus (immigration of ideas) |
| `LlmRewriteOperator` | yes | style ∈ {random change, invert, exaggerate, soften, add a condition, more specific, more general}; word limit taken from the locus; cached by (text, style, seed, model) |
| `GaussianOperator` | no | numeric trait loci: value + N(0, σ), clamped to `traitRange` |

| Guard | Rejects |
|---|---|
| `CleanTextGuard` | *normalises* first: strips "Here is the modified prompt:", quotes, keeps the first sentence |
| `MaxWordsGuard` | longer than the locus `maxWords` (bloat control) |
| `CharsetGuard` | characters outside plain text |
| `ChangedGuard` | identical to the parent (case-insensitive) |

Flow per locus: roll `perLocusRate` with the `mutation` stream → pick an
operator by weight among those whose `CanMutate` is true → up to `retries`
attempts through the guards → if the LLM failed, use `fallback` → register
the allele with full lineage → publish `GeneMutated`. The pipeline's
counters (`tried`, `ok` per operator) feed the "mutant establishment rate"
metric.

## Development (genotype → phenotype)

```csharp
public interface IDevelopment
{
    Task<Phenotype> DevelopAsync(Genome genome, AlleleRegistry alleles, SpeciesDefinition species,
                                 IRandom rng, CancellationToken ct);
}
public sealed class Phenotype { public IReadOnlyDictionary<string, float> Values { get; } }
```

| Implementation | Phenotype | Applied to |
|---|---|---|
| `NoDevelopment` | empty | default: option A reads genes directly |
| `TraitDevelopment` | trait loci values (speed, vision, size) | components that implement `IPhenotypeReceiver` (e.g. `FoodSense.vision`) |
| `LlmDevelopment` | utilities per action, thresholds (one LLM call at birth, cached) | `UtilityBackend` (option B) |

`AgentFactory` calls `IPhenotypeReceiver.Apply(phenotype)` on every
component after `OnSpawned`, so body traits can evolve without the
decision layer knowing.

## Population policy

```csharp
public interface IPopulationPolicy
{
    int InitialCount { get; }
    int Floor { get; }        // below → immigrants from the founder pool
    int Cap { get; }          // at or above → MatingSystem refuses births
}
```

`FloorAndCapPolicy(init 30, floor 10, cap 60)` is the prototype. `HallOfFamePolicy`
refills from the best genomes seen so far instead of founders; a
`FixedCountPolicy` keeps scripted predators at exactly *n*.

## Selection is implicit

There is no fitness function in the loop: agents that eat, avoid
predators and mate leave more offspring. Fitness-like numbers (lifespan,
offspring, food, steals) are **measurements**, published in `AgentDied`
and aggregated by the recorder and the dashboards. An explicit-fitness
generational GA is still possible as a swap of systems
([11](11-change-scenarios.md#8-explicit-fitness-generational-ga)), which
makes "implicit vs explicit selection" a comparison students can run.

## Experimental controls without flags

| Control | How it is expressed | Where |
|---|---|---|
| C2 no mutation | `perLocusRate = 0` | profile modifier `SetMutationRate(0)` |
| C3 shuffled genomes | `GenomeShuffleDecorator` outermost in the decision chain | modifier `WrapDecision(...)` |
| C4 random founders | founder pool = control alleles | modifier `UseFounderPool(...)` |
| C5 rule-based | terminal backend = `RuleBasedBackend` | modifier `ReplaceTerminalBackend(...)` |
| C7 asexual | `CloneParent` + solo mating condition | modifiers `SetCrossover`, `SetMating` |

None of the framework classes contains an `if (shuffled)`; controls are
compositions, so a new control is a new decorator or modifier.
