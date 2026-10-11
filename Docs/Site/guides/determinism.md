---
uid: guide-determinism
---

# Determinism and replay

The same scene, seed and brain answers give the same run, event for event, on the same machine and build (RAND-11). It
doesn't matter how the run is driven: Responsive or Freeze, one tick per frame or a thousand, views on or off, in the
editor, headless or in a player build. That is what makes a comparison between two gene pools fair, lets a long run with
a language model be replayed without calling the model again, and turns "it behaved oddly at tick 4 000" into a bug you
can reproduce. This page explains the mechanisms and the rules your code keeps so that it stays true.

## Random streams

Every random number of the simulation comes from a <xref:EvoSim.RandomStream>: a PCG32 generator whose starting state is
the SHA-256 of the text `seed/name`, for example `1234/prey/sampling`. The World's
<xref:EvoSim.RandomStreams> hands out one stream per name. The same seed and name always give the same numbers, and draws
from one stream never shift another stream's numbers (RAND-02).

Streams are per purpose and, where a species is involved, per species (RAND-03):

| Stream | Used for |
|---|---|
| `world`, `world/<layer>` | the map: terrain, cover patches, initial food; they use the **world seed** |
| `<layer>` (e.g. `Grass`) | the regrowth of one food layer |
| `act-order` | the order animals act in (ACT-32) |
| `<species>/founders` | founder and newcomer genomes and positions |
| `<species>/sampling` | drawing actions from the brain's probabilities |
| `<species>/actions` | wandering, sidesteps, strike rolls |
| `<species>/litter` | litter sizes, babies' headings |
| `<species>/mutation` | crossover and mutation choices, drawn at conception |
| `<species>/migration` | who leaves above the cap |

So adding a species doesn't change the draws of the others, and adding a food layer doesn't change another layer's map.
A purpose of your own is just a new name: `t.Stream(s, "lightning")` makes `prey/lightning` on first use. The
**world seed** is the run seed unless **Separate World Seed** is on: then many runs can share one map while everything
else varies (RAND-04). The names `world` and `act-order` are reserved: no species id (V-07) or layer name (V-18) may take
them.

## The events hash

Every event (a founder, a birth, a death, an immigrant, an egg lost, a new species) is one line of canonical JSON, keys
sorted (RAND-10). The World's <xref:EvoSim.EventLog> chains them into a running SHA-256: it starts from the hash of the
two seeds, and each event's hash covers the previous hash and the event's line. The final hash identifies the run: two
runs with the same hash recorded the same events in the same order, and two seeds never share a hash (RAND-13).

| Where | What |
|---|---|
| [World.Events.Hash](xref:EvoSim.EventLog.Hash), [ShortHash](xref:EvoSim.EventLog.ShortHash) | 64 hex digits, and the first 16 |
| `summary.json` in the run folder | `events_hash`, `events_sha`, and `events` (the count) |
| a headless run's result line, the player with `-quitAtEnd` | the hash at the end |

## What a replay needs

| Ingredient | Where it lives |
|---|---|
| the configuration | the scene; `run_info.json` lists every serialized setting of the World's tree under `settings` |
| the seed and the world seed | the World's **Seed** and **World Seed**; `run_info.json` |
| the brain's answers | within a run, the decision memo; across runs, the answer cache, `Library/EvoSim/AnswerCache` by default, which also keeps the mutator's answers |
| the same machine and build | floating-point results may differ across machines and scripting backends; hashes are compared on one machine only (RAND-12) |

A language model may not answer the same question twice in the same way. The answer cache removes that doubt: a live
run writes every answer to it, and running the same command again replays from it with no model call (RAND-21, test
T-RAND-06). To replay in a player build, give it the cache folder with `-cache`. A brain with a memory of its own is
neither memoised nor cached: its runs repeat only if the brain itself does.

## The same run everywhere

The World makes the run independent of how it is driven:

- a decision always waits for its answers; it is never made on old observations (SPACE-13);
- every random draw happens in a phase, on the main thread, at a fixed point of the tick: mutation choices are drawn at
  conception, not when the mutator answers;
- the views, the camera and the overlays only read the simulation (SPACE-12); views have no collider and sit on a layer
  the sense rays ignore.

The tests check it: Freeze and Responsive give the same hash with late answers (T-SPACE-07, R-04), one tick per call
gives the same hash as ten (R-04), views shown or hidden give the same hash (T-SPACE-06), a late mutator changes nothing
(T-MUT-10), the reference scenes keep their pinned hashes (R-01, `Tests/Golden/hashes.json`), and each scenario run twice
gives one hash (`EvoSim.Batch.RunScenarios`).

## The rules that keep it

1. **No other randomness** (RAND-01): no `UnityEngine.Random`, no `System.Random`, no `Guid.NewGuid`, no clock used as a
   seed. Draw from the stream you are given, or ask the World for one by name.
2. **One stream per purpose and species** (RAND-02, RAND-03): a species' draws come from its own streams, so adding a
   species changes nothing for the others.
3. **Ordered loops only** (RAND-05): loop over lists, in creation or id order. Never decide by iterating a `Dictionary`
   or a `HashSet`, whose order is not defined; looking a key up in one is fine.
4. **No time.** Never read `Time.deltaTime`, `Time.time`, a frame count or the wall clock in simulation code: the tick is
   the only clock (SPACE-10).
5. **Views only read** (SPACE-12). A view never writes to an animal or to the world.
6. **Asynchronous work doesn't leak its timing.** Work on another thread never touches the World or a Unity object, and
   its result doesn't depend on when it finishes. The phase that needs it waits through `WaitsFor` (TICK-03).
7. **`WaitsFor` only looks.** In Responsive mode it is asked again at every call while the tick waits: it must not draw
   random numbers or change the world.
8. **No statics** (ARCH-07). Two Worlds may run side by side, and a World runs again after `Initialize`: keep state in
   your module's fields and reset it in `Initialize`.

## The static checks

`Tests/EditMode/Core/StaticChecks.cs` (tier T0, *Window ▸ General ▸ Test Runner*, EditMode) reads the source code
instead of running it:

| Test | What it looks for |
|---|---|
| `NoGlobalRandomness` (T-RAND-01) | `UnityEngine.Random`, `new System.Random(`, `Guid.NewGuid`, `DateTime.Now.Ticks` and the like |
| `NoIterationOverHashCollections` (T-RAND-04) | a `foreach` over a `Dictionary` or a `HashSet` (or their `Keys`, `Values`) |
| `ModulesKeepNoAnimalState` (T-CORE-05) | a species module field that holds animals or maps animal ids |
| `CoreNamesNoSpecies` (T-CORE-08) | a species or action name written in the core code |

They scan `Assets/EvoSim/Runtime`, `Http` and `Samples`, not your folder: in your own code the rules are yours to keep.
The conformance suites check behaviour instead. A phase, for example, must run without animals and leave a populated
world's hash unchanged when it is switched off and on (T-TICK). Add a test of your own that runs twice. This one tests
the `LightningPhase` of [A tick, phase by phase](the-tick.md#births-and-deaths-during-a-tick):

```csharp
public class LightningTests : WorldFixture
{
    [Test, Description("RAND-11: the same seed gives the same run")]
    public void SameSeedSameRun()
    {
        string Run(string name)
        {
            var w = New(7, name: name).Lab1(prey: 20, predators: 3).Phase<LightningPhase>().Build();
            w.Advance(300);
            return w.Events.Hash;
        }
        Assert.AreEqual(Run("first"), Run("second"));
    }
}
```

## When two runs disagree

1. **Compare the configurations.** Diff the two `run_info.json` files: seed, world seed, wait mode, brain, controls and
   every serialized setting are there. A difference here is usually the whole story.
2. **Find the first differing event.** Compare the two `events.jsonl` files line by line, for example with
   `git diff --no-index runA/events.jsonl runB/events.jsonl`. The `t` field of the first differing line is the first
   tick where the runs visibly part. Events are births and deaths, so the real cause can be a few ticks earlier.
3. **Find the first differing tick.** Build both worlds in a test, advance them one tick at a time, and compare a
   fingerprint of every animal after each tick:

   ```csharp
   static string Fingerprint(World w) => string.Join(";", w.AllSpecies.SelectMany(s => s.Animals)
       .Select(x => $"{x.Id}:{x.Position.x:R},{x.Position.z:R},{x.Action}"));

   for (int t = 0; t < 300; t++)
   {
       a.Advance(1); b.Advance(1);
       Assert.AreEqual(Fingerprint(a), Fingerprint(b), $"the runs part at tick {t}");
   }
   ```

4. **Find the phase.** Advance both worlds to the tick before, then run that tick's phases by hand, one by one, as the
   tests do with `w.Phases[i].Run(w.Context)`, and compare after each. This skips the waiting, so use a brain that
   answers at once, such as the random brain, and don't advance these worlds afterwards.
5. **Look for the usual causes** in the code that runs there: an iteration over a `Dictionary` or `HashSet`, a draw
   from the wrong stream or in `WaitsFor`, a `static` field, state not reset in `Initialize`, a read of `Time`, or a
   result computed on another thread.

## Go further

- [A tick, phase by phase](the-tick.md): waiting, stop conditions, the tick context.
- [Decisions and brains](brains-and-decisions.md): the memo and the answer cache.
- [Running and building](running-and-building.md): headless runs, batch scenarios, the player's `-cache`.
- [Recording and outputs](recording-and-outputs.md): the files of a run folder.
- Specification: [12 — Tick order and determinism](../spec/12-tick-order-and-determinism.md).
