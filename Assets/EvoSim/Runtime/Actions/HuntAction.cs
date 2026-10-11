using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Chase the nearer of the nearest huntable animal and the nearest carcass it may eat (a carcass wins a tie, being a
    /// sure meal); strike the animal or eat from the carcass on arrival (07 §4).
    /// </summary>
    /// <remarks>
    /// <para>The reference hunt action of the predator, and teaching-path exercise 12. Each tick it:</para>
    /// <list type="bullet">
    /// <item><description>finds the nearest visible prey (<see cref="ActContext.NearestAnimal"/> with
    /// <see cref="AnimalSet.Prey"/>: not killed, not hidden in cover) and the nearest carcass it may eat
    /// (<see cref="ActContext.NearestCarcass"/>);</description></item>
    /// <item><description>if there is a carcass at least as near as the prey (or no prey), runs to it, stopping
    /// <c>stopAt</c> meters away, and scavenges it on arrival (<see cref="Interaction.Scavenge"/>);</description></item>
    /// <item><description>else, if there is prey, runs to it and strikes it on arrival
    /// (<see cref="Interaction.Strike"/>);</description></item>
    /// <item><description>with neither in sight, searches (ACT-04).</description></item>
    /// </list>
    /// <para>The strike kills with the hunter's kill chance (<see cref="Diet.KillChanceAgainst"/>), at most once per
    /// tick (ACT-12); a kill or a portion makes the hunter busy digesting (<see cref="Digestion"/>).</para>
    /// </remarks>
    public class HuntAction : AnimalAction
    {
        /// <inheritdoc/>
        protected override string DefaultDescription => "chase the nearest visible prey animal or carcass; next to a prey animal,\n  try to kill and eat it; next to a carcass, eat from it";
        /// <summary>True: senses such as the sample <c>ChasedSense</c> see an animal that chose this action as hunting.</summary>
        public override bool Hunts => true;

        [SerializeField, Min(0f), Tooltip("Stop this far from the target, in meters (reference 1).")]
        float stopAt = 1f;
        [SerializeField, Tooltip("The sense whose token says whether prey is in sight (CTRL-10).")]
        string relevantSense = "Prey";

        /// <summary>
        /// Runs to the nearer of the nearest prey and the nearest edible carcass (the carcass on a tie) and strikes or
        /// scavenges on arrival; searches when neither is in sight (ACT-04).
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>True when the prey sense (<c>relevantSense</c>, reference <c>"Prey"</c>) reports prey in sight (CTRL-10).</summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }

        /// <summary>Warns when the species has no prey to strike and no carcass in its diet (V-21).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (World.PreyOf(Species).Count == 0 && !(Species.Module<Diet>()?.Has(EdibleTargetKind.Carcass) ?? false))
                report.Warning("V-21", this, $"Hunt has no prey to hunt in species '{Species.DisplayName}'.");
        }
    }
}
