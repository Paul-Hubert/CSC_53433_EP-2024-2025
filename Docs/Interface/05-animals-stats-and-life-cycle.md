# 05 — Animals, stats and life cycle

## 1. The animal record

An animal is data. Every module reads and writes animals through this record.

- **ANIM-01 (MUST)** An animal has: an id; its species; a position and a heading;
  its stats and traits; its genome; its generation and parent ids; its birth
  tick and age; its current action (or none) and whether that action is
  searching; whether it bred in the current decision period; a busy counter;
  whether it was killed this tick and by whom; and counters of meals and
  offspring. Modules MAY add stats and traits, never untyped fields.
- **ANIM-02 (MUST)** Animal ids are unique within a run across all species,
  increase with each new animal, and are never reused.
- **ANIM-03 (MUST)** An animal belongs to one species for its whole life.

## 2. Stats

A stat is a per-animal number that changes during life.

- **ANIM-10 (MUST)** A stat has a name, an initial value at birth (which may
  differ for founders, newcomers and babies) and an optional minimum and maximum.
  It is declared by a module; any module may read it.
- **ANIM-11 (MUST)** Age is the only built-in stat: ticks since birth (or
  hatching). Energy, stamina and any other stat come from modules, so a species
  without a stamina module has no stamina.
- **ANIM-12 (SHOULD)** A stat can be sensed as **levels** with thresholds:
  low < `low` ≤ medium ≤ `high` < high ([06](06-senses-and-observations.md)).

## 3. Traits

A trait is a per-animal number fixed at birth: a parameter that varies between
individuals because genes set it.

- **ANIM-15 (MUST)** A trait has a name and a default (the module's setting).
  At birth every trait takes its default, then number genes expressed into it
  replace or scale it ([09](09-genes-alleles-and-genomes.md)).
- **ANIM-16 (MUST)** Any parameter that a gene may change is read from the
  animal's trait, never from the module's setting directly. Without a gene the
  trait equals the setting, so the behaviour is the same.
- **ANIM-17 (MUST)** Traits don't change during life unless their module
  declares them changeable.
- **ANIM-18 (SHOULD)** A trait has a valid range. A gene that would set it
  outside is clamped, and the editor warns about genes whose founder values fall
  outside.

Natural traits for number genes: maximum stamina, stamina recovery, walking and
running speed, vision, maturity age, litter size, kill chance.

## 4. Metabolism: energy and stamina (reference modules)

- **ANIM-20 (MUST)** Each living animal pays its metabolism once per tick, after
  it acted, based on what it did that tick: metres moved, the action chosen,
  whether it was busy.
- **ANIM-21 (MUST)** With a stamina module, an animal never moves more metres in
  a tick than its stamina; each metre moved costs one point. An animal without
  stamina for any movement stays where it is, whatever it chose.
- **ANIM-22 (MUST)** Gains never raise a stat above its maximum.
- **ANIM-23 (SHOULD)** "Did not move" means moved less than a small epsilon
  (1 mm), so rounding never counts as moving.

**Reference values** (the same for prey and predators unless stated):

| Situation | Energy | Stamina |
|---|---|---|
| Start: founder or newcomer | 60 | full |
| Start: baby | 40 (paid by the parents, [11](11-reproduction-and-population.md)) | full |
| Maximum | 100 | prey 60, predators 30 |
| Tick with movement | −0.7 (base) − 0.5 per metre | −1 per metre |
| Tick without movement, action rest | −0.2 | +2, up to the maximum |
| Tick without movement, any other action | −0.7 | +2, up to the maximum |
| Extra while stamina recovers (not full, no movement) | −0.3 | — |
| Busy tick (digesting) | −0.2, plus −0.3 while stamina recovers | +2 |
| Graze one food item (prey) | +25 | — |
| Kill (predators) | +60 | — |
| Carcass portion (predators) | +30 | — |

So walking one metre costs 1.2 energy, a predator running 2 m costs 1.7, and a
predator has 15 ticks of full-speed running from full stamina. Stamina is
sensed as levels: prey low < 20 ≤ medium ≤ 40 < high; predators low < 10 ≤
medium ≤ 20 < high. Energy levels: low < 30 ≤ medium ≤ 70 < high.

## 5. Busy states

- **ANIM-30 (MUST)** While an animal is busy (busy counter > 0) it doesn't decide
  and doesn't move. It pays the busy cost and recovers stamina. The counter
  drops by one per tick; when it reaches 0 the animal has no action and decides
  at the start of the next tick.
- **ANIM-31 (MUST)** A module that makes an animal busy says why (the reason is
  logged and shown in the inspector).

**Reference: digestion.** After a kill or a carcass portion the eater is busy for
`round(digestTicks × gain / killGain)` ticks: 50 after a kill (+60), 25 after a
portion (+30). Grazing never makes an animal busy.

## 6. Age and maturity

- **ANIM-35 (MUST)** Every living animal ages by one tick per tick, in the death
  phase, babies born that tick included.
- **ANIM-36 (MUST)** An animal is an adult when its age ≥ `maturity` (reference
  150 ticks, a trait).

## 7. Death

- **ANIM-40 (MUST)** Causes of death in the reference: `starvation` (energy ≤ 0
  at the death check), `old_age` (age > `maxAge`, reference 1 500), `killed`
  (struck by a hunter; the prototype logs it as `predator`, with the killer's
  id), `migrated` (removed above the cap, [11](11-reproduction-and-population.md)).
  Modules MAY add causes (thirst, disease, drowning) with their own names.
- **ANIM-41 (MUST)** A killed animal is marked at the moment of the strike. From
  then on in that tick it is not sensed, not targeted, doesn't act and doesn't
  breed. It is removed in the death phase.
- **ANIM-42 (MUST)** Every death is recorded once, with cause, age, generation,
  meals, offspring and, for a kill, the killer.
- **ANIM-43 (MUST)** Dead animals are gone before the next tick's decisions.

## 8. Counters

| Counter | Meaning |
|---|---|
| meals | items grazed, kills and carcass portions eaten (the prototype's `food`) |
| offspring | babies this animal parented (each baby of a litter counts) |
| generation | the larger parent generation + 1; founders and newcomers are 0 |
