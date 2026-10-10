# 31 — Scenarios

A scenario is a world plus a set of overrides (CFG-02), seeds, a length, a brain
and a list of expectations. Scenarios serve three purposes: regression tests
(T2–T4, [30](30-tests.md)), lab activities, and evidence that the system bends in
the directions the owner asked for (any number of species, hide versus flee,
number genes, asynchronous mutation, runtime species).

Expectations are of three kinds:

- **Invariants**: always checked (30 §5); any violation fails.
- **Exact**: reproducibility facts (same hash twice, zero model calls on replay).
- **Directional**: "A has fewer kills than B in at least 4 of 5 seeds". Never a
  precise population number, which would break at every retuning.

## 1. Scenario asset

```yaml
# Scenarios/S06_HideVsFlee.asset (shown as YAML)
name: S06 Hide versus flee
scene: Scenes/Lab1_Full
variants:
  hide-and-flee:  {}                                                       # the reference prey
  flee-only:      { remove: [prey/Actions/hide, prey/Senses/Cover] }
  prototype:      { remove: [prey/Actions/hide, prey/Senses/Cover],
                    set: { prey/Actions/flee/FleeAction/fleeIntoCover: true } }
seeds: [1234, 7, 42, 99, 2026]
ticks: 5000
brain: JEV                                   # answers cached locally; a scripted policy in T3
expect:
  - invariants
  - deterministic                              # each variant and seed twice → same hash
  - "kills(hide-and-flee) < kills(flee-only) in >= 4/5 seeds"
  - "share(prey, hide) > 0.02 in hide-and-flee"
tier: T4
```

The scenario runner window ([21](21-editor-tooling.md)) and the batch-mode entry
point `EvoSim.Batch.RunScenario` both read these assets.

## 2. The scenario matrix

| Id | Scenario | Varies | Brain | Tier |
|---|---|---|---|---|
| S00 | Empty world | no species | — | T2 |
| S01 | Lone grazer | 1 species, food only | scripted | T2 |
| S02 | Lab 1 reference (full) | the reference ecology | JEV | T4 |
| S03 | Lab 1 small | 48 × 48 | random (T2), JEV (T4) | T2 / T4 |
| S04 | Null brain | random brain against S02 | random | T4 |
| S05 | Controls | C2, C3, C4, C7 | random | T2 |
| S06 | Hide versus flee | actions, senses, genes | scripted, then JEV | T3 / T4 |
| S07 | No cover | environment module removed | JEV | T4 |
| S08 | Cannibals | diet includes own species | scripted | T2 |
| S09 | Food chain of three | 3 species, multiple threats | scripted, then JEV | T3 / T4 |
| S10 | Six-species web | 6 species, a pure scavenger | scripted, then JEV | T3 / T4 |
| S11 | A species added during a run | `World.AddSpecies` at a given tick | random | T2 |
| S12 | Number genes | stamina gene with and without a cost | JEV | T4 |
| S13 | Mutation operators | LLM deck, intensity ladder, none | fake mutator, then real | T2 / T4 |
| S14 | Eggs and slow mutators | incubation × mutator delay | random + fake mutator | T2 |
| S15 | Observation wording | V1 / V2 | LLM | T4 |
| S16 | Water and thirst | new stat, sense, action, gene | scripted, then JEV | T3 / T4 |
| S17 | Terrain and locomotion | ground module, locomotion subclasses | scripted | T3 |
| S18 | Moving carcasses | an entity that moves | scripted | T2 |
| S19 | Act orders | species in turn, all mixed, simultaneous | random (T3), JEV (T4) | T3 / T4 |
| S20 | Cap rules | migrate, block | JEV | T4 |
| S21 | Brain failure | dead host, strict and not | fake transport | T2 |
| S22 | Resume by replay | stop and rerun | fakes, then JEV | T2 / T4 |
| S23 | Scale | 2 000 animals | random | T3 |
| S24 | Camera sense | attachments | fake image brain | T2 |
| S25 | Decision timing | period 1 / 4 / 8, staggered | random | T2 |
| S26 | Names in genes | a species renamed | LLM | T4 |
| S27 | Lab 1 with an LLM | JEV and Ollama, short run | JEV, gemma | T4 |
| S28 | Egg eaters | eggs made edible by components | scripted, then JEV | T2 / T4 |

## 3. Scenarios in detail

### S00 Empty world
A world with ground, environment and phases, no species. **Expect**: 1 000 ticks
without error; food regrows to saturation; no events but none required.

### S01 Lone grazer
One species (eat, rest, mate; energy, stamina; food layer), no hunters, with a
scripted policy (eat when food is in sight, mate when a ready partner is,
otherwise rest). **Expect**: population reaches the cap within 3 000 ticks in 5/5 seeds; deaths
only by starvation, old age or migration; food count settles (its mean over
ticks 2 000–3 000 varies by less than 20 % between halves).

### S02 Lab 1 reference
The reference ecology of [04 §5](04-species-and-food-web.md#5-reference-the-lab-1-ecology):
`prey` (eat, flee, hide, follow, rest, mate; a cover sense; *flee into cover*
off), `predator` (hunt, follow, rest, mate), the prototype's parameters, hungry
cover, carcasses, litters, migration, no incubation, 192 × 192, JEV for both
species. **Expect** (T4): R-02 ranges; no newcomer in 10 000 ticks (R-03); prey
deaths split between killed, starved and migrated, none of them above 70 % of
the total. Its random-brain twin gives the R-01 pinned hashes (T3).

### S03 Lab 1 small
S02 at 48 × 48 with the small populations. **Expect** with the random brain (T2):
invariants and reproducibility, the smoke test of the whole system; with JEV
(T4): as S02, faster.

### S04 Null brain
S02 with the random brain for both species. **Expect**: predators need newcomers
(their floor) in 5/5 seeds, unlike S02; prey populations not lower than in S02.
Shows that behaviour, not the world alone, keeps hunters alive.

### S05 Controls
S03 with the random brain, four times: C2 (no mutation: the number of alleles stays at the founders'),
C3 (shuffled: queries carry other animals' genes), C4 (random founders: founders
use control sentences), C7 (asexual: one parent per birth). **Expect**: each
control's defining fact, and every run reproducible.

### S06 Hide versus flee
The owner's competition between running and hiding, three prey variants (see
the asset above). **Expect** with a scripted policy that hides when a threat is
close and cover is near (T3, the mechanics): fewer kills with hide than
flee-only in ≥ 4/5 seeds. With JEV (T4, the behaviour): hide chosen mostly when
a threat is close and cover is in sight; the hide gene's contrast pair moves P(hide) the right
way (G1 on the new locus), and over a long run the hide and flee genes'
frequencies are reported by the gene pool tools.

### S07 No cover
S02 without the cover module, the hide action and the cover sense. **Expect**:
validation reports no error (all three removed together); more prey killed than in S02 in
≥ 4/5 seeds.

### S08 Cannibals
One species that grazes and also strikes its own kin (`kill chance` 0.1).
**Expect**: an animal never targets itself; killed animals have killers of the
same species; the flee action's threat set contains the species itself.

### S09 Food chain of three
Rabbit (grazes), fox (strikes rabbits), wolf (strikes foxes and rabbits).
**Expect**: derived threats Rabbit {Fox, Wolf}, Fox {Wolf}, Wolf {}; the rabbit's
threat sense reports the nearer of a fox and a wolf; prompts and situation texts
name the species; 5 000 ticks without error. Survival of all three is reported,
not required.

### S10 Six-species web
Two grazers, two mid hunters, one apex hunter, one pure scavenger (scavenge only).
**Expect**: one batch per brain per tick; the scavenger lives only on carcasses
(its meals are all portions); the food web window shows every edge; tick time
within twice S02's.

### S11 A species added during a run
S03 with a test script that calls `World.AddSpecies` at tick 1 000, moving the
prey of the west half into a new species cloned from the prey (deciding *when*
to split, speciation, is left for later). **Expect**: a `species_created` event; the new species has a new
id, its own cap, floor and streams; it is hunted by the predators (inherited
relation); events of other species before tick 1 000 are unchanged.

### S12 Number genes
S03 with a stamina gene (founders 45 / 60 / 75, Gaussian σ 5, range 20–120) in two
variants: no cost, and a base cost that grows with maximum stamina. **Expect**:
without the cost, mean `stamina.max` rises toward the upper bound in ≥ 4/5 seeds;
with the cost, it ends below the no-cost value in ≥ 4/5 seeds. Teaches why number
genes need trade-offs.

### S13 Mutation operators
S03 with three operators for text genes: the LLM deck (a `FakeMutator` that makes
a scripted small edit in T2, the real mutator in T4), the intensity ladder, none.
**Expect** (T2): mutant counts follow the rate; the ladder only changes intensity
words; "none" adds no allele. **Expect** (T4): rejection rates by reason are
recorded; on 100 mutated founder sentences, the judge prompt
([32 §2](32-integrity-prompts-and-ci.md#2-brain-and-mutator-integrity-checks))
finds at least 80 % still usable rules after one mutation.

### S14 Eggs and slow mutators
S03 with incubation 0 (the reference), 3 and 10, and a `FakeMutator` answering
after 0, 2 or 5 `Advance` calls. **Expect**: for one incubation value, the same hash whatever the
delay; with incubation ≥ the delay, the world never waits on the mutator (wait
counter 0); with incubation 0, it waits at each birth tick.

### S15 Observation wording
The gate's observation set with V1 and V2 (distances in meters). **Expect**
(T4): G1 holds in both; differences in action shares between V1 and V2
are reported (Lab activity F).

### S16 Water and thirst
Terrain preview with lakes; the prey get the thirst stat, the water sense, the
drink action and its gene (the recipes of [22](22-extending-recipes.md)).
**Expect**: deaths by thirst occur when drink is removed and almost vanish
(fewer than 10 % of deaths) with it, with a scripted policy (T3); with JEV (T4),
drink is chosen mostly when thirst is high.

### S17 Terrain and locomotion
Terrain preview (15 % water, 10 % mountains) with the kinematic locomotion for
both species, then a slope locomotion ([22 §14](22-extending-recipes.md#14-a-locomotion-slopes-cost-more))
for the predator only, then (once written) a NavMesh locomotion. **Expect**:
invariants (no animal on water or mountain); with slopes, the predator never
climbs past its limit and pays more energy on hills; with the NavMesh, fewer
hunts end in "stuck" sidesteps; all deterministic on one machine.

### S18 Moving carcasses
Hunters get a "drag" action that moves a carcass 1 m per tick toward cover.
**Expect**: carcass positions change only in the act and environment phases;
scavengers' carcass sense follows the moved carcass; the view follows.

### S19 Act orders
S02 with each act order. **Expect**: each reproducible; with "species in turn"
the R-02 ranges hold; the other two are reported side by side (kills, food
contention), not judged.

### S20 Cap rules
S02 with migrate and block. **Expect**: with block, populations sit at the cap
and generations per 1 000 ticks are fewer than with migrate in ≥ 4/5 seeds (the
prototype's history).

### S21 Brain failure
An HTTP brain with a fake transport that fails after tick 100. **Expect**: strict
→ the run stops cleanly at tick 100 with every file written and `summary.json`
saying why; non-strict → it goes on with uniform answers, failures counted, none
cached.

### S22 Resume by replay
S03 stopped at tick 300 by a stop file, then run again. **Expect**: the first 300
ticks replay with 0 model calls (fakes in T2; JEV in T4) and the same events.

### S23 Scale
S02 with caps × 6 (about 2 000 animals). **Expect**: tick time under 12 ms with
the random brain (the brain's own time excluded); memo hit rate reported; no allocation per tick in the act phase.

### S24 Camera sense
A species with a camera sense (64 × 64 image) and a fake brain that accepts
images. **Expect**: attachments reach the brain; those queries are never memoised;
with a brain that doesn't accept images, validation fails (V-51).

### S25 Decision timing
S03 with decision period 1, 4, 8 and with staggering. **Expect**: brain queries per
tick follow the period; with staggering, the largest batch is below half the
unstaggered one.

### S26 Names in genes
The predator species named "wolf" in one run and "shadow" in another, its
threat-sense label following the name, with the prey gene "Run from any wolf you
see.". **Expect** (T4, LLM): P(flee) when a "shadow" is close is lower than when
a "wolf" is; the editor flags the gene
(V-23). Shows that names are part of what the brain reads.

### S27 Lab 1 with an LLM
S02 for 500 ticks with JEV for both species (and with gemma4:12b on S03).
**Expect** (T4): no failed call; queries per tick, memo hit rate and time per
tick recorded and compared with the prototype's (JEV ≈ 2.8 s per tick in the
full world); no species needs newcomers.

### S28 Egg eaters
S03 with incubation 20, an `Edible` on the egg kind and a third species whose
diet is `egg:prey` ([22 §15](22-extending-recipes.md#15-food-make-eggs-edible)).
**Expect** (T2, scripted egg eaters): eaten eggs never hatch and are recorded as
lost; without the `Edible`, V-24 fires and no egg is eaten. With JEV (T4): the
prey's births drop compared with S03 at incubation 20 without egg eaters.

