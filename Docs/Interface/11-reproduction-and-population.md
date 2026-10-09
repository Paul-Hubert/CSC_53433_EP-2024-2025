# 11 — Reproduction and population

## 1. Mating

- **REPRO-01 (MUST)** An animal is **ready to mate** when it is an adult
  (ANIM-36) and its energy is at least `mateEnergy` (reference 50). Readiness is
  what kin senses report (06 §4).
- **REPRO-02 (MUST)** Sexual reproduction (reference): in the breeding phase, an
  animal whose current action is mate, that is ready and hasn't bred in this
  decision period, breeds with a ready animal of its species within reach
  (1 m) that hasn't bred either. **One partner's choice is enough**: the
  partner's own action doesn't matter.
- **REPRO-03 (MUST)** Each animal breeds at most once per decision period.
  Seekers are taken in a deterministic order (a named stream or by id), and each
  breeds with the nearest eligible partner (ties by id).
- **REPRO-04 (MUST)** Asexual reproduction (a control, the previous lab's regime):
  a ready animal that chose mate reproduces alone and pays the whole cost.
- **REPRO-05 (MUST)** Killed animals don't breed (ANIM-41).

## 2. Litters

- **REPRO-10 (MUST)** A mating conceives a litter of k babies, k drawn uniformly
  from [`litterMin`, `litterMax`] (reference [2, 4]) with the litter stream. The
  litter is then cut so that no parent pays more energy than it has:
  with `share = childEnergy / number of parents`,
  `affordable = min over parents of floor(energy / share)`, and
  `k = max(litterMin, min(k, affordable))`. The litter never goes below
  `litterMin`, so a parent can be left with zero or less energy (and starve at
  the death check). With the `block` cap rule (POP-01) the litter is also cut to
  the places left under the cap, possibly to zero.
- **REPRO-11 (MUST)** Each parent pays `share × k` energy, its offspring counter
  grows by k, and it is marked as having bred this period.
- **REPRO-12 (MUST)** Each baby gets its own crossover (GENE-32) and its own
  mutations (MUT-01), starts with `childEnergy` energy (reference 40), full
  stamina, age 0, a random heading, generation = the larger parent generation + 1,
  and appears at the first parent's position.
- **REPRO-13 (MUST)** A baby has no action until its first decision, at the start
  of the next tick (DEC-01).

`litterMin = litterMax = 1` gives the prototype's rule before 2026-10-09.
Reference consequence: a parent at 50 energy pays for 2 babies (40) and keeps 10;
a parent at 100 can pay for 4.

## 3. Eggs and incubation

- **REPRO-20 (MUST)** A conception produces one egg per baby, an entity at the
  first parent's position, holding: parents, generation, the genome after
  crossover, the pending mutations, the conception tick and the hatch tick
  (conception + `incubation` ticks).
- **REPRO-21 (MUST)** `incubation` = 0 means the babies hatch in the same tick,
  as in the prototype. It is the reference value (owner decision); a few ticks
  MAY be set to hide the mutator's latency (MUT-33).
- **REPRO-22 (MUST)** Eggs are not animals: they don't sense, decide, move, eat,
  breed or count toward caps. They hatch in the hatch phase, in conception order.
- **REPRO-23 (MAY)** Eggs can be targets of a diet (egg eaters) or be destroyed
  by an environment rule. A destroyed egg never hatches and is recorded. In the
  reference, eggs can't be eaten.
- **REPRO-24 (MUST)** Hatching waits for the egg's genome to be complete
  (MUT-32), creates the animal as in REPRO-12, and records a birth event with
  the litter size and the mutations.

## 4. Population limits

- **POP-01 (MUST)** Each species has a cap and a cap rule:
  - **migrate** (reference): births go on at the cap. In the migration phase, if
    the species is above its cap, animals chosen uniformly at random leave the
    world (death cause `migrated`) until it is back at the cap. Animals born or
    hatched this tick are spared, unless too few older animals remain.
  - **block**: no conception while the species is at or above its cap; litters
    are cut to the places left.
- **POP-02 (MUST)** Migration chooses with its own stream and ignores genes,
  stats and position, so it favours no gene.
- **POP-03 (MUST)** Each species has a floor. In the floor phase, while a species
  has fewer animals than its floor, a **newcomer** is added: a founder genome, a
  random walkable position, the founder energy, generation 0, recorded as an
  immigrant.
- **POP-04 (MUST)** At tick 0 each species gets `initialPopulation` founders:
  founder genomes at random walkable positions, recorded as founders (not
  newcomers).
- **POP-05 (SHOULD)** A world where a species needs newcomers is reported
  (count in the summary): newcomers bring founder genes and restart that species'
  evolution. The owner's aim is worlds that never need them.

**Reference values.**

| | Prey (full / small) | Predators (full / small) |
|---|---|---|
| Initial population | 136 / 24 | 28 / 4 |
| Floor | 10 / 10 | 3 / 3 |
| Cap | 270 / 40 | 68 / 6 |
| Cap rule | migrate | migrate |

**Options tried and rejected** (kept as examples of population-rule modules, off):

| Option | Rule | Outcome |
|---|---|---|
| Egg bank | below the floor, a random egg laid in the last 2 000 ticks hatches (crossover and mutation at hatching, real parents) before any founder newcomer | lineages survived crashes, crashes went on; "not a valid idea" |
| Prey per predator | hunters breed only while there are at least q prey per hunter in the whole world | stopped crashes; rejected: no animal could know the head count |
| Prey seen | a hunter breeds only with at least m prey within its vision | weak: hunters gather where prey still are |
| One shared cap | one cap for all species together, migration over the total | crashes came back; one cap per species restored |
| Block instead of migrate | no births at the cap | populations sat at the cap and births only filled free places |

History in one line: blocked births and single babies led to boom-and-bust
crashes; litters of 2–4 and migration at the cap made both species last without
newcomers (2026-10-09).
