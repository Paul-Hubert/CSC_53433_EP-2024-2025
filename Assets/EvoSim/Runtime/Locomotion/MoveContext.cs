using UnityEngine;

namespace EvoSim
{
    /// <summary>What a locomotion gets to move animals: the ground, the distance function, the streams, stamina budgets.</summary>
    public sealed class MoveContext
    {
        public World World { get; }
        public Ground Ground => World.Ground;
        public int Tick => World.Tick;

        /// <summary>A student's own act or movement phase builds one to call Locomotion.Move (22 §5).</summary>
        public MoveContext(World world) { World = world; }

        /// <summary>The world distance (SPACE-02).</summary>
        public float Distance(Vector3 a, Vector3 b) => World.Distance(a, b);

        /// <summary>A species' stream, e.g. Stream(species, "actions") for sidesteps (RAND-03).</summary>
        public RandomStream Stream(Species s, string purpose) => World.Random.For(s, purpose);

        /// <summary>How far this animal may move this tick: its stamina, or no limit without a stamina module (ANIM-21).</summary>
        public float StaminaBudget(Animal a)
        {
            var stamina = a.Species.Module<Stamina>();
            return stamina != null ? stamina.Budget(a) : float.PositiveInfinity;
        }
    }
}
