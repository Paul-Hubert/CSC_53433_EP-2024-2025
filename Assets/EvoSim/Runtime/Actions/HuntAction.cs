using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Chase the nearer of the nearest huntable animal and the nearest carcass it may eat (a carcass wins a tie, being a
    /// sure meal); strike the animal or eat from the carcass on arrival (07 §4).
    /// </summary>
    public class HuntAction : AnimalAction
    {
        protected override string DefaultDescription => "chase the nearest visible prey animal or carcass; next to a prey animal,\n  try to kill and eat it; next to a carcass, eat from it";

        [SerializeField, Min(0f), Tooltip("Stop this far from the target, in meters (reference 1).")]
        float stopAt = 1f;
        [SerializeField, Tooltip("The sense whose token says whether prey is in sight (CTRL-10).")]
        string relevantSense = "Prey";

        public override void Act(Animal a, ActContext c)
        {
            var prey = c.NearestAnimal(a, AnimalSet.Prey);             // not killed, not hidden in cover
            var carcass = c.NearestCarcass(a);
            if (carcass != null && (prey == null || carcass.Value.Distance <= prey.Value.Distance))
            {
                c.RunTo(carcass.Value.Position, stopAt);
                c.OnArrival(Interaction.Scavenge(carcass.Value.Entity));
                return;
            }
            if (prey == null) { c.Search(); return; }
            c.RunTo(prey.Value.Animal, stopAt);
            c.OnArrival(Interaction.Strike(prey.Value.Animal));
        }

        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }

        public override void Validate(ValidationReport report)
        {
            if (World.PreyOf(Species).Count == 0 && !(Species.Module<Diet>()?.Has(EdibleTargetKind.Carcass) ?? false))
                report.Warning("V-21", this, $"Hunt has no prey to hunt in species '{Species.DisplayName}'.");
        }
    }
}
