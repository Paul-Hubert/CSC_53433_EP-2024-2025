# 07 — Actions and locomotion

The brain chooses an action; the action does the work, every tick until the
next decision. An action picks a target and produces an **intent** (a
direction, walk or run, how far at most), the species' **locomotion** turns the
intent into movement over the ground, and contacts trigger **interactions**
(graze, strike, scavenge).

## 1. Actions

- **ACT-01 (MUST)** An action has a name (unique in its species), a short
  description for the prompt, and usually one text gene ([09](09-genes-alleles-and-genomes.md)).
- **ACT-02 (MUST)** The chosen action runs every tick from the decision until the
  next decision (or until the animal becomes busy or dies). It looks for its
  target again each tick, so it follows a moving target.
- **ACT-03 (MUST)** Each tick an action produces at most one intent and at most
  one interaction. An intent is a direction (or none: stay), whether the animal
  walks or runs, and optionally the farthest it should go this tick (to stop at a
  target). Helpers build the usual intents: toward a point stopping at a
  distance, away from a point, wander.
- **ACT-04 (MUST)** **Nothing to act on → search.** When the action has no target
  in sight (no food, threat, kin, prey, carcass, ready partner, cover within the
  animal's vision), the animal wanders instead and the choice is counted as
  *searching* (the prototype's "invalid"), once per decision. Eating with no food
  in sight means searching for food.
- **ACT-05 (MUST)** Actions only target what the animal's senses could report:
  within its vision, not hidden from it, not killed, not itself.
- **ACT-06 (MUST)** An action never changes another animal's stats or position
  directly. Effects on others go through interactions, which check the diet and
  the reach.

## 2. Locomotion

Each species has one **locomotion** component. An action only says where it
wants to go; the locomotion decides what actually happens, and whatever it does
is the tick's result (owner decision). The reference locomotion is kinematic: no
rigid bodies, no physics.

- **MOVE-01 (MUST)** Each species has exactly one locomotion component. It
  receives each animal's intent and moves the animal. Whatever it does (exactly
  along the direction, sliding along an obstacle, or something else for a
  physics locomotion) is the result of the tick; no other module assumes the
  intent was followed exactly.
- **MOVE-02 (MUST)** Whatever the locomotion, an animal never ends a tick on
  non-walkable ground or outside the world (SPACE-03, SPACE-04).
- **MOVE-03 (MUST)** Speeds belong to the species: its walking and running
  speeds are traits of its locomotion (reference: prey walk 1 and run 1 m per
  tick, predators walk 1 and run 2). Movement is also limited by stamina
  (ANIM-21).
- **MOVE-04 (MUST)** The reference **kinematic** locomotion moves in a straight
  line along the intended direction, by the smallest of the speed, the stamina
  and the intent's maximum distance, so moving toward a target never overshoots
  its stop distance. Blocked by a border or an obstacle, it slides along it; if
  that makes no progress, the animal takes a random sidestep (its own random
  stream) rather than staying stuck.
- **MOVE-05 (MUST)** Wandering is a persistent random walk: the animal keeps its
  heading, and with probability `wanderTurnP` (reference 0.25) per tick turns by
  one of ±45° or ±90°; blocked, it picks a new random heading. It walks.
- **MOVE-06 (MUST)** The locomotion reports the meters actually moved;
  metabolism and stamina use that number. A locomotion MAY add costs of its own
  (slopes, mud, water).
- **MOVE-07 (SHOULD)** Locomotion is a base class meant to be inherited:
  terrain-aware movement with slope costs, NavMesh paths around obstacles,
  physics with rigid bodies, later a learned controller. Each species can have
  its own (owner decision: these come later; the reference is kinematic).

**Reference (prototype, grid).** Steps went to one of 8 neighbour cells, the one
that most reduces (or increases) the straight-line distance, with random
tie-breaks. The Unity reference replaces this with straight lines; the behaviour
to keep is the same: approach, stop next to it, run away, wander.

## 3. Interactions

- **ACT-10 (MUST)** An interaction happens when the animal is within **reach** of
  its target after moving, in the same tick. Reference reaches: graze = the item
  is in the animal's cell (within 0.5 m of the item); strike, scavenge, mate =
  within 1 m.
- **ACT-11 (MUST)** Interactions follow the diet (SPEC-13). Results:

  | Interaction | Effect |
  |---|---|
  | graze | the item is consumed; the animal gains the item's energy (edible component × diet scale) |
  | strike | with probability `killP` (a trait of the hunter; reference 0.5) the target is killed (ANIM-41); the hunter gains the target's energy (its species' edible component × diet scale), becomes busy (digestion) and, if the edible component says so, a carcass appears at the target's position |
  | scavenge | one portion of the carcass is eaten; the eater is recorded as having eaten from it; it gains the portion's energy and becomes busy |
  | hide | none: being in cover is a state of the position, not an interaction |

- **ACT-12 (MUST)** A hunter strikes at most once per tick. A struck animal that
  survives can be struck again by another hunter in the same tick.
- **ACT-13 (MUST)** Breeding is not an interaction of the mate action: the mate
  action only brings the animal to a partner; breeding happens in its own phase
  ([11](11-reproduction-and-population.md)).

## 4. Reference actions

| Action | Target | Intent | On contact | Searches when |
|---|---|---|---|---|
| **eat** | nearest item of a grazed layer | walk to it (stop 0); stay if already on it | graze (on the tick of arrival too) | no item within vision |
| **flee** | nearest threat | run away from it | — | no threat within vision |
| **follow** | nearest kin | walk toward it, stop at 1 m | — | no kin within vision |
| **rest** | — | stay | — | never |
| **mate** | nearest kin that is ready to mate | walk toward it, stop at 1 m | (breeding phase) | no ready partner within vision |
| **hunt** | the nearer of: nearest huntable animal (not killed, not hidden) and nearest edible carcass; a carcass wins a tie | run toward it, stop at 1 m | strike an animal, or scavenge a carcass | neither within vision |
| **hide** (Unity) | nearest cover | walk (or run) into it, stop 0; stay once inside | — | no cover within vision |

Options recorded from the prototype:

- **Flee into cover** (prototype behaviour, an option of flee): a fleeing animal
  already in cover stays put; with cover within `coverSeek` (6 m) it runs into
  it instead of running away. Off in the Unity reference, where hide is its own
  action, sense and gene (owner decision); on, it reproduces the prototype for
  comparison (scenario S06).
- **Predator interference** (off, rejected): a strike succeeds with
  `killP / (1 + c × n)`, n = other hunters within r m of the target.

Why speeds and stamina matter (reference): a hunter runs 2 m per tick for 15
ticks, a fleeing prey 1 m per tick for 60. The hunter wins short chases, the prey
long ones; a prey animal gets caught when the hunter starts close, or when it
stops (to eat, rest, mate), runs out of stamina, runs toward the hunter or hits
a border. Decisions come only every 4 ticks, so a hunter can close in between
two decisions; a strike happens in the tick the hunter arrives, so fleeing has to
start before it is adjacent.

## 5. The brain does not move animals

- **ACT-20 (MUST)** Actions are ordinary code. The brain's output is only which
  action runs (CORE-06). An action reads the animal's genes only through its
  traits (number genes); it never interprets gene sentences, which is the
  brain's job.

## 6. Act order

Animals act one after another or together; the order changes who reaches food
first and whether a prey animal moves before a hunter strikes.

- **ACT-30 (MUST)** The act order is a setting of the world, one of:

  | Policy | Order | Notes |
  |---|---|---|
  | **Species in turn** (default, owner decision; the prototype's order) | species in the world's species order; within a species, animals one by one in a fresh random order each tick | prey move before predators strike |
  | **All mixed** | every animal of every species one by one, in one fresh random order each tick | owner's alternative: no species goes first |
  | **Simultaneous** | every animal computes its intent from the same state; all move; then interactions resolve in a fresh random order | easiest to batch; contested items go to a random winner (owner decision) |

- **ACT-31 (MUST)** In the sequential policies an animal acts on the state left
  by those before it (positions, eaten items, kills). In the simultaneous policy
  intents use the state at the start of the act phase.
- **ACT-32 (MUST)** The random order comes from a named stream, so the same seed
  gives the same order.
- **ACT-33 (SHOULD)** Reference numbers (populations, kills) are measured with
  "species in turn"; other policies need their own measurements.
