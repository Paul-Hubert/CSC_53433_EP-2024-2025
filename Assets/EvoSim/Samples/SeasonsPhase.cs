using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §9: a phase before Environment that slows the grass's regrowth in the second half of every year.</summary>
    public class SeasonsPhase : TickPhase
    {
        [SerializeField, Min(2), Tooltip("Ticks per year (reference 2 000).")]
        int yearLength = 2000;
        [SerializeField, Range(0f, 1f), Tooltip("Regrowth factor in winter (reference 0.2).")]
        float winterRegrowFactor = 0.2f;
        [SerializeField, Tooltip("The food layer's name (reference Grass).")]
        string layer = "Grass";

        FoodGrid food;

        public bool IsWinter(int tick) => tick % yearLength >= yearLength / 2;

        public override void Initialize()
        {
            food = null;
            foreach (var s in World.Services)
                if (s is FoodGrid g && g.LayerName == layer) food = g;
        }

        public override void Run(TickContext t)
        {
            if (food != null) food.RegrowFactor = IsWinter(t.Tick) ? winterRegrowFactor : 1f;
        }

        public void Configure(int ticksPerYear, float winterFactor)
        {
            yearLength = ticksPerYear;
            winterRegrowFactor = winterFactor;
        }

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
