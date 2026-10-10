namespace EvoSim.Testing
{
    /// <summary>A test phase that throws, for the RAND-20 test.</summary>
    public class ThrowingPhase : TickPhase
    {
        public override void Run(TickContext t) => throw new System.InvalidOperationException("boom");
    }
}
