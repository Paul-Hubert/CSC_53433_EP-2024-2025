---
uid: extend-worked-example-alarm-calls
---

# Worked example: alarm calls

This page follows one change from idea to tested scene, written the way you would write yours: in `Assets/Student`,
without touching `Assets/EvoSim`. The idea: a prey can **call** in passing, as it runs away or hides, at a cost in energy;
its kin nearby **hear** the call and can flee or hide before they see the danger themselves. Evolution decides whether
calling pays.

The change touches the four kinds of extension:

| Piece | Class | Kind | Page |
|---|---|---|---|
| the world remembers calls | `AlarmCalls` and `FadeAlarmCalls` | a <xref:EvoSim.WorldService> and a <xref:EvoSim.TickPhase> | [Phases, services and world hooks](world-modules.md) |
| an animal calls | `AlarmCallAction` | an <xref:EvoSim.AnimalAction> done in passing (an overlay) with an interaction | [An action](action.md) |
| evolution tunes calling | a <xref:EvoSim.TextGene> on the action | a gene, no code | [Genes](genes.md) |
| kin hear the call | `AlarmSense` | a <xref:EvoSim.Sense> | [A sense](sense.md) |

The files are `Assets/Student/AlarmCalls.cs`, `FadeAlarmCalls.cs`, `AlarmCallAction.cs`, `AlarmSense.cs`, the tests in
`Assets/Student/Tests/AlarmCallsTests.cs`, and the scene builder in `Assets/Student/Editor/AlarmCallsScene.cs`. The code
below is that code, shortened: the `///` comments are left out, and so are the members only the tests and the scene
builder use (`Configure`, `Memory`, `Calls`, `Range`, `Cost`), `FadeAlarmCalls.Validate`, and `AlarmSense`'s call to
`base.Initialize()`. The action's prompt rule and `IsRelevant` are described after its code.

## 1. The world: a list of calls

Several modules share the calls, so they live in a service under the World. A call is a small struct: who, where, when,
how far. The calls are kept in a `List`, in the order they were made, so every query gives the same answer in every run
(RAND-05). The service draws no random number.

```csharp
public readonly struct AlarmCall
{
    public readonly Species Species; public readonly int CallerId; public readonly Vector3 Position;
    public readonly int Tick; public readonly float Range;
    public AlarmCall(Species s, int id, Vector3 p, int tick, float range) { Species = s; CallerId = id; Position = p; Tick = tick; Range = range; }
}

public class AlarmCalls : WorldService
{
    [SerializeField, Min(1)] int memory = 4;               // ticks a call stays audible: one decision period
    readonly List<AlarmCall> calls = new List<AlarmCall>();
    public int CallsMade { get; private set; }

    public override void Initialize() { calls.Clear(); CallsMade = 0; }
    public void Register(Animal caller, int tick, float range)
    {
        calls.Add(new AlarmCall(caller.Species, caller.Id, caller.Position, tick, range));
        CallsMade++;
    }
    public bool IsAudible(AlarmCall c, int now) => now - c.Tick <= memory;
    public float? NearestHeard(Animal listener, int now)
    {
        float? best = null;
        foreach (var c in calls)                           // a list: the same order in every run
        {
            if (c.Species != listener.Species || c.CallerId == listener.Id || !IsAudible(c, now)) continue;
            float d = new Vector2(c.Position.x - listener.Position.x, c.Position.z - listener.Position.z).magnitude;
            if (d <= c.Range && (best == null || d < best.Value)) best = d;
        }
        return best;
    }
    public int Forget(int now) => calls.RemoveAll(c => !IsAudible(c, now));
}
```

Old calls are already ignored by `NearestHeard`. A phase forgets them so the list stays short. It does nothing in a world
without the service (TICK-07):

```csharp
public class FadeAlarmCalls : TickPhase
{
    AlarmCalls calls;
    public override void Initialize() => calls = World.Service<AlarmCalls>();
    public override void Run(TickContext t) { if (calls != null) calls.Forget(t.Tick); }
}
```

## 2. The action: a call done in passing

A chosen action runs every tick until the next decision (ACT-02), up to a whole decision period. A first version stood
still to call: next to a predator, for up to 4 ticks, the caller paid 4 times and was caught, and evolution would only
have learnt to keep quiet. That was an artefact of timing, not a result. So the call is an **overlay**, an action done
in passing ([Actions done in passing](action.md#actions-done-in-passing), ACT-07): `Overlay` returns true. When the
brain's draw picks `alarm`, the engine draws a second action, the carrier, from the rest of the row (flee, hide, eat…),
which moves the animal; the call itself happens once, on the tick of the decision.

`Act` only plans (ACT-06), and plans no move: it asks for an interaction, and the act phase ignores an overlay's intent.
The interaction runs after the carrier's, once the animal moved. It is the only place that changes the caller (its
energy, through <xref:EvoSim.Energy>) and the world (a new call). Its reach is unlimited, as in the sample
<xref:EvoSim.Samples.DragAction>: the call happens wherever the carrier took the animal. Nothing stops a call with no
predator in sight, a false alarm: the gene and the brain decide whether that pays. The `[RequiresModule]` attribute
makes validation warn when the World has no `AlarmCalls` (V-26), and makes the conformance suite add one.

```csharp
[RequiresModule(typeof(AlarmCalls))]
public class AlarmCallAction : AnimalAction
{
    [SerializeField, Min(1f)] float range = 20f;           // meters
    [SerializeField, Min(0f)] float cost = 0.5f;           // energy per call
    AlarmCalls calls;
    Energy energy;

    public override bool Overlay => true;                  // call, and do the next most likely choice (flee, hide…)
    protected override string DefaultDescription => $"give an alarm call that kin within {Bands.Number(range)} meters hear";
    public override void Initialize() { calls = World.Service<AlarmCalls>(); energy = Species.Module<Energy>(); }
    public override void Act(Animal a, ActContext c)
    {
        if (calls != null) c.OnArrival(new Call(this));    // no intent: the carrier moves the animal
    }

    sealed class Call : IInteraction
    {
        readonly AlarmCallAction action;
        public Call(AlarmCallAction owner) { action = owner; }
        public float Reach => float.PositiveInfinity;      // wherever the carrier took the caller
        public void Apply(Animal a, InteractionContext c)
        {
            if (action.energy != null) action.energy.Pay(a, action.cost);
            action.calls.Register(a, c.Tick, action.range);
        }
    }
}
```

The real file also writes a prompt rule (*"An alarm call costs 0.5 energy; kin within 20 meters hear it."*) and declares
the action relevant when the `Predator` sense sees one (CTRL-10). The engine adds its own rule for the species, *"Some
actions are done in passing (alarm): the animal also does its next most likely choice."* The species needs another
action beside the overlay to carry the animal (V-19), which the prey of HideVsFlee has six times over.

## 3. The gene: how often to call

An action needs a <xref:EvoSim.TextGene> on its GameObject (V-05). No code: set its **Founders** to
*"Call out whenever you see a predator."*, *"Stay silent, save your energy."*, *"Warn your kin when danger is near."* and
*"Call only when you are safe in cover."*, keep **Has Neutral** (*"No preference."*), and set **Contrast Pro** to
*"Always give an alarm call."* and **Contrast Anti** to *"Never give an alarm call."* for the brain tests (GENE-23). The
species' `LlmMutation` then rewrites these sentences, and selection keeps the ones that pay.

## 4. The sense: did kin call?

Three tokens, a V2 sentence per token with the distance in meters (SENSE-12), and no change to the world (SENSE-03):

```csharp
[RequiresModule(typeof(AlarmCalls))]
public class AlarmSense : Sense
{
    static readonly IReadOnlyList<string> tokens = System.Array.AsReadOnly(new[] { "none", "close", "far" });
    [SerializeField, Min(0.5f)] float closeDistance = 10f;
    AlarmCalls calls;

    public override IReadOnlyList<string> Tokens => tokens;
    protected override string DefaultLabel => "Alarm";
    public override void Initialize() => calls = World.Service<AlarmCalls>();
    public override int Read(Animal a, SenseContext s)
    {
        float? d = calls != null ? calls.NearestHeard(a, s.Tick) : null;
        return d == null ? 0 : d.Value <= closeDistance ? 1 : 2;
    }
    public override string Write(int token, TextStyle style)
    {
        if (style != TextStyle.V2) return $"{Label}: {tokens[token]}.";
        string near = Bands.Number(closeDistance);
        return token == 1 ? $"A kin gave an alarm call less than {near} meters away."
             : token == 2 ? $"A kin gave an alarm call {near} meters away or more." : "No kin is giving an alarm call.";
    }
}
```

## 5. The scene

`Assets/Student/Scenes/AlarmCalls.unity` is the reference HideVsFlee scene plus four objects:

1. Under the World's `Environment`: `Alarm calls`, with `AlarmCalls`.
2. Under the World's `Phases`, just after `Act`: `Fade alarm calls`, with `FadeAlarmCalls`.
3. Under `Prey ▸ Actions`, last: `alarm`, with `AlarmCallAction` and its text gene.
4. Under `Prey ▸ Senses`, last: `Alarm`, with `AlarmSense`.

You can add them by hand. The example builds them with a short editor script instead, so the scene can be rebuilt
identically: *EvoSim ▸ Student ▸ Build Alarm Calls Scene*, or from the command line with the editor closed:
`Unity.exe -batchmode -nographics -projectPath . -executeMethod AlarmCallsScene.BuildFromCommandLine`.
The script lives in `Assets/Student/Editor` with its own editor-only assembly, `Student.Editor`.

## 6. The tests

`AlarmCallsTests` derives from <xref:EvoSim.Testing.WorldFixture>. It checks that a call is heard close or far by kin
within range, and not by the caller, kin out of range or another species; that a call fades after `memory` ticks; that
the call is made once while the carrier, flee, runs the animal away, and costs once; that a false alarm is possible; the
prompt lines; the sense's texts; that nothing breaks without the service; and that the same seed gives the same events
hash. A test gives an animal a carrier and an overlay with `Choose(carrier, overlay)`. How often the overlay and each
carrier are drawn is the engine's business, tested in `OverlayTests`. The heart of the first test:

```csharp
public class AlarmHearingTests : WorldFixture
{
    [Test]
    public void KinWithinRangeHearTheCall()
    {
        var w = New().Flat(60, 60).Food(0f, 0f).Carcasses().Phase<ActPhase>().Service<AlarmCalls>("Alarm calls")
            .Species("prey", s => s.PreyBody().Action<AlarmCallAction>("alarm").Action<FleeAction>("flee")
                                   .Action<RestAction>("rest").Sense<AlarmSense>())
            .Species("predator", s => s.PredatorBody().Action<RestAction>("rest")).Build();
        var prey = w.FindSpecies("prey");
        Place.Animal(w.FindSpecies("predator"), Place.At(5, 10)).Choose("rest");
        var caller = Place.Animal(prey, Place.At(10, 10));            // flees, and calls where the flight took it
        var near = Place.Animal(prey, Place.At(15, 10));
        caller.Choose("flee", "alarm");                               // the carrier, then the overlay
        near.Choose("rest");
        w.Advance(1);
        var sense = prey.Module<AlarmSense>();
        Assert.AreEqual("close", sense.Tokens[sense.Read(near, new SenseContext(w))]);
    }
}
```

The four classes also go through the conformance suites: in the Test Runner, `ConformanceTests` ▸ `SenseConformance`
▸ `AlarmSense`, `ActionConformance` ▸ `AlarmCallAction` and `PhaseConformance` ▸ `FadeAlarmCalls`.

## 7. A run

*EvoSim ▸ Student ▸ Run Alarm Calls Headless (L0)* runs the scene for 400 ticks with the random brain, twice, and logs
what the calls did. With seed 1234 the prey decided 3 204 times and drew
`alarm` 438 times; 438 calls were made (one per draw, beside its carrier, predator in sight or not), and 2 333 decisions
were taken while hearing a call, 800 of which fled or hid. The two runs give the same events hash. The random brain chooses `alarm`
without looking; with a real brain (L1, L2) the gene and the sense start to matter, and you can compare how often the prey calls,
and how many prey survive, with and without the `alarm` action.

## Read next

- [The samples](samples.md): thirst, water, seasons and slopes, written the same way.
- [Testing your module](testing-your-module.md) and [Determinism and replay](../guides/determinism.md).
- [Documenting your code](documenting-your-code.md): the `///` comments of the real files follow it.
