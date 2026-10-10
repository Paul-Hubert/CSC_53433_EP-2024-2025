namespace EvoSim
{
    /// <summary>What a sense may read at a decision: the world state at the start of the tick (SENSE-04).</summary>
    public sealed class SenseContext
    {
        public World World { get; }

        internal SenseContext(World world) { World = world; }
    }
}
