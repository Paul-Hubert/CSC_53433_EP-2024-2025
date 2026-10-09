# 06 — Senses and observations

At each decision an animal senses its surroundings as a short tuple of discrete
values. Discrete values keep the prompt short and let identical situations be
answered once (the memo answers 50–80 % of decisions in the reference runs).

## 1. Senses and tokens

- **SENSE-01 (MUST)** A sense reads one animal and the world and returns one
  **token** from a finite list declared before the run (the list includes the
  token for "nothing", e.g. `none`).
- **SENSE-02 (MUST)** An animal's **observation** is the tuple of the tokens of
  its species' senses, in the species' sense order. Two observations are equal
  when their tuples are equal; nothing else (positions, ids) is part of it.
- **SENSE-03 (MUST)** A sense is a function of the world state at the decision
  and of the animal: the same state gives the same token. A sense that needs
  randomness uses its own named stream.
- **SENSE-04 (MUST)** Senses are read when an animal decides. All animals that
  decide in a tick sense the same state: the state at the start of the tick.
- **SENSE-05 (MUST)** The number of possible observations of a species (the
  product of its senses' token counts) is computed before the run and shown.
  The editor warns above a threshold (reference: 100 000), because a large space
  defeats the memo and the answer cache.
- **SENSE-06 (MUST)** Hidden things are never sensed: animals in cover from the
  species they hide from, animals killed this tick, entities that are used up.

## 2. Situation text

- **SENSE-10 (MUST)** Each sense writes its token as a short text fragment. The
  situation text is the fragments in sense order, joined by spaces. Writing is
  deterministic: the same observation always gives the same text.
- **SENSE-11 (MUST)** At least one style exists: the terse style **V1**
  ("Energy: low."). Senses MAY also write a first-person style **V2** ("I am
  hungry and weak."). The style is one setting for the whole world (or brain).
- **SENSE-12 (MUST)** A distance fragment states the range of its band in the
  world's unit, and "none" states the vision ("Food: 1-4 m away.", "Predator:
  none within 20 m."), so the brain knows how far things are and how far it sees.
- **SENSE-13 (SHOULD)** The unit word is a setting ("m", "metres", "cells"). With
  "cells" and integer bands the reference text matches the prototype's text
  exactly, which lets cached answers and gate results be compared.

**Reference texts (prototype, unit "cells").**

| Style | Prey | Predator |
|---|---|---|
| V1 | `Energy: low. Stamina: high. Food: 2-4 cells away. Predator: 5-10 cells away. Animal: 11-20 cells away, ready to mate. Age: adult.` | `Energy: medium. Stamina: low. Prey: 2-4 cells away. Carcass: none within 20 cells. Other predator: none within 20 cells. Age: adult.` |
| V2 | `I am hungry and weak. I am rested. The nearest food is 2-4 cells away. The nearest predator is 5-10 cells away. The nearest other animal is 11-20 cells away. It is ready to mate. I am an adult.` | `I have some energy. I am out of breath. The nearest prey is 2-4 cells away. No carcass within 20 cells. No other predator within 20 cells. I am an adult.` |

Details of the reference wording: on a food item V1 says `Food: here.` and V2
`I am standing on food.`; the adjacent band reads `1 cell away`; V2 energy
levels read *I am hungry and weak.* / *I have some energy.* / *I am well fed and
strong.*, stamina levels *I am out of breath.* / *I am getting tired.* / *I am
rested.*; V1 adds `, ready to mate` or `, not ready to mate` to the kin fragment
when readiness is seen, V2 adds *It is ready to mate.* / *It is not ready to
mate.*

## 3. Distance bands

- **SENSE-20 (MUST)** A banded sense has increasing edges e₁ < e₂ < … < eₖ and a
  vision v > eₖ. A distance d falls in the first band with d ≤ eᵢ, in the last
  band ("far") if eₖ < d ≤ v, and is `none` if d > v or nothing is there.
- **SENSE-21 (MUST)** Band edges and vision are validated before the run
  (increasing, positive, below the vision).
- **SENSE-22 (SHOULD)** Vision is a trait, so a gene may change it; the text then
  states each animal's own vision.

**Reference.** Edges [1, 4, 10] m and vision 20 m for both species:

| Token | Distance | Text (continuous) | Text (prototype, cells) |
|---|---|---|---|
| `here` (resources only) | the item is within reach | here | here |
| `adjacent` | ≤ 1 m | 1 m away | 1 cell away |
| `close` | 1–4 m | 1-4 m away | 2-4 cells away |
| `medium` | 4–10 m | 4-10 m away | 5-10 cells away |
| `far` | 10–20 m | 10-20 m away | 11-20 cells away |
| `none` | nothing within 20 m | none within 20 m | none within 20 cells |

## 4. Reference senses

| Sense | Tokens | Rule |
|---|---|---|
| Energy level | low, medium, high | thresholds 30 / 70 |
| Stamina level | low, medium, high | thresholds prey 20 / 40, predators 10 / 20 |
| Age | young, adult | adult from `maturity` (150 ticks) |
| Nearest food | here + bands + none | nearest item of the grazed layer |
| Nearest threat ("Predator") | bands + none | nearest animal of a threat species, busy or not |
| Nearest prey ("Prey") | bands + none | nearest animal of a hunted species, not killed, not hidden in cover |
| Nearest carcass ("Carcass") | bands + none | nearest carcass this animal may eat from: portions left, not its own kill, not eaten from by it yet |
| Nearest kin with readiness ("Animal", "Other predator") | none, or one of 4 bands × (ready, not ready) | nearest other animal of its species; whether it is ready to mate ([11](11-reproduction-and-population.md)) is seen within `partnerRange` (reference 20 m = the vision); beyond it, readiness is unknown and not written |
| Nearest cover ("Cover", Unity addition) | here + bands + none | `here` when standing in cover |

That gives **4 860** possible prey observations (3 × 3 × 6 × 5 × 9 × 2) and
**4 050** predator observations (3 × 3 × 5 × 5 × 9 × 2).

Animals in the reference don't sense directions, terrain, cover (in the
prototype), how many animals or items there are, or anything about another
animal beyond its readiness. A prey animal doesn't know whether a predator is
busy digesting. Natural additions: direction tokens (ahead, left, right,
behind), terrain ahead (water, cliff), cover, thirst.

## 5. Batched senses and raycasts

- **SENSE-30 (SHOULD)** A sense that needs raycasts (line of sight, occlusion by
  terrain or trees) declares its rays first. The sense phase casts the rays of
  all deciding animals of all species in one batch, then each sense computes its
  tokens from the results.
- **SENSE-31 (MUST)** A batched sense gives the same tokens as evaluating it
  animal by animal.
- **SENSE-32 (SHOULD)** Spatial structures (indexes of animals by species, of
  entities by kind) are built once per sense phase and shared by all senses.

## 6. Rich senses: images and other attachments

- **SENSE-40 (MAY)** A sense may return an attachment instead of, or with, its
  token: for example a small camera image from the animal's eyes, for a brain
  that reads images.
- **SENSE-41 (MUST)** A query with an attachment is not memoised or cached
  unless the sense gives a cache key for it (e.g. a coarse summary). A species
  with such a sense can only use a brain that accepts attachments; this is
  checked before the run.

## 7. Words for the keyword brain

- **SENSE-50 (SHOULD)** A sense declares the words that genes may use to refer to
  its states, with the token(s) they mean: the energy sense maps
  *hungry, starving, low energy, weak* to `low` and *well fed, full, strong* to
  `high`; the threat sense maps *predator, danger, threat* to "a threat is in
  sight". The keyword brain reads these tables ([08 §6](08-decisions-brains-and-prompts.md#6-reference-brains)).
