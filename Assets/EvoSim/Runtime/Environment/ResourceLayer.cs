using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Edible items spread over the ground, e.g. grass (03 §1). The layer's <see cref="EvoSim.Edible"/> says how
    /// it is eaten; diets name the layer by its GameObject's name.
    /// </summary>
    /// <remarks>
    /// <para>Derive from it, implement <see cref="Count"/>, <see cref="Nearest"/> and <see cref="Consume"/>, override
    /// <see cref="Regrow"/> if the layer grows back, and place the component under the World (reference: "Grass" under
    /// Environment) with an <see cref="EvoSim.Edible"/>. Give the GameObject a unique name: it is the layer's name in
    /// diets, senses and random streams, and must not be "world", "act-order" or contain "/" (V-18).</para>
    /// <para>Lifecycle: <see cref="WorldModule.Initialize"/> runs after the ground's (build empty storage there);
    /// <see cref="WorldModule.Begin"/> runs once every module is ready, cover included (place the first items there, with
    /// the stream "world/&lt;layer name&gt;"); <see cref="Regrow"/> runs once per tick in the environment phase.</para>
    /// <para>Rules to keep: consuming removes the item at once, so two animals never eat the same one (ENV-02); the layer
    /// changes only in <see cref="Regrow"/> and <see cref="Consume"/>, and draws random numbers only from the stream it is
    /// given (ENV-03); ties in <see cref="Nearest"/> are broken by a fixed key (SPACE-08).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Bushes on a 4 m grid that each hold one berry and grow it back (the shape of FoodGrid, cut down).
    /// public class BerryBushes : ResourceLayer
    /// {
    ///     [SerializeField] float regrowP = 0.01f;
    ///     CellGrid grid;
    ///     bool[] berries;
    ///     int count;
    ///
    ///     public override int Count =&gt; count;
    ///
    ///     public override void Initialize()
    ///     {
    ///         grid = new CellGrid(World.Ground.Bounds, 4f);
    ///         berries = new bool[grid.Count];
    ///         count = 0;
    ///     }
    ///
    ///     public override bool Nearest(Vector3 p, float radius, out ResourceItem item)
    ///     {
    ///         int c = grid.Nearest(berries, p, radius, World.Ground, out float d);
    ///         item = c &gt;= 0 ? new ResourceItem(this, c, World.Ground.OnGround(grid.Center(c)), d) : default;
    ///         return c &gt;= 0;
    ///     }
    ///
    ///     public override bool Consume(ResourceItem item)
    ///     {
    ///         if (item.Layer != this || item.Cell &lt; 0 || !berries[item.Cell]) return false;   // gone already (ENV-02)
    ///         berries[item.Cell] = false;
    ///         count--;
    ///         return true;
    ///     }
    ///
    ///     public override void Regrow(RandomStream rng)
    ///     {
    ///         for (int c = 0; c &lt; grid.Count; c++)
    ///             if (!berries[c] &amp;&amp; rng.NextDouble() &lt; regrowP) { berries[c] = true; count++; }
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class ResourceLayer : WorldService
    {
        /// <summary>The name diets and senses use (ARCH-08).</summary>
        public string LayerName => ServiceName;

        /// <summary>The layer's edible component (SPEC-10), or null: then nothing can eat it.</summary>
        public Edible Edible
        {
            get
            {
                var e = GetComponent<Edible>();
                return e != null && e.enabled ? e : null;
            }
        }

        /// <summary>How many items the layer holds (ENV-01).</summary>
        public abstract int Count { get; }

        /// <summary>The nearest item within radius of a point (ENV-01, SPACE-07); false if none.</summary>
        /// <remarks>Measure with the world's distance (<see cref="World.Distance"/>, SPACE-02), and break ties by a fixed key (SPACE-08).</remarks>
        /// <param name="p">The point searched from, usually an animal's position.</param>
        /// <param name="radius">The largest distance, in meters (inclusive).</param>
        /// <param name="item">The nearest item, or <c>default</c> when none is found.</param>
        /// <returns>True when an item was found within the radius.</returns>
        public abstract bool Nearest(Vector3 p, float radius, out ResourceItem item);

        /// <summary>The nearest item within reach of a point (ENV-01); false if none.</summary>
        /// <param name="p">The animal's position.</param>
        /// <param name="reach">The reach, in meters (reference graze reach 0.5).</param>
        /// <param name="item">The nearest item within reach, or <c>default</c>.</param>
        /// <returns>True when an item is within reach.</returns>
        public virtual bool WithinReach(Vector3 p, float reach, out ResourceItem item) => Nearest(p, reach, out item);

        /// <summary>Removes the item at once; false if it was already gone (ENV-02).</summary>
        /// <remarks>Called by the interaction resolver when an animal grazes; the act order decides who eats a contested item.</remarks>
        /// <param name="item">An item this layer returned from <see cref="Nearest"/>, this tick.</param>
        /// <returns>True when the item was there and is now eaten; false when it was gone or belongs to another layer.</returns>
        public abstract bool Consume(ResourceItem item);

        /// <summary>Regrowth, once per tick in the environment phase, with the layer's own stream (ENV-03).</summary>
        /// <remarks>Does nothing by default: a layer that never grows back.</remarks>
        /// <param name="rng">The layer's stream, named <see cref="StreamName"/>; draw only from it, in a fixed order.</param>
        public virtual void Regrow(RandomStream rng) { }

        /// <summary>The name of the layer's regrowth stream (RAND-03): the layer's name.</summary>
        public virtual string StreamName => LayerName;
    }
}
