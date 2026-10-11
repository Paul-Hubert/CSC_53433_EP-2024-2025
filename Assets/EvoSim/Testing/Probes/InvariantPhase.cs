namespace EvoSim.Testing
{
    /// <summary>A last phase for test worlds: fails the test on the first broken invariant (30 §5).</summary>
    /// <remarks>Add it with <see cref="Lab.CheckInvariants"/>, after the other phases.</remarks>
    public class InvariantPhase : TickPhase
    {
        /// <summary>How many times the invariants were checked (once per tick), to prove the phase ran.</summary>
        public int Checked;

        /// <summary>Runs <see cref="Invariants.Check"/> and fails the test with the tick and the first five problems.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            var problems = Invariants.Check(World);
            Checked++;
            if (problems.Count > 0)
                NUnit.Framework.Assert.Fail($"tick {t.Tick}: {problems.Count} invariant(s) broken: {string.Join("; ", problems.GetRange(0, System.Math.Min(5, problems.Count)))}");
        }
    }
}
