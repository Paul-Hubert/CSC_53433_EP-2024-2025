# 40 — Open questions

Blind spots found while writing the contract, and decisions still to take. Each
names the documents it affects and a proposed default; until the owner decides,
the default holds.

## 1. Decisions for the course owner

| # | Question | Proposed default | Affects |
|---|---|---|---|
| 1 | **Reference values in continuous space.** Euclidean distance shrinks the vision disc to about 75 % of the prototype's 20-cell square, and straight-line steering is faster than 8-neighbour steps on diagonals. Retune, or keep the prototype's numbers? | keep them, measure the first accepted Unity runs, and make those the new references (R-02) | 02, 06, 14, 30 |
| 2 | **Unit word in texts**: "m" (honest in Unity) or "cells" (the prototype's text, its caches and gate results stay comparable)? | "m"; "cells" for comparison runs | 06, 08 |
| 3 | **Generated or frozen prompts.** Generated prompts never contradict the settings but differ from the prototype's, so gates must be measured again. | generated; the prototype's frozen prompts in the comparison scenes | 08, 32 |
| 4 | **Default act order**: species in turn (the prototype) or all mixed? | species in turn, so the references hold; all mixed as a lab variant (S19) | 07, 12 |
| 5 | **Breeding inside or after the act phase** (TICK-06). | after all animals acted | 12 |
| 6 | **Eggs**: incubation by default (0 = the prototype, or a few ticks to hide the mutator's latency)? Can eggs be eaten? Do they count toward the cap? Where are they laid? | 4 ticks; not eaten; not counted; at the first parent's position | 10, 11 |
| 7 | **Number genes**: which traits first (stamina, speed, vision, litter size)? Are they written in the prompt ("Stamina: high")? Is a cost compulsory? | stamina and vision first; not in the prompt; a cost recommended, warned about (V-47) | 05, 09 |
| 8 | **Species names**: how are runtime species named ("Rabbit 2", a name from a list, an LLM-made name)? Do founder sentences use species names ("Run from wolves") or roles ("Run from predators")? | numbered names; founder sentences use roles, so they work in any world | 04, 09 |
| 9 | **Speciation criterion** (if any): genetic distance, geography, a teacher's button? | none by default; a teacher's button and the S11 example | 04, 22 |
| 10 | **Cannibals and kin**: should a cannibal flee from its own kind, and still follow and mate with it? | yes: threats include itself; follow and mate unchanged | 04, 07 |
| 11 | **Hide versus flee**: keep the prototype's *flee into cover* anywhere? | only in the comparison scenes; hide is its own action elsewhere | 03, 07 |
| 12 | **G2 failing** (random text moves gemma; genes move JEV too little): which brain is the class default, and is MI_G ≥ 0.25 kept? | keyword brain by default, JEV on a lab server; the threshold stays until the owner decides (prototype decision 2) | 08, 15, 32 |
| 13 | **The neutral gene** "No preference." mutates into vague rules: exempt it from mutation? (prototype decision 14) | not exempt (MUT-22 allows it per gene) | 10 |
| 14 | **Founder pools** are drafts awaiting review (H1). | use them as they are | 09 |
| 15 | **Lab 1 platform**: the Python prototype, Unity, or both during the transition? | both: Python for the first sessions, Unity when T0–T3 are green | — |

## 2. Technical blind spots

- **Real time.** The owner chose lockstep. A real-time mode (decisions applied
  when they arrive, on old observations) could still be useful for demos and VR
  interaction; it is allowed only as a labelled, non-reproducible mode (SPACE-15).
- **Physics and learned locomotion.** The reference motors are kinematic. A
  physics motor (rigid bodies) or a learned locomotion controller (a DRL policy
  that turns an intent into joint torques, as in the quadruped IK scenes of the
  repository) fits the `Motor` interface, but floating-point physics breaks the
  events hash across machines. Such motors stay outside the determinism
  guarantee; the brain/intent split keeps them possible.
- **Camera senses.** Whether a JEV-style decision model can read images is not
  known; a vision-language model per decision is costly, and image queries
  can't be memoised (SENSE-41). A coarse cache key (e.g. a downsampled hash)
  needs a measurement of how often it repeats.
- **Floating-point determinism across machines.** Pinned hashes (R-01) are per
  platform and scripting backend. CI pins them on its own runner.
- **The answer cache** is a JSON-lines file per brain in this proposal, not the
  prototype's SQLite, so Unity can't reuse the prototype's caches. A shared class
  cache (on a lab server) would save most of the LLM cost; format and location
  are open.
- **Golden fixtures from Python** (R-08) need a small exporter in
  `prototype/experiments/`. Worth it only if exact text and keyword-brain parity
  matter; otherwise drop R-08.
- **Simultaneous act order and fairness.** Contested items go to the first in a
  random order; alternatives (nearest wins, split) change behaviour and are not
  specified.
- **Slope and terrain costs.** Allowed (SPACE-06) but not specified: how much more
  energy and stamina does uphill cost, and does the brain know (a slope sense)?
- **Directions in observations.** The prototype senses distances only. Directions
  (ahead, left, right, behind) multiply the observation space by 4 per sense;
  worth it only with a brain that uses them.
- **Scale.** Plain C# lists are enough up to a few thousand animals. Beyond that,
  spatial queries and metabolism can move to Burst jobs without changing the
  module API; the brain remains the bottleneck.
- **Unity upgrade.** 6000.3 will change soon. The fenced files (ARCH-10) are the
  HTTP clients, waiting, NavMesh queries and the ray batch helper; the rest uses
  stable APIs.
- **CI hardware.** T4 needs a self-hosted runner with a 16 GB GPU, Docker and
  Ollama; T0–T3 need a Unity license secret or a self-hosted Unity runner.

## 3. Rules that may change after the first Unity runs

| Rule | Why it may change |
|---|---|
| SENSE-05 threshold (100 000 situations) | the memo's real hit rate with continuous positions |
| REPRO-03 (nearest partner) | the prototype took the first ready partner in its list; nearest may change breeding rates |
| ACT-10 reaches (0.5 m graze, 1 m strike) | continuous movement may need larger reaches to avoid near-misses at speed 2 |
| R-02 ranges | measured afresh on the first accepted runs |
