using UnityEngine;

namespace EvoSim
{
    /// <summary>What a locomotion gets to move animals: the ground, the distance function, the streams, stamina budgets.</summary>
    /// <remarks>The act phase makes one at initialization and passes it to every <see cref="Locomotion.MoveAll"/>.</remarks>
    public sealed class MoveContext
    {
        /// <summary>The world being simulated.</summary>
        public World World { get; }
        /// <summary>The world's ground (heights, walkable points, sliding and sidesteps), or null in a world without one.</summary>
        public Ground Ground => World.Ground;
        /// <summary>The current tick of the world.</summary>
        public int Tick => World.Tick;

        /// <summary>
        /// Creates a context for a world. The act phase makes one; a student's own act or movement phase builds one to
        /// call <see cref="Locomotion.Move"/> (20 §5).
        /// </summary>
        /// <param name="world">The world whose ground, distance and random streams it uses.</param>
        public MoveContext(World world) { World = world; }

        /// <summary>The world distance (SPACE-02).</summary>
        /// <param name="a">One point.</param>
        /// <param name="b">The other point.</param>
        /// <returns>The distance in meters: horizontal in the reference ground.</returns>
        public float Distance(Vector3 a, Vector3 b) => World.Distance(a, b);

        /// <summary>A species' stream, e.g. Stream(species, "actions") for sidesteps (RAND-03).</summary>
        /// <param name="s">The species whose stream it is: the locomotion's own <see cref="SpeciesModule.Species"/>.</param>
        /// <param name="purpose">The stream's name within the species, such as <c>"actions"</c>.</param>
        /// <returns>The stream; in a run, the same species and purpose always give the same stream.</returns>
        public RandomStream Stream(Species s, string purpose) => World.Random.For(s, purpose);

        /// <summary>How far this animal may move this tick: its stamina, or no limit without a stamina module (ANIM-21).</summary>
        /// <param name="a">The animal about to move.</param>
        /// <returns>The meters it may move: its stamina, never below 0, or infinity without a <see cref="Stamina"/> module.</returns>
        public float StaminaBudget(Animal a)
        {
            var stamina = a.Species.Module<Stamina>();
            return stamina != null ? stamina.Budget(a) : float.PositiveInfinity;
        }
    }
}
