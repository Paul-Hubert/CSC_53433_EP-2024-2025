using UnityEngine;

namespace EvoSim
{
    /// <summary>Go to the nearest visible food and eat it (07 §4). Nothing in sight: search (ACT-04).</summary>
    public class EatAction : AnimalAction
    {
        protected override string DefaultDescription => "go to the nearest visible food and eat it";

        [SerializeField, Tooltip("A layer name; empty = every layer the diet grazes; \"egg:<species>\" = eggs (22 §15).")]
        string target = "";
        [SerializeField, Tooltip("The sense whose token says whether food is in sight (CTRL-10).")]
        string relevantSense = "Food";

        Species eggsOf;
        bool eatsEggs;

        public override void Initialize()
        {
            eatsEggs = target.StartsWith("egg:");
            eggsOf = eatsEggs ? World.FindSpeciesByName(target.Substring(4).Trim()) : null;
        }

        public override void Act(Animal a, ActContext c)
        {
            if (eatsEggs)
            {
                var egg = c.NearestEgg(a, eggsOf);
                if (egg == null) { c.Search(); return; }
                c.WalkTo(egg.Value.Position, stopAt: 0f);
                c.OnArrival(Interaction.EatEgg(egg.Value.Entity));
                return;
            }
            var food = c.NearestResource(a, target);                    // within vision, diet-checked
            if (food == null) { c.Search(); return; }                   // ACT-04
            if (food.Value.Distance <= c.GrazeReach) c.Stay();          // already on it
            else c.WalkTo(food.Value.Position, stopAt: 0f);             // MOVE-04
            c.OnArrival(Interaction.Graze(food.Value));                 // ACT-10, on the tick of arrival too
        }

        public override bool IsRelevant(ObservationView o) => Seen(o.Token(relevantSense));

        static bool Seen(string token) => token != null && token != "none";

        public void SetTarget(string layerOrEggs) => target = layerOrEggs ?? "";
    }
}
