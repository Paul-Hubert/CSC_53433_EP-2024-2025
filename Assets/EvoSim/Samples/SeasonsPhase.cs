using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §9: a phase before Environment that slows the grass's regrowth in the second half of every year.</summary>
    /// <remarks>
    /// <para><b>What it adds.</b> Seasons: a year of <c>yearLength</c> ticks whose first half is summer (regrowth as set on
    /// the food layer) and second half winter (regrowth times <c>winterRegrowFactor</c>).</para>
    /// <para><b>Mechanism.</b> A <see cref="TickPhase"/>: <see cref="Initialize"/> finds the food layer once, <see cref="Run"/>
    /// runs every tick at its place in the phase list and changes one setting of the layer, which the environment phase
    /// then uses (TICK-02). <see cref="Validate"/> checks the layer exists and the phase order (V-20, V-08).</para>
    /// <para><b>In a scene.</b> Add it on a new GameObject among the World's <c>Phases</c> children, before
    /// <c>Environment</c>: phases run in hierarchy order (TICK-01).</para>
    /// </remarks>
    public class SeasonsPhase : TickPhase
    {
        [SerializeField, Min(2), Tooltip("Ticks per year (reference 2 000).")]
        int yearLength = 2000;
        [SerializeField, Range(0f, 1f), Tooltip("Regrowth factor in winter (reference 0.2).")]
        float winterRegrowFactor = 0.2f;
        [SerializeField, Tooltip("The food layer's name (reference Grass).")]
        string layer = "Grass";

        FoodGrid food;   // the food layer named by `layer`, found in Initialize; null when there is none

        /// <summary>True in the second half of the year that contains this tick.</summary>
        /// <param name="tick">A tick number.</param>
        /// <returns>Whether that tick is in winter.</returns>
        public bool IsWinter(int tick) => tick % yearLength >= yearLength / 2;

        /// <summary>Finds the food layer named by the <c>layer</c> setting among the World's services.</summary>
        public override void Initialize()
        {
            food = null;
            foreach (var s in World.Services)
                if (s is FoodGrid g && g.LayerName == layer) food = g;
        }

        /// <summary>Sets the layer's regrowth factor: the winter factor in winter, 1 in summer.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            if (food != null) food.RegrowFactor = IsWinter(t.Tick) ? winterRegrowFactor : 1f;
        }

        /// <summary>Sets the phase from code (tests).</summary>
        /// <param name="ticksPerYear">The length of a year, in ticks.</param>
        /// <param name="winterFactor">The regrowth factor in winter, 0 to 1.</param>
        public void Configure(int ticksPerYear, float winterFactor)
        {
            yearLength = ticksPerYear;
            winterRegrowFactor = winterFactor;
        }

        /// <summary>Reports an error when the food layer doesn't exist (V-20) or the phase comes after Environment (V-08).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            bool found = false;
            foreach (var s in World.Services) if (s is FoodGrid g && g.LayerName == layer) found = true;
            if (!found) report.Error("V-20", this, $"Seasons act on the food layer '{layer}', which doesn't exist.");
            var phases = World.Phases;
            int me = -1, env = -1;
            for (int i = 0; i < phases.Count; i++)
            {
                if (phases[i] == this) me = i;
                if (phases[i] is EnvironmentPhase) env = i;
            }
            if (env >= 0 && me > env) report.Error("V-08", this, "Seasons must come before the Environment phase.");
        }
    }
}
