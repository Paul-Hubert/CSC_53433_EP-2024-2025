using System.Collections.Generic;
using EvoSim;
using UnityEngine;

/// <summary>A sense: whether the animal hears an alarm call of its kin, and whether it is close or far.</summary>
/// <remarks>
/// <para><b>What it adds.</b> Three tokens, <c>none</c>, <c>close</c> and <c>far</c>, so the brain can flee or hide when
/// kin warn of a predator it cannot see itself. V1 writes <c>Alarm: close.</c>; V2 writes a sentence with the distance in
/// meters.</para>
/// <para><b>Mechanism.</b> A <see cref="Sense"/> that asks the world's <see cref="AlarmCalls"/> for the nearest call the
/// animal hears. It only reads (SENSE-03): no random number, no change to the world.</para>
/// <para><b>In a scene.</b> A child of the species' <c>Senses</c> object named <c>Alarm</c>, with this component. The
/// World must hold an <see cref="AlarmCalls"/> service (V-26 otherwise).</para>
/// </remarks>
[RequiresModule(typeof(AlarmCalls))]
public class AlarmSense : Sense
{
    static readonly IReadOnlyList<string> tokens = System.Array.AsReadOnly(new[] { "none", "close", "far" });

    [SerializeField, Min(0.5f), Tooltip("A call heard within this distance is close, in meters (10).")]
    float closeDistance = 10f;

    AlarmCalls calls;   // the world's alarm calls, found in Initialize; null when the world has none

    /// <inheritdoc/>
    public override IReadOnlyList<string> Tokens => tokens;

    /// <inheritdoc/>
    protected override string DefaultLabel => "Alarm";

    /// <summary>Finds the world's <see cref="AlarmCalls"/>.</summary>
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        calls = World.Service<AlarmCalls>();
    }

    /// <summary>
    /// <c>close</c> when the nearest call heard is within the close distance, <c>far</c> when it is farther, and
    /// <c>none</c> when no call is heard or the world has no alarm calls.
    /// </summary>
    /// <inheritdoc/>
    public override int Read(Animal a, SenseContext s)
    {
        if (calls == null) return 0;
        float? d = calls.NearestHeard(a, s.Tick);
        if (d == null) return 0;
        return d.Value <= closeDistance ? 1 : 2;
    }

    /// <summary>V1: <c>Alarm: close.</c>; V2: a sentence such as "A kin gave an alarm call less than 10 meters away.".</summary>
    /// <inheritdoc/>
    public override string Write(int token, TextStyle style)
    {
        if (style != TextStyle.V2) return $"{Label}: {tokens[token]}.";
        string near = Bands.Number(closeDistance);
        switch (token)
        {
            case 1: return $"A kin gave an alarm call less than {near} meters away.";
            case 2: return $"A kin gave an alarm call {near} meters away or more.";
            default: return "No kin is giving an alarm call.";
        }
    }

    /// <summary>Sets the sense from code (tests).</summary>
    /// <param name="close">The distance under which a call is close, in meters.</param>
    public void Configure(float close) => closeDistance = close;
}
