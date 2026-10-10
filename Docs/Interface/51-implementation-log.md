# 51 — Implementation log

Details moved out of [50](50-implementation-status.md) to keep it under 200 lines.

## Decisions taken while implementing, M0–M11

- M0 — tests from the command line — the editor is open, so batch mode can't
  run (rule 8); the owner asked to use the `unity` CLI and com.unity.pipeline
  instead. Tests run in the open editor with the commands above.
- M0 — the owner's upgrade to 6000.3.9f1 was uncommitted — committed as
  M0.1 together with the explicit Newtonsoft package, so the lock matches
  the manifest.
- 20 §7 — assemblies — `EvoSim.Testing` is constrained to
  `UNITY_INCLUDE_TESTS` (NUnit for `Stat`), so it never ships in a build;
  Runtime and Http are auto-referenced so student scripts in
  `Assembly-CSharp` can derive from the base classes.
- 22, rule 7 of the prompt — samples — `Assets/EvoSim/Samples/` will get its
  own `EvoSim.Samples` assembly (from M11), so the test assemblies can
  reference the sample modules; 20 §7 doesn't list it.

- RAND-13 — an empty world has no events, so equal hashes for any seed — the events hash chain starts from
  SHA-256 of the seeds: h0 = SHA-256("evosim:seed:worldSeed"), h(i) = SHA-256(h(i-1) ‖ line i).
- RAND-03/04 — which streams the world seed drives — "world" and every "world/<layer>" stream (cover map,
  initial food), so adding a layer never changes another layer's map.
- GENE-01 — duplicate gene labels in a species aren't in the V-catalogue — added V-14 (error).
- SENSE-22 / ACT-05 — who declares "vision" — banded senses declare the trait "vision" (default 20); a species
  without one uses 20 m (WorldQueries.ReferenceVision).
- ACT-10 — reach comparisons — within reach means distance ≤ reach + 1 mm (Units.Epsilon), so an animal that stopped
  at exactly 1 m interacts; the reaches (graze 0.5, strike 1, scavenge 1) are settings of the ActPhase, the
  mate reach of the MatingRule.
- ANIM-30 — when the busy countdown starts — in the tick after the animal became busy (a kill at tick t keeps
  the hunter busy for ticks t+1…t+50; it decides at t+51).
- ANIM-40 — kill cause name — "killed" in events; compatibility mode will write "predator" (OUT-05).
- GENE-22 — "letters" in the allowed characters — ASCII letters only, so "é" fails V-40 (T-GENE-07).
- CORE-03 — Species' default display name — "species" (not "prey") so core code names no species (T-CORE-08).
- 20 §3.6 — action prompt lines — `AnimalAction.DefaultDescription` holds each reference action's line from
  08 §7; the inspector field overrides it when not empty.
- 20 §3.4 — the inspector's "current target" — Animal.TargetId/TargetPoint, set by the act phase (also used by
  the "chased" sense recipe, 22 §13).

- SENSE-22 — per-animal vision in texts — each text states the trait's default vision (one text per token, so
  equal observations give equal texts, SENSE-10); per-animal vision changes the band, not the wording.
- SPEC-20 — threat/prey sense labels — empty label = the species' display name when the set has one species
  ("Predator" for `predator`), so renaming a species changes what the brain reads; else "Predator"/"Prey".
- 15 §2 — the 48 situations — the prototype's own set (prototype/data/observations_v2.jsonl, copied to
  Data/Observations), mapped onto the Unity prey (stamina high, cover none where it had none); the predator's
  48 are energy × prey band × carcass × kin.
- PROMPT-01 — line breaks — the reference texts keep the prototype prompts' hard wraps (JEV was trained on
  them); the 08 §7 example is reproduced word for word (T-PROMPT-01).
- PROMPT-01 — rule order — modules in hierarchy order; the search rule sits on the species' own GameObject (first);
  the carcass line is written by the diet that scavenges, so it comes before the breeding line as in predator_v3.
- T-SENSE-08 — golden file name — Tests/Golden/situations.tsv (tab-separated, easier to review) instead of .json.
- DEC-04 — staggering and per-species periods — an optional DecisionSchedule module.
- MUT-31 — batching rounds — one mutator batch in flight at a time; queued tries (new first tries and redraws)
  go out together when it returns. Results don't depend on batching (tries are drawn at conception).
- MUT-12 — guard order — as the prototype: "invalid" (empty, > maxWords, same as parent ignoring case, forbidden
  characters) before "unchanged" (punctuation only). T-MUT-07 checks against the prototype's own clean().
- REPRO-10 — block cap — the litter is cut to cap − living animals (eggs don't count, REPRO-22).
- 22 — Samples — reference module defaults may name species in tooltips; T-CORE-08 checks Core, Phases, Recording.
- OUT-05 — the prototype's names ("predator" kill cause, "pred_" columns, "<id>s" summary key) — kept in
  Runtime/Compatibility/PrototypeFormat, outside the core folders (T-CORE-08).
- CFG-01 — "range or unit" for numbers — T-CFG-01 accepts Range/Min or a reference value in the tooltip.
- 20 §2 — building scenes while the editor holds an unsaved untitled scene — the builder writes an empty scene file
  and opens it additively (NewScene additive is refused then); the owner's open scene is never touched.
- R-01 — the scripted prey policy — flee a close threat, else eat food in sight, else mate with a ready kin, else
  rest (2 % on the others); hashes pinned from the first run.
- DEC-30 / R-06 — memo memory in long runs — keys stored as 128-bit hashes, identical rows shared, at most
  World.memoCapacity keys (262 144); beyond, queries go to the brain (and its cache) again (40 §2 #11).
- R-07 — per-tick allocations — module look-ups cached per species, species streams cached without building names,
  carcasses pooled, index loops on the act path, spatial cells made once: full world 1.5 ms per tick, no allocation
  in the act phase (Unity's Mono counts no per-thread allocations: R-07 reads the managed heap between two probes).
- Answer cache folder — Library/EvoSim/AnswerCache (gitignored; deleting Library loses it); the file is
  .jsonl per brain ("mutator-<model>" for the mutator), rows stored with round-trip precision.

## P1 audit, phase 1 (2026-10-10): the auditors' details

Four read-only audits (32 §3 P1, items 1–8); the merged table is in 50. Their scripts (rebuild.py, secret_scan.py,
secret_guards.py, the rule maps) stayed in the session's scratch folder.

### Item 1, chapters 01–08

### P1 item 1, chapters 01–08: MUST rules vs their citing tests

Read-only audit, 2026-10-10, branch llm-evolution (working tree as found). Nothing in the repository was changed,
Unity was not launched. Paths relative to the repository root.

Method
- MUST rules extracted from Docs/Interface/01…08 with the regex `\*\*([A-Z]+-\d+) \(MUST\)\*\*`: 116 rules.
- Citations extracted with the exact regex of `CoverageTests.CitedRules` (scratch script `cite.py`, output `citemap.txt`),
  splitting matches that are really test ids (`T-ACT-10` matching `ACT-10`) from real rule citations.
- Every citing test was read; the implementation in Runtime/ (and Http/ where relevant) was read for each rule.
- Accepted deviations (50 "Decisions…"/"Disagreements", 40 §2) were not reported: e.g. ANIM-30 countdown start,
  ACT-10 reach + 1 mm, SENSE-22 stated vision, the self-pinned situations.tsv (40 §2 #2), Stamina/Litter concrete-type
  look-ups (40 §2 #10).
- Verdicts: **real** = the test's assertions would fail if the rule were broken in a plausible way; **weak** = the
  test is cited but its assertions don't (or only partly) check the rule; **none** = nothing checks it.

#### A. Findings (most severe first)

##### A1. CoverageTests counts test ids as rule citations (bug in the M11 gate test)
`Assets/EvoSim/Tests/EditMode/Core/CoverageTests.cs:45`: `\b([A-Z]+)-(\d+)…` matches inside `T-DEC-02`, because `\b`
sits between `-` and `D`. So `Description("T-DEC-02 (DEC-10): …")` (DecisionTests.cs:41) adds **DEC-02** to
`CitedRules()`, `T-ANIM-02 (ANIM-10, …)` adds ANIM-02, `T-ACT-10 (07 §4)` adds ACT-10, and so on.
56 of the 116 rules of 01–08 receive such a spurious credit (ACT-01…06/10, ANIM-01/02/03/10, CORE-01…07/09,
DEC-01/02/03/10/11, ENV-01/02/03, MOVE-01…06, PROMPT-01/02/03/05/06, SENSE-01…06/10, SPACE-01/02/03/04/06/07,
SPEC-01…05). Today each of them also has a genuine citation, so `EveryMustRuleIsCited` isn't wrong yet, but it can
no longer detect a lost citation for half the rules (e.g. drop "DEC-02" from AnimalTests.cs:119 and the gate still
passes thanks to T-DEC-02, a query test).
Fix: `(?<![A-Z]-)\b([A-Z]+)-(\d+)…` (or strip `T-[A-Z]+-\d+` before matching); add a unit test with
"T-DEC-02 (DEC-10)" → {DEC-10}.

##### A2. DEC-02 "a busy animal never decides" — no real test
- Cited only by T-ANIM-06 `BusyAnimals` (AnimalTests.cs:119). Its world (`ActWorld`, AnimalTests.cs:13-14) has only
  ActPhase and DeathPhase: no Sense/Ask/Choose phase, so nothing can decide in it.
- T-DEC-01 `DecisionTicks` (DecisionTests.cs:26) makes an animal busy at tick 1 for 2 ticks, i.e. only on non-decision
  ticks where it already holds an action, so it wouldn't decide anyway.
- Consequence: deleting `|| a.IsBusy` at Runtime/Phases/SensePhase.cs:28 passes every cited test (only the pinned
  R-01 hashes, which don't cite DEC-02, would notice). Same gap for the "doesn't decide" half of ANIM-30.
- Implementation is right (SensePhase.cs:28).
Fix: in T-DEC-01 make an animal busy across tick 4 (e.g. BusyTicks = 6 at t = 2) and assert it is absent from
PerTick(4) and decides once its busy state ends.

##### A3. ACT-04 "nothing to act on → search" — cited tests accept "stay"
- ActionTests.cs:59-60 `Assert.IsTrue(c.Intent.IsStay || c.Intent.Wandering || …)` and :63
  `Assert.AreEqual(a.Searching ? 1 : 0, sp.Counters.Invalid)` (0 == 0 when it stays);
  ConformanceTests.cs:137 `Assert.IsTrue(solo.Searching || ctx.Intent.IsStay, …)`.
- Only hide is positively checked (ActionTests.cs:375 NoCoverModule).
- Replacing `c.Search()` by `c.Stay()` in EatAction.cs:35, FleeAction.cs:42, FollowAction.cs:18, HuntAction.cs:28 or
  MateAction.cs:23 passes both conformance tests. Implementation is right today.
Fix: for actions with `!Rests`, assert `Searching && Intent.Wandering && Counters.Invalid == 1` after 4 ticks alone;
keep the "stays" escape only for actions that declare they never search.

##### A4. ACT-31 "sequential orders act on the state left by earlier animals; simultaneous uses the start state" — weak
- WorldPlayTests.cs:104-117 (T-MOVE-05) asserts only that each order is reproducible and the three hashes differ;
  LongScenarioTests.cs:35-57 (S06) compares kill counts with/without hide.
- 30-tests T-MOVE-05 asks for "sequential orders see earlier moves, simultaneous sees the start state"; nothing does.
  Removing `World.Space.Moved(a)` (ActPhase.cs:158) or interleaving Plan/Move in `ActTogether` (ActPhase.cs:105-120)
  keeps both tests green. Implementation is right today.
Fix: a placed test: A moves away from B in A's turn; in AllMixed/SpeciesInTurn B (acting later) targets A's new
position; in Simultaneous B targets A's start position.

##### A5. ACT-13 "breeding is not an interaction of the mate action" — weak
- ActionTests.cs:217-228 checks `prey.Animals.Count` and `Counters.Births` after 3 ticks in `Eco()`
  (ActionTests.cs:13-17: Act, Death, Environment phases; Lab.PreyBody has no Litter). Births are only counted in
  HatchPhase (HatchPhase.cs:65), which this world lacks, so an egg conceived in the act phase would never show.
Fix: assert `Counters.EggsLaid == 0` right after the act phase, then run the BreedPhase and assert a litter (positive
control, as 30-tests T-ACT-08 says).

##### A6. SPEC-10/SPEC-13/ACT-11 for species added during a run (risk, runtime)
- Diet entries resolve the *named* species' Edible (Diet.cs:95-103) and match clones through `DescendsFrom`
  (EdibleTarget.cs:18); `InteractionResolver.Strike` takes the energy and edibility from that entry
  (InteractionResolver.cs:57-58, 68), while the carcass uses the killed species' own Edible (CarcassSystem.cs:30).
- Input: `var deer = w.AddSpecies(prey, "deer", prey); deer.GetComponent<Edible>().enabled = false;` → deer stays in
  `PreyOf(predator)` and is struck for prey's 60 energy, although "without an edible component, nothing can eat
  that thing" (SPEC-10). Any change of the clone's Edible energy is likewise ignored by strikes.
- Not reachable with the reference content (no speciation rule yet, 40 §3), hence a risk.
Fix: in `Striking(s)` use `s.GetComponent<Edible>()` of the struck species (and its energy), or re-resolve the entry
per species.

##### A7. ACT-05 vs the kin sense's partnerRange (risk, runtime)
- NearestAnimalSense reports readiness only within `partnerRange` (NearestAnimalSense.cs:100), but MateAction targets
  `NearestReadyKin` within the full vision (MateAction.cs:22 → WorldQueries.cs:74-78).
- Input (the T-SENSE-06 setup, SenseTests.cs:144 with partnerRange 4): a ready partner at 15 m is written without
  readiness, yet `mate` walks to it — the action targets something the senses can't report (ACT-05). Equal in the
  reference (partnerRange = vision = 20).
Fix: have the kin sense expose its partner range and let MateAction (or WorldQueries) use it; or validate
partnerRange == vision.

##### A8. ANIM-17 "traits don't change during life unless declared changeable" — vacuous test, unenforced
- AnimalTests.cs:41 builds a world with only a DeathPhase, so nothing that could touch traits runs in the 1 000 ticks.
- `Animal.SetTrait` (Animal.cs:63) never reads `TraitId.Changeable` (TraitId.cs:10; the flag is read nowhere); tests
  themselves change a non-changeable trait during life (ActionTests.cs:206). No runtime module does it today.
Fix: make SetTrait refuse (or warn) for non-changeable traits outside creation; run T-ANIM-03 with the reference
phases.

##### A9. ANIM-35 "babies born that tick included" — untested
- AnimalTests.cs:161-170 places an animal *between* ticks (Place.Animal before Advance) and checks age 1. No test runs
  Breed/Hatch then Death in one tick and checks a baby's age (T-REPRO-07, ReproductionTests.cs:160-168, stops before
  the death phase). DeathPhase.cs ages them correctly today.
Fix: in T-ANIM-08 breed with incubation 0 through the reference phases and assert the new babies have age 1 at the
end of their birth tick.

##### A10. CORE-05 "no hidden per-animal state in modules" — test checks a different rule
- RandomTests.cs:131-147 (T-CORE-09) proves two worlds share no static state (ARCH-07). A module keeping
  `Dictionary<int, float>` keyed by animal id would pass. (A grep of Runtime/Http/Samples found no such state, so
  the code is fine.)
Fix: a reflection/static check over SpeciesModule/WorldService fields typed with Animal or keyed by animal id, or a
conformance step "two animals swapped between worlds behave identically".

##### A11. SENSE-04 "all deciders sense the start-of-tick state" — weak
- SenseTests.cs:84-99 reads a frozen state twice and in reverse order; it never runs a tick, so it can't see a sense
  read after something moved (e.g. a SensePhase placed after ActPhase would pass). The ordering is enforced only
  indirectly (V-08 puts Ask after Sense, not Sense before Act).
Fix: run the reference phases one tick with a ScriptedBrain recording `q.Observation`, and compare with observations
recomputed from a snapshot taken before the tick.

##### A12. ANIM-31 "the reason is logged" (doc)
- The reason is set (Digestion.cs:24), shown (Editor/AnimalInspectorWindow.cs:47) and cleared (ActPhase.cs:178), but
  never logged: no event, no log line; 13 defines no busy event. T-ANIM-07 (AnimalTests.cs:156) checks the field only.
Fix: either reword ANIM-31 ("kept on the animal record and shown in the inspector") or add a `busy` field to an event
(changes R-01 hashes, so it needs the owner).

##### A13. SPACE-04, SPACE-06 — real checks exist but are not the cited ones (doc)
- SPACE-04 is cited only by T-SPACE-03 (MovementTests.cs:35-56) on a FlatGround that is walkable everywhere inside;
  non-walkable points are exercised by ConformanceTests.cs:262 (PondGround) and S17 (LongScenarioTests.cs:147,
  SceneTests TerrainLocomotion), which cite MOVE-02/SPACE-05 only.
- SPACE-06 is cited only by T-SPACE-02 (EnvironmentTests.cs:11-15), which checks that height doesn't count in
  distances; "height follows the ground" is asserted only as `y > 1f` in T-MOVE-07 (MovementTests.cs:204), which
  cites MOVE-07.
Fix: add SPACE-04 to the PondGround/S17 descriptions; assert `a.Position.y == ground.Height(a.Position)` in T-MOVE-07
and cite SPACE-06 there.

##### A14. SENSE-10…13 rest on a self-pinned snapshot; the independent check cites nothing (doc)
- T-SENSE-08 (SenseTests.cs:177-206) compares with Tests/Golden/situations.tsv, which Golden.Check writes from the
  current output when missing (Golden.cs:26-32); only "no 'cell'" (SENSE-13) is asserted independently.
- The word-for-word check of the 06 §2 examples is SenseTests.cs:208 `ReferenceTexts`, whose description cites no rule.
Fix: cite SENSE-10/11/12/13 in `ReferenceTexts`; optionally make Golden.Check fail on a missing file under CI.

##### A15. Misleading citations (doc)
- ENV-12 in S06 (LongScenarioTests.cs:35): the flee-only world still has its cover layer (only the hide action and
  cover sense are removed, :45-46). Real ENV-12 tests: ActionTests.cs:367, SenseTests.cs:281-284.
- ACT-02 in the T-ACT template (ConformanceTests.cs:96): it never checks the per-tick target look-up. Real: T-ACT-02.
- SPACE-13 is cited only by probe tests (TickLoopTests.cs:49, TickLoopPlayTests.cs:54 with DelayedPhase); the tests
  that wait on a real (scripted/HTTP) brain — R-04 RegressionTests.cs:179 and HttpBrainTests.cs:286 — cite
  SPACE-11/14 but not SPACE-13.
- ACT-20's static check scans only Runtime/Actions (ActionTests.cs:233); Samples/DragAction.cs, DrinkAction.cs and
  student actions are outside it (none reads genes today).

#### B. Every MUST rule of 01–08

Format: rule — citing test(s) (file:line, method) — verdict — note. "(TID)" = also credited by a test-id match only.

##### 01
- CORE-01 — PopulationTests.cs:42 MigrationIgnoresGenes — real (narrow: the one selection rule ignores genes/energy) — no "fitness" anywhere in Runtime/Http (grep).
- CORE-02 — DiscoveryTests.cs:135 TestModulesAreDiscovered — real — no test brain in it (ScriptedBrain lives in EvoSim.Testing, used everywhere).
- CORE-03 — StaticChecks.cs:26 CoreNamesNoSpecies — real (scope Core/Phases/Recording literals).
- CORE-04 — WorldPlayTests.cs:141 ManyShapesOfWorld — real; module limits via T-DEC-05.
- CORE-05 — RandomTests.cs:131 WorldsDontShareState — **weak** (A10).
- CORE-06 — DecisionTests.cs:239 DecidingChangesNothing — real.
- CORE-07 — MutationTests.cs:138 BlindPrompts; HttpMutatorTests.cs:37 OllamaMutatorRequest — real.
- CORE-09 — RandomTests.cs:95 SameSeedSameHash; TickLoopPlayTests.cs:65 — real.

##### 02
- SPACE-01 — MovementTests.cs:20 PositionsAreContinuous — real.
- SPACE-02 — EnvironmentTests.cs:11 DistancesAreHorizontalAndReplaceable — real (layers, index, entities).
- SPACE-03 — MovementTests.cs:35 AnimalsStayInside — real for animals (entities not checked).
- SPACE-04 — MovementTests.cs:35 — **weak as cited** (flat ground); real in uncited ConformanceTests.cs:262, S17 (A13).
- SPACE-06 — EnvironmentTests.cs:11 — **weak as cited** (only the distance half); y-follow only in MovementTests.cs:204 (A13).
- SPACE-07 — EnvironmentTests.cs:40 TiesAreBrokenByAFixedKey — real (viewer at distance 0 is never returned).
- SPACE-08 — EnvironmentTests.cs:40 — real (animals, cells, entities).
- SPACE-10 — TickLoopPlayTests.cs:36 — real (implicit: integer ticks from 0 throughout; the test itself is SPACE-11).
- SPACE-11 — TickLoopTests.cs:115; RegressionTests.cs:179 (T3); TickLoopPlayTests.cs:36 — real.
- SPACE-12 — ViewPlayTests.cs:56 ViewsNeverChangeTheRun — real.
- SPACE-13 — TickLoopTests.cs:49; TickLoopPlayTests.cs:54 — real for the tick loop, via a probe phase (A15).

##### 03
- ENV-01 — EnvironmentTests.cs:69 FoodQueriesMatchBruteForce; ConformanceTests.cs:172 — real.
- ENV-02 — ActionTests.cs:329 ContestedItems; ConformanceTests.cs:172 — real.
- ENV-03 — EnvironmentTests.cs:103 RegrowthFollowsItsProbability; ConformanceTests.cs:172 — real.
- ENV-10 — EnvironmentTests.cs:141 CoverQueriesMatchBruteForce — real.
- ENV-11 — ActionTests.cs:348 CoverHidesFromThreats — real (+ SenseTests.cs:265).
- ENV-12 — ActionTests.cs:367 NoCoverModule; LongScenarioTests.cs:35 (misleading, A15) — real.
- ENV-20 — ActionTests.cs:380; EnvironmentTests.cs:169 — real (id, position, lifetime; nearest query in T-SPACE-02/04).
- ENV-22 — same two — real.
- ENV-23 — ActionTests.cs:292 EggEaters — real.

##### 04
- SPEC-01 — PromptTests.cs:162 NamesInTexts — real (id uniqueness via V-07 in DiscoveryTests.cs:110, uncited).
- SPEC-02 — DiscoveryTests.cs:86 — real (≥ 1 action via V-04 in DiscoveryTests.cs:110, uncited).
- SPEC-03 — DiscoveryTests.cs:86 — real.
- SPEC-04 — ReproductionTests.cs:252 GenomesNeverCrossSpecies — real.
- SPEC-05 — DiscoveryTests.cs:12 NestedSpeciesIsAnError — real.
- SPEC-10 — ActionTests.cs:292; FoodWebTests.cs:17; SceneTests S09; LongScenarioTests.cs:65; ScenarioTests S28 — real (runtime-species risk A6).
- SPEC-15 — ActionTests.cs:292; FoodWebTests.cs:47 — real ("no diet eats nothing" only via V-22).
- SPEC-11 — ActionTests.cs:251; FoodWebTests.cs:35; LongScenarioTests.cs:65 — real.
- SPEC-12 — FoodWebTests.cs:17; LongScenarioTests.cs:65; ScenarioTests.cs:105 — real.
- SPEC-13 — ActionTests.cs:275; FoodWebTests.cs:47; LongScenarioTests.cs:65 — real (A6).
- SPEC-20 — PromptTests.cs:162; SceneTests S09 — real.
- SPEC-30 — WorldPlayTests.cs:20 SpeciesAddedDuringARun — real.
- SPEC-31 — WorldPlayTests.cs:20 — real for template == parent (A6).
- SPEC-32 — WorldPlayTests.cs:44 ExtinctIdsAreNeverReused — real.

##### 05
- ANIM-01 — ReproductionTests.cs:266 AnimalIds — real (structural; fields are compile-time).
- ANIM-02 — ReproductionTests.cs:266 — real, partial: uniqueness over living animals only, order per species list.
- ANIM-03 — ReproductionTests.cs:266 — real (Animal.Species is readonly).
- ANIM-10 — AnimalTests.cs:21; ConformanceTests.cs:148 — real.
- ANIM-11 — AnimalTests.cs:21 — real.
- ANIM-15 — AnimalTests.cs:38 — real.
- ANIM-16 — AnimalTests.cs:38; ConfigTests.cs:66 TraitsDriveBehaviour — real.
- ANIM-17 — AnimalTests.cs:38 — **weak** (A8).
- ANIM-20 — MovementTests.cs:151; AnimalTests.cs:64 EnergyTable — real.
- ANIM-21 — AnimalTests.cs:64, :98 — real.
- ANIM-22 — AnimalTests.cs:21, :64; ConformanceTests.cs:148 — real.
- ANIM-30 — AnimalTests.cs:119 BusyAnimals — real for move/cost/stamina/action cleared; **"doesn't decide" untested** (A2).
- ANIM-31 — AnimalTests.cs:142 Digestion — **weak** (field only; "logged" not implemented, A12).
- ANIM-35 — AnimalTests.cs:161 AgeAndMaturity — **weak** for "babies born that tick" (A9).
- ANIM-36 — AnimalTests.cs:161; ConfigTests.cs:66 (maturity trait) — real.
- ANIM-40 — AnimalTests.cs:178; PopulationTests.cs:112; ScenarioTests.cs:39 (T3) — real.
- ANIM-41 — AnimalTests.cs:202 KilledAnimalsAreOutOfTheTick — real (sensing: SenseTests.cs:265; breeding: ReproductionTests.cs:212).
- ANIM-42 — AnimalTests.cs:178 — real, partial (cause, killer, once; age/gen/meals/offspring written at World.Deaths.cs:16-21 but not asserted).
- ANIM-43 — AnimalTests.cs:202 — real.

##### 06
- SENSE-01 — ConformanceTests.cs:59; SenseTests.cs:24 — real.
- SENSE-02 — SenseTests.cs:69 ObservationsAreTuples — real (thin: both animals see nothing).
- SENSE-03 — SenseTests.cs:84 — real (no reference sense draws randomness).
- SENSE-04 — SenseTests.cs:84 — **weak** (A11).
- SENSE-05 — SenseTests.cs:160 ObservationSpace — real.
- SENSE-06 — SenseTests.cs:24 (cites it, checks only token range); SenseTests.cs:265 HiddenThingsAreNeverSensed — real (used-up entities only via EntitySystem.Nearest code, not asserted).
- SENSE-10 — ConformanceTests.cs:59; SenseTests.cs:177 — real (determinism) / snapshot self-pinned (A14).
- SENSE-11 — SenseTests.cs:177 — real-ish via snapshot; word-for-word in uncited ReferenceTexts (A14).
- SENSE-12 — SenseTests.cs:177 — **weak as cited** (self-pinned); real in uncited SenseTests.cs:208 (A14).
- SENSE-13 — SenseTests.cs:177 — real ("cell" never appears).
- SENSE-20 — SenseTests.cs:101 BandBoundaries — real.
- SENSE-21 — SenseTests.cs:125 BandEdgesAreValidated — real.
- SENSE-31 — SenseTests.cs:230 BatchedEqualsSequential — real.
- SENSE-41 — DecisionTests.cs:266 Attachments — real.

##### 07
- ACT-01 — ActionTests.cs:26 ActionConformance — real.
- ACT-02 — ActionTests.cs:66 TargetsAreLookedUpEachTick — real; ConformanceTests.cs:96 cites it without checking (A15).
- ACT-03 — ActionTests.cs:86 ReferenceIntents — real.
- ACT-04 — ActionTests.cs:26; ConformanceTests.cs:96 — **weak** (A3).
- ACT-05 — ActionTests.cs:348 CoverHidesFromThreats — real for hidden/killed/self; vision bound implicit; partnerRange risk (A7).
- ACT-06 — ActionTests.cs:26; ConformanceTests.cs:96 — real.
- MOVE-01 — MovementTests.cs:170 TheLocomotionDecides — real ("exactly one" via V-13, ActPhase.cs:216, catalogue test).
- MOVE-02 — MovementTests.cs:35; ConformanceTests.cs:262; LongScenarioTests.cs:147 — real (note: ActPhase.cs:149-154 moves a stray animal back, so the property is enforced centrally).
- MOVE-03 — MovementTests.cs:58; AnimalTests.cs:98; ConformanceTests.cs:262 — real (traits: ConfigTests.cs:66).
- MOVE-04 — ActionTests.cs:122; MovementTests.cs:58, :82 — real.
- MOVE-05 — MovementTests.cs:115 — real.
- MOVE-06 — MovementTests.cs:151, :170 — real.
- ACT-10 — ActionTests.cs:122, :135 — real.
- ACT-11 — ActionTests.cs:171 StrikeOutcomes; ScenarioTests.cs:105 — real (A6 for runtime species).
- ACT-12 — ActionTests.cs:200 OneStrikePerTick — real.
- ACT-13 — ActionTests.cs:217 MatingIsNotAnInteraction — **weak** (A5).
- ACT-20 — ActionTests.cs:230 ActionsDontReadGenes — real for Runtime/Actions only (A15).
- ACT-30 — ActionTests.cs:329; WorldPlayTests.cs:104 — real (orders exist and differ; contested items).
- ACT-31 — LongScenarioTests.cs:35; WorldPlayTests.cs:104 — **weak** (A4).
- ACT-32 — WorldPlayTests.cs:104 — real (same seed, same order/hash).

##### 08
- DEC-01 — AnimalTests.cs:119 (no decide phase); DecisionTests.cs:16 DecisionTicks; ScenarioTests.cs:197 — real (tick 4 itself not asserted).
- DEC-02 — AnimalTests.cs:119 (+TID DecisionTests.cs:41) — **none** (A2).
- DEC-03 — DecisionTests.cs:16 — real, partial (asserts BredThisPeriod cleared; Searching is set at :29 but never asserted).
- DEC-10 — DecisionTests.cs:41 — real.
- DEC-11 — DecisionTests.cs:64 BrainConformance — real.
- DEC-12 — DecisionTests.cs:64; HttpBrainTests.cs:286 — real.
- DEC-13 — DecisionTests.cs:105 BrainsPerSpecies — real.
- DEC-14 — DecisionTests.cs:124 BrainLimits (+ :266 for attachments) — real.
- DEC-20 — DecisionTests.cs:141 SamplingTemperature — real (stream use in ChooseActionsPhase.cs:54).
- DEC-30 — DecisionTests.cs:157 MemoAndDuplicates — real.
- DEC-31 — DecisionTests.cs:181 — real.
- DEC-32 — DecisionTests.cs:157 — real.
- DEC-34 — DecisionTests.cs:208; HttpBrainTests.cs:197 — real.
- DEC-40 — DecisionTests.cs:208; HttpBrainTests.cs:197 — real (resume covered by RAND-20/21 tests).
- DEC-41 — HttpBrainTests.cs:177 PointsRepair — real.
- PROMPT-01 — PromptTests.cs:63 AssembledPrompt — real (08 §7 example word for word).
- PROMPT-02 — PromptTests.cs:63 — real.
- PROMPT-03 — PromptTests.cs:73 NumbersComeFromSettings — real.
- PROMPT-05 — PromptTests.cs:110 PromptIds — real.
- PROMPT-06 — EditorToolTests.cs:46 PreviewEqualsWhatTheBrainReceives — real for the preview; the token estimate (SpeciesInspector.cs:155) is shown, not asserted.
- PROMPT-07 — PromptTests.cs:124 — real (scans only base `Brain` fields; OllamaPointsBrain/JevBrain keep the instruction as a const/override, so fine).

#### C. Summary
- No real test: **DEC-02**.
- Only weak tests (or the rule's key clause untested): ACT-04, ACT-13, ACT-31, ANIM-17, ANIM-31 (logging), ANIM-35
  (babies), ANIM-30 (no-decide half), CORE-05, SENSE-04.
- Real only through uncited tests: SPACE-04, SPACE-06, SENSE-12.
- Runtime contradictions found: none in the reference configuration; two risks outside it (A6 species added during a
  run with a different Edible; A7 partnerRange < vision) and one unenforced rule (A8 SetTrait).
- Scratch files: cite.py / citemap.txt (01–08), cite_all.py / citemap_all.txt (01–22: 192 rules, all cited, none only
  through a test id).

### Item 1, chapters 09–22

### P1 item 1, chapters 09–22: MUST rules versus their tests

Scope: the 77 `**XXX-NN (MUST)**` rules of 09, 10, 11, 12, 13, 14, 15 and 21 (20 and 22 have no rule in that form;
the ARCH rules are written without "(MUST)"). This was a read-only pass: no Unity run, and every test is assumed to
pass (it does). Paths are relative to `Assets/EvoSim/` unless they start with `Docs/`.

Rule-to-test map: `rulemap.txt`, produced by `map_rules2.py` with the same regex as `Tests/EditMode/Core/CoverageTests.cs`.
`[T-id only]` marks a match that comes only from a test id such as "T-GENE-03".

Verdicts: **real** means the test checks the rule. **partial** means one named part of the rule is unchecked.
**weak** means the citing test can't catch a plausible violation.

#### Findings (most severe first)

1. **RAND-20, bug (strict mutator only).** `Runtime/Mutation/MutatorService.cs:128` calls `World.Stop("mutator failure")`
   from `Answer()`, which runs inside `Pump()`. `HatchPhase.WaitsFor`/`JobsPending.IsDone` calls `Pump()` during the
   Hatch phase. At that point `World.Advance` (`Runtime/Core/World.cs:459-460`) reaches `if (stopped) break;`, so the tick ends after
   Act and Breed have already moved animals, charged the parents and laid the eggs, and before Hatch, Deaths and Record run.
   `World.Stop`'s own contract (`World.cs:238-241`) says it is called "at a tick boundary, or inside a tick before
   anything changed the world". Concrete input: `MutatorService.Strict = true`, a `FakeHttpTransport` failing `/api/chat`
   (as in `HttpMutatorTests.FailingMutator`), and two ready adults choosing mate. The run stops at tick 0, mid-tick.
   `final_population.json` then shows the parents with 40 energy less and no babies, and `events.jsonl` has no births.
   No test covers strict mutator failure.
   *Fix*: in strict mode, record the failure and call `RequestStop` (stop at the next boundary). Let the eggs hatch
   unmutated or carry them over. Add a test: strict, failing mutator → `Tick` = 1 and the outputs are consistent.
   *Doc*: RAND-20 also lists "an exception" among the stops at a tick boundary, but an exception in a phase must
   stop mid-tick (`World.cs:464-468`; `TickLoopTests.ExceptionsStopTheRun` accepts this).

2. **MUT-03 / MUT-05 (and MUT-04), bug for number genes.** `GaussianMutation.cs:18` clamps to `[min, max]`.
   Take a parent at the range edge, for example 120 with range [20, 120]. Every draw with N ≥ 0 returns exactly 120.
   `HatchPhase.cs:46` then `Register`s 120 and gets the **parent allele back** (GENE-10). It still does
   `Successes++` (line 48) and writes a mutation `{parent: X, child: X, operator: gauss}` to the birth event (49-56).
   About half the "successful" mutations of a gene sitting at its edge are no-ops, which is the end state 09 §5 predicts.
   MUT-03 says a success "registers a new allele". MUT-04 says an unchanged allele is a failure and is counted.
   T-MUT-09 (`MutationTests.cs:202`) only checks the range.
   *Fix*: in `HatchPhase.Hatch`, treat `allele == m.Parent` as a failure (`Mutations.Reject("unchanged")`). Add a test:
   parent 120, σ 5, 1 000 tries → no event with parent == child.

3. **OUT-04, weak (the test can't fail).** `Tests/PlayMode/OutputPlayTests.cs:99-114` (T-OUT-03) sets `OLLAMA_API_KEY`.
   The world it builds is `Lab1`, which has a RandomBrain and a FakeMutator (`Testing/Lab.cs:186-194`). No runtime code
   reads `OLLAMA_API_KEY`, and the HTTP key variable defaults to "" (`Http/HttpBrain.cs:20`,
   `Http/HttpMutatorClient.cs:19`). The secret never enters the process's code paths, so it can't leak. The asset scan
   looks for a string the test made up a moment earlier, and logs aren't checked at all. Only the configuration half of
   OUT-04 is really tested (V-61, `HttpBrainTests.KeysOnlyInEnvironment`).
   *Fix*: use an Ollama brain and mutator with `apiKeyVariable = "OLLAMA_API_KEY"` over `FakeHttpTransport`. Capture
   `Application.logMessageReceived`, including HTTP failures (whose messages include the host), and check both the
   run files and the logs.

4. **EDIT-01, partial.** The CI half ("an EditMode test validates every scene **and module prefab**", also T-EDIT-03) only
   validates `Scenes/*.unity` (`Tests/EditMode/Regression/RegressionTests.cs:215-231`). `Modules/` holds 19 prefabs.
   `Modules/Genes/StaminaGene.prefab` and `Modules/Senses/Carcass.prefab` are used by no scene (their GUIDs aren't in any
   `.unity`), so nothing ever validates them. `EditorToolTests.EveryMenuModuleExists` (`EditorToolTests.cs:36-44`) says
   it "validates inside a new species", but it only asserts that `AddModule(...)` isn't null and never calls `Prepare`.
   *Fix*: make that test call `Prepare` on the built world and assert no errors, or add a T-EDIT-03 loop over
   `Modules/**/*.prefab`.

5. **MUT-20 (S13 citation), test bug: vacuous loops.** `Tests/EditMode/Scenarios/ScenarioTests.cs:156` and `:167` filter
   alleles with `Origin == "mutation"`. Mutants are registered with origin `"mutant"` (`HatchPhase.cs:46`), and no code
   uses "mutation" as an origin (grep). So the intensity-ladder check (MUT-21) and the "no operator: no new allele" check
   assert nothing. MUT-20 still has a real test (T-MUT-09).
   *Fix*: filter on `"mutant"` and assert that the filtered set isn't empty (ladder) or is empty (none).

6. **Coverage gate (all rules), risk.** There are two flaws in `Tests/EditMode/Core/CoverageTests.cs`:
   - The rule regex (line 28) `\*\*([A-Z]+-\d+) \(MUST\)\*\*` misses `**TICK-04 (MUST) The reference phase list.**`
     (`Docs/Interface/12-tick-order-and-determinism.md:14`, the only rule written that way). TICK-04 is never checked.
   - The citation regex (line 45) `\b([A-Z]+)-(\d+)` also matches inside test ids, so "T-GENE-03" counts as citing
     GENE-03, "T-MUT-05" as MUT-05, and so on. Today every rule also has a genuine citation (checked over all 193
     rules: none is cited only through a test id), so nothing is hidden yet. But dropping, say, "GENE-01…04" from
     `GeneTests.cs:11` would leave the gate green.
   *Fix*: use `(?<![A-Z]-)` before the rule id, and allow text after `(MUST)` (or write TICK-04 like the others).

7. **REPRO-24, partial.** The record half ("records a birth event with the litter size and the mutations") is checked
   for `mutations` (T-MUT-03), but no test reads `litter` or `laid` (grep over Tests: no `"litter"` or `"laid"` key).
   `HatchingWaitsForTheMutator` (`MutationTests.cs:235-252`) only asserts `WaitCount` and `Hatched > 0`.
   *Fix*: with incubation 2, assert `litter` equals the eggs of that conception and `laid` equals the conception tick.

8. **REPRO-03, weak.** `ReproductionTests.cs:68-86` puts a, b and c at x = 10, 10.4 and 10.8. For a, the nearest partner
   is also the lowest-id one, so an implementation that took "the first ready partner by id" (the prototype's rule,
   40 §4) passes too. Ties by id are never tested. The implementation (`BreedPhase.cs:41-53`) is correct.
   *Fix*: a at 10, b at 10.9, c at 10.4 → a must pair with c. Add an equidistant pair for the tie rule.

9. **GENE-13, bug (non-default setting).** `Genome.cs:44` hashes `Value.Canonical()`, which uses the default 4 decimals.
   The registry compares at `gene.Decimals` (`AlleleRegistry.cs:27`; `NumberGene.decimals` can be 0–8). Concrete input:
   a NumberGene with decimals = 1. Run A registers 60.04 first and run B registers 60.01 first. By GENE-10 both runs hold
   one and the same allele, but Key and BrainKey hash "60.0400" in A and "60.0100" in B, so the same genes get different
   keys across runs. T-GENE-05 covers only text genes.
   *Fix*: `Canonical(Species.Genes[i].Decimals)`.

10. **GENE-06, risk.** `Species.CreateAnimal` (`Runtime/Core/Species.cs:214-236`) never checks `genome.Species == this`.
    For example, `Place.Animal(predator, p, preyGenome)` (`Testing/Place.cs:12-27`) loops over the predator's 4 genes,
    and the prey genome has 6, so a predator is created carrying a prey genome with no error. T-SPEC-02
    (`ReproductionTests.cs:252`) only tests the `Genome` constructor and crossover. No runtime path does this today.
    *Fix*: throw in `CreateAnimal` and add that case to T-SPEC-02.

11. **RAND-05, weak.** T-RAND-04 (`DecisionTests.cs:251-264`) pre-fills the memo with keys "k0…k199" that are never
    looked up. It varies no other dictionary or set, so iterating, for example, `World.FoodWeb`'s
    `Dictionary<Species, …>` (`World.FoodWeb.cs:8-10`) or `AnswerCache.files` would go unnoticed. The rule holds today:
    a grep found no `foreach` over a Dictionary or HashSet on a decision path; Recording iterates only SortedDictionary.
    *Fix*: a static check, in the style of T-RAND-01, that rejects `foreach` over Dictionary or HashSet fields in
    Runtime/Phases, Core, Reproduction and so on.

12. **EDIT-02, partial (risk).** The "offers a fix button where possible" half has no test, and most of it is missing.
    The code creates 8 `ValidationFix` objects: V-01, V-06, V-12, V-13, V-30, V-31, V-41 and V-43 (`TextGene.cs:77,79`,
    `Species.cs:157`, `World.cs:311,399`, `BandedSense.cs:84`, `LevelSense.cs:58`, `ActPhase.cs:217`). The catalogue
    lists 25 fixes (21 §2), including V-04 "add Rest", V-24 "add an Edible", V-35/V-36/V-46 "clamp" and V-61 "move to an
    environment variable". `EachValidatorFires` only asserts `m.Object != null`.
    *Fix*: implement the easy fixes, and assert `m.Fix != null` for codes whose catalogue row offers one.

13. **GENE-03, weak citing test.** `GeneTests.GeneKinds` (`GeneTests.cs:33-34`) asserts only that `lines.Count == 1` after
    `Express("Never rest.")`, not that the line is ("rest", "Never rest."). The genes-block content is checked by
    T-PROMPT-01 (`PromptTests.cs:63`), which doesn't cite GENE-03.
    *Fix*: assert the key and value of the pair.

14. **OUT-02, doc.** OUT-02 says every event has `id` (the animal), but 13 §3 gives `species_created` no id, and the code
    follows the table (`World.Species.cs:33`, id −1 is omitted). `egg_lost.id` is an egg id from a separate counter
    (`EntitySystem.cs:21`), so it can equal an animal id. T-OUT-01 exempts `species_created`
    (`OutputPlayTests.cs:46`).
    *Fix*: "every event about an animal has `id`; egg events carry the egg's id".

15. **GENE-12, doc.** "`llm#3` = instruction 3 of the deck" conflicts with the deck listed 1–7 in 10 §2. Unity
    (`MutatorService.cs:138`, `HatchPhase.cs:53`) and the prototype (`prototype/promptevo/evolution/mutation.py:125`) use
    0-based indices, and T-MUT-03 accepts `prompt` in 0–6.
    *Fix*: write "the 0-based index into the deck".

##### Smaller gaps (details only)
- **GENE-32, partial.** T-GENE-08 checks that four consecutive `Cross` calls differ. Nothing checks the litter path, that
  is, that `BreedPhase` calls `Cross` per baby. It does (`BreedPhase.cs:88-92`).
- **REPRO-20, partial.** Eggs are checked to exist and to hatch on time. Their contents (parents, generation, pending
  mutations, conception and hatch ticks) and their position at the first parent are never asserted.
  `BreedPhase.cs:93-98` is correct.
- **REPRO-22, partial.** "In conception order" is not asserted. `EggSystem.Due` iterates in insertion order, so it is
  correct.
- **MUT-13, partial.** `RejectedEverywhere` checks 5 tries but not that each one uses a new instruction draw and a new
  seed. FakeMutator's prompts and seeds are available to assert this.
- **MUT-30, partial.** T-MUT-10 delays whole batches. "Answers delivered in a different order" (30-tests) isn't tested.
  The service maps answers by index, so it is correct.
- **MUT-14.** No test checks that a different temperature or model misses the cache. The key includes both
  (`MutatorRequest.cs:17`).
- **RAND-03.** The world-level "adding a species doesn't change the others' draws" is shown only at the stream level
  (`RandomTests.StreamsAreIndependent`). T-SPEC-06 compares only the ticks before the species is added.
- **RAND-01.** The static scan covers Runtime and Http only (`StaticChecks.cs:12`), not Samples or students' scripts
  outside EvoSim. No violation was found in Samples.
- **RAND-20.** The wall-clock limit has no test.
- **CTRL-01.** No test checks that the controls are recorded in run_info.json (`RunInfo.cs:33-38` writes them). C4 number
  genes (uniform in range) have no test. In S05 (`ScenarioTests.cs:96-100`), the C3 part computes `own` and never uses
  it, so it doesn't check "queries carry other animals' genes". T-CTRL-01 covers that properly.
- **EDIT-03.** The test checks only the EvoSim.Runtime assembly, not EvoSim.Http or EvoSim.Samples. A grep found no
  `UnityEditor` in any of them.
- **OUT-01.** The "own folder" suffix (`RunRecorder.cs:70`) is never exercised by running twice under the same name.
- **TICK-02.** The "changes only what it is responsible for" half is untested (hard to test in general).
- **TeachingPathGrading and conformance tests** (GENE-30, MUT-20/30, REPRO-10, TICK-01) are extra citations. They are
  real but shallow. For example, `PhaseConformance` disables and re-enables a phase before `Initialize`, which can't
  change anything.
- **POP-01 block, doc (accepted decision).** "Litters cut to cap − living, eggs don't count" (50 decisions) means several
  conceptions in one tick each see the same places left. Example: cap 10, 8 ready animals in 4 pairs, incubation 0 →
  4 × 2 = 8 eggs → 16 animals after hatching. 30 §5 checks "≤ cap" only for migrate. POP-01's wording ("litters are cut
  to the places left") suggests otherwise, so it would help to state the overshoot.

#### Every MUST rule of 09–22, its citing tests and a verdict

| Rule | Citing tests (file:line, method) | Verdict |
|---|---|---|
| GENE-01 | Genes/GeneTests.cs:11 GeneKinds; V-14 fixture ValidationCatalogueTests.cs:69 | real |
| GENE-02 | GeneTests.cs:11 GeneKinds, :41 RegistryCollapsesEqualValues; accepting operators via MutationTests.cs:50 (uncited) | real (spread over 3 tests) |
| GENE-03 | GeneTests.cs:11 GeneKinds | weak (count only; content in T-PROMPT-01) — #13 |
| GENE-04 | Animals/AnimalTests.cs:38 NumberGenesSetTraits; GeneTests.cs:11 | real |
| GENE-05 | Core/DiscoveryTests.cs:60 GenesBindToTheNearestAction | real |
| GENE-06 | Reproduction/ReproductionTests.cs:252 GenomesNeverCrossSpecies | real for the constructor; risk in CreateAnimal — #10 |
| GENE-10 | GeneTests.cs:41 | real |
| GENE-11 | GeneTests.cs:58 AlleleIds | real |
| GENE-12 | GeneTests.cs:68 Lineage (round trip only); MutationTests.cs:72 MutationsAreRecorded (pipeline) | real; doc on llm#k — #15 |
| GENE-13 | GeneTests.cs:88 GenomeKeysDependOnValues (text only) | real for text; bug for number precision — #9 |
| GENE-20 | GeneTests.cs:103 FoundersAreUniform (χ²) | real |
| GENE-24 | GeneTests.cs:148 NeutralSettings; MutationTests.cs:254 NeutralCanBeFrozen | real |
| GENE-21 | GeneTests.cs:137 FounderPoolsAreFrozen | real |
| GENE-22 | GeneTests.cs:122 FounderGuards (+ V-40 fixture) | real |
| GENE-30 | ReproductionTests.cs:227 UniformCrossoverIsFair; TeachingPathGradingTests.cs:77 | real |
| GENE-31 | ReproductionTests.cs:88 Asexual | real |
| GENE-32 | ReproductionTests.cs:227 (siblings = 4 Cross calls) | partial (litter path unchecked) |
| MUT-01 | Mutation/MutationTests.cs:27 MutationRate | real |
| MUT-02 | MutationTests.cs:50 OperatorChoice | real |
| MUT-03 | MutationTests.cs:72 MutationsAreRecorded | real for text; bug for number no-ops — #2 |
| MUT-04 | MutationTests.cs:102 RejectedEverywhere; Http/HttpMutatorTests.cs:88 FailingMutator | real (no-op success: #2) |
| MUT-05 | MutationTests.cs:102 (attempts/successes/rejections/failures); HttpMutatorTests.cs:45 (ModelCalls) | real |
| MUT-10 | MutationTests.cs:126 DeckIsData | real |
| MUT-11 | MutationTests.cs:138 BlindPrompts; HttpMutatorTests.cs:37 OllamaMutatorRequest | real |
| MUT-12 | MutationTests.cs:167 CleaningMatchesThePrototype (12 golden cases cover every listed case) | real |
| MUT-13 | MutationTests.cs:102 | partial (new instruction/seed per try not asserted) |
| MUT-14 | MutationTests.cs:181; HttpMutatorTests.cs:71; PlayMode/OutputPlayTests.cs:137 | real |
| MUT-20 | MutationTests.cs:202 GaussianIsClamped; Conformance/ConformanceTests.cs:201; Scenarios/ScenarioTests.cs:133 (vacuous loops, #5) | real (via T-MUT-09) |
| MUT-30 | PlayMode/WorldPlayTests.cs:119 SlowMutatorSameEvents; ConformanceTests.cs:201; TeachingPathGradingTests.cs:133 | real (reordering untested) |
| MUT-31 | MutationTests.cs:219 MutationRounds | real |
| MUT-32 | MutationTests.cs:235 HatchingWaitsForTheMutator | real |
| REPRO-01 | ReproductionTests.cs:35 Readiness | real |
| REPRO-02 | ReproductionTests.cs:52 OnePartnersChoiceIsEnough | real |
| REPRO-03 | ReproductionTests.cs:68 OncePerPeriodAndDeterministicPairing | weak (nearest/ties) — #8 |
| REPRO-04 | ReproductionTests.cs:88 Asexual | real |
| REPRO-05 | ReproductionTests.cs:212 BreedingAfterActing | real |
| REPRO-10 | ReproductionTests.cs:106 LitterSizes; PopulationTests.cs:11 (block cut); TeachingPathGradingTests.cs:112 | real |
| REPRO-11 | ReproductionTests.cs:132 ParentsPay | real |
| REPRO-12 | ReproductionTests.cs:152 Babies | real (heading not asserted) |
| REPRO-13 | ReproductionTests.cs:152 Babies (+ T-DEC-01) | real |
| REPRO-20 | ReproductionTests.cs:176 EggsAndIncubation | partial (egg contents/position) |
| REPRO-21 | ReproductionTests.cs:176 | real |
| REPRO-22 | ReproductionTests.cs:176 | real (conception order not asserted) |
| REPRO-24 | MutationTests.cs:235 | partial (litter/laid never read) — #7 |
| POP-01 | Reproduction/PopulationTests.cs:11 CapRules; ScenarioTests.cs:39 LoneGrazer (T3) | real |
| POP-02 | PopulationTests.cs:42 MigrationIgnoresGenes | real |
| POP-03 | PopulationTests.cs:68 Newcomers | real |
| POP-04 | PopulationTests.cs:89 Founders | real |
| TICK-01 | Core/TickLoopTests.cs:11; Regression/RegressionTests.cs:233; ConformanceTests.cs:235 | real |
| TICK-02 | TickLoopTests.cs:36 ChangesAreSeenByLaterPhasesOnly | real (visibility half) |
| TICK-03 | TickLoopTests.cs:49 WaitModesGiveTheSameHash; PlayMode/TickLoopPlayTests.cs:54 | real |
| TICK-04 | RegressionTests.cs:233 ReferencePhaseOrder | real, but the coverage gate can't see the rule — #6 |
| TICK-05 | PopulationTests.cs:99 NewcomersDecideNextTick | real |
| TICK-06 | ReproductionTests.cs:212 BreedingAfterActing | real |
| TICK-07 | TickLoopTests.cs:11 | real |
| RAND-01 | Core/StaticChecks.cs:14 NoGlobalRandomness | real (Runtime + Http only) |
| RAND-02 | Core/RandomTests.cs:10, :29; Environment/EnvironmentTests.cs:103 | real |
| RAND-03 | RandomTests.cs:29; WorldPlayTests.cs:20 | real at the stream level; the world-level citation is weak |
| RAND-04 | RandomTests.cs:40; EnvironmentTests.cs:206 WorldSeedFixesTheMap | real |
| RAND-05 | Decisions/DecisionTests.cs:251 InsertionOrderDoesntMatter | weak — #11 |
| RAND-10 | RandomTests.cs:95, :108 EventsAreCanonical; TickLoopPlayTests.cs:65 | real |
| RAND-11 | RandomTests.cs:95; RegressionTests.cs:40 (R-01), :179 (R-04); TickLoopPlayTests.cs:65; WorldPlayTests.cs:119 | real |
| RAND-13 | RandomTests.cs:95, :128; TickLoopPlayTests.cs:65 | real |
| RAND-20 | TickLoopTests.cs:130, :160; DecisionTests.cs:208; HttpBrainTests.cs:197; OutputPlayTests.cs:137 | real for the tested causes; bug in strict mutator — #1 |
| OUT-01 | OutputPlayTests.cs:32 RunFiles | real (uniqueness suffix not exercised) |
| OUT-02 | OutputPlayTests.cs:32 | real; doc (species_created / egg ids) — #14 |
| OUT-03 | OutputPlayTests.cs:63 GenomesRebuiltFromFiles; MutationTests.cs:72 | real (strong) |
| OUT-04 | OutputPlayTests.cs:99 NoSecrets (vacuous); HttpBrainTests.cs:266 KeysOnlyInEnvironment (real, V-61) | partial: the output/log half isn't really tested — #3 |
| CFG-01 | Config/ConfigTests.cs:14 EveryFieldIsDocumented | real (per the accepted decision: range or reference value) |
| CFG-02 | ConfigTests.cs:36 ScenarioOverrides; Scenarios/ScenarioAssetTests.cs:25 | real |
| CFG-03 | ConfigTests.cs:36; OutputPlayTests.cs:60 | real |
| CFG-04 | ConfigTests.cs:66 TraitsDriveBehaviour | real |
| CTRL-01 | WorldPlayTests.cs:59 (C3), :89 (C4, C7); ScenarioTests.cs:67 (C2, C4, C7); MutationTests.cs:27 | real (run_info recording and C4 numbers untested) |
| CTRL-02 | WorldPlayTests.cs:59 ShuffledControl; ScenarioTests.cs:67 | real |
| EDIT-01 | Tooling/ValidationCatalogueTests.cs:115, :127; Tooling/EditorToolTests.cs:119 PlayModeGate; RegressionTests.cs:215 | partial: module prefabs never validated — #4 |
| EDIT-02 | ValidationCatalogueTests.cs:115; EditorToolTests.cs:127 | partial: fix buttons — #12 |
| EDIT-03 | EditMode/AssemblyLayoutTests.cs:16 | real (Runtime assembly only) |

Summary: every rule has at least one genuine citation; none is cited only through a test id.
- Only weak tests: RAND-05, REPRO-03 and GENE-03 (GENE-03's content is covered by an uncited prompt test).
- A named half untested: OUT-04 (outputs/logs), EDIT-01 (module prefabs), EDIT-02 (fixes), REPRO-24 (litter/laid),
  REPRO-20 (egg contents), GENE-32 (litter path) and MUT-13 (redraws).
- Implementation contradicts the rule: RAND-20 (strict mutator), MUT-03/05 (number no-ops) and GENE-13 (number
  precision). GENE-06 is a risk.

### Items 2–6: discovery, determinism, animals as data, boundaries, tick order

### P1 audit, phase 1 — items 2 to 6 (architecture, determinism, data, boundaries, tick order)

Read-only static audit of `Assets/EvoSim/{Runtime,Http,Samples}` (Editor where it touches a run) against
`Docs/Interface/` (20, 12, 07, 10, 01, 04, 09, 21). Unity was not launched (two instances busy), so nothing was
re-run; "same seed twice" relies on the existing tests (T-RAND-05, T-CORE-09, TickLoopTests freeze-vs-responsive,
R-01), which 50-implementation-status.md reports as passing. Accepted deviations (50 "Decisions"/"Disagreements",
40 §2) were not reported.

Severity: bug = provable wrong behaviour against a MUST (ARCH rules are MUST for the reference, 20 §1);
risk = can go wrong but not proven on the shipped configuration; doc = code defensible, contract unclear.

---------------------------------------------------------------------------------------------------------------

#### Findings (most severe first)

##### F1 — ARCH-06 / DEC-13 (and OUT-04 via V-61): a disabled or foreign Brain is still used, never initialised or validated — bug
- `Runtime/Core/Species.cs:40` `Brain => brain != null ? brain : World.DefaultBrain` — no check that the brain is
  enabled, on an active GameObject, or a service of *this* World. Same for `World.defaultBrain`
  (`Runtime/Core/World.cs:46,92`).
- Discovery only keeps enabled services (`World.cs:319-330`); `Initialize()` runs on `services` (`World.cs:343`),
  `Validate()` on `modules` (`World.cs:372`), `Begin()` on `modules` (`World.cs:276`).
- `AskBrainsPhase.Validate` raises V-10 only when `s.Brain == null` (`Runtime/Phases/AskBrainsPhase.cs:95-96`);
  `AskBrainsPhase.Run` sends to it (`:31,49,58`).
- run_info / summary list brains from `ServicesOf<Brain>()` only (`Runtime/Recording/RunInfo.cs:40`,
  `RunRecorder.cs:327`).
- `ScenarioAsset.Apply` even picks brains with `GetComponentsInChildren<Brain>(true)` (inactive included)
  (`Runtime/Config/ScenarioAsset.cs:75-77`).
- Sequence: Lab1 scene (World default = JEV). Untick the JEV component (or deactivate its GameObject) to "switch it
  off" (ARCH-01). `Initialize()` succeeds (no V-10); every decision goes to the disabled JevBrain;
  `HttpBrain.Initialize` (caller reset), `JevBrain.Initialize` (head/tooLong reset), `HttpBrain.Validate`
  (V-20 head files, `HttpChecks.Host`, `HttpChecks.Secrets` = V-61) never run; run_info.json "brains" omits it.
  An OllamaPointsBrain in that state never runs `Begin()` → `ModelIdentity` = "model@unknown" → cache keys differ
  from the enabled run.
- Fix: in `Prepare`, resolve each species' brain; if it is not in `services` (disabled, inactive, under a
  species, or another World) raise V-10 (error) — or fall back to the World default with a warning; make
  ScenarioAsset search with `includeInactive:false` + `Ownership.IsEnabled`.

##### F2 — SPEC-30 / CORE-08 / V-07: `World.AddSpecies` never validates the clone; a duplicate display name is accepted — bug
- `Runtime/Core/World.Species.cs:17-27`: only the id is de-duplicated (`for (n=2; usedSpeciesIds.Contains(id); …)`),
  `s.SetNames(id, name)` keeps `name` as display name even if another species has it; the `ValidationReport`
  passed to `Discover` is never read; no module `Validate()` is called; no SPEC-05 nesting check on the clone.
- Proof in the repo: `Tests/PlayMode/WorldPlayTests.cs:53-55` — `AddSpecies(predator, "predator")` → id
  "predator-2", display name "predator", the same as the existing species (V-07 would be an error at Prepare).
  SPEC-30: "A new species gets a new id **and name**". `FindSpeciesByName("predator")` then resolves by id to the
  old species, while both species' prompts/situation texts say "predator".
- Also: a template carrying V-06/V-14/V-01 problems, or a diet that names nothing, is added silently.
- Fix: de-duplicate the display name like the id (or reject), run the same structure + module validation as
  `Prepare` steps 5-8 on the clone, and stop/throw on errors.

##### F3 — SPEC-03 (signature): computed before the food web, so label-derived signatures are unstable — bug (low)
- `Species.InitializeModules` computes `Signature` (`Runtime/Core/Species.cs:183-188`) in step 6 of
  `World.Prepare` (`World.cs:361-362`); the food web is derived in step 7 (`World.cs:367`).
- `NearestAnimalSense.DefaultLabel` (`Runtime/Senses/NearestAnimalSense.cs:34-43`) uses
  `World.Resolve(Species, targets)`; `threatsOf/preyOf` are only cleared/rebuilt in `DeriveRelations`
  (`World.FoodWeb.cs:14-21`), not in `ResetSystems` (`World.Animals.cs:34-40`).
- Sequence: predator species renamed "wolf" (S26/B-08, `Editor/IntegrityGates.cs:486`), prey's threat sense with
  an empty label. First `Prepare` on a fresh World: threats unknown → label "Predator" → signature hashes
  "Predator(...)". Any later `Prepare` on the same object (editor Validate, PlayModeGate, re-Initialize,
  play mode without domain reload): stale `threatsOf` → label "Wolf" → different signature. A signature accepted
  in the inspector (V-12 fix) then raises V-12 "signature changed" in batch/CI (fresh object), and vice versa.
  The prompt and situation texts (built after step 7) correctly say "Wolf", so the signature also disagrees with
  what the brain reads on fresh worlds.
- Fix: compute the signature after `DeriveRelations` (and clear the relation maps in `ResetSystems`).

##### F4 — RAND-20 (tick boundary) with TICK-04: a strict mutator failure stops the run in the middle of the tick, after Act and Breed — bug (non-default setting)
- `Runtime/Mutation/MutatorService.cs:128` calls `World.Stop` from `Answer`, reached from `Pump()` via
  `JobsPending.IsDone` (`:170-177`) in `HatchPhase.WaitsFor` (`Runtime/Phases/HatchPhase.cs:15-27`) or
  `HatchPhase.Run` (`:31`). `World.Advance` then breaks (`World.cs:460`) with `nextPhase` = Hatch.
- Effect for tick t: Act (moves, meals, kills: `Counters.Kills++` in `InteractionResolver.cs:66`) and Breed
  (energy paid, `Offspring`, eggs laid) are applied; Deaths/Migration/Environment/Floor/Record never run.
  Killed animals keep `IsGone == false`, so `summary.json` counts them in `pop_final` and `final_population`
  (`RunRecorder.cs:270-289, 295, 303`) while `deaths.killed` lacks them (kills > killed deaths); `ticks` = t.
  RAND-20: "stops cleanly at a tick boundary". (The strict *brain* stop in ChooseActions happens before any world
  change, which is acceptable.)
- Only with `MutatorService.strict = true` (off in the reference).
- Fix: on a strict mutator failure, record the reason and `RequestStop` at the boundary, or detect it in
  `BreedPhase` before any egg is laid; or roll the stop to the next boundary like `RequestStop`.

##### F5 — ARCH-12 / ARCH-03 (phases call modules on lists): `Locomotion.MoveAll` is dead; `Sense.ReadAll` / `AnimalAction.ActAll` don't exist — bug (low)
- `Runtime/Locomotion/Locomotion.cs:41-45` declares the batch entry point; no caller anywhere (grep). `ActPhase.Move`
  calls `loco.Move(...)` per animal (`Runtime/Phases/ActPhase.cs:147`); `SensePhase` calls
  `Species.Observe` → `Sense.Read` per animal (`SensePhase.cs:37-41`, `Species.cs:264-269`). Only
  `DeathRule.CheckAll` is wired (`DeathPhase.cs:32`). `SpatialIndex` is a sealed class, no `ISpatialIndex`.
- Sequence: a student/locomotion overrides `MoveAll` (20 §10's seam) → never invoked; results come from `Move`.
- Fix: have ActPhase group by species and call `MoveAll` (Simultaneous and per-species turns can), add
  `ReadAll/ActAll` defaults, or drop the seam from the docs.

##### F6 — ARCH-06 / GENE-05: disabling an action component leaves its text gene alive as a *free* gene — risk
- `Ownership.NearestAction` skips disabled actions (`Runtime/Core/Ownership.cs:30-39`); `Species.BindGenes` then
  warns V-06 and keeps the gene as free (`Species.cs:153-158`); `Gene.Label` falls back to the GameObject name
  (`Genes/Gene.cs:15-16`).
- Sequence: prefab "eat" = [EatAction][TextGene] (20 §2). Untick EatAction only ("disable it to switch it off",
  ARCH-01): the species has no eat action, but the genome keeps locus `prey.eat`, the prompt's genes block keeps
  `- eat: "Eat when hungry."`, and the signature keeps the gene. Only a V-06 warning. T-CORE-03
  (`Tests/EditMode/Core/DiscoveryTests.cs:49-50`) disables the gene explicitly too, so the test hides this.
- Fix: treat a TextGene on (or under) a GameObject whose action is disabled as absent, or make that case a V-06
  error; extend T-CORE-03 to disable only the action.

##### F7 — ACT-20 / GENE-03 / CORE-06 / CORE-07: the boundaries hold in today's code but nothing enforces them — risk
- Verified: no action, sense, locomotion, stat, death rule or sample reads `Genome`, allele text, `LastSituation`
  or `LastProbabilities` (grep). Mutator body = one user message with exactly MUT-11's text
  (`Mutation/MutationText.cs:34-38`, `Http/OllamaMutatorClient.cs:35-50`, `Http/OpenAIMutatorClient.cs:27-39`).
- But `Animal.Genome`, `LastSituation`, `LastProbabilities` are public fields (`Runtime/Core/Animal.cs:13,28-29`)
  reachable from any `Act(Animal a, …)` / `Read(Animal a, …)`; T-ACT-09 scans only `Runtime/Actions`
  (`Tests/EditMode/Actions/ActionTests.cs:233-234`) — not `Samples/` (DragAction, DrinkAction), `Senses/`,
  `Locomotion/`, `Stats/`, and not `Last*`. Brains receive the live `Species` (`DecisionQuery.Species`) and are
  WorldServices with `World`; operators receive a `Gene` (→ `World`, all animals). CORE-07 also relies on
  `LlmMutation.contextLine` being one fixed line, which no validator checks (a newline or world data in it would
  pass).
- Fix: widen T-ACT-09 to every module folder + Samples and add `LastSituation|LastProbabilities|LastObservation`;
  consider `internal` + read-only views for brains; validate `contextLine` (one line, no placeholders).

##### F8 — SPEC-05 / ARCH-06: the nested-species check is asymmetric for disabled species — doc
- `World.cs:303-313`: a disabled species is skipped *before* the nesting check; `Ownership.SpeciesAbove` uses
  `GetComponentInParent<Species>(true)` with no enabled check (`Ownership.cs:26-27`).
- Outer [Species, disabled] > Inner [Species, enabled] → V-01 error. Outer [Species, enabled] > Inner [Species,
  disabled] > EatAction → no error; the EatAction's nearest species is Inner, so it silently belongs to nobody.
- Contract: SPEC-05 says nesting is an error; ARCH-06 says disabled = absent. Decide which wins and apply it both
  ways (e.g. nesting is always V-01 whatever the enabled state).

##### F9 — ARCH-05 / ARCH-07: a World inside another World is not detected — risk
- `World.Prepare` collects with `GetComponentsInChildren<Species|WorldModule|SpeciesModule>` (`World.cs:303,319,332`)
  without stopping at a nested World; no V-check for a World above/below a World (grep).
- Sequence: Experiments [World] > Run2 [World] > Prey [Species]. Outer and inner both discover Prey and Run2's
  phases/services; `Discover`/`Bind` overwrite `World` on each `Initialize`; both worlds tick the same species.
- Fix: nearest-World ownership (skip subtrees owned by another World) or a V-0x error.

##### F10 — duplicate names (ACT-01/GENE-01 are checked; these are not) — risk
- Sense labels: no uniqueness check; `Species.FindSense` and `ObservationView.Token` return the first
  (`Species.cs:99-103`, `Senses/ObservationView.cs:16-22`); two Threats senses both read "Predator".
- Service/layer names: two FoodGrids named "Grass" → `Service<ResourceLayer>("Grass")` reaches only the first
  (`World.cs:130-134`, `Food/Diet.cs:92`), and both share the streams "world/Grass" (initial map,
  `Environment/FoodGrid.cs:73`) and "Grass" (regrowth, `ResourceLayer.cs:40`, `EnvironmentPhase.cs:11`) — the
  `RandomStreams` cache returns the same object, so the layers' draws interleave.
- Species id vs display name cross-collisions: V-07 checks ids among ids and names among names
  (`World.cs:349-355`), while `FindSpeciesByName` matches ids first (`World.cs:157-162`): species X (id "wolf",
  name "predator") + species Y (id "predator") → diet/sense "predator" resolves to Y.
- Stream namespace isn't reserved beyond "world": a layer named "act-order" shares the act-order stream (RAND-02).
- Fix: V-errors for duplicate sense labels, duplicate service names per type, id/name cross-collisions, and
  layer names that collide with reserved stream names.

##### F11 — RAND-11 / SPACE-12: views carry colliders; a ray mask including layer 2 makes senses depend on rendering — risk
- `ViewPool.Make` uses `CreatePrimitive(Capsule)` (with its CapsuleCollider) or the Body prefab, moves everything
  to layer 2 "Ignore Raycast" (`Views/ViewPool.cs:45-69`); views are placed in `LateUpdate` at interpolated
  positions (`World.Views.cs:55-96`); `RayBatch` uses the caller's mask unchanged (`Senses/RayBatch.cs:53-55,78`).
- Default mask excludes layer 2, so the reference is safe. A BatchedSense with `~0`/`Physics.AllLayers` hits views
  whose position depends on frame timing and that don't exist in batch/tests → different hashes.
- Fix: remove colliders from views (or put them on a dedicated layer) and AND every RayBatch mask with
  `~(1 << IgnoreRaycastLayer)`.

##### F12 — RAND-03: one `act-order` stream for all species contradicts "adding a species doesn't change the draws of the others" — doc
- `ActPhase.cs:57,68-73`: species-in-turn shuffles species 1, then species 2, … from the same stream, so species
  2's order depends on species 1's living count, and inserting a species earlier in the hierarchy changes every
  later species' act order. The RAND-03 table lists `act-order` as one world stream, so this follows the table
  but not the rule's rationale.
- Fix (contract or code): per-species `<species>/act-order` streams in species-in-turn, or state the exception.

##### F13 — ARCH-07: no mutable static is written, but some statics are mutable collections — risk (low)
Every non-const static field in Runtime/Http/Samples:
| Field | Type | Mutable? | Written after init? |
|---|---|---|---|
| `ActContext.TurnAngles` (Runtime/Actions/ActContext.cs:12) | `static readonly float[]` | elements yes | no |
| `AnimalWords.Words` (Runtime/Genes/AnimalWords.cs:9) | `static readonly string[]` | elements yes | no |
| `PromptWriter.Placeholder` (Runtime/Brains/PromptWriter.cs:19) | Regex | no (immutable, thread-safe) | — |
| `MutationText.Quoted/Preamble/FirstSentence/Allowed` (Runtime/Mutation/MutationText.cs:14-17) | Regex | no | — |
| `ViewPool.ColorId/BaseColorId` (Runtime/Views/ViewPool.cs:13-14) | `static readonly int` | no | — |
| `HttpChecks.VariableName/KeyLike` (Http/HttpChecks.cs:9-10) | Regex | no | — |
| `ChasedSense.tokens` (Samples/ChasedSense.cs:11) | `static readonly List<string>`, returned as `Tokens` | **yes, exposed** | no |
| `DiesOfThirst.causes` (Samples/DiesOfThirst.cs:8) | `static readonly string[]`, returned as `Causes` | **yes, exposed** | no |
| `IntensityLadder.Ladder` (Samples/IntensityLadder.cs:12) | `static readonly string[]` | elements yes (private) | no |
No static non-readonly field, static event, static property with a setter, singleton or `RuntimeInitializeOnLoad`
in these assemblies. ChasedSense/DiesOfThirst are teaching samples, so the pattern will be copied; a cast of
`Tokens` to `List<string>` would mutate every species and world. Fix: instance fields or `ReadOnlyCollection`.

---------------------------------------------------------------------------------------------------------------

#### Checked and found consistent (no finding)

**Item 2 — discovery and ownership.** `Ownership.Owned` = enabled components on active GameObjects whose nearest
Species is the owner, depth-first (`Ownership.cs:10-20`), matching 20 §4 step 4; action/gene/sense orders are
"n-th found depth-first"; genes bind to the nearest enabled action on/above them within the species, stopping at
the Species GameObject (`Ownership.cs:30-39`); duplicate action names V-03, gene labels V-14, species ids/names
V-07, nested species V-01 with a fix, species module without species V-02, world module under a species V-02
warning. Signature = actions, gene labels+kinds, sense labels+tokens (`Species.cs:191-206`).

**Item 3 — determinism.** No `UnityEngine.Random`, unseeded `System.Random`, `Guid`, time-as-randomness in
Runtime/Http/Samples (T-RAND-01 regex + manual grep). `Stopwatch`/`DateTime` only for wall-clock stop, Fast-speed
frame budget, latency stats, run folder name and `run_info.started`. Every `Dictionary`/`HashSet` is used for
look-ups only; the iterated ones are `SortedDictionary` (CanonicalJson sorts keys too). Ties are broken by id or
cell index in `SpatialIndex.Nearest`, `EntitySystem.Nearest`, `CellGrid.Nearest`, `BreedPhase.NearestPartner`;
`CoverLayer` sorts with a total order. Float sums run in index order. Brain answers are written by query index
(`BrainAnswer.SetRow`, JEV `choice.index`, Ollama per request), mutator replies by request index and processed in
in-flight order; mutation tries are drawn at conception (MUT-30), so batching/arrival time doesn't change results.
Freeze and Responsive go through the same `WaitsFor`/`Pump` path; `Advance` resumes at the waiting phase;
`BeforeTick` only at a real tick start. Views only read (`SyncViews`); the editor's Prompt preview / Ask-the-brain
don't touch memo, cache, streams or events.

**Item 4 — animals as data.** `Animal` is a plain sealed class; the only MonoBehaviour messages in Runtime/Http
are World's Awake/FixedUpdate/Update/LateUpdate and three `OnDestroy` disposers; `AnimalView` holds only an id.
No SpeciesModule keeps per-animal state: module fields are settings, cached look-ups (`Energy`, `TraitId`,
species lists, token tables) or per-call scratch; per-animal data lives in stats/traits/record fields; phases
hold per-tick scratch arrays only.

**Item 5 — boundaries.** Only `AskBrainsPhase` turns genomes into prompt text (`DecisionQuery.BrainGenes`);
`World.ExpressionGenomeFor`/`Species.CreateAnimal` express number genes into traits at birth; crossover/hatch
copy alleles. MUT-11 prompt is exact and is the only content of the HTTP request (plus model/seed/temperature
options). See F7 for what is *not* enforced.

**Item 6 — tick order and act order.** Reference phase list in `Testing/Lab.cs:121-124` and the scenes (T-TICK-01)
= TICK-04; V-08 checks Ask after Sense, Choose after Ask, Hatch after Breed. `ActPhase`: species-in-turn builds
each species' living list when its turn starts and shuffles it with `act-order` (ACT-30/32); all-mixed shuffles
one list; sequential actors plan → move (index updated, ACT-31) → interact → settle one at a time and skip
animals killed earlier; simultaneous plans all from the start state, moves all, resolves interactions in a fresh
random order (contested item → random winner via `Consume`/`Killed`), settles non-killed. Busy animals stay and
count down in Settle. Deaths: killed first, then +1 age, then rules (ANIM-35/40).

---------------------------------------------------------------------------------------------------------------

#### Noticed outside items 2–6 (for the other auditors)
- `Diet.Declare` declares `kill.chance` only if the diet strikes at Declare time (`Food/Diet.cs:40-44`);
  after `AddSpecies` → `RebuildFoodWeb` a diet that now strikes a new species by name has `Strikes == true` but an
  invalid `KillChance`, so `killP = 0` (`Actions/InteractionResolver.cs:62`): that hunter never kills it.
- `OllamaPointsBrain.Begin` / `OllamaMutatorClient.Begin` ask the server for the digest; with the server down the
  brain's `ModelIdentity` becomes "...@unknown", which is part of the answer-cache key (`Brains/AnswerCache.cs:45`)
  → a replay without the server misses the cache (RAND-21/DEC-33). The mutator cache key uses only the model name.
- `FleeAction.relevantSense = "Predator"` etc. vs SPEC-20 labels derived from display names ("Wolf"): IsRelevant
  would return false for renamed species — but `IsRelevant` has no caller at all (grep), so CTRL-10 relevance is
  computed elsewhere or not at all.
- Locus ids `species.Id + "." + Label` can collide if ids or labels contain "." (no validation).

### Items 7–8: outputs and validation

### P1 items 7 and 8 — Outputs (OUT-01..05) and Validation (EDIT-01)

Audit date 2026-10-10, branch llm-evolution (HEAD cde9e9c + uncommitted M12 work). Read-only; nothing in the
repository was changed. Scripts and their outputs are in this folder:

| File | What |
|---|---|
| `rebuild.py`, `rebuild.out` | rebuilds every genome from `events.jsonl` + `alleles.jsonl`, checks inheritance, mutations, the events hash chain, `final_population.json`, `summary.json` counts and `stats.csv` cumulative columns |
| `secret_scan.py`, `secret_scan.out` | OUT-04 scan of Assets/EvoSim, .github/workflows, Docs/prompts and the run logs |
| `secret_guards.py`, `secret_guards.out` | Python ports of V-61 (`HttpChecks.Secrets`) and `RunInfo.MaskedCommandLine`, run on concrete inputs |

Accepted deviations not reported: V-14 (GENE-01 decision), "killed" kill cause (40 §2 #6), StartMutation rename,
reports folder (40 §2 #9), T2 with C2 (40 §2 #8), gene_timeline/hide (40 §2 #12), RAND-13 hash seeding.

#### 1. Item 7 — Outputs

##### 1.1 Genome rebuild on real runs (OUT-01..03)

Eight run folders were found: six in `Logs/EvoSim/runs/` (S03 default, seeds 1234/42/7, commit c5bf389, random
brain, 1 000 ticks) and two in the scratchpad copy (S03 jev, seed 1234, 50 ticks, JEV + Ollama mutator).

`python -I rebuild.py <folders>` → **all genomes rebuilt and equal to `final_population.json` in all 8 runs**:

- every founder/immigrant/birth genome's allele ids exist in `alleles.jsonl`; one allele per locus;
- every birth: parents exist, `gen` = 1 + max(parent gen), each locus comes from a parent (no mutation was listed
  in these runs: 0 mutations — see 1.4);
- every death refers to a living animal and has cause/age/gen/food/offspring; kills have `killer`;
- the chained hash recomputed from the file lines (h0 = SHA-256("evosim:seed:worldSeed")) equals
  `summary.events_hash` and `events_sha`; event count equal;
- every line is byte-identical to Python `json.dumps(sort_keys=True, ensure_ascii=False)`; ticks non-decreasing;
  keys sorted; each event has kind, t, id, species (OUT-02);
- summary per species (founders, births, hatched, immigrants, pop_final, eggs_lost, deaths by cause, max_gen) and
  `alleles` equal the counts from the events; `stats.csv` births/immigrants/deaths/pop at every row equal the
  events with t ≤ row−1 (0 mismatches over 58 rows).

Only problem found: **the two 50-tick JEV runs have no `stats.csv`** (see F5).

The repository test T-OUT-02 (`Tests/PlayMode/OutputPlayTests.cs:63-97`) rebuilds genomes tick by tick with
mutation rate 0.5 and compares with the live state — a real check of OUT-03 including mutations.

##### 1.2 File formats against 13 §2–§5

- `events.jsonl`: fields of founder/immigrant/birth/death/egg_lost/species_created as 13 §3 (HatchPhase.cs:68-75,
  World.Animals.cs:115/128, World.Deaths.cs:16-22, EggSystem.cs:47-49, World.Species.cs:33/42).
  `laid` is written only when HatchTick > ConceivedTick (HatchPhase.cs:74); with no incubation (the default) no
  birth has it although 13 §3 says "a hatched egg also has laid" — doc.
- `alleles.jsonl`: id, locus, text|value, origin, parent_id, operator, model, seed — present (RunRecorder.cs:254-266).
  Alleles are de-duplicated by value (AlleleRegistry.cs:27-28): a mutant equal to an existing allele reuses it, so
  the parent→child edge exists only in the birth event's `mutations` (not in `parent_id`). Rebuild unaffected.
- `final_population.json`: id, gen, genome, species — present.
- `summary.json`: every 13 §5 item present, but **`backend_s` (brain time) is always 0** (F2).
- `run_info.json`: scene, git, unity, brains (model incl. digest, mode, strict), mutator (model, temperature,
  client identity), seeds, wait mode, act order, controls, full settings snapshot. Hosts/API of brains and the
  mutator appear only inside `settings` by path, not in the `brains`/`mutator` sections 13 §2 names — doc.
  `git` has no dirty flag (the runs at c5bf389 were made with uncommitted changes) — note only.
- `stats.csv`: every 13 §4 column present (portions placed before act_*; `alleles` after the first species).

##### 1.3 Findings — outputs

**F1 (risk, high) RAND-20 / OUT-03 — leaving Play mode loses alleles.jsonl, final_population.json, summary.json.**
`RunRecorder.OnDestroy` only closes the streams (RunRecorder.cs:241); `WriteAlleles` runs at tick % 5000 == 0
(RunRecorder.cs:219) and in `OnRunStopped` (RunRecorder.cs:224-238). No World/Runtime code calls `Stop` on
OnDisable/OnDestroy/OnApplicationQuit (grep: the only teardown hook in Runtime is RunRecorder.cs:241), and the only
playModeStateChanged handler (PlayModeGate.cs:15-24) handles ExitingEditMode. Input: open Lab1_Small, Play, exit
Play at tick 1 000 → folder holds events.jsonl, stats.csv, run_info.json only; genomes are allele ids with no
texts, so OUT-03 fails for the most common interactive way of ending a run. Fix: World.OnDisable →
`Stop("destroyed")` when running; or append each allele to alleles.jsonl when registered (AlleleRegistry.Registered
already exists) and flush it with events.

**F2 (bug) 13 §5 — brain time always 0.** `SpeciesCounters.BrainMilliseconds` (SpeciesCounters.cs:16) is never
incremented anywhere in Assets (grep finds only the declaration and the read at RunRecorder.cs:306). Evidence: the
JEV run `S03_Lab1Small-jev-s1234-20261010-150800/summary.json` has prey backend_queries 236, llm_calls 245,
backend_s 0. Fix: add the measured batch time per species in AskBrainsPhase when the brain's answer completes.

**F5 (bug, minor) 13 §2 / T-OUT-01 — no stats.csv for runs shorter than statsEvery.** The file is created lazily on
the first row (RunRecorder.cs:123-128), and rows are written only at (t+1) % statsEvery == 0 (RecordPhase.cs:11).
Evidence: both 50-tick JEV runs have no stats.csv (`rebuild.out`). Fix: create the file and header in Begin.

**F6 (bug, minor) OUT-05 — compatibility summary loses the run's failure total.** The run-level `failures`
(sum over species, RunRecorder.cs:338) is overwritten by the first species' own `failures` when its summary is
merged at the top level (RunRecorder.cs:342; species key at :309). Input: compatibility run where only the
predator's brain fails 3 times (non-strict) → `summary.failures` = 0. Fix: merge species keys without
overwriting run keys (or rename the run key).

**F10 (risk) SPEC-30 — species added during a run never get stats columns.** Columns are built once, on the first
row (RunRecorder.cs:123-127, 142-158); `WriteStats` emits only those columns (RunRecorder.cs:132-136). Input:
`World.AddSpecies` at tick 150 → the new species is absent from stats.csv for the rest of the run. Speciation is
deferred (40 §3), so risk. Fix: one stats file per species, or rewrite the header when species are added.

**F12 (doc) OUT-02 — `id`.** OUT-02 says every event has `id` (the animal); `species_created` has none
(SimEvent.cs:24 omits id −1) and `egg_lost.id` is an entity id from `NextEntityId` (EntitySystem.cs:21), a
counter separate from `NextAnimalId` (World.cs:165), so an egg_lost id can equal a living animal's id in the
same species. Readers must dispatch on kind first. Fix: say so in 13 §1/§3, or write `egg` instead of `id`.

##### 1.4 Note on the 13:32 runs (input to F8)
The six default runs (commit c5bf389) have `mutations: tried 13, failed 13, ok 0, calls 0` and
`run_info.mutator.client = ""`: the scene had a MutatorService and no MutatorClient, so every try ended in
"no mutator" (MutatorService.cs:91) — silently, with no validation message (F8). Current scenes have a client.

##### 1.5 OUT-04 — secrets

`python -I secret_scan.py <repo> <repo>/Logs/EvoSim <copy>/Logs/EvoSim` — 735 files (code, .unity, .prefab,
.asset, .json/.jsonl, .txt, .md, .yml, run folders): **no real secret**. 3 hits, all test fixtures
(HttpBrainTests.cs:281 `user:secret@`, OutputPlayTests.cs:102 fake `sk-evosim-test-…`). All serialized
`apiKeyVariable` fields in scenes/prefabs are empty; hosts are localhost. The workflow takes
`${{ secrets.ANTHROPIC_API_KEY }}` (evosim.yml:84). Keys are read only via
`Environment.GetEnvironmentVariable(ApiKeyVariable)` per call (HttpCaller.cs:116), sent as a bearer header
(SystemHttpTransport.cs:31), never stored; RecordingTransport keeps no key. No Debug.Log of requests.

**F9 (risk) V-61 heuristics miss common key shapes, and only two fields are checked.** Port in
`secret_guards.py` (regexes copied from HttpChecks.cs:9-10): a key pasted into `apiKeyVariable` is not flagged when
it looks like an identifier — `gsk_4f9a…` (Groq), `ghp_A1b2…` (GitHub), `AIzaSyA1…` (Google, no dash), a 40-hex
token starting with a letter — nor `?key=AIza…` in the host. Such a value would also be written to
`run_info.json` (`settings/.../apiKeyVariable`, RunInfo.cs:50). V-61 runs only in HttpBrain.Validate (:126) and
HttpMutatorClient.Validate (:121), while 21 §2 says "a serialized field of a scene, prefab or asset". Fix: treat
any value that is not an existing environment-variable name *and* is longer than ~20 chars as a key; add
prefixes gsk_/ghp_/github_pat_/AIza/xox; scan all serialized strings of the World (FieldPath.Snapshot) with the
same rule.

**F11 (risk) T-OUT-03 cannot fail.** OutputPlayTests.cs:102-113 sets `OLLAMA_API_KEY`, but no component reads that
variable: every `apiKeyVariable` in scenes and builders is empty and "OLLAMA_API_KEY" appears nowhere else in
Assets (grep). The key never enters the process path, and logs (Editor.log, Logs/) aren't scanned although 30 says
"no output, log or asset". Fix: configure the test world's mutator/brain with `apiKeyVariable = "OLLAMA_API_KEY"`
and a FakeHttpTransport, run, then scan the run folder, Logs/ and the log file.

**F13 (risk, low) OUT-04 — command-line masking.** `MaskedCommandLine` (RunInfo.cs:61-77) masks only after args
containing key/token/secret/password: `-serial SC-ABCD-…` (Unity licence), `-header "Authorization: Bearer …"`,
`-auth …` and `-apiKey:sk-…` are written to run_info.json unmasked (`secret_guards.out`). No EvoSim option takes a
key, so low. Fix: add serial/auth/bearer/credential and the `:` separator; or record only the options EvoSim reads.

#### 2. Item 8 — Validation catalogue (EDIT-01)

Raised codes (grep `Error|Warning|Info("V-NN"` in Runtime, Http, Editor, Samples):

| Code | Cat. | Raised at (severity) | Test that triggers it |
|---|---|---|---|
| V-01 | E | World.cs:310 (E) | catalogue fixture; DiscoveryTests |
| V-02 | E | World.cs:334 (E); **World.cs:324 (W, world module under a species)** | fixture (E path) |
| V-03 | E | World.cs:388 | fixture |
| V-04 | E | World.cs:384 | fixture; EditorToolTests |
| V-05 | W | World.cs:389 | fixture |
| V-06 | W | Species.cs:156/162 | fixture |
| V-07 | E | World.cs:353/355/411 | fixture |
| V-08 | E | AskBrainsPhase:92, ChooseActionsPhase:74, HatchPhase:88, SeasonsPhase(sample):49 | fixture (Choose before Ask) |
| V-09 | W | World.cs:409 (Deaths only) | fixture |
| V-10 | E | AskBrainsPhase:96 | fixture |
| V-11 | E | AskBrainsPhase:98/103 | fixture (actions); DecisionTests (both halves) |
| V-12 | W | World.cs:398 | fixture |
| V-13 | E | ActPhase:216 | fixture |
| V-14 | (not in 21 §2; accepted decision) | World.cs:393 (E) | fixture |
| V-20 | E | E at FleeAction:63, Diet:176, LlmMutation:58, BreedPhase:146, senses, HttpChecks:26, JevBrain:202, TerrainGround:158; **W at BreedPhase:147/148, MatingRule:56, Metabolism:74, Starvation:16** | fixture (Diet) |
| V-21 | W | FleeAction:61, HuntAction:42, NearestAnimalSense:130 | fixture |
| V-22 | W | Diet:181 | fixture |
| V-23 | W | World.Checks:68 | fixture |
| V-24 | E | Diet:179 | fixture |
| V-25 | I | World.Checks:22/29 | fixture |
| V-30 | E | BandedSense:83 | fixture |
| V-31 | E | LevelSense:57 | fixture |
| V-32 | E | CapRule:30/31 | fixture |
| V-33 | E | Litter:42 | fixture |
| V-34 | W | Litter:45 | fixture |
| V-35 | E | CoverLayer:87/88, FoodGrid:139, FlatGround:26, Locomotion:60 — **no decision-period check** | fixture (FoodGrid) |
| V-36 | W | World.cs:394 (**duplicate trait declaration**), NumberGene:63 (default outside range, gene-bound traits only) | fixture hits the duplicate-declaration path only |
| V-40 | E | TextGene:68/74 | fixture; GeneTests |
| V-41 | E | TextGene:77, NumberGene:62 | fixture |
| V-42 | W | TextGene:69 | fixture |
| V-43 | W | TextGene:79 | fixture; GeneTests |
| V-44 | I | TextGene:81 | fixture |
| V-45 | E | NumberGene:59, Metabolism:76 | fixture |
| V-46 | W | NumberGene:61 | fixture |
| V-47 | I | NumberGene:65 | fixture |
| V-48 | W | BreedPhase:150 | fixture |
| V-50 | W/E | World.cs:403 (E) /405 (W) | fixture (E: 3^12×29 160); SenseTests (W: 3^11) |
| V-51 | E | AskBrainsPhase:107 | fixture; DecisionTests |
| V-53 | E | World.cs:407 | fixture; PromptTests |
| V-60 | W | **LlmMutation:60 (W, static: no MutatorService)**; reachability only as text in BrainHealthWindow:60 via TestConnection (HttpBrain:109, HttpMutatorClient:105) | fixture (static half only); **no test calls TestConnection** |
| V-61 | E | HttpChecks:16/19 | fixture; HttpBrainTests |
| V-62 | E | AnswerCache:135 | fixture |

- Catalogue codes never raised: none outright, but two catalogue *conditions* are never checked: V-35
  "decision period < 1" (F3) and V-36 "trait default outside its range" for traits no number gene binds (F4);
  V-60 "unreachable" lives outside the ValidationReport engine (on-demand button only).
- Codes raised but not in the catalogue: V-14 only (accepted).
- Codes with no triggering test: V-60 reachability half (F7); V-36's catalogue condition (F4); V-35 decision period (F3).
- Severity mismatches: V-02 (W variant) and V-20 (5 W variants) against catalogue E (F14). The catalogue test
  checks only `report.Has(code)` (ValidationCatalogueTests.cs:123), never severity, and passes for V-36 through a
  different condition — so mismatches and wrong-reason passes can't be caught.

##### 2.1 Findings — validation

**F3 (bug) V-35 / EDIT-01 — decision period < 1 is never validated; 0 crashes the first tick.** 21 §2 V-35 lists
"a decision period < 1"; no code checks it (grep `decisionPeriod` in Runtime: World.cs:38 field with [Min(1)] —
inspector only — and the property at :88 whose getter returns the raw field). Scenario overrides set the raw field
by reflection (FieldPath.cs:27, path "World/decisionPeriod" special-cased at :60; applied before Initialize,
ScenarioAsset.cs:82). Input: a ScenarioAsset variant with `World/decisionPeriod = 0` → validation green →
`t.Tick % World.DecisionPeriod` (SensePhase.cs:29) throws DivideByZeroException at tick 0 → the run stops with
"exception in Sense" (World.cs:467). Fix: `if (decisionPeriod < 1) report.Error("V-35", this, …)` in ValidateCore,
plus a fixture.

**F4 (bug, minor) V-36 — catalogue condition unchecked for module-declared traits; fixture passes for another
reason.** `DeclareTrait` (SpeciesBuilder.cs:51-62) never compares default with [min, max]; the only range check is
NumberGene.cs:63 (gene-bound traits). Input: Stamina `max` = 2000 (allowed by [Min(1f)], Stamina.cs:11) → trait
stamina.max default 2000 outside [1, 1000] (Stamina.cs:28) → no message. The V-36 fixture
(ValidationCatalogueTests.cs:82, second Stamina with max 999 — inside [1, 1000]) fires only through the
"declared twice with different defaults" problem (SpeciesBuilder.cs:56-58 → World.cs:394). Fix: check
default ∉ [min, max] in DeclareTrait's problems; give duplicate declarations their own code; fix the fixture.

**F7 (risk) V-60 — the catalogue's condition has no triggering test.** The test header says the reachability
half is "covered in the HTTP tests" (ValidationCatalogueTests.cs:15-16), but `TestConnection` is referenced only
by HttpBrain.cs:109, HttpMutatorClient.cs:105 and BrainHealthWindow.cs:30/36 — no test calls it. The fixture
(ValidationCatalogueTests.cs:95) triggers the static LlmMutation check instead. Fix: a test with a
FakeHttpTransport that fails → TestConnection returns a message (and the window shows V-60); or a
ValidationReport entry on demand.

**F8 (risk) EDIT-01 — a MutatorService without a MutatorClient is not reported.** Every LLM mutation then ends in
"no mutator" (MutatorService.cs:91), silently; `HasClient` (MutatorService.cs:47) is used nowhere. Evidence: the
six 13:32 runs, `mutations: tried 13, failed 13, calls 0`, `run_info.mutator.client: ""`. The catalogue's V-48
("no operator accepts the gene") doesn't cover it. Fix: a warning (V-60's static half or a new code) in
MutatorService.Validate when no client exists and any LlmMutation has rate > 0.

**F14 (doc + risk) severity and meaning drift.** V-02 W at World.cs:324 (a world module under a species — not the
catalogue's V-02); V-20 W at BreedPhase.cs:147/148, MatingRule.cs:56, Metabolism.cs:74, Starvation.cs:16
(missing companion module — not "names something that doesn't exist"); V-35 E reused for ground size and
octaves (FlatGround.cs:26, CoverLayer.cs:88); V-60 W reused for "no MutatorService" (LlmMutation.cs:60); V-36 W for
duplicate trait declarations. The catalogue test asserts presence only (ValidationCatalogueTests.cs:123). Fix: add
these conditions to 21 §2 with their own codes and severities (e.g. V-15 world module under a species W, V-26
missing companion module W), and make each fixture assert `(code, expected severity)`.

**F15 (risk) EDIT-01 — CI half and V-09 coverage.** No EditMode test runs the validation engine on module prefabs
(SceneTests.cs:97-114 only checks that seven action prefabs exist with an action and a gene; 19 prefabs exist);
scenes are validated (RegressionTests.cs:215). V-09 checks only the Deaths phase (World.cs:408): a world with a
Breed phase and no Hatch phase (eggs never hatch) or without a Record phase (no stats rows, no periodic flush)
gets no message. Fix: validate each module prefab inside a minimal world in CI; extend V-09 to Hatch-without-Breed
pairs and Record.


## M12.4–M12.5 (2026-10-10): the CPU mutator, P1 phase 2, the open system

Owner, 2026-10-10: "Use a better mutator on the cpu. Fix all audit items, no grading rules for students, there is no
criteria to attain, they can do whatever they want, and should be able to make anything with this system."

### The mutator: gemma4:26b on the CPU
- Ollama `gemma4:26b` (mixture of experts, about 19 GB of RAM, digest 001e5dafc3c7): num_gpu 0, num_ctx 1 024,
  thinking off, temperature 1.2; about 2.5 s per call. Changed in the five scenes, the scene builder,
  MutatorService's default and CI's `ollama pull`. `qwen3.5:0.8b` stays the small, faster choice (tooltip).
- B-10 (32 §2), same judge (gemma4:12b on the CPU) and slot topics, 40 founders × 10 mutations, 400 mutator and 80
  judge calls: guards pass 97.5 % of first answers; usable 72.5 % after one mutation, 62.5 % after ten (gate 80 %
  after one; the prototype measured 50 % after ten). qwen3.5:0.8b on the same run: 90 % / 67.5 % / 17.5 %.
- B-10 now also rates the founders (step 0, `usable_founders`), the baseline: 92.5 %. A first run was stopped by
  the machine (low memory with both models and the editor loaded); the second ran with the editor closed and one
  model loaded at a time (a watcher unloaded the idle one), 520 calls, same 72.5 % / 62.5 % (same seeds).

### P1 phase 2: what each row of the audit table became
1. AskBrainsPhase: V-10 when a species' brain isn't an enabled service of this World, fix "Use <the World's first
   brain>". WorldIntegrityTests.BrainsMustBeServicesOfThisWorld (disabled, inactive, another World's).
2. World.ValidateSettings: V-35 with a clamp fix for the decision period, sampling temperature, ticks per
   FixedUpdate, memo capacity, tick limit, wall-clock limit, Fast budget and real-time speed. DecisionPeriodOverride.
3. AddSpecies: a new id and display name (name-2…), the checks of Prepare (the species, its modules, the World's
   modules); on an error nothing stays (species removed, food web rebuilt, the object destroyed) and an
   InvalidOperationException lists the errors; alleles and `species_created` only after that; then every world
   module's new `OnSpeciesAdded`. AddedSpeciesAreValidated.
4. Strict mutator: `World.RequestStop("mutator failure: …")` stops at the end of the tick. StrictMutatorStopsAtTheBoundary.
5. Hatch: an allele equal to its parent's is a failure ("unchanged"), like an unfinished job ("not finished") and a
   value the gene doesn't allow ("not allowed"); `llm#n` parsed with TryParse. MutationsThatChangeNothingFail.
6. BrainAnswer.Milliseconds (creation to Complete), shared among its queries into the species' BrainMilliseconds.
   BrainTimeIsRecorded (a JEV fake that answers in 20 ms).
7. Signatures after the food web (Prepare step 7, and AddSpecies); relations cleared on reset. SignaturesAreStable.
8. The batch seams run: SensePhase reads each species group through `Sense.ReadAll`, ActPhase plans through
   `AnimalAction.ActAll` and moves through `Locomotion.MoveAll` (sequential orders call them with one animal,
   simultaneous with each group). BatchEntryPoints (both orders, probes counting batch calls).
9. Genome keys hash numbers at each gene's precision (`Canonical(gene.Decimals)`). GenomeKeysUseTheGenesPrecision.
10. stats.csv opened with its header in Begin; the compatibility merge keeps the run's own keys; V-36 checks a trait
    default against its range (SpeciesBuilder), V-38 a stat or trait declared twice differently. ShortRunsHaveStats,
    CompatibilityKeepsTheRunsFailures.
11. CoverageTests: MUST rules matched by `\*\*([A-Z]+-\d+) \(MUST\)`; citations exclude test ids with `(?<![A-Z]-)`.
    CitationsAreReadCorrectly.
12. S13 filters origin "mutant", asserts the set non-empty and that births happened; the ladder's founders rest
    "Sometimes … when tired" so mutants appear.
13. T-OUT-03: an Ollama brain and mutator that read OLLAMA_API_KEY over fake transports; run files, logs, the editor
    log and every asset scanned.
14. EveryModulePrefabValidates: each module prefab prepared inside a species.
15. BusyAnimalsNeverDecide (removing `|| a.IsBusy` now fails).
16. World.OnDisable stops the run ("world disabled", or "… during <phase> of tick n" mid-tick); RunRecorder.OnDestroy
    finishes a run that started. LeavingPlayWritesEverything (PlayMode).
17. A gene whose nearest action is disabled is absent (Ownership.OnDisabledAction). T-CORE-03 asserts no gene line.
18. V-63 (W): an LLM mutation with a rate above 0 and no MutatorService, or a service without a client.
19. ActOrderTests: later actors see earlier moves (three act orders), eggs come from the Breed phase, babies age on
    their birth tick; the Searchers assertion in ActionConformance (ACT-04); EggTests.FromConceptionToBirth
    (REPRO-20/22/24, GENE-32).
20. Strikes and scavenging resolve the struck species' own Edible, enabled. ClonesAreEatenThroughTheirOwnEdible.
21. New errors: V-16 a World inside a World, V-17 duplicate sense labels, V-18 duplicate layer or brain names and
    reserved layer names ("world", "act-order", "/"); V-07 also for an id that is another species' display name and
    an id with "."; V-15 (W) a world module under a species; the nesting check is symmetric.
22. Secrets: keys like sk-, sk_, hf_, gsk_, gh[pousr]_, github_pat_, xox, AIza, Bearer, Basic and `?key=`, and
    credentials in URLs, in every serialized string of the World's components, enabled or not (V-61, "Clear it");
    run_info masks arguments that name a key, token, secret, password, auth, bearer, credential, serial or cookie.
23. Views: colliders off; ray masks drop the Ignore Raycast layer. SensesNeverSeeTheViews.
24. T-ACT-09 scans every module folder and the samples; V-49 the LLM context line must be one line.
25. Own codes and severities (V-15, V-26, V-37, V-38), fix buttons for 24 codes. ValidationCatalogueTests asserts
    (code, severity) for every fixture, FixesRepairTheirProblem applies each fix and validates again.
26. Weak tests strengthened: TraitsAreFixedAfterCreation (ANIM-17: SetTrait throws after creation unless the trait
    is changeable), BabiesAgeOnTheirBirthTick (ANIM-35), DecidersSenseTheStartOfTheTick (SENSE-04),
    GenomesMatchTheirSpecies (GENE-06), REPRO-03 (nearest partner, tie by id), MUT-13 (seeds and prompts),
    CacheKeysHoldModelAndTemperature, UnreachableServers (V-60), NoIterationOverHashCollections (RAND-05),
    ModulesKeepNoAnimalState.
27. stats.csv rewritten with the new columns when a species is added (AddedSpeciesGetColumns); mate's radius
    min(vision, partner range) (MateStaysWithinThePartnerRange); the samples' static lists read-only.
28. Contract wording, not code: 40 §2 #15.
29. Citations fixed in ConformanceTests, LongScenarioTests, RegressionTests R-04, HttpBrainTests, SenseTests.

Decisions:
- RAND-03 — one `act-order` stream for all species — kept: per-species streams would change every pinned hash for
  no behaviour a student needs; the contract should state the exception (40 §2 #15).
- V-20, V-36, V-62 — no fix button: the target, the value or the folder is a person's choice.
- Not done (small): MUT-30 with answers arriving out of order (applied by index, untested with a reordering
  transport); TICK-02 (no test of the phase set's minimum); PhaseConformance stays shallow.

### The open system
No grading (22 §0 stubs stay a way in); every reference module can be replaced or extended:
- Lifecycle: `SpeciesModule.OnBorn/OnDied` and `WorldModule.OnBorn/OnDied` (founders, babies, immigrants; every
  death after it is recorded), `WorldModule.OnSpeciesAdded`.
- Statistics: `IStatsColumns` on a species module (prefixed columns) or a world module (columns at the end); a death
  cause recorded by any code (`World.RecordDeath`) gets its `deaths_<cause>` column; a new column rewrites the header.
- `[RequiresModule(typeof(T))]`: V-26 (W) with an "Add T" fix when T is missing (skipped when the module reported its
  own error); the conformance suites and ActionConformance add T before testing. The samples Drink and DiesOfThirst use it.
- Food: `Diet.KillChanceAgainst(hunter, prey)`, `Diet.EnergyFrom(eater, entry, energy)`; Grazing, Striking,
  Scavenging, EatingEggs virtual; `InteractionContext.Feed(animal, energy, digest)` for custom interactions.
- Brains: `Brain.Memoizable` (false: no memo, no sharing, no cache; every query reaches the brain) and
  `DecisionQuery.AnimalId`.
- Space: several Cover services combine (hidden by any, the nearest of all); `NearestEntity<T>` across every
  EntitySystem of T (WorldQueries, SenseContext, ActContext).
- Views: `AnimalView.OnShow(animal)` each frame after placing (read only).
- Prompts: placeholders read fields, then properties; `{{` and `}}` write a brace.
- Actions: `AnimalAction.Hunts` (HuntAction true); the sample ChasedSense reads it instead of the class.
- Mutation: `MutationOperator.UsesMutatorService` (LlmMutation true): the Ollama mutator client reads the digest when
  any operator uses the service.
- run_info's `null_brain`: every species decided by the random brain, not only the default.
- Public or virtual for students' own phases and modules: MoveContext's constructor; DecisionState Add, AddAnswer,
  Clear; Decision.Answer, AnswerIndex; Species.RebuildPrompt, RemoveGone; SensePhase.Attachments, Context;
  ActContext.GrazeReach; Energy.Gain/Pay, DecisionSchedule.IsDue, MatingRule.Sexual/IsAdult/IsReady,
  Litter.ChildEnergy, Incubation.Ticks, Digestion.AfterMeal, Cover.Hides/IsHiddenFrom, CapRule.Cap,
  CarcassSystem.Create; Carcass and Egg no longer sealed. The Student assemblies reference EvoSim.Http and
  EvoSim.Samples.
- Tests: OpenHooksTests (ten), ViewPlayTests.ViewsHearTheirAnimal.
