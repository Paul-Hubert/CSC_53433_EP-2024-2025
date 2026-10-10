namespace EvoSim.Testing
{
    /// <summary>A last phase for test worlds: fails the test on the first broken invariant (30 §5).</summary>
    public class InvariantPhase : TickPhase
    {
        public int Checked;

        public override void Run(TickContext t)
        {
            var problems = Invariants.Check(World);
            Checked++;
            if (problems.Count > 0)
                NUnit.Framework.Assert.Fail($"tick {t.Tick}: {problems.Count} invariant(s) broken: {string.Join("; ", problems.GetRange(0, System.Math.Min(5, problems.Count)))}");
        }
    }
}
