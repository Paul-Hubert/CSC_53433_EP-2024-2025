namespace EvoSim.Testing
{
    /// <summary>A test phase that draws from a named stream every tick and records a "probe" event, so the hash depends on the draws.</summary>
    public class RandomEventPhase : TickPhase
    {
        public string Stream = "probe";
        public int DrawsPerTick = 1;

        public override void Run(TickContext t)
        {
            var rng = t.Stream(Stream);
            for (int i = 0; i < DrawsPerTick; i++)
                t.World.Events.Record(new SimEvent("probe", t.Tick, -1, null).With("stream", Stream).With("value", rng.Range(0, 1000000)));
        }
    }
}
