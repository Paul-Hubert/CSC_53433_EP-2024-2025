namespace EvoSim
{
    /// <summary>What an action may do in one tick: queries and intents only (ACT-06). Filled in by the act phase.</summary>
    public sealed class ActContext
    {
        public World World { get; }

        internal ActContext(World world) { World = world; }
    }
}
