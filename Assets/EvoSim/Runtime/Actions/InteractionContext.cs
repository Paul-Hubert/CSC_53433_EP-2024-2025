namespace EvoSim
{
    /// <summary>What a custom interaction may use when it applies: the world, the ground, the actor's streams.</summary>
    /// <remarks>
    /// The <see cref="InteractionResolver"/> owns one and passes it to every <see cref="IInteraction.Apply"/>. Use
    /// <see cref="Feed"/> for a meal, so that it counts like the reference ones, and <see cref="Stream"/> for any random
    /// draw (RAND-03).
    /// </remarks>
    public sealed class InteractionContext
    {
        /// <summary>The world being simulated: its species, services and queries.</summary>
        public World World { get; }
        /// <summary>The world's ground (heights, walkable points, water), or null in a world without one.</summary>
        public Ground Ground => World.Ground;
        /// <summary>The current tick of the world.</summary>
        public int Tick => World.Tick;

        /// <summary>Creates the context of a resolver; only the <see cref="InteractionResolver"/> makes one.</summary>
        internal InteractionContext(World world) { World = world; }

        /// <summary>
        /// Feeds an animal as a meal does: energy gained, the meal counted, digestion after it when
        /// <paramref name="digest"/>.
        /// </summary>
        /// <param name="a">The animal that eats, usually the actor.</param>
        /// <param name="energy">The energy gained, in points; the energy stat stays at or below its maximum (ANIM-22).</param>
        /// <param name="digest">
        /// True to make the animal busy digesting afterwards, as after a kill (<see cref="Digestion"/>, ANIM-30).
        /// </param>
        /// <remarks>
        /// It adds one meal to the animal's and its species' counters, like grazing, a kill or a portion. It checks no
        /// diet and takes the energy as given: <see cref="Diet.EnergyFrom"/> is not applied.
        /// </remarks>
        /// <example>
        /// A snack eaten where the animal stands, from the test probe <c>FeedAction</c>:
        /// <code>
        /// sealed class Snack : IInteraction
        /// {
        ///     readonly float energy;
        ///     public Snack(float e) { energy = e; }
        ///     public float Reach =&gt; 0.5f;
        ///     public void Apply(Animal actor, InteractionContext c) =&gt; c.Feed(actor, energy);
        /// }
        /// </code>
        /// </example>
        public void Feed(Animal a, float energy, bool digest = false) => InteractionResolver.Eat(a, energy, digest);

        /// <summary>A species' stream (RAND-03).</summary>
        /// <param name="s">The species whose stream it is, usually the actor's.</param>
        /// <param name="purpose">The stream's name within the species, such as <c>"actions"</c>.</param>
        /// <returns>The stream; in a run, the same species and purpose always give the same stream.</returns>
        public RandomStream Stream(Species s, string purpose) => World.Random.For(s, purpose);
    }
}
