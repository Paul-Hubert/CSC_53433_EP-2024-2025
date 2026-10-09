# 03 — Environment

Everything in the world that is not an animal: what grows on the ground
(resource layers), where animals can hide (cover), and objects with a position
(entities such as carcasses and eggs). Each is a replaceable module; a world may
have none, one or several of each kind.

## 1. Resource layers

A resource layer holds edible items spread over the ground, for example grass.
The diet of a species says which layers it grazes and how much energy an item
gives ([04](04-species-and-food-web.md)).

- **ENV-01 (MUST)** A resource layer answers: the nearest item within a radius
  of a point (position and distance), whether an item is within reach of a
  point, and how many items it holds. It can remove (consume) one item.
- **ENV-02 (MUST)** Consuming an item removes it at once: two animals can never
  eat the same item. When two animals reach for one item in the same tick, the
  act order decides who gets it ([07 §6](07-actions-and-locomotion.md#6-act-order)).
- **ENV-03 (MUST)** A layer that changes over time (regrowth) changes only in its
  own phase, once per tick, with its own random stream.
- **ENV-04 (SHOULD)** A layer may forbid items in some places (water, cover,
  steep ground) and may vary its growth rate by place (near water, by biome).

**Reference: the food layer of Lab 1.**

| Rule | Reference value |
|---|---|
| Storage | one item at most per 1 m × 1 m cell, at the cell centre |
| Start | each walkable cell outside cover holds an item with probability `initialFraction` = 0.1 |
| Regrowth | every tick, each empty walkable cell outside cover grows an item with probability `regrowP` = 0.0015 (≈ 670 ticks to regrow an eaten cell), the same everywhere: food appears uniformly at random |
| Near water (terrain only) | cells within 3 m of water regrow 2 × faster |
| In cover | no item at the start and no regrowth ("hungry cover"): hiding and eating compete |
| Item energy | `eatGain` = 25 for the species that graze it (prey) |
| Reach | an item is within reach when the animal stands in its cell (Python: on the same cell) |

The food layer can also come from the terrain's grass detail layer (foliage
lab): the detail map is the cell grid, eating removes the grass.

## 2. Cover

- **ENV-10 (MUST)** A cover module answers whether a point is in cover and where
  the nearest cover is within a radius.
- **ENV-11 (MUST)** An animal in cover is hidden from the species that hunt it:
  their senses don't report it and their actions can't target it or strike it.
  Its kin and the species it hunts still see it. (Who hides from whom is a
  setting of the cover module; the reference hides prey from their threats.)
- **ENV-12 (MUST)** Cover is optional: a world without a cover module behaves as
  if no point were in cover.
- **ENV-13 (SHOULD)** Using cover is a behaviour the brain can choose, with its
  own action ("hide"), its own sense ("cover: 2-4 m away") and its own gene, in
  competition with fleeing ([07](07-actions-and-locomotion.md)).

**Reference (Python).** 20 % of walkable ground is cover: the top 20 % of a
two-octave value-noise map, so it comes in clustered patches (thickets). In the
prototype cover is not an action, not a sense and not a gene: a *fleeing* prey
animal already in cover stays put, and one with cover within 6 m runs into it
instead of running away. The Unity reference splits this into a separate hide
action (owner decision); the old coupling stays available as an option of the
flee action ([07 §4](07-actions-and-locomotion.md#4-reference-actions)).

## 3. Entities

An entity is a world object with a position that is not an animal: a carcass,
an egg, a water hole, a fruit that falls.

- **ENV-20 (MUST)** An entity has an id, a kind, a position and a remaining
  lifetime (or none). Entities of a kind are found through the same spatial
  queries as animals (nearest within a radius, with a filter).
- **ENV-21 (MAY)** An entity's position may change (a carcass dragged away, a
  fruit rolling). It changes only in a phase, never from the visuals.
- **ENV-22 (MUST)** An entity whose lifetime runs out, or which is used up, is
  removed in the environment phase of that tick.

**Reference: carcasses.**

| Rule | Reference value |
|---|---|
| Created by | a successful strike (a kill), at the killed animal's position |
| Portions | `portions` = 2: one each for up to 2 animals other than the killer (0 = no carcass) |
| Who may eat | a species whose diet lists carcasses of the killed species; not the killer; each eater at most one portion |
| Energy | `carcassGain` = 30 per portion |
| Lifetime | `rotTicks` = 100 ticks, eaten or not |
| After eating | the eater is busy (digesting) for a time proportional to the energy: 25 ticks for 30 energy when a kill (60) gives 50 ([05](05-animals-stats-and-life-cycle.md)) |
| Found by | the hunter's sense "carcass" and its hunt action (the nearer of prey and carcass; a carcass wins a tie, being a sure meal) |

Eggs are entities too ([11](11-reproduction-and-population.md)).

## 4. Options tried and rejected

Kept in the prototype, off, and recorded so that students know they were tried:

| Option | Effect | Outcome |
|---|---|---|
| Food in cover (`cover_food: true`) | cover grows food like other ground | safe feeding places made crashes worse; replaced by hungry cover |
| Ridges | mountain lines with gaps split the world | hunters could not reach prey across them and died out |

## 5. Extending the environment

New layers and entities are the natural content of the terrain and foliage
labs: water (drinking, with a thirst stat), fruit trees (food that depends on
place), seasons (a regrowth rate that changes with time), moving carcasses,
burrows. Each comes with its sense and, if animals use it on purpose, its action
and gene ([22](22-extending-recipes.md)).
