# 08 — Decisions, brains and prompts

## 1. When animals decide

- **DEC-01 (MUST)** Every `decisionPeriod` ticks (reference 20, owner 2026-10-11:
  5 × the former 4, so a brain is asked a fifth as often) every living animal
  that is not busy decides: all together on ticks 0, 20, 40, …, or each on its own
  ticks when decisions are staggered (DEC-04, the reference). On the other ticks only animals
  without an action decide: babies just born or hatched, newcomers, and animals
  whose busy state ended on the previous tick.
- **DEC-02 (MUST)** A busy animal never decides (ANIM-30).
- **DEC-03 (MUST)** At a decision the animal's *searching* and *bred this
  period* flags are cleared.
- **DEC-04 (MAY)** A species may have its own decision period, and decisions may
  be staggered (an animal decides when `(tick + id) % period == 0`) to spread the
  brain's load. Both change behaviour. The reference staggers: every species of the
  reference scenes has a staggered schedule (owner 2026-10-10: under JEV a run is
  about 13 % faster and never stalls ~10 s every fourth tick); its own period is off.
  A world without a schedule decides all together, as the Python prototype does.

## 2. Queries and the brain interface

- **DEC-10 (MUST)** Each deciding animal produces one **query**: its species, the
  genes its brain reads (text genes as sentences, in locus order, with their
  labels), its observation, its situation text in the brain's style, and any
  attachments (SENSE-40).
- **DEC-11 (MUST)** A brain answers a batch of queries with one probability vector
  per query: one entry per action of the query's species, in the species' action
  order, every entry ≥ 0, summing to 1 (± 1e-6). Answers come back in the order
  of the queries.
- **DEC-12 (MUST)** A brain is deterministic given its caches and seeds: the same
  query always gives the same vector (LLMs answer at temperature 0 with a fixed
  seed, or from the cache).
- **DEC-13 (MUST)** Each species is decided by one brain, chosen per species with
  a world default. Different species MAY use different brains in one world. A
  brain that cannot mix species in one batch receives one batch per species.
- **DEC-14 (MUST)** A brain states its limits (maximum actions, maximum prompt
  length, attachments supported) and every species using it is checked against
  them before the run.
- **DEC-15 (SHOULD)** All queries of a tick that go to one brain are sent as one
  batch (one request where the server accepts a list), after the memo removed
  the ones already answered.

## 3. Drawing the action

- **DEC-20 (MUST)** The action is drawn from the vector with the sampling stream.
  With a sampling temperature τ ≠ 1 the vector is first replaced by p^(1/τ),
  renormalised (τ < 1 sharpens, τ > 1 flattens). Reference τ = 1.
- **DEC-21 (SHOULD)** The animal keeps its last observation, situation text and
  vector, for the inspector and the logs.

## 4. Memo and answer cache

- **DEC-30 (MUST)** Within a run the answer for a key *(species, brain, brain-visible
  genome key, observation)* is computed once and reused. A memo hit gives
  exactly what the brain would have answered.
- **DEC-31 (MUST)** The brain-visible genome key is a hash of the species id and
  the alleles the brain reads (text genes, in locus order). Number genes the
  brain doesn't read are not part of it, so they don't defeat the memo.
- **DEC-32 (MUST)** Within one batch, queries with the same key are sent once.
- **DEC-33 (SHOULD)** A persistent **answer cache** stores answers across runs,
  keyed by everything that can change an answer: the brain and its model
  (name and digest or pinned revision), the prompt id (a hash of the template),
  the brain mode, the text style, the genome key and the situation text. A run
  repeated with the same seed replays from the cache without model calls.
- **DEC-34 (MUST)** A failed answer is never stored in the memo or the cache.

**Reference.** The memo answers 52–80 % of prey decisions and 46–78 % of predator
decisions in the reference runs.

## 5. Failures

- **DEC-40 (MUST)** In **strict** mode (reference for LLM brains in long runs) a
  brain call that still fails after its retries stops the run cleanly: every
  output is written and the run can be resumed. In non-strict mode the
  decision gets a uniform vector, is counted as a failure and is not cached.
- **DEC-41 (MUST)** Malformed answers are repaired by documented rules or
  rejected as failures. Reference (points answers): negative points count as 0,
  every action gets +0.01, then the vector is normalised; an all-zero answer
  therefore becomes uniform.
- **DEC-42 (SHOULD)** Client retries back off (reference: 3 tries, waits of 1, 2,
  4 s) and time out (reference: 60–120 s).

## 6. Reference brains

### Random

Uniform over the species' actions. The null model: random hunters can't sustain
themselves, which shows the brain matters.

### Keyword brain (prototype only, not in the Unity system)

The Unity system doesn't include this brain (owner decision). It is recorded
here because the prototype's reference numbers came from it, and because writing
a rule-based brain is a good student exercise ([22 §7](22-extending-recipes.md#7-a-brain)).

A transparent "ideal interpreter": default scores from the situation plus a push
from each gene's keywords. No model, fast; in the prototype, the brain for
laptops without a GPU and for CI. Logits = defaults + gene weights;
probabilities = softmax(logits / T), T = 1.

**Default scores (prey).**

| Action | Score |
|---|---|
| eat | no food in sight: 1.5 (searching); else here 3.0, adjacent 2.5, close 2.0, medium 1.5, far 1.0, plus an energy push: low +1.5, medium +0.5, high −1.0 |
| flee | threat adjacent 3.5, close 3.0, medium 1.0, far 0.0, none −3.0 |
| follow | kin adjacent −0.5, close −0.5, medium 0.3, far 0.3, none −2.5 |
| rest | −0.5; +0.5 if energy high and the threat is none or far; plus a catch-breath push by stamina: low +2.0, medium +0.3, high 0 |
| mate | if the kin is ready, the animal is adult and its energy isn't low: kin adjacent 2.5, close 2.5, medium 1.5, far 1.0; else −3.0 |

**Default scores (predator).** hunt: by the nearer of prey and carcass, adjacent
3.5, close 3.0, medium 2.0, far 1.0, none 1.5 (searching), plus the energy push;
follow: as the prey; rest: −0.5, +1.0 if energy high, plus the catch-breath push;
mate: as the prey.

**Gene weight** of the gene of action *a* (text lower-cased):

1. Intensity, first match wins: *never, do not, don't, avoid, refuse* −2.5;
   *rarely, seldom, hardly* −1.2; *always, whatever happens, at all costs* +2.5;
   *whenever, often, usually, eagerly, quickly* +1.2; *sometimes, occasionally,
   maybe* +0.3. With no intensity word, a word of the action itself (eat: *eat,
   food, feed, graze, forage*; flee: *flee, run, escape, hide, danger, predator*;
   follow: *follow, stay close, group, companion, herd, pack*; rest: *rest, sleep,
   stay still, wait, save energy, stop, catch your breath, recover*; mate: *mate,
   partner, breed, offspring*; hunt: *hunt, chase, attack, kill, prey, strike,
   pounce, stalk, carcass, carrion, scavenge, leftovers*) gives +0.8. Otherwise 0
   and the gene has no effect.
2. Conditions: if the sentence (before any "unless") names conditions (words
   declared by the senses, SENSE-50: *hungry*, *well fed*, *tired*, *predator*,
   *very close*, *food is close*, *food is far*, *alone*, *safe*, *old*,
   *plentiful*, *carcass*, *prey is close*, *no prey*) and none holds, the
   weight is × 0.2.
3. "unless X": if a condition in X holds, the weight becomes −0.5 × itself.

Known weakness kept on purpose for students to find: some patterns lack word
boundaries ("strong" matches "strongest").

### LLM, points mode (Ollama)

One chat call per distinct query. The prompt (§7) ends with: *Distribute 100
points across the actions according to how likely this animal is to choose
each.* The answer is constrained by a JSON schema (one integer 0–100 per action,
all required). Options: temperature 0, a fixed seed, a fixed context length
(4 096; without it the server reloads the model), thinking off. Points become
probabilities by DEC-41. Measured: about 4 decisions per second on a 16 GB GPU
(gemma4:12b); 73 % of answers add up to exactly 100.

### JEV choice (a distilled decision model on vLLM)

One forward pass per distinct query, no text generated. The request text:

```text
[kind] choice
[state] <the species prompt with genes and situation, without the answer instruction>
[question] Which action does this animal take now?
[options]
A) eat
B) flee
...
[decision]:
```

The completion request asks for one token restricted to the option letters, with
their log-probabilities. Each option's logit is its log-probability plus a
per-slot bias from the model's decision head; probabilities are
softmax(logits / T), with T from the model's calibration file (both pinned to a
model revision). At most 16 options (A–P). The model was trained on inputs up
to 1 024 tokens; longer inputs are counted, not cut. Options are always listed in
the species' action order: the model changes 11.5 % of its answers when only the
order changes. Measured: about 12 decisions per second.

## 7. The prompt

- **PROMPT-01 (MUST)** An LLM prompt is assembled from, in order: a header (what
  is being decided, in what world); the action list, one line per action from
  the action's description, in action order; rule lines contributed by modules
  (the search rule, speeds, stamina, breeding, carcasses, cover); the genes
  block; the situation; the brain's answer instruction.
- **PROMPT-02 (MUST)** The genes block has one line per text gene, in locus order:
  `- <label>: "<sentence>"`, the label being the action name for action genes.
- **PROMPT-03 (MUST)** Numbers in rule lines come from the configuration
  (placeholders filled from the modules' settings), so the prompt never
  contradicts the world. The prototype's prompts hard-code them.
- **PROMPT-04 (MAY)** A species may use a **frozen** prompt text with the
  placeholders `{genes}`, `{situation}` and `{ask}` instead of the assembled one,
  for example the prototype's prompts below.
- **PROMPT-05 (MUST)** The prompt id (a hash of the template or of the assembled
  text with placeholders) is part of the cache key.
- **PROMPT-06 (MUST)** The editor shows the full prompt for a chosen animal or
  situation, with an estimate of its token count, checked against the brain's
  limit.
- **PROMPT-07 (MUST)** Every part except the answer instruction is a text field
  in the inspector: the species' header, each action's line, each module's rule
  lines (owner decision). The answer instruction ("Distribute 100 points…") is
  hard-coded in each brain, as in the prototype, because the brain's parsing
  depends on it.

**Reference: the prototype's prey prompt** (`teacher_v5`):

```text
You decide what a wild animal does next in a simple grid world.

Actions:
- eat: go to the nearest visible food and eat it
- flee: run away from the nearest predator
- follow: move toward the nearest other animal
- rest: stay still to catch your breath and save energy
- mate: walk to the nearest ready partner in sight and breed with it
If the chosen action has nothing to act on in sight (no food, predator, animal or
ready partner), the animal searches the surroundings instead.
Animals move one cell per step. Predators run two cells per step when they
hunt, but they have half an animal's stamina, so a long chase tires them first.
Every cell moved costs stamina. Standing still brings it back, which costs some
energy until stamina is full. Without stamina an animal cannot move.
Breeding needs only one of the two to choose mate: an adult that chooses mate
breeds as soon as it reaches a ready partner, whatever the partner is doing.

This animal's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
{genes}

Situation: {situation}

{ask}
```

**Reference: the prototype's predator prompt** (`predator_v3`):

```text
You decide what a predator does next in a simple grid world.

Actions:
- hunt: chase the nearest visible prey animal or carcass; next to a prey animal,
  try to kill and eat it; next to a carcass, eat from it
- follow: move toward the nearest other predator
- rest: stay still to catch your breath and save energy
- mate: walk to the nearest ready partner (another predator) in sight and breed with it
If the chosen action has nothing to act on in sight (no prey, carcass, other
predator or ready partner), the predator searches the surroundings instead.
Predators run two cells per step when they hunt and walk one cell otherwise.
Prey animals move one cell per step but have twice a predator's stamina.
Every cell moved costs stamina. Standing still brings it back, which costs some
energy until stamina is full. Without stamina a predator cannot move.
A kill leaves a carcass that up to two other predators can eat from.
Breeding needs only one of the two to choose mate: an adult that chooses mate
breeds as soon as it reaches a ready partner, whatever the partner is doing.

This predator's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
{genes}

Situation: {situation}

{ask}
```

**Reference: the Unity default prey prompt**, as the inspector fields assemble
it for a founder in one situation. Numbers come from placeholders such as
`{predator.Locomotion.runSpeed}`; the last line is the points brain's hard-coded
answer instruction.

```text
You decide what a wild animal does next in a simple world.

Actions:
- eat: go to the nearest visible food and eat it
- flee: run away from the nearest predator
- hide: go to the nearest cover and stay in it; predators can't see or catch an animal in cover
- follow: move toward the nearest other animal
- rest: stay still to catch your breath and save energy
- mate: walk to the nearest ready partner in sight and breed with it
If the chosen action has nothing to act on in sight (no food, predator, cover,
animal or ready partner), the animal searches the surroundings instead.
Animals move 1 meter per step. Predators run 2 meters per step when they hunt,
but they have 30 stamina against an animal's 60, so a long chase tires them first.
Every meter moved costs stamina. Standing still brings it back, which costs some
energy until stamina is full. Without stamina an animal cannot move.
Breeding needs only one of the two to choose mate: an adult that chooses mate
breeds as soon as it reaches a ready partner, whatever the partner is doing.

This animal's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
- eat: "Eat whenever food is close."
- flee: "Run from any predator you see."
- hide: "Hide when a predator is close."
- follow: "Stay close to other animals."
- rest: "Rest when you are tired."
- mate: "Look for a partner when energy is high."

Situation: Energy: low. Stamina: high. Food: 1-4 meters away. Predator: 4-10 meters away. Cover: 1-4 meters away. Animal: none within 20 meters. Age: adult.

Distribute 100 points across the actions according to how likely this animal is to choose each.
```

Where each part comes from: the header from the species; the action lines from
the actions; "If the chosen action has nothing to act on…" from the search rule;
the speed lines from the locomotion components; the stamina lines from the
stamina stat; the breeding lines from the mating rule; the carcass line (in the
predator's prompt) from the carcass system; the instincts paragraph and the
genes from the genes block; the situation from the senses; the last line from
the brain.
