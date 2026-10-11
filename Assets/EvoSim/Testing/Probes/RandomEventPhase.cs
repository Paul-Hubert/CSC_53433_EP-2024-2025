namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase that draws from a named stream every tick and records a "probe" event, so the hash depends on the draws.
    /// </summary>
    /// <remarks>Used to check that runs repeat with their seed and that streams are independent (RAND-02, RAND-10).</remarks>
    public class RandomEventPhase : TickPhase
    {
        /// <summary>The name of the random stream it draws from.</summary>
        public string Stream = "probe";
        /// <summary>How many draws (and events) per tick.</summary>
        public int DrawsPerTick = 1;

        /// <summary>Records <see cref="DrawsPerTick"/> "probe" events, each with a draw in [0, 1 000 000) from its stream.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            var rng = t.Stream(Stream);
            for (int i = 0; i < DrawsPerTick; i++)
                t.World.Events.Record(new SimEvent("probe", t.Tick, -1, null).With("stream", Stream).With("value", rng.Range(0, 1000000)));
        }
    }
}
