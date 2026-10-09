# 30 — Tests

Every MUST rule of the contract has at least one automatic test; each test names
the rules it covers. Tests are written with the Unity Test Framework: **EditMode**
for logic (fast, no scene), **PlayMode** for whole worlds running ticks.

## 1. Tiers

| Tier | What | Where | When | Time |
|---|---|---|---|---|
| **T0** | validation of every scene and module prefab; static checks of the code | EditMode | every push | < 1 min |
| **T1** | unit tests of every system, conformance suites for every module | EditMode | every push | < 2 min |
| **T2** | short whole-world runs with the keyword or a scripted brain | PlayMode | every push | < 10 min |
| **T3** | regression: pinned hashes, statistical ranges over seeds, soak, performance | PlayMode / batch mode | nightly and before merging | < 2 h |
| **T4** | real LLMs: brain smoke tests, gates, mutator quality, a short LLM run | batch mode on a GPU machine | nightly, and on demand | hours |
| **T5** | coding-agent audit of the implementation against this contract | a Claude Code session | weekly and before releases | hours |

T4 and T5 are described in [32](32-integrity-prompts-and-ci.md).

## 2. Test infrastructure (`EvoSim.Testing`)

| Helper | Does |
|---|---|
| `WorldBuilder` | builds a world in code: `new WorldBuilder(seed).Flat(20, 20).Food(…).Species("Rabbit", s => s.Action<EatAction>().Sense<…>()).Build()`; no scene needed |
| `ScriptedBrain` | answers given vectors per (species, observation) or a function; counts calls and batches; can delay answers by N `Advance` calls |
| `FakeMutator` | answers mutation prompts with a scripted function (e.g. append "quickly"); records every prompt; can delay or fail |
| `FakeClock` driver | advances ticks without Play mode, switches wait modes, varies ticks per call |
| `Place` | puts animals, items, carcasses and cover at exact positions |
| `Golden` | reads and writes golden files under `Tests/Golden/` (situation texts, prompts, probability vectors, hashes) |
| `Stat` | statistical asserts: a binomial or mean within k standard errors, run over a fixed list of seeds |
| `Invariants` | checks after every tick in test worlds (§5) |

Tests never call real servers below T4. Every random choice in a test comes from
a fixed seed.

## 3. Unit tests by system (T1, EditMode unless marked T2)

Format: **id** — given → expect *(rules)*.

### Core and discovery

- **T-CORE-01** — a species with another species inside → `Initialize` fails with V-01, nothing spawned *(SPEC-05, ARCH-05)*.
- **T-CORE-02** — a test-only sense, action, phase and brain defined in the test assembly, dropped into a built world → discovered, run, and recorded, with no change to runtime code *(CORE-02)*.
- **T-CORE-03** — a module on an inactive GameObject, and a disabled component → absent from the species' lists and from the signature *(ARCH-06)*.
- **T-CORE-04** — a gene on a child of an action, a gene on the action's GameObject, a gene elsewhere marked free → bound to that action, that action, and free *(GENE-05, ARCH-05)*.
- **T-CORE-05** — two actions swapped in the hierarchy → action order, probability order, prompt order and signature all follow; V-12 raised *(SPEC-02, SPEC-03)*.
- **T-CORE-06** — one module prefab used by two species with different overrides → each species resolves its own threats, layers and values *(SPEC-06, ARCH-08)*.
- **T-CORE-07** — worlds with 1 species / 1 action / 0 genes / 0 senses, and with 6 species → initialise and run 100 ticks *(CORE-04)*.
- **T-CORE-08** — static check: the runtime assembly contains no species, action or sense names ("prey", "predator", "eat"…) outside the reference module classes' defaults *(CORE-03)*.
- **T-CORE-09** — two worlds built from the same prefabs, run interleaved tick by tick → each gives the hash it gives alone (no shared or static state) *(CORE-05, ARCH-07)*.
- **T-CORE-10** — after the Sense, Ask brains and Choose actions phases no position, stat or item has changed *(CORE-06)*.

### Space and time

- **T-SPACE-01** — an animal moved by an intent of 0.37 m → its position changed by exactly that vector, nothing snapped *(SPACE-01)*.
- **T-SPACE-02** — distances between points with different heights → horizontal Euclidean; a replaced distance function is used by every sense and action *(SPACE-02, SPACE-06)*.
- **T-SPACE-03** — 10 000 random intents (fleeing into corners, running at walls, NavMesh edges) → no animal ever outside the rectangle or on non-walkable ground *(SPACE-03, SPACE-04, MOVE-01)*.
- **T-SPACE-04** — two candidates at the same distance → the same one chosen in 100 runs and with the candidates inserted in reverse order *(SPACE-07, SPACE-08)*.
- **T-SPACE-05** *(T2)* — the same world run with 1, 3 and 10 ticks per call, and with `Time.fixedDeltaTime` 0.02 and 0.005 → identical hashes *(SPACE-10, SPACE-11)*.
- **T-SPACE-06** *(T2)* — views hidden versus shown, interpolation on or off → identical hashes *(SPACE-12)*.
- **T-SPACE-07** *(T2)* — a `ScriptedBrain` that answers 3 `Advance` calls late, in Responsive and Freeze modes → the tick doesn't move past Choose actions before the answers, decisions use their own tick's observations, and both modes give the same hash *(SPACE-13, SPACE-14, TICK-03)*.

### Environment

- **T-ENV-01** — nearest item, within-reach test, count → match a brute-force search over all items *(ENV-01)*.
- **T-ENV-02** — two animals reaching for one item in one tick, under each act order → exactly one eats it; who eats follows the act order *(ENV-02, ACT-30)*.
- **T-ENV-03** — regrowth over 2 000 ticks on an empty layer → count within 4 standard errors of `cells × p × ticks`; extra draws on another stream leave the regrowth identical *(ENV-03, RAND-02)*.
- **T-ENV-04** — hungry cover → no item ever in cover, at the start or after 5 000 ticks *(ENV-04)*.
- **T-ENV-05** — a prey animal in cover next to a hunter → not in the hunter's senses, not its hunt target, never struck; its kin still sense it *(ENV-11, SENSE-06, ACT-05)*.
- **T-ENV-06** — a world without a cover module → hide searches, the cover sense says none, nothing is hidden *(ENV-12)*.
- **T-ENV-07** — a carcass → at most 2 portions, never to the killer, at most one per eater, removed when eaten up or after 100 ticks *(ENV-20, ENV-22)*.
- **T-ENV-08** — in-cover test and nearest cover within a radius → match a brute-force search over the cover cells *(ENV-10)*.

### Species and food web

- **T-SPEC-01** — display names "Rabbit" and "Wolf" → situation texts and prompts use them; renaming changes the text and the cache key *(SPEC-01, SPEC-20)*.
- **T-SPEC-02** — a genome of one species offered to another → rejected; crossover across species throws *(SPEC-04, GENE-06)*.
- **T-SPEC-03** — wolves striking rabbits → the rabbits' threats are {Wolf}, derived; adding a lynx diet entry adds Lynx without other changes *(SPEC-10, SPEC-12)*.
- **T-SPEC-04** — a cannibal species → hunts kin, never itself *(SPEC-11)*.
- **T-SPEC-05** — a grazer next to a carcass and a hunter next to food → no interaction happens *(SPEC-13)*.
- **T-SPEC-06** *(T2)* — `AddSpecies` from a template at tick 300 → new id, own population, streams and allele namespace; relations inherited both ways; other species' events unchanged up to tick 300 *(SPEC-30, SPEC-31, RAND-03)*.
- **T-SPEC-07** *(T2)* — a species goes extinct (no floor) → its id is never reused; a later new species gets a new id *(SPEC-32)*.

### Animals, stats, life cycle

- **T-ANIM-01** — births, newcomers and founders across species → ids unique and increasing; the record has every field of ANIM-01 *(ANIM-01, ANIM-02, ANIM-03)*.
- **T-ANIM-02** — a declared stat with a maximum → written above it, reads the maximum; age is present without any module; no stamina module → no stamina stat *(ANIM-10, ANIM-11, ANIM-22)*.
- **T-ANIM-03** — a trait with default 60, a number gene setting 75, another multiplying by 1.5, one setting 500 with range [1, 120] → 75, 90, 120; no gene → 60; unchanged after 1 000 ticks *(ANIM-15, ANIM-16, ANIM-17, GENE-04)*.
- **T-ANIM-04** — the energy table of [05 §4](05-animals-stats-and-life-cycle.md#4-metabolism-energy-and-stamina-reference-modules), one case per row (walk 1 m, run 2 m, rest, other action still, recovering, busy, graze, kill, portion) → exact energy and stamina after one tick *(ANIM-20, ANIM-21, ANIM-22)*.
- **T-ANIM-05** — stamina 0.6 and an intent of 2 m → moves 0.6 m; stamina 0 → doesn't move, whatever the action *(ANIM-21, MOVE-02)*.
- **T-ANIM-06** — a busy animal for 3 ticks → no decision, no movement, busy cost paid, stamina recovers; decides at the start of the 4th tick *(ANIM-30, DEC-02, DEC-01)*.
- **T-ANIM-07** — digestion after a kill and after a portion → 50 and 25 ticks, reason "digesting" recorded *(ANIM-31)*.
- **T-ANIM-08** — a baby born this tick → age 1 after the death phase; adult at age 150 *(ANIM-35, ANIM-36)*.
- **T-ANIM-09** — energy 0 at the death check, age 1 501, a strike that kills, a migration → causes starvation, old_age, killed (with killer), migrated; each recorded once *(ANIM-40, ANIM-42)*.
- **T-ANIM-10** — a prey animal killed early in the act phase (all-mixed order) → later in the tick it is not sensed, not targeted, doesn't act or breed; gone before the next decisions *(ANIM-41, ANIM-43, REPRO-05)*.

### Senses and observations

- **T-SENSE-01** — every reference sense in 1 000 random worlds → returns a declared token index; the "nothing" token when nothing is there *(SENSE-01)*.
- **T-SENSE-02** — two animals in different places with the same tokens → equal observations, one memo key *(SENSE-02)*.
- **T-SENSE-03** — the same world state read twice, and read by animals deciding in the same tick in a different order → same tokens *(SENSE-03, SENSE-04)*.
- **T-SENSE-04** — band boundaries: distances 0.99, 1.0, 1.01, 4.0, 4.01, 10.0, 20.0, 20.01 → adjacent, adjacent, close, close, medium, medium, far, none; an item within reach → here *(SENSE-20)*.
- **T-SENSE-05** — edges [4, 1, 10], [1, 4, 25] with vision 20 → validation errors *(SENSE-21)*.
- **T-SENSE-06** — readiness: a ready partner at 15 m with partner range 20 and 4 → "ready to mate" written, then not written *(06 §4)*.
- **T-SENSE-07** — observation-space size of the reference species → 4 860 and 4 050; above the threshold → V-50 *(SENSE-05)*.
- **T-SENSE-08** — golden situation texts (V1 and V2, unit "cells") for the 48 reference situations → byte-identical to the prototype's texts *(SENSE-10, SENSE-11, SENSE-12, SENSE-13)*.
- **T-SENSE-09** — a batched raycast sense versus the same sense evaluated one animal at a time → same tokens for 500 animals *(SENSE-31)*.
- **T-SENSE-10** — a camera sense with a brain that rejects attachments → V-51; with one that accepts them → those queries never hit the memo unless a cache key is given *(SENSE-41)*.

### Actions and locomotion

- **T-ACT-01** — conformance suite over every `AnimalAction` in the project: has a name unique in its species, a description; in a world with nothing in sight → searches, flagged once per decision; never changes another animal *(ACT-01, ACT-04, ACT-06)*.
- **T-ACT-02** — a chosen action over 4 ticks with a moving target → the target is looked up each tick *(ACT-02)*.
- **T-ACT-03** — each reference action (eat, flee, follow, rest, mate, hunt, hide) in a placed scene → the intent of [07 §4](07-actions-and-locomotion.md#4-reference-actions): target, walk or run, stop distance *(ACT-03)*.
- **T-ACT-04** — eat 0.8 m from an item → walks to it and grazes in the same tick *(ACT-10, MOVE-03)*.
- **T-ACT-05** — strike, scavenge, graze at 1.01 m and 0.99 m (graze 0.51 and 0.49) → no interaction, interaction *(ACT-10)*.
- **T-ACT-06** — 10 000 strikes with kill chance 0.5 → 50 % within 4 standard errors; a kill leaves a carcass, makes the hunter busy, adds the gain *(ACT-11)*.
- **T-ACT-07** — a hunter next to two prey → strikes at most once per tick; a surviving prey struck by a second hunter the same tick can die *(ACT-12)*.
- **T-ACT-08** — mate next to a ready partner → no birth in the act phase; the birth happens in the breed phase *(ACT-13)*.
- **T-ACT-09** — static check: no action class reads `TextGene` values or allele texts *(ACT-20)*.
- **T-ACT-10** — hunt with a carcass and a prey at the same distance → goes for the carcass *(07 §4)*.
- **T-MOVE-01** — walk and run intents with traits 1 and 2 m → 1 and 2 m moved (stamina allowing); never past the stop distance *(MOVE-02, MOVE-03)*.
- **T-MOVE-02** — flee from a hunter → distance grows each tick in open ground; in a corner the animal slides or sidesteps and is never stuck for more than 3 ticks *(MOVE-04)*.
- **T-MOVE-03** — wandering for 100 000 ticks → turns on 25 % of ticks (± 4 s.e.), only by ±45° or ±90°; new heading when blocked *(MOVE-05)*.
- **T-MOVE-04** — the metres reported by the motor equal the distance actually moved and are what metabolism charges *(MOVE-06)*.
- **T-MOVE-05** *(T2)* — the three act orders on the same seed → different but each reproducible hashes; sequential orders see earlier moves, simultaneous sees the start state *(ACT-30, ACT-31, ACT-32)*.

### Decisions, brains, prompts

- **T-DEC-01** — decision ticks with period 4 → every non-busy animal decides on 0, 4, 8; a baby born on tick 5 decides on tick 6; flags cleared at each decision *(DEC-01, DEC-03)*.
- **T-DEC-02** — a query → has species, brain-visible genes in locus order with labels, observation, situation text in the world's style, attachments *(DEC-10)*.
- **T-DEC-03** — conformance suite over every `Brain`: rows have the species' length, entries ≥ 0, sum 1 ± 1e-6, same order as the queries; the same query twice → the same row *(DEC-11, DEC-12)*.
- **T-DEC-04** — two species with different brains, one brain that can't mix species → each brain gets only its species, batched per species *(DEC-13)*.
- **T-DEC-05** — 17 actions with the JEV brain; a prompt over its token limit → errors before the run *(DEC-14, V-11)*.
- **T-DEC-06** — sampling with τ = 1, 0.5, 2 over 100 000 draws → frequencies match p, p², √p normalised, within 4 s.e. *(DEC-20)*.
- **T-DEC-07** — 50 animals in 3 distinct situations with one genome → 3 brain queries; next tick, the same situations → 0 *(DEC-30, DEC-32)*.
- **T-DEC-08** — two animals that differ only in a number gene the brain doesn't read → one memo key *(DEC-31)*.
- **T-DEC-09** — a brain failing every call, strict → the run stops cleanly with all outputs; non-strict → uniform rows, failures counted, nothing stored in memo or cache *(DEC-34, DEC-40, RAND-20)*.
- **T-DEC-10** — points answers {eat: 120, flee: −5}, all zeros, a missing action → repaired by DEC-41 (or rejected as documented) *(DEC-41)*.
- **T-DEC-11** — keyword brain golden cases from the prototype: the genome of the prototype's Lab 1 activity C in "energy low, food close, predator close, no animal" → eat 0.31, flee 0.69 (± 0.01); the predator genome at energy low / high with prey medium → hunt 0.95 / 0.32 *(08 §6)*.
- **T-DEC-12** — keyword brain directed tests: for each contrast pair, "Always…" gives the action more probability than "Never…" in every relevant observation; control sentences change nothing *(08 §6, CTRL-10)*.
- **T-PROMPT-01** — an assembled prompt → header, then one line per action in order, rules from the modules present, genes block `- label: "sentence"`, situation, ask *(PROMPT-01, PROMPT-02)*.
- **T-PROMPT-02** — run speed changed from 2 to 3 → the rule line says three (or 3) *(PROMPT-03)*.
- **T-PROMPT-03** — the prototype's frozen prompts with the golden genes and situations → byte-identical to the prototype's prompts *(PROMPT-04)*.
- **T-PROMPT-04** — a one-character change in a template → a new prompt id and a cache miss *(PROMPT-05, DEC-33)*.
- **T-PROMPT-05** *(editor)* — the species inspector's preview equals the prompt the brain receives for the same animal *(PROMPT-06)*.

### Genes, alleles, genomes

- **T-GENE-01** — every gene kind → id, label, kind, founder pool; text expression only adds a prompt line; number expression only sets its trait *(GENE-01, GENE-02, GENE-03, GENE-04)*.
- **T-GENE-02** — registering "Eat  when hungry." and "Eat when hungry." → one allele, first origin kept; 0.30001 and 0.30004 at 4 decimals → one allele *(GENE-10)*.
- **T-GENE-03** — allele ids per locus → `prey.eat:0`, `prey.eat:1`…; compatibility mode → `eat:0` *(GENE-11, OUT-05)*.
- **T-GENE-04** — a mutant allele → its record carries parent, operator, model, seed; the lineage of a 5-step chain is rebuilt from the records *(GENE-12)*.
- **T-GENE-05** — the same genes in two runs with different registration orders → the same genome key *(GENE-13)*.
- **T-GENE-06** — 10 000 founders → each locus uniform over its pool (χ² test), neutral included *(GENE-20)*.
- **T-GENE-07** — a founder sentence of 13 words, one with "é" or digits outside the allowed set → V-40 *(GENE-22)*.
- **T-GENE-08** — 10 000 crossovers → each locus from parent A in 50 % (± 4 s.e.), independent between loci; siblings of a litter differ when parents differ *(GENE-30, GENE-32)*.
- **T-GENE-09** — asexual reproduction → the baby's genome equals the parent's before mutation *(GENE-31)*.
- **T-GENE-10** — founder pools frozen: two runs with different seeds → the same pool contents and allele ids for founders *(GENE-21)*.

### Mutation

- **T-MUT-01** — rate 0.03 over 100 000 baby-loci → 3 % attempts (± 4 s.e.); rate 0 → no new allele *(MUT-01, CTRL-01)*.
- **T-MUT-02** — a text gene and a number gene with no operator configured → the species defaults apply; one operator per locus *(MUT-02)*.
- **T-MUT-03** — a mutation → new allele registered and listed in the birth event with locus, parent, child, operator detail, value *(MUT-03, OUT-03)*.
- **T-MUT-04** — a `FakeMutator` whose every answer is rejected → after 5 tries the inherited allele stays; counters record the rejections by reason *(MUT-04, MUT-05, MUT-13)*.
- **T-MUT-05** — a deck file with comments and blank lines → only instruction lines are drawn; adding a line adds an instruction *(MUT-10)*.
- **T-MUT-06** — the prompt the mutator receives → exactly MUT-11's text (golden), with and without a context line; it contains no world data, no other gene *(MUT-11, CORE-07)*.
- **T-MUT-07** — cleaning and guards, golden cases from the prototype (quoted answers, "Here is the new sentence: …", two sentences, 13 words, same as parent with other case, only punctuation changed, forbidden characters) → the prototype's results *(MUT-12)*.
- **T-MUT-08** — the same mutation prompt and seed twice through the cache → one model call *(MUT-14)*.
- **T-MUT-09** — Gaussian mutation near the range edges → always clamped; same seed, same value *(MUT-20)*.
- **T-MUT-10** — answers delivered in a different order or later (fake delays) → identical genomes and events *(MUT-30, RAND-11)*.
- **T-MUT-11** — 12 babies conceived in one tick, 3 answers rejected → one first batch of all requests, then one redraw batch *(MUT-31)*.
- **T-MUT-12** — incubation 2 with answers 5 ticks late → hatching waits; with answers in time → no wait *(MUT-32, REPRO-24)*.

### Reproduction and population

- **T-REPRO-01** — readiness at age 149 / 150 and energy 49.9 / 50 → not ready, ready *(REPRO-01)*.
- **T-REPRO-02** — an animal choosing mate next to a ready partner choosing rest → a litter; two animals choosing mate not in reach → none *(REPRO-02)*.
- **T-REPRO-03** — three ready seekers in contact → each breeds at most once per decision period; the pairing is the same in every run *(REPRO-03)*.
- **T-REPRO-04** — asexual mode → one parent, whole cost *(REPRO-04)*.
- **T-REPRO-05** — litter [2, 4] over 10 000 matings → sizes uniform; parents at 50 energy → litter cut to 2; parents at 30 energy → litter 2 anyway, energy below zero *(REPRO-10)*.
- **T-REPRO-06** — a litter of 3 → each parent pays 60, offspring +3, flagged as bred *(REPRO-11)*.
- **T-REPRO-07** — babies → energy 40, full stamina, age 0, generation max + 1, at the first parent's position, no action until the next tick *(REPRO-12, REPRO-13)*.
- **T-REPRO-08** — incubation 0 and 5 → hatch in the same tick / 5 ticks later, in conception order; eggs are not sensed, don't move, don't count toward the cap *(REPRO-20, REPRO-21, REPRO-22)*.
- **T-POP-01** — migrate rule: a species 7 above its cap with 3 babies born this tick → 7 older animals leave, babies stay; block rule → no conception at the cap *(POP-01)*.
- **T-POP-02** — 1 000 migrations in a population where half carry allele X → X leaves in 50 % (± 4 s.e.); no correlation with energy or position *(POP-02, CORE-01)*.
- **T-POP-03** — a species below its floor → newcomers with founder genomes, generation 0, immigrant events, until the floor *(POP-03)*.
- **T-POP-04** — tick 0 → initial populations as founders at walkable positions, founder events *(POP-04)*.

### Tick order, randomness, outputs, configuration, controls

- **T-TICK-01** — a recording phase inserted between each pair of phases → the reference order of TICK-04; a test phase added or removed runs or vanishes without other changes *(TICK-01, TICK-04, TICK-07)*.
- **T-TICK-02** — a phase changes the world → only later phases of the same tick see it *(TICK-02)*.
- **T-TICK-03** — decisions read the state after the previous tick's floor phase (a newcomer added at the floor decides at the next tick start) *(TICK-05)*.
- **T-RAND-01** — static check: no `UnityEngine.Random`, unseeded `System.Random`, `Guid.NewGuid` or time-based seeds in the runtime assembly *(RAND-01)*.
- **T-RAND-02** — extra draws inserted into the food stream → births, act order and mutations unchanged *(RAND-02, RAND-03)*.
- **T-RAND-03** — a world seed fixed and the run seed varied → same map and initial food, different runs *(RAND-04)*.
- **T-RAND-04** — dictionaries filled in different insertion orders inside the simulation's caches → identical hash *(RAND-05)*.
- **T-RAND-05** *(T2)* — same seed twice → same hash; another seed → another hash *(RAND-10, RAND-11, RAND-13, CORE-09)*.
- **T-OUT-01** *(T2)* — a 500-tick run → its own folder with every file of [13 §2](13-outputs-and-recording.md#2-files); events in tick order, keys sorted, each with kind, t, id, species *(OUT-01, OUT-02)*.
- **T-OUT-02** *(T2)* — rebuild every animal's genome at every tick from `events.jsonl` + `alleles.jsonl` → equals the live state *(OUT-03)*.
- **T-OUT-03** *(T2)* — with a fake API key in the environment → the key appears in no output, log or asset *(OUT-04, V-61)*.
- **T-CFG-01** — every module field → one owner, a unit and a range in its tooltip (reflection over `[SerializeField]`) *(CFG-01)*.
- **T-CFG-02** — a scenario overriding `Prey/Litter/max` and `World/decisionPeriod` → applied, and saved in `run_info.json` *(CFG-02, CFG-03)*.
- **T-CFG-03** — every parameter read by a reference module through a trait when a number gene targets it *(CFG-04, ANIM-16)*.
- **T-CTRL-01** *(T2)* — shuffled control → each decision's query carries the genes of another living animal of the species, drawn from the sampling stream; reproducible *(CTRL-01, CTRL-02)*.
- **T-CTRL-02** *(T2)* — random founders → founder genes are control sentences / random in range; asexual → one parent *(CTRL-01)*.
- **T-RAND-06** *(T2)* — stop at tick 300 and rerun the same command → the first 300 ticks replay with 0 model calls and the same events *(RAND-20, RAND-21)*.

### Editor

- **T-EDIT-01** — every validator V-01…V-62 has a fixture that triggers it and one that doesn't; every message references an object *(EDIT-01, EDIT-02)*.
- **T-EDIT-02** — the runtime assembly has no reference to `UnityEditor` *(EDIT-03)*.
- **T-EDIT-03** *(T0)* — validate every scene in `Scenes/` and every prefab in `Modules/` → no errors *(EDIT-01)*.

## 4. Conformance suites for students' modules

Parameterised tests that run against **every** class of a kind found in the
project, so a new module is tested the moment it exists. They are the "templates"
the recipes of [22](22-extending-recipes.md) refer to.

| Suite | Runs on | Checks |
|---|---|---|
| T-SENSE template | every `Sense` | tokens declared and non-empty; `Read` returns a declared index in 200 random worlds; `Write` is deterministic and non-empty for every token; nothing hidden is reported; batched equals sequential |
| T-ACT template | every `AnimalAction` | searches when alone; intent only (no position or stat of another animal changed); description present; bound gene or V-05; keyword pattern compiles |
| T-ANIM stat template | every module declaring a stat | stays in range for 5 000 ticks; initial values for founders and babies |
| T-ENV layer template | every `ResourceLayer` | consume once; nearest equals brute force; changes only in its phase with its own stream |
| T-DEC brain template | every `Brain` | row shape, sum, order; same query same answer; failure behaviour |
| T-MUT template | every `MutationOperator` | receives only gene and allele; same seed same result; result passes the gene kind's checks or is rejected |
| T-TICK phase template | every `TickPhase` | runs on an empty world; declares what it waits for; doesn't break the hash of an unchanged world when disabled and re-enabled |

## 5. Invariants checked every tick in test worlds

Positions inside the world and walkable; no NaN; stats within their ranges; ids
unique; no dead or killed animal in the lists after the death phase; every action
index valid; every probability row valid; populations ≤ cap after migration and ≥
floor after the floor phase; carcass portions ≥ 0; every genome of the right
length and species.

## 6. Regression tests (T3)

| Id | Test | Rules |
|---|---|---|
| **R-01** | **Pinned hashes**: for each reference scene and seeds 1234, 7, 42, the events hash after 2 000 ticks (keyword brain) is stored in `Tests/Golden/hashes.json`. A change fails the test; re-pinning is a deliberate commit that states why. | RAND-11 |
| **R-02** | **Statistical ranges** over 5 seeds × 5 000 ticks: mean populations, time at floor and cap, deaths by cause, mean lifespan, generations, kills, searching rate, memo hit rate, within the ranges recorded when the reference was accepted (first accepted Unity runs; the prototype's numbers in `Docs/prompt-genome/03` §13 as a sanity check). | ACT-33 |
| **R-03** | **No crashes**: in the Lab 1 full world with the keyword brain, no species needs a newcomer over 10 000 ticks in 8 of 8 seeds. | POP-05 |
| **R-04** | **Equivalences**: Freeze = Responsive; 1 = 10 ticks per frame; rendering on = off; Mono = IL2CPP build (SHOULD). | SPACE-11, SPACE-14, RAND-12 |
| **R-05** | **Replay**: a 500-tick run with a recording `ScriptedBrain` and `FakeMutator` caches, rerun → 0 calls, same hash. | RAND-21, MUT-14, DEC-33 |
| **R-06** | **Soak**: 100 000 ticks, keyword brain, full world → no exception, invariants hold, memory stable (less than 5 % growth after tick 10 000). | — |
| **R-07** | **Performance**: one tick of the full world (340 animals, keyword brain) under 2 ms on the CI machine; 0 bytes allocated per tick in the act phase after warm-up (GC recorder). | 20 §9 |
| **R-08** | **Prototype fixtures** (optional, if the Python exporter is written): situation texts, keyword-brain vectors, mutation cleaning and genome keys for 500 random cases match the prototype's. | SENSE-13, 08 §6, MUT-12 |
| **R-09** | **Analysis tools**: the prototype's `gene_report` and `gene_timeline` read a Unity run written in compatibility mode without error. | OUT-05 |
