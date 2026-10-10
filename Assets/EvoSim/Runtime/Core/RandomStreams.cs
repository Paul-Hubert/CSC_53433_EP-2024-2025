using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// The named random streams of one world (RAND-02…04). Asking twice for a name returns the same stream.
    /// Map layers use "world/&lt;layer name&gt;", so adding a layer never changes another layer's map.
    /// </summary>
    public sealed class RandomStreams
    {
        readonly Dictionary<string, RandomStream> streams = new Dictionary<string, RandomStream>();
        // species id → purpose → stream: asking every tick builds no name string (R-07)
        readonly Dictionary<string, Dictionary<string, RandomStream>> bySpecies = new Dictionary<string, Dictionary<string, RandomStream>>();

        /// <summary>The run seed: every stream but <c>world</c> derives from it.</summary>
        public long Seed { get; }
        /// <summary>The seed of the <c>world</c> stream (terrain, cover, initial food); may differ from the run seed (RAND-04).</summary>
        public long WorldSeed { get; }

        public RandomStreams(long seed, long worldSeed)
        {
            Seed = seed;
            WorldSeed = worldSeed;
        }

        public RandomStreams(long seed) : this(seed, seed) { }

        /// <summary>The stream with this name (RAND-02). "world" uses the world seed.</summary>
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
        public RandomStream For(Species species, string purpose)
        {
            if (!bySpecies.TryGetValue(species.Id, out var mine)) bySpecies.Add(species.Id, mine = new Dictionary<string, RandomStream>());
            if (!mine.TryGetValue(purpose, out var stream)) mine.Add(purpose, stream = Get(species.Id + "/" + purpose));
            return stream;
        }

        /// <summary>"world" and "world/&lt;layer&gt;" streams make the map: they use the world seed (RAND-04).</summary>
        public static bool IsWorldStream(string name) => name == WorldStream || name.StartsWith(WorldStream + "/", System.StringComparison.Ordinal);

        /// <summary>The stream for terrain, cover and initial food.</summary>
        public RandomStream World => Get(WorldStream);

        public const string WorldStream = "world";
        public const string ActOrderStream = "act-order";
    }
}
