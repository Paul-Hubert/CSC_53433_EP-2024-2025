using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// The named random streams of one world (RAND-02…04). Asking twice for a name returns the same stream.
    /// Map layers use "world/&lt;layer name&gt;", so adding a layer never changes another layer's map.
    /// </summary>
    /// <remarks>
    /// <para>The World makes a new set at each Initialize, from its seed and world seed (<see cref="World.Random"/>).
    /// Streams are per purpose and, where a species is involved, per species (RAND-03): <c>&lt;species&gt;/founders</c>,
    /// <c>/sampling</c>, <c>/actions</c>, <c>/litter</c>, <c>/mutation</c>, <c>/migration</c>; world streams
    /// <c>act-order</c>, a layer's name for its regrowth, and <c>world</c> or <c>world/&lt;layer&gt;</c> for the map.</para>
    /// <para>A new purpose of your own is just a new name: it gets its own stream and shifts no other.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var regrowth = World.Random.Get(layer.StreamName);                 // a world stream
    /// var map = World.Random.Get("world/" + ServiceName);               // the map: uses the world seed
    /// var litter = World.Random.For(species, "litter");                 // "&lt;species id&gt;/litter"
    /// </code>
    /// </example>
    public sealed class RandomStreams
    {
        readonly Dictionary<string, RandomStream> streams = new Dictionary<string, RandomStream>();   // every stream made so far, by name
        // species id → purpose → stream: asking every tick builds no name string (R-07)
        readonly Dictionary<string, Dictionary<string, RandomStream>> bySpecies = new Dictionary<string, Dictionary<string, RandomStream>>();

        /// <summary>The run seed: every stream but <c>world</c> derives from it.</summary>
        public long Seed { get; }
        /// <summary>The seed of the <c>world</c> stream (terrain, cover, initial food); may differ from the run seed (RAND-04).</summary>
        public long WorldSeed { get; }

        /// <summary>Makes the streams of a world with its own map seed.</summary>
        /// <param name="seed">The run seed.</param>
        /// <param name="worldSeed">The seed of the <c>world</c> streams (RAND-04).</param>
        public RandomStreams(long seed, long worldSeed)
        {
            Seed = seed;
            WorldSeed = worldSeed;
        }

        /// <summary>Makes the streams of a world whose map uses the run seed.</summary>
        /// <param name="seed">The run seed, also used for the map.</param>
        public RandomStreams(long seed) : this(seed, seed) { }

        /// <summary>The stream with this name (RAND-02). "world" uses the world seed.</summary>
        /// <param name="name">The stream's name, e.g. <c>"act-order"</c> or <c>"world/Grass"</c>.</param>
        /// <returns>The stream, made on first use; the same object for the same name afterwards.</returns>
        public RandomStream Get(string name)
        {
            if (!streams.TryGetValue(name, out var s))
            {
                s = new RandomStream(IsWorldStream(name) ? WorldSeed : Seed, name);
                streams.Add(name, s);
            }
            return s;
        }

        /// <summary>A species' own stream, "&lt;species id&gt;/&lt;purpose&gt;" (RAND-03).</summary>
        /// <param name="species">The species the draws are about.</param>
        /// <param name="purpose">What they are for, e.g. <c>"sampling"</c> or <c>"litter"</c>.</param>
        /// <returns>The stream, the same object for the same species and purpose.</returns>
        public RandomStream For(Species species, string purpose)
        {
            if (!bySpecies.TryGetValue(species.Id, out var mine)) bySpecies.Add(species.Id, mine = new Dictionary<string, RandomStream>());
            if (!mine.TryGetValue(purpose, out var stream)) mine.Add(purpose, stream = Get(species.Id + "/" + purpose));
            return stream;
        }

        /// <summary>"world" and "world/&lt;layer&gt;" streams make the map: they use the world seed (RAND-04).</summary>
        /// <param name="name">A stream name.</param>
        /// <returns>True for <c>world</c> and every name that starts with <c>world/</c>.</returns>
        public static bool IsWorldStream(string name) => name == WorldStream || name.StartsWith(WorldStream + "/", System.StringComparison.Ordinal);

        /// <summary>The stream for terrain, cover and initial food.</summary>
        public RandomStream World => Get(WorldStream);

        /// <summary>The name of the map's stream, <c>world</c>; no species or layer may take it (V-07, V-18).</summary>
        public const string WorldStream = "world";
        /// <summary>The name of the stream that draws the order animals act in, <c>act-order</c> (ACT-32).</summary>
        public const string ActOrderStream = "act-order";
    }
}
