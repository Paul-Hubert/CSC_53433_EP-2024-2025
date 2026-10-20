using EvoSim;
using EvoSim.Testing;
using NUnit.Framework;

/// <summary>Tests of the alarm calls: <see cref="AlarmCalls"/>, <see cref="AlarmCallAction"/>, <see cref="AlarmSense"/> and
/// <see cref="FadeAlarmCalls"/>, in worlds built in code.</summary>
public class AlarmCallsTests : WorldFixture
{
    /// <summary>
    /// A 60 m world with the act phase, the alarm calls and the fading phase, two species that can call and hear, "prey"
    /// (alarm, flee, rest) and "deer" (alarm, rest), and a "predator" that threatens the prey only. No species has a brain:
    /// tests choose the actions, the call as an overlay beside a carrier (ACT-07).
    /// </summary>
    /// <param name="cost">The energy each tick of calling costs, in points.</param>
    /// <param name="name">The world's name.</param>
    /// <returns>The world, initialised, with no animals.</returns>
    World Meadow(float cost = 0.5f, string name = "meadow") =>
        New(name: name).Flat(60, 60).Food(0f, 0f).Carcasses().Phase<ActPhase>().Phase<FadeAlarmCalls>()
            .Service<AlarmCalls>("Alarm calls", c => c.Configure(4))
            .Species("prey", s => s.PreyBody().Action<AlarmCallAction>("alarm", a => a.Configure(20f, cost)).Action<FleeAction>("flee")
                                   .Action<RestAction>("rest").Sense<AlarmSense>(x => x.Configure(10f)))
            .Species("deer", s => s.PreyBody().Action<AlarmCallAction>("alarm").Action<RestAction>("rest").Sense<AlarmSense>())
            .Species("predator", s => s.PredatorBody().Action<RestAction>("rest"))
            .Build();

    /// <summary>Places a resting predator.</summary>
    /// <param name="w">The world, built by <see cref="Meadow"/>.</param>
    /// <param name="x">Its x coordinate, in meters.</param>
    /// <param name="z">Its z coordinate, in meters.</param>
    /// <returns>The predator.</returns>
    static Animal Predator(World w, float x, float z)
    {
        var p = Place.Animal(w.FindSpecies("predator"), Place.At(x, z));
        p.Choose("rest");
        return p;
    }

    /// <summary>The token an animal's alarm sense reads now.</summary>
    /// <param name="w">The world.</param>
    /// <param name="a">The listener.</param>
    /// <returns>"none", "close" or "far".</returns>
    static string Hears(World w, Animal a)
    {
        var sense = a.Species.Module<AlarmSense>();
        return sense.Tokens[sense.Read(a, new SenseContext(w))];
    }

    [Test, Description("A call is heard by kin within range, close or far; not by the caller, kin out of range, or another species")]
    public void KinWithinRangeHearTheCall()
    {
        var w = Meadow();
        var prey = w.FindSpecies("prey");
        Predator(w, 5, 10);
        var caller = Place.Animal(prey, Place.At(10, 10));         // its carrier runs it away from the predator; it calls from there
        var near = Place.Animal(prey, Place.At(15, 10));           // a few meters from the call: close
        var far = Place.Animal(prey, Place.At(28, 10));            // over 10 m: far, within the 20 m range
        var outOfRange = Place.Animal(prey, Place.At(40, 10));     // beyond the range
        var deer = Place.Animal(w.FindSpecies("deer"), Place.At(12, 10));
        caller.Choose("flee", "alarm");
        foreach (var a in new[] { near, far, outOfRange, deer }) a.Choose("rest");
        Assert.AreEqual("none", Hears(w, near), "nobody called yet");

        w.Advance(1);
        Assert.AreEqual(1, w.Service<AlarmCalls>().CallsMade);
        Assert.AreEqual("close", Hears(w, near));
        Assert.AreEqual("far", Hears(w, far));
        Assert.AreEqual("none", Hears(w, outOfRange), "out of range");
        Assert.AreEqual("none", Hears(w, deer), "another species");
        Assert.AreEqual("none", Hears(w, caller), "an animal doesn't hear its own call");
    }

    [Test, Description("A call fades: heard for Memory ticks after the last call, then forgotten by the fading phase")]
    public void CallsFade()
    {
        var w = Meadow();
        var prey = w.FindSpecies("prey");
        var calls = w.Service<AlarmCalls>();
        Predator(w, 5, 10);
        var caller = Place.Animal(prey, Place.At(10, 10));
        var near = Place.Animal(prey, Place.At(15, 10));
        caller.Choose("flee", "alarm");                           // one call, then the overlay ends: silent from now on
        near.Choose("rest");
        w.Advance(1);
        w.Advance(calls.Memory - 1);
        Assert.AreEqual("close", Hears(w, near), "still heard within Memory ticks");
        w.Advance(2);
        Assert.AreEqual("none", Hears(w, near), "faded");
        Assert.AreEqual(0, calls.Calls.Count, "the fading phase forgot it");
        Assert.AreEqual(1, calls.CallsMade);
    }

    [Test, Description("The call is an overlay (ACT-07): made on the tick of the decision while the carrier, flee, runs the animal away; it costs once")]
    public void CallingWhileFleeing()
    {
        float SpentIn(World w)
        {
            Predator(w, 15, 20);
            var a = Place.Animal(w.FindSpecies("prey"), Place.At(20, 20));
            a.Choose("flee", "alarm");
            Assert.AreEqual("flee + alarm", a.ActionLabel);
            float before = a.StatOf("energy");
            w.Advance(3);
            Assert.Greater(a.Position.x, 22f, "the carrier runs the caller away from the predator");
            Assert.AreEqual(1, w.Service<AlarmCalls>().CallsMade, "one call, on the first tick");
            Assert.AreEqual("flee", a.ActionLabel, "the overlay ended after its tick; the flight goes on");
            return before - a.StatOf("energy");
        }
        float free = SpentIn(Meadow(0f, "free"));
        float paid = SpentIn(Meadow(2f, "paid"));
        Assert.AreEqual(2f, paid - free, 1e-4f, "2 points, once");
    }

    [Test, Description("With no predator in sight the animal may still call, a false alarm: it calls once and goes on with its carrier")]
    public void FalseAlarm()
    {
        var w = Meadow();
        var a = Place.Animal(w.FindSpecies("prey"), Place.At(20, 20));
        a.Choose("rest", "alarm");
        w.Advance(2);
        Assert.IsFalse(a.Searching, "the overlay plans no search");
        Assert.AreEqual(Place.At(20, 20), a.Position, "the carrier, rest, kept it still");
        Assert.AreEqual(1, w.Service<AlarmCalls>().CallsMade);
    }

    [Test, Description("The prompt lists the action and its rule, and the engine's line for actions done in passing (PROMPT-01, PROMPT-03)")]
    public void PromptLines()
    {
        var text = Meadow().FindSpecies("prey").Prompt.Text;
        StringAssert.Contains("- alarm: give an alarm call that kin within 20 meters hear\n", text);
        StringAssert.Contains("An alarm call costs 0.5 energy; kin within 20 meters hear it.\n", text);
        StringAssert.Contains("Some actions are done in passing (alarm): the animal also does its next most likely choice.\n", text);
    }

    [Test, Description("The sense's tokens and texts, V1 and V2, with distances in meters (SENSE-10, SENSE-12)")]
    public void SenseTexts()
    {
        var w = Meadow();
        var sense = w.FindSpecies("prey").Module<AlarmSense>();
        CollectionAssert.AreEqual(new[] { "none", "close", "far" }, sense.Tokens);
        Assert.AreEqual("Alarm: close.", sense.Write(1, TextStyle.V1));
        Assert.AreEqual("No kin is giving an alarm call.", sense.Write(0, TextStyle.V2));
        Assert.AreEqual("A kin gave an alarm call less than 10 meters away.", sense.Write(1, TextStyle.V2));
        Assert.AreEqual("A kin gave an alarm call 10 meters away or more.", sense.Write(2, TextStyle.V2));
    }

    [Test, Description("Without an AlarmCalls service nothing is called and the sense hears nothing; validation warns (V-26)")]
    public void WithoutTheServiceNothingBreaks()
    {
        var w = New(name: "no service").Flat(40, 40).Food(0f, 0f).Phase<ActPhase>()
            .Species("prey", s => s.PreyBody().Action<AlarmCallAction>("alarm").Action<RestAction>("rest").Sense<AlarmSense>()).Build();
        var a = Place.Animal(w.FindSpecies("prey"), Place.At(20, 20));
        a.Choose("rest", "alarm");
        w.Advance(2);
        Assert.AreEqual("none", Hears(w, a));
        StringAssert.Contains("V-26", w.LastReport.ToString());
    }

    [Test, Description("RAND-10: the Lab ecology with alarm calls and the random brain gives the same events hash twice, and calls are made")]
    public void SameSeedSameHash()
    {
        (string hash, int calls) Run()
        {
            var w = New(42, name: "alarm hash").Ecology(40f,
                    prey: s => s.Population(16).Action<AlarmCallAction>("alarm", founders: new[] { "Call out whenever you see a predator." })
                                .Sense<AlarmSense>(),
                    predator: s => s.Population(3))
                .Service<AlarmCalls>("Alarm calls").ReferencePhases().Phase<FadeAlarmCalls>().DefaultBrain<RandomBrain>().Build();
            w.Advance(200);
            return (w.Events.Hash, w.Service<AlarmCalls>().CallsMade);
        }
        var first = Run();
        var second = Run();
        Assert.AreEqual(first.hash, second.hash);
        Assert.AreEqual(first.calls, second.calls);
        Assert.Greater(first.calls, 0, "the random brain chose to call");
    }
}
