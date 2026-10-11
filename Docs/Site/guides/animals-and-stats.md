---
uid: guide-animals-and-stats
---

# Animals, stats and death

An animal is plain data: an <xref:EvoSim.Animal> record owned by its species, not a GameObject. It holds where the
animal is, what it is doing, and its numbers: **stats** that change during its life (energy, stamina) and **traits**
fixed at birth (speeds, maximum stamina). Modules declare the stats and traits they need, so a species without a
stamina module has no stamina at all (ANIM-11). This page covers the record, the reference stats, what a tick costs,
busy states and how animals die.

## The animal record

| Fields | Meaning | Written by |
|---|---|---|
| `Id`, `Species` | unique id in the run, never reused (ANIM-02); its species for life | creation |
| `Origin`, `Generation`, `Parents`, `BornTick`, `Genome` | `"founder"`, `"immigrant"` or `"birth"`; 0 for founders and newcomers, else one more than the larger of its parents' generations | creation |
| `Age` | ticks since birth or hatching | the death phase, +1 per tick |
| `Position`, `Heading` | where it stands; the direction it faces, in degrees | the locomotion; the act phase and wandering |
| `Action`, `Searching`, `BredThisPeriod` | index of its current action (-1: none); whether it searches; whether it bred since its last decision | the choose-actions phase; `ActContext.Search`; the breed phase |
| `BusyTicks`, `BusyReason` | ticks left busy, and why | a module such as <xref:EvoSim.Digestion>; the act phase counts down |
| `Killed`, `KilledBy` | struck dead this tick, and by whom | the interaction resolver |
| `Meals`, `Offspring` | items grazed, kills and portions eaten; babies parented | every meal; the breed phase |
| `IsGone` | dead or migrated; the record stays readable | <xref:EvoSim.World.RecordDeath(EvoSim.Animal,System.String)> |
| `MetersMoved`, `TargetId`, `TargetPoint` | this tick's movement, and what its action was about | the act phase |
| `LastObservation`, `LastSituation`, `LastProbabilities` | its last decision, for the inspector | the choose-actions phase |
| `a[stat]`, `a.Trait(trait)` | the values of the declared stats and traits | the modules that use them |

Modules may add stats and traits, never untyped fields (ANIM-01): what belongs to one animal goes in its stats.

## Stats and traits

| | Stat | Trait |
|---|---|---|
| What | a number that changes during life | a number fixed at birth |
| Examples | `energy`, `stamina`, `thirst` | `speed.walk`, `speed.run`, `stamina.max`, `stamina.regen`, `kill.chance`, `maturity`, `vision` |
| Declared with | `DeclareStat(name, start, min, max)` | `DeclareTrait(name, default, min, max)` |
| Id | <xref:EvoSim.StatId> | <xref:EvoSim.TraitId> |
| Read | `a[id]` | `a.Trait(id)` |
| Write | `a[id] = v`, clamped to its range (ANIM-22) | only at creation, unless declared changeable (ANIM-17) |

Each module declares what it needs in `Declare`, on the species' <xref:EvoSim.SpeciesBuilder>, before any animal
exists. The ids it gets back are how it reads and writes the values later. Declaring a name that already exists
returns the first declaration (V-38 warns if they differ). Anyone can look a declaration up by name on
`Species.Declarations`.

A stat starts at a fixed value (<xref:EvoSim.StatStart.Fixed(System.Single)>) or full, at a trait's value
(<xref:EvoSim.StatStart.FullTrait(EvoSim.TraitId)>). Its maximum can be a trait: stamina is capped by each animal's
own `stamina.max`. A trait starts at its default, the module's setting; a number gene may replace or scale it
(ANIM-15). Modules always read the trait, never their own setting, so a gene changes behaviour with no extra code
(ANIM-16). See [Genes and mutation](genes-and-mutation.md).

## Energy and stamina

<xref:EvoSim.Energy> declares the stat `energy`. Founders and newcomers start at **60**, babies get the energy their
parents pay for them (**40**, the litter's **Child Energy**), and the maximum is **100**: a meal never raises it
higher. It has no minimum. At 0 or below, the animal starves at the next death check.

<xref:EvoSim.Stamina> is the number of meters an animal can still move. It declares the traits `stamina.max` (prey
**60**, predators **30**) and `stamina.regen` (**2** per tick), and the stat `stamina`, which starts full. Each meter
moved costs one point; the locomotion never moves an animal farther than its stamina (ANIM-21). A tick without
movement (less than 1 mm, ANIM-23) brings back `stamina.regen` points, up to the maximum, and costs **0.3** energy
while stamina is below full.

## Metabolism: what a tick costs

<xref:EvoSim.Metabolism> charges every living animal once per tick, right after it acted (ANIM-20), from what it did:
the meters its locomotion reported, its action, and whether it was busy at the start of the tick.

| Situation (reference values, prey and predators) | Energy | Stamina |
|---|---|---|
| moved *d* meters | −0.7 − 0.5 × *d* | −*d* |
| still, with the rest action | −0.2 | +2, up to the maximum |
| still, any other action | −0.7 | +2, up to the maximum |
| still and busy (digesting) | −0.2 | +2, up to the maximum |
| while stamina recovers (still, not full) | −0.3 more | — |
| graze one food item | +25 | — |
| a kill | +60 | — |
| a carcass portion | +30 | — |

So walking 1 m costs 1.2 energy, a predator running 2 m costs 1.7, and a predator can run at full speed for 15 ticks
from full stamina. A locomotion may add a cost of its own for the move (slopes, mud), and the metabolism may also
charge a cost that grows with a trait (off in the reference; see
[Stats, costs and death rules](../extending/stats-and-death.md)).

## Busy states and digestion

While an animal is busy it doesn't decide and doesn't move; it pays the busy cost and recovers stamina (ANIM-30). A
module that makes an animal busy sets `BusyTicks` and gives a reason, shown in the inspector (ANIM-31). The act
phase counts down at the end of each busy tick. At 0 the animal has no action, and it decides at the start of the
next tick.

In the reference only <xref:EvoSim.Digestion> does it, on the predator. After a kill or a carcass portion the eater is
busy for `round(Digest Ticks × gain / Reference Gain)` ticks: **50** after a kill (+60), **25** after a portion (+30),
with the reason `digesting`. Grazing never makes an animal busy, and the prey has no digestion module.

## Age, maturity and generations

Every living animal ages by one tick per tick, in the death phase, babies born that tick included (ANIM-35). An
animal is an adult when its age reaches its `maturity` trait (**150** ticks, declared by the
<xref:EvoSim.MatingRule>, ANIM-36); an adult with at least 50 energy is ready to mate. Founders and newcomers are
generation 0; a baby is one generation more than the larger of its parents' generations. See
[Reproduction and population](reproduction-and-population.md).

## Death

The death phase (<xref:EvoSim.DeathPhase>) runs once per tick:

1. Animals killed this tick die, with the cause `killed` and the killer's id.
2. Every living animal ages by one tick.
3. Each <xref:EvoSim.DeathRule> of the species is asked about every living animal, in module order. The first rule
   that names a cause decides; later rules are not asked about that animal.
4. Each death is recorded once (ANIM-42), and the dead leave their species before the next tick's decisions
   (ANIM-43).

| Cause | Rule | When (reference) | `stats.csv` column |
|---|---|---|---|
| `starvation` | <xref:EvoSim.Starvation> | energy ≤ 0 | `<species>_deaths_starve` |
| `old_age` | <xref:EvoSim.OldAge> | age above **Max Age** (1 500 ticks) | `<species>_deaths_age` |
| `killed` | a strike (ANIM-41) | killed by a hunter | `<species>_deaths_pred` |
| `migrated` | the cap rule (mode Migrate), in the migration phase | the species is above its cap | `<species>_deaths_migrated` |

Modules may add causes of their own, such as thirst; each gets a `<species>_deaths_<cause>` column (ANIM-40).

## The main types

| Type | Role |
|---|---|
| <xref:EvoSim.Animal> | One animal: plain data, read and written by every module |
| <xref:EvoSim.SpeciesBuilder> | Collects the stats and traits a species' modules declare |
| <xref:EvoSim.StatId>, <xref:EvoSim.TraitId> | The ids to read and write a stat or a trait |
| <xref:EvoSim.Energy>, <xref:EvoSim.Stamina> | The reference stats |
| <xref:EvoSim.Metabolism> | What a tick costs |
| <xref:EvoSim.Digestion> | Makes an eater busy after a kill or a portion |
| <xref:EvoSim.DeathRule> | A cause of death; <xref:EvoSim.Starvation> and <xref:EvoSim.OldAge> in the reference |

## Settings

In the reference scenes these modules sit on the species' `Stats` and `Life` children.

| Module | Field | Reference |
|---|---|---|
| Energy | Max, Start | 100, 60 |
| Stamina | Max, Regen Per Tick, Regen Energy Cost | prey 60, predators 30; 2; 0.3 |
| Metabolism | Base Cost, Move Cost, Rest Cost | 0.7, 0.5 per meter, 0.2 |
| Metabolism | Cost Trait, Cost Per Unit | empty (none), 0.005 |
| Digestion (predators) | Digest Ticks, Reference Gain | 50, 60 |
| Old Age | Max Age | 1 500 ticks |
| Mating Rule | Mate Energy, Maturity | 50, 150 ticks |
| Litter | Child Energy | 40 |

## In the editor

- **Animal Inspector** (*Window ▸ EvoSim ▸ Animal Inspector*): in Play mode, click an animal in the Scene view to read
  its stats, traits, genome, current action, busy reason, parents and offspring.
- **Species inspector**: select a species and press **Show the computed tables** to list every declared stat and
  trait, with its default, its range and the module that declared it.
- **Ecology Monitor** (*Window ▸ EvoSim ▸ Ecology Monitor*): population and deaths by cause over time.

## Go further

- [Add a stat, change costs, add a death rule](../extending/stats-and-death.md).
- [Actions and movement](actions-and-movement.md): where moving and resting come from.
- [Reproduction and population](reproduction-and-population.md): litters, caps and newcomers.
- Specification: [05 — Animals, stats and life cycle](../spec/05-animals-stats-and-life-cycle.md).
