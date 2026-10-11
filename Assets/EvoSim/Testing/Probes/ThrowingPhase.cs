namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase that throws, for the RAND-20 test: the World stops the run cleanly ("exception in ThrowingPhase") and
    /// lets the exception through.
    /// </summary>
    public class ThrowingPhase : TickPhase
    {
        /// <summary>Throws an <see cref="System.InvalidOperationException"/> ("boom").</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t) => throw new System.InvalidOperationException("boom");
    }
}
