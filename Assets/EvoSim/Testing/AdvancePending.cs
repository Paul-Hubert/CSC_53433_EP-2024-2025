namespace EvoSim.Testing
{
    /// <summary>Done once the world has had a number of Advance calls; Wait() (Freeze) delivers at once (30 §2).</summary>
    public sealed class AdvancePending : Pending
    {
        readonly World world;
        readonly int readyAt;
        bool forced;

        public AdvancePending(World world, int delayCalls)
        {
            this.world = world;
            readyAt = world.AdvanceCalls + delayCalls;
        }

        public override bool IsDone => forced || world.AdvanceCalls >= readyAt;
        public override void Wait() => forced = true;
    }
}
