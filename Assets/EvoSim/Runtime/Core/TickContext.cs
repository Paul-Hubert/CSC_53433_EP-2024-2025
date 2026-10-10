namespace EvoSim
{
    /// <summary>What a phase gets each tick: the world, the tick number and the named streams.</summary>
    public sealed class TickContext
    {
        public World World { get; }
        public int Tick => World.Tick;
        public RandomStreams Random => World.Random;

        internal TickContext(World world) { World = world; }

        /// <summary>A species' stream for a purpose, e.g. Stream(prey, "sampling") (RAND-03).</summary>
        public RandomStream Stream(Species species, string purpose) => World.Random.For(species, purpose);

        /// <summary>A world stream by name, e.g. "act-order".</summary>
        public RandomStream Stream(string name) => World.Random.Get(name);
    }
}
