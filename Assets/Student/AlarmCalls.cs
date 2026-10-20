using System.Collections.Generic;
using EvoSim;
using UnityEngine;

/// <summary>
/// The alarm calls of every species: where and when an animal called, so that its kin can hear it for a few ticks.
/// </summary>
/// <remarks>
/// <para><b>What it adds.</b> A memory of calls shared by the whole world. <see cref="AlarmCallAction"/> adds a call,
/// <see cref="AlarmSense"/> asks for the nearest call an animal can hear, and <see cref="FadeAlarmCalls"/> forgets the old
/// ones every tick.</para>
/// <para><b>Mechanism.</b> A <see cref="WorldService"/>: one object under the World that other modules find in their
/// <c>Initialize</c> with <c>World.Service&lt;AlarmCalls&gt;()</c>. The calls are kept in a <see cref="List{T}"/>, in the
/// order they were made, so every query gives the same answer for the same run (RAND-05). It draws no random number.</para>
/// <para><b>In a scene.</b> Add it on a new child of the World's <c>Environment</c> object, named <c>Alarm calls</c>.</para>
/// </remarks>
public class AlarmCalls : WorldService
{
    [SerializeField, Min(1), Tooltip("Ticks a call stays audible after it was made (4: one decision period of the reference).")]
    int memory = 4;

    readonly List<AlarmCall> calls = new List<AlarmCall>();   // every call still remembered, oldest first

    /// <summary>The number of ticks a call stays audible after the tick it was made in.</summary>
    public int Memory => memory;

    /// <summary>The calls still remembered, oldest first; some may be too old to hear until <see cref="Forget"/> runs.</summary>
    public IReadOnlyList<AlarmCall> Calls => calls;

    /// <summary>The number of calls made since the run started, by every species.</summary>
    public int CallsMade { get; private set; }

    /// <summary>Forgets every call and resets the count; called by the World before the run, and in Edit mode.</summary>
    /// <inheritdoc/>
    public override void Initialize()
    {
        calls.Clear();
        CallsMade = 0;
    }

    /// <summary>Records a call made by an animal where it stands now.</summary>
    /// <param name="caller">The animal that calls.</param>
    /// <param name="tick">The tick of the call.</param>
    /// <param name="range">How far the call carries, in meters.</param>
    public void Register(Animal caller, int tick, float range)
    {
        calls.Add(new AlarmCall(caller.Species, caller.Id, caller.Position, tick, range));
        CallsMade++;
    }

    /// <summary>True when a call is still audible at a tick: made at most <see cref="Memory"/> ticks before it.</summary>
    /// <param name="call">A remembered call.</param>
    /// <param name="now">The tick of the listener.</param>
    /// <returns>Whether the call can still be heard.</returns>
    public bool IsAudible(AlarmCall call, int now) => now - call.Tick <= memory;

    /// <summary>
    /// The distance to the nearest call a listener hears now: made by another animal of its species, still audible, and
    /// within the call's range. Hidden callers are heard too: a call is a sound, not a sight.
    /// </summary>
    /// <param name="listener">The animal that listens.</param>
    /// <param name="now">The tick of the listener.</param>
    /// <returns>The distance on the ground to the nearest call heard, in meters, or null when no call is heard.</returns>
    public float? NearestHeard(Animal listener, int now)
    {
        float? best = null;
        foreach (var call in calls)                                    // a list: the same order in every run (RAND-05)
        {
            if (call.Species != listener.Species || call.CallerId == listener.Id || !IsAudible(call, now)) continue;
            float d = GroundDistance(call.Position, listener.Position);
            if (d <= call.Range && (best == null || d < best.Value)) best = d;
        }
        return best;
    }

    /// <summary>Forgets the calls that can no longer be heard at a tick.</summary>
    /// <param name="now">The current tick.</param>
    /// <returns>The number of calls forgotten.</returns>
    public int Forget(int now) => calls.RemoveAll(c => !IsAudible(c, now));

    /// <summary>Sets the service from code (tests).</summary>
    /// <param name="memoryTicks">The ticks a call stays audible, at least 1.</param>
    public void Configure(int memoryTicks) => memory = Mathf.Max(1, memoryTicks);

    /// <summary>The distance between two points on the ground, ignoring height, in meters.</summary>
    /// <param name="a">One point.</param>
    /// <param name="b">The other point.</param>
    /// <returns>The horizontal distance.</returns>
    static float GroundDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
}

/// <summary>One alarm call: who called, where, when and how far it carries.</summary>
public readonly struct AlarmCall
{
    /// <summary>The caller's species: only its kin hear the call.</summary>
    public readonly Species Species;
    /// <summary>The caller's id: an animal doesn't hear its own call.</summary>
    public readonly int CallerId;
    /// <summary>Where the caller stood when it called.</summary>
    public readonly Vector3 Position;
    /// <summary>The tick of the call.</summary>
    public readonly int Tick;
    /// <summary>How far the call carries, in meters.</summary>
    public readonly float Range;

    /// <summary>Makes a call.</summary>
    /// <param name="species">The caller's species.</param>
    /// <param name="callerId">The caller's id.</param>
    /// <param name="position">Where the caller stood.</param>
    /// <param name="tick">The tick of the call.</param>
    /// <param name="range">How far the call carries, in meters.</param>
    public AlarmCall(Species species, int callerId, Vector3 position, int tick, float range)
    {
        Species = species;
        CallerId = callerId;
        Position = position;
        Tick = tick;
        Range = range;
    }
}
