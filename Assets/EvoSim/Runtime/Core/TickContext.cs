namespace EvoSim
{
    /// <summary>What a phase gets each tick: the world, the tick number and the named streams.</summary>
    /// <remarks>
    /// The World makes one context per run and passes it to every <see cref="TickPhase.WaitsFor"/> and
    /// <see cref="TickPhase.Run"/>. Take every random number from its streams: a species' own stream for anything
    /// about that species (RAND-03), so adding a species or a draw never shifts another stream's numbers (RAND-02).
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void Run(TickContext t)
    /// {
    ///     foreach (var s in t.World.AllSpecies)
    ///     {
    ///         var rng = t.Stream(s, "migration");                      // "&lt;species id&gt;/migration"
    ///         // ... choose who leaves with rng ...
    ///     }
    ///     var order = t.Stream(RandomStreams.ActOrderStream);           // a world stream, "act-order"
    /// }
    /// </code>
    /// </example>
    public sealed class TickContext
    {
        /// <summary>The World that runs the tick.</summary>
        public World World { get; }
        /// <summary>The current tick number (<see cref="World.Tick"/>).</summary>
        public int Tick => World.Tick;
        /// <summary>The World's named random streams (RAND-02).</summary>
        public RandomStreams Random => World.Random;

        /// <summary>Makes the context of a World; the World makes one per run.</summary>
        internal TickContext(World world) { World = world; }

        /// <summary>A species' stream for a purpose, e.g. Stream(prey, "sampling") (RAND-03).</summary>
        /// <param name="species">The species the draws are about.</param>
        /// <param name="purpose">What the draws are for: <c>"founders"</c>, <c>"sampling"</c>, <c>"actions"</c>,
        /// <c>"litter"</c>, <c>"mutation"</c>, <c>"migration"</c>, or a purpose of your own.</param>
        /// <returns>The stream <c>&lt;species id&gt;/&lt;purpose&gt;</c>, the same object every time it is asked for.</returns>
        public RandomStream Stream(Species species, string purpose) => World.Random.For(species, purpose);

        /// <summary>A world stream by name, e.g. "act-order".</summary>
        /// <param name="name">The stream's name; <c>"world"</c> and <c>"world/…"</c> streams use the map's seed (RAND-04).</param>
        /// <returns>The stream with that name, the same object every time it is asked for.</returns>
        public RandomStream Stream(string name) => World.Random.Get(name);
    }
}
