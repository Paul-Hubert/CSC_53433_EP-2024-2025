# 40 — Open questions

The questions of the first draft were answered by the owner on 2026-10-09 (the
README's second-round table lists every decision). This page keeps what those
answers settled, where it landed in the documents, and what is still open or
deferred.

## 1. Settled in the second round

| Question | Decision | Now in |
|---|---|---|
| Reference values in continuous space | the prototype's values unchanged; Euclidean distances, straight-line movement; the first accepted Unity runs become the new reference numbers | README, 02, 07, 14, 30 R-02 |
| Speed | set per species, on its locomotion component | 07 MOVE-03, 20 §3.7 |
| Unit word in texts | "meters" | 06 SENSE-12/13, 08 §7 |
| Prompts | the answer instruction is hard-coded in each brain; header, action lines and rule lines are inspector fields | 08 PROMPT-07 |
| Observation wording | in each sense's code; labels and thresholds in the inspector | 06 SENSE-10 |
| Act order | species in turn by default; simultaneous: random winner | 07 ACT-30 |
| Breeding | its own phase, after the act phase | 12 TICK-06 |
| Eggs | no incubation by default; a few ticks may hide the mutator's latency | 10 MUT-33, 11 REPRO-21 |
| Number genes | none in the reference; traits keep the prototype's values | 05 §3, 09 |
| Species names | `prey` and `predator` | 04 §5, 20 §2 |
| Cannibals | flee from their own kind; "is a cannibal chasing me?" is a student sense | 04 SPEC-11, 22 §13 |
| Hide and flee | separate actions, genes and senses | 03, 04 §5, 07 §4, 08 §6 |
| G2 | stays a gate, tracked rather than blocking | 15 §3, 32 §2 |
| Neutral gene, founder pools, caps | configured on their components | 09 GENE-24, 10 MUT-22, 11 |
| Platform and teaching | Unity; reference modules written like student modules; students reimplement them first, then invent | 20 ARCH-11, 22 §0, 30 §4 |
| Real time | not by default | 02 SPACE-15 |
| Physics | kinematic by default; an action gives only a direction and the locomotion decides | 07 §2, 20 §3.7 |
| Floating-point determinism | per machine only | 12 RAND-12, 30 R-04, 32 |
| Answer cache | a local JSON file | 14, 20 §3.11 |
| Python golden fixtures | dropped | 30 (R-08 removed, snapshots pinned from Unity runs instead) |
| Unity version | the owner moves the project to 6000.3 before starting | README, 20 ARCH-10 |
| CI hardware | the owner's machine as a self-hosted runner | 32 §1, §4 |

## 2. Still open

| # | Question | Default until decided | Affects |
|---|---|---|---|
| 1 | **Default brain in class**: the keyword brain on every laptop, or JEV served from the owner's machine (or a lab server) to the whole class? | keyword brain by default; JEV opt-in | 08, 14 |
| 2 | **Founder pools** (H1), including the new draft hide sentences ("Hide when a predator is close.", "Stay in cover when danger is near.", "Hide only when you are tired.", "Leave cover to find food when hungry.") and the hide contrast pair | use them as they are | 09 |
| 3 | **The keyword brain's hide scores** (proposed in 08 §6): cover here 3.0 … none −2.0, −3.0 with no threat in sight | as proposed; tune with S06 | 08 |
| 4 | **Eggs in the world**: can they be eaten? Do they count toward the cap? With no incubation this doesn't arise yet. | not eaten; not counted; laid at the first parent's position | 11 |

## 3. Deferred by the owner ("later")

| Topic | What is ready for it now |
|---|---|
| Speciation (when to split a species) | `World.AddSpecies` and the relation inheritance rules (SPEC-30/31), scenario S11 |
| Names of species created during a run | species ids are never reused (SPEC-32) |
| Camera senses | the attachment rules (SENSE-40/41) and scenario S24 with a fake brain |
| A real-time mode | SPACE-15 keeps it outside the measurements |
| Physics, slopes, NavMesh, learned locomotion | the inheritable `Locomotion` base class (20 §3.7) and the slope recipe (22 §14) |
| Directions and angles in observations | senses are free to add tokens; the observation-space warning (V-50) |
| Scale beyond a few hundred animals | the seams of 20 §10 |
| A shared answer cache for a class | the cache key (DEC-33) doesn't depend on where the file lives |

## 4. Rules to revisit after the first Unity runs

| Rule | Why it may change |
|---|---|
| ACT-10 reaches (0.5 m graze, 1 m strike) | continuous movement at 2 m per tick may need larger reaches to avoid near-misses |
| REPRO-03 (nearest partner) | the prototype took the first ready partner in its list; nearest may change breeding rates |
| SENSE-05 threshold (100 000 situations) | the memo's real hit rate with 29 160 prey situations |
| R-02 ranges | measured afresh on the first accepted runs |
