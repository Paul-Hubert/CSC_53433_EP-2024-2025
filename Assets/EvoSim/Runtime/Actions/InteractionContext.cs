namespace EvoSim
{
    /// <summary>What a custom interaction may use when it applies: the world, the ground, the actor's streams.</summary>
    public sealed class InteractionContext
    {
        public World World { get; }
        public Ground Ground => World.Ground;
        public int Tick => World.Tick;

        internal InteractionContext(World world) { World = world; }

        /// <summary>Feeds an animal as a meal does: energy gained, the meal counted, digestion after it when <paramref name="digest"/>.</summary>
        public void Feed(Animal a, float energy, bool digest = false) => InteractionResolver.Eat(a, energy, digest);

        /// <summary>A species' stream (RAND-03).</summary>
        public RandomStream Stream(Species s, string purpose) => World.Random.For(s, purpose);
    }
}
