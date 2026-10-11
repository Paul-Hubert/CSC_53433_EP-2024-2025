---
uid: guide-food-web
---

# The food web

Who eats whom is data, not code (04). Two components decide it together: the eaten thing carries an
<xref:EvoSim.Edible>, which says how much energy it gives, and the eater's <xref:EvoSim.Diet> lists what it eats. A
meal happens only if both sides allow it (SPEC-13). From the diets the World derives each species' **prey** and
**threats**, which senses and actions use to find whom to hunt and whom to flee. Any number of species can share a
world, and adding one changes no code.

## How it works

1. **Edible.** A resource layer, a species or an entity kind (eggs) becomes edible when its GameObject carries an
   <xref:EvoSim.Edible> component. Without one, nothing can eat it, whatever the diets say (SPEC-10).
2. **Diet.** Each species that eats has one <xref:EvoSim.Diet>. Its entries name what it eats, each with an energy
   scale (SPEC-15). A species with no diet eats nothing.
3. **Resolution.** When the world initializes, each diet resolves its entries by name into
   <xref:EvoSim.EdibleTarget>s (ARCH-08). Then the World derives the food web: a species whose diet strikes another is
   that species' threat, and the other is its prey (SPEC-12).
4. **Use.** Senses and actions look for "threats", "prey" or "kin" through an <xref:EvoSim.AnimalSet>. When an
   interaction applies, the resolver asks the eater's diet again whether this target may be eaten.

## Diet entries

| Entry | Eats | How | Energy of one meal |
|---|---|---|---|
| a layer's name, e.g. `Grass` | the layer's food items | grazed: the item is consumed | the layer's Edible **Energy** (25 for grass) |
| a species' id or display name, e.g. `prey` | that species' living animals | struck: killed with the hunter's kill chance | the species' Edible **Energy** (60 for the prey) |
| `carcass:<species>`, e.g. `carcass:prey` | carcasses that species leaves | scavenged: one portion at a time | the species' Edible **Carcass Energy** (30) |
| `egg:<species>` | that species' eggs, while they incubate | eaten whole | the egg system's Edible **Energy** |

Each entry has an **Energy Scale** (default 1) that multiplies the energy of that food for this eater. A diet that
strikes a species also declares the hunter's `kill.chance` trait; its default is the diet's **Kill Chance** (0.5).
For the steps to make something edible or change a diet, see
[Food, cover, entities and ground](../extending/environment.md#who-eats-what).

## Where the Edible goes

| What is eaten | The Edible sits on |
|---|---|
| a layer's items | the layer's GameObject (`Environment/Grass` in the reference scenes) |
| a species' animals | the species' own GameObject, next to its <xref:EvoSim.Species> component |
| a species' carcasses | the same Edible as its animals: **Carcass Portions** above 0 make it leave carcasses; the world also needs a <xref:EvoSim.CarcassSystem> |
| a species' eggs | the <xref:EvoSim.EggSystem>'s GameObject (`Environment/Eggs`) |

A disabled Edible counts as absent when the world starts (ARCH-06).

## What a meal gives

The energy of a meal is the Edible's energy times the entry's **Energy Scale**. The diet's
[EnergyFrom](xref:EvoSim.Diet.EnergyFrom(EvoSim.Animal,EvoSim.EdibleTarget,System.Single)) can then adjust it for
the eater (the reference returns it as it is), and <xref:EvoSim.Energy> adds it, never above the maximum (ANIM-22).
Every meal counts in the animal's `Meals` and its species' meal counter. After a kill or a carcass portion, a species
with a <xref:EvoSim.Digestion> module is busy digesting; grazing and eggs never make an animal busy.

## Threats, prey and kin

The World builds the relations from the diets, in species order, when it initializes and again whenever a species is
added during a run. They are never configured twice (SPEC-12).

| Set | Means | Used by (reference) |
|---|---|---|
| Threats | species whose diet strikes this one | the prey's "Predator" sense and its flee action |
| Prey | species this one's diet strikes | the predator's "Prey" sense and its hunt action |
| Kin | this species | the kin senses ("Animal", "Other predator"), follow and mate |
| Listed | species named on the module, by id or display name | a flee from wolves only, for example |

<xref:EvoSim.World.ThreatsOf(EvoSim.Species)> and <xref:EvoSim.World.PreyOf(EvoSim.Species)> return these lists.
An animal standing in cover is hidden from its threats: their senses don't report it, their actions can't target it,
and they never strike it (ENV-11). Its kin and its own prey still see it.

## Carcasses

When a strike kills an animal whose Edible has **Carcass Portions** above 0, a carcass appears where it died
(reference: 2 portions of 30 energy). The killer has already eaten: it may not eat from its own kill. Any other
animal whose diet lists `carcass:<species>` may eat one portion each. The carcass is gone when its portions are
eaten, or when it rots after **Carcass Ticks** (100); rotting happens in the environment phase.

The reference hunt action runs to the nearer of the nearest prey and the nearest carcass it may eat, and a carcass
wins a tie, being a sure meal. A diet that eats carcasses adds the carcass system's line to its species' prompt:
*"A kill leaves a carcass that up to 2 other predators can eat from."*

## Cannibals

A species may list itself in its diet (SPEC-11). It is then among its own prey and its own threats: its hunt action
chases its own kind, and its flee action runs from it, like from any threat. An animal never targets itself.

## Species added during a run

A species cloned from another during a run (SPEC-30) inherits its relations in both directions (SPEC-31): it eats what
its parent species eats, and the hunters of the parent hunt it too, through its own Edible. The World rebuilds the
food web when the species is added (<xref:EvoSim.World.RebuildFoodWeb>).

## The reference food web

| | `prey` | `predator` |
|---|---|---|
| Edible | yes: 60 per kill, carcass of 2 portions × 30, rots in 100 ticks | no |
| Diet | `Grass` (25 per item) | `prey` (kill chance 0.5), `carcass:prey` |
| Threats (derived) | predator | none |
| Prey (derived) | none | prey |
| Eggs | not edible | not edible |

## The main types

| Type | Role |
|---|---|
| <xref:EvoSim.Edible> | Makes a layer's items, a species' animals or an entity kind edible, with their energy |
| <xref:EvoSim.Diet> | What a species eats; resolves its entries and answers "may I eat this?" |
| <xref:EvoSim.Diet.Entry> | One line of a diet: a target name and an energy scale |
| <xref:EvoSim.EdibleTarget> | One resolved entry: kind, target, its Edible, the scale |
| <xref:EvoSim.AnimalSet> | Threats, prey, kin or listed species |
| <xref:EvoSim.CarcassSystem>, <xref:EvoSim.Carcass> | Carcasses: portions, eaters, rotting |

## Settings

| Component | Field | Reference |
|---|---|---|
| Edible | Energy | grass 25 per item, prey 60 per kill |
| Edible | Carcass Portions, Carcass Energy, Carcass Ticks | prey 2, 30, 100 ticks |
| Diet | Eats (Target, Energy Scale) | prey: Grass; predator: prey, carcass:prey; scale 1 |
| Diet | Kill Chance | 0.5 |
| Flee action, Nearest Animal sense | From, Targets (and Listed) | Threats |

## In the editor

- **Food Web window** (*Window ▸ EvoSim ▸ Food Web*): species and layers as a graph. Green edges graze, red strike,
  brown scavenge carcasses, yellow eat eggs. In Edit mode press **Build the food web**, which validates the world to
  resolve the diets. Below the graph: the derived threats of each species and the food-web messages.
- **Species inspector**: **Show the computed tables** lists what the species eats with the energy of each, its own
  Edible, and its derived threats.
- **Validation** reports a diet entry that names nothing (V-20), a flee, hunt or sense with nobody to look for (V-21), a
  species with no food (V-22), a diet target with no Edible (V-24, with a fix), and an Edible no diet lists (V-25).

## Go further

- [Food, cover, entities and ground](../extending/environment.md#who-eats-what): make something edible, change a diet.
- [Actions and movement](actions-and-movement.md): how grazing, strikes and scavenging happen on arrival.
- [The environment](environment.md): food layers, cover, carcasses and eggs.
- Specification: [04 — Species and the food web](../spec/04-species-and-food-web.md).
