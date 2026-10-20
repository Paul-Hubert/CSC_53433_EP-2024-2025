using EvoSim;

/// <summary>A step of every tick that forgets the alarm calls nobody can hear any more.</summary>
/// <remarks>
/// <para><b>What it adds.</b> Calls fade: without this phase the <see cref="AlarmCalls"/> list would only grow. The
/// queries already ignore old calls, so the phase changes no answer; it only keeps the list short.</para>
/// <para><b>Mechanism.</b> A <see cref="TickPhase"/>: <see cref="Initialize"/> finds the service once, <see cref="Run"/>
/// asks it to forget, once per tick, at the phase's place among the World's <c>Phases</c> children (TICK-01).</para>
/// <para><b>In a scene.</b> Add it on a new child of the World's <c>Phases</c> object, after <c>Act</c>, where the calls
/// are made.</para>
/// </remarks>
public class FadeAlarmCalls : TickPhase
{
    AlarmCalls calls;   // the world's alarm calls, found in Initialize; null when the world has none

    /// <summary>Finds the world's <see cref="AlarmCalls"/> service.</summary>
    /// <inheritdoc/>
    public override void Initialize() => calls = World.Service<AlarmCalls>();

    /// <summary>Forgets the calls too old to be heard; does nothing in a world without alarm calls (TICK-07).</summary>
    /// <inheritdoc/>
    public override void Run(TickContext t)
    {
        if (calls == null) return;
        calls.Forget(t.Tick);
    }

    /// <summary>Warns when the world has no <see cref="AlarmCalls"/> service: the phase would have nothing to do.</summary>
    /// <inheritdoc/>
    public override void Validate(ValidationReport report)
    {
        if (World.Service<AlarmCalls>() == null)
            report.Warning("V-26", this, "FadeAlarmCalls needs an AlarmCalls service under the World.");
    }
}
