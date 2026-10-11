---
uid: extend-recording
---

# Recording and statistics

Your module can leave four kinds of trace in a run's files ([Recording and outputs](../guides/recording-and-outputs.md)):

- **statistics columns**, numbers over time in `stats.csv`: implement <xref:EvoSim.IStatsColumns>;
- **causes of death**, each with its own `deaths_<cause>` column: `World.RecordDeath`, or a <xref:EvoSim.DeathRule>;
- **events of your own** in `events.jsonl`, one per occurrence, part of the run's history and of its hash: a
  <xref:EvoSim.SimEvent>;
- **listeners** that see every event as it is recorded, without changing anything: an <xref:EvoSim.IEventSink>.

Use a column for a quantity you want to plot, an event for something that happens and must be traceable later.

## Statistics columns

<xref:EvoSim.IStatsColumns> has two members:

| Member | What it does, when it is called |
|---|---|
| [StatColumns](xref:EvoSim.IStatsColumns.StatColumns) | the column names, without the species prefix, always in the same order |
| [StatValue](xref:EvoSim.IStatsColumns.StatValue(System.String)) | one column's value now; called by the <xref:EvoSim.RunRecorder> for every row, at the end of the record phase, every `Stats Every` ticks (100) |

Where the columns go depends on what implements it:

| Implemented on | Columns | Values |
|---|---|---|
| a <xref:EvoSim.SpeciesModule> | in the species' block, after its `act_<action>` columns, prefixed: `mean_age` becomes `prey_mean_age` | that species' |
| a <xref:EvoSim.TickPhase> or a <xref:EvoSim.WorldService> | at the end of the row, unprefixed (phases first) | the world's |

Name columns in lower case with underscores, like the built-in ones. A name already in the row is skipped. Values are
written with at most 4 decimals. A column that appears during the run rewrites the file's header, with empty cells in
the rows before it.

`StatValue` only reads: it must not change the world, record events or draw random numbers. A column never changes the
run or its hash.

### Example: the mean age

```csharp
using System.Collections.Generic;
using EvoSim;

// Adds the column <species>_mean_age to stats.csv: the mean age of the living animals, in ticks.
public class MeanAgeColumn : SpeciesModule, IStatsColumns
{
    static readonly string[] columns = { "mean_age" };

    public IEnumerable<string> StatColumns => columns;

    public double StatValue(string column)
    {
        int n = 0;
        double sum = 0;
        foreach (var a in Species.Animals)
            if (!a.IsGone) { n++; sum += a.Age; }
        return n > 0 ? sum / n : 0;
    }
}
```

Put it on any GameObject under the species, for example its `Stats` object. A world-level column works the same way
on a <xref:EvoSim.WorldService> under the World's `Recording` object.

## Causes of death

Every death goes through [World.RecordDeath](xref:EvoSim.World.RecordDeath(EvoSim.Animal,System.String)): it marks the
animal gone, records its `death` event with the cause, counts it, and calls every `OnDied` hook (ANIM-42). Any cause
string you pass gets its column, `<species>_deaths_<cause>`, with no other step.

There are two ways to kill an animal:

- **A death rule**, for a condition checked every tick on every animal (thirst, disease). The death phase asks it and
  records the death for you. List the causes it can give in <xref:EvoSim.DeathRule.Causes>: their columns then exist
  from the first row. See [Stats, costs and death rules](stats-and-death.md).
- **Your own phase**, for an event that strikes from outside. Call `RecordDeath`, then remove the dead from the
  species' list with `Species.RemoveGone()` once your loop is over:

```csharp
using EvoSim;
using UnityEngine;

// Lightning: each tick, each animal dies with a small chance, whatever its genes. Place it among the Phases, after Deaths.
public class LightningPhase : TickPhase
{
    [SerializeField, Range(0f, 1f), Tooltip("Chance per animal per tick.")] float chance = 0.0005f;

    public override void Run(TickContext t)
    {
        foreach (var s in World.AllSpecies)
        {
            var rng = t.Stream(s, "lightning");                 // the species' own stream (RAND-03)
            foreach (var a in s.Animals)
                if (!a.IsGone && rng.Chance(chance)) World.RecordDeath(a, "lightning");
            s.RemoveGone();                                     // after the loop: the dead leave the list
        }
    }
}
```

The column `prey_deaths_lightning` appears at the first such death; rows before it leave it empty.

## Events of your own

Build a <xref:EvoSim.SimEvent> with its kind, the tick, the animal's id (-1 for none) and the species id (null for a
world event), add fields with [With](xref:EvoSim.SimEvent.With(System.String,System.Object)), and record it with
[World.Events.Record](xref:EvoSim.EventLog.Record(EvoSim.SimEvent)). The egg system records lost eggs this way:

```cs
World.Events.Record(new SimEvent("egg_lost", World.Tick, egg.Id, egg.Species.Id)
    .With("parents", new List<int>(egg.Parents))
    .With("reason", reason));
```

A world event, from a seasons phase:

```cs
World.Events.Record(new SimEvent("season", t.Tick, -1, null).With("winter", winter));
```

Field values are numbers, strings, bools, lists and dictionaries of those. The line is written as canonical JSON: keys
sorted, at most 6 decimals. Record from simulation code only, a phase or a module's hook, once per occurrence and in a
fixed order: the event becomes part of the events hash. Never record from a view, an editor window or a sink.

## Listening to events

An <xref:EvoSim.IEventSink> receives every event, with its canonical line, right after it is hashed:
[OnEvent](xref:EvoSim.IEventSink.OnEvent(EvoSim.SimEvent,System.String)). The run recorder and the Event Log window
are sinks.

Register with [AddSink](xref:EvoSim.EventLog.AddSink(EvoSim.IEventSink)). The World makes a new event log each time it
initialises, so register in `Begin`, when the run's log exists, and unregister when the run stops. This world service
counts the kills of the last 100 ticks and adds them as a column:

```csharp
using System.Collections.Generic;
using EvoSim;

public class RecentKills : WorldService, IEventSink, IStatsColumns
{
    static readonly string[] columns = { "recent_kills" };
    readonly Queue<int> killTicks = new Queue<int>();      // the tick of each kill, oldest first

    public override void Begin()                            // the run's event log exists from here on
    {
        killTicks.Clear();
        World.Events.AddSink(this);
    }

    public override void OnRunStopped(string reason) => World.Events.RemoveSink(this);

    public void OnEvent(SimEvent e, string line)           // read only: record nothing, change nothing
    {
        if (e.Kind == "death" && "killed".Equals(e["cause"])) killTicks.Enqueue(e.Tick);
        while (killTicks.Count > 0 && killTicks.Peek() <= e.Tick - 100) killTicks.Dequeue();
    }

    public IEnumerable<string> StatColumns => columns;

    public double StatValue(string column)
    {
        int n = 0;
        foreach (int t in killTicks) if (t > World.Tick - 100) n++;
        return n;
    }
}
```

Put it on a GameObject under the World's `Recording` object. A sink only reads: it must not record events, change the
world or draw random numbers.

## What changes the events hash

The hash identifies a run ([Determinism and replay](../guides/determinism.md)). Two runs with the same hash recorded
the same events in the same order.

| Changes the hash | Leaves it unchanged |
|---|---|
| the run seed or the world seed: the chain starts from them | the wait mode, run speed, frame rate and ticks per frame (RAND-11) |
| anything that changes what happens: a setting, a module, a phase or its place, a brain's answers | the views, the editor windows, `LiveStatistics` |
| an event added, removed, reordered or given another field, such as your own `SimEvent` | statistics columns and event sinks that only read |
| a value written differently in an event | the compatibility mode, `Write Files`, the run's folder and `run_info.json` |

Draws from a stream of your own don't shift the numbers of other streams (RAND-02): only what your module then changes
in the world shows in the hash. Compare hashes on one machine (RAND-12).

## Test it

Build a world with a recorder that writes files, run it, and read `stats.csv`
([Testing your module](testing-your-module.md)):

```csharp
using System.IO;
using EvoSim;
using EvoSim.Testing;
using NUnit.Framework;

public class MeanAgeColumnTests : WorldFixture
{
    [Test]
    public void AddsItsColumn()
    {
        var b = New(19, name: "mean-age").Lab1(prey: 20, predators: 3).Recorder(true, "Logs/EvoSim/test-runs", "mean-age");
        b.Root.transform.Find("prey").gameObject.AddComponent<MeanAgeColumn>();
        var w = b.Build();
        w.Advance(200);
        w.Stop("test");
        var header = File.ReadAllLines(Path.Combine(w.Service<RunRecorder>().RunFolder, "stats.csv"))[0].Split(',');
        CollectionAssert.Contains(header, "prey_mean_age");
    }
}
```

`OpenHooksTests.ModulesAddStatsColumns` (in `Assets/EvoSim/Tests/EditMode/Core`) checks a species column, a world column
and a cause of death recorded by a phase, row by row. To check that a module doesn't change a run, compare
`w.Events.Hash` with and without it.

## Read next

- Probes that do this: `HookProbe` (a species module with columns) and `WorldHookProbe` (a world service with a column),
  in `Assets/EvoSim/Testing/Probes`.
- <xref:EvoSim.RunRecorder>, <xref:EvoSim.EventLog>, <xref:EvoSim.LiveStatistics>, <xref:EvoSim.CanonicalJson>.
- [Phases, services and world hooks](world-modules.md): phases, services, `Begin`, `OnBorn` and `OnDied`.
- Specification: [13 — Outputs and recording](../spec/13-outputs-and-recording.md),
  [12 — Tick order and determinism](../spec/12-tick-order-and-determinism.md).
