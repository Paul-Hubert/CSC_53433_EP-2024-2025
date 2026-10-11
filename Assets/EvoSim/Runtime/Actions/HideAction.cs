using UnityEngine;

namespace EvoSim
{
    /// <summary>Go to the nearest cover and stay in it (07 §4, ENV-13). No cover in sight, or no cover module: search.</summary>
    /// <remarks>
    /// <para>The reference hide action of the prey, and teaching-path exercise 7. Each tick it:</para>
    /// <list type="bullet">
    /// <item><description>stays when the animal already stands in cover (<see cref="ActContext.InCover"/>);</description></item>
    /// <item><description>else finds the nearest cover within vision (<see cref="ActContext.NearestCover"/>) and walks
    /// to its point with a stop distance of 0, or runs there when <c>run</c> is ticked;</description></item>
    /// <item><description>with no cover in sight, or no cover module in the world (ENV-12), searches (ACT-04).</description></item>
    /// </list>
    /// <para>In cover the animal is hidden from the species that hunt it: they can neither see nor strike it (ENV-11).</para>
    /// </remarks>
    public class HideAction : AnimalAction
    {
        /// <inheritdoc/>
        protected override string DefaultDescription => "go to the nearest cover and stay in it; predators can't see or catch an animal in cover";

        [SerializeField, Tooltip("Run into cover instead of walking.")]
        bool run;
        [SerializeField, Tooltip("The sense whose token says whether cover is in sight (CTRL-10).")]
        string relevantSense = "Cover";

        /// <summary>Stays in cover, or walks (or runs) to the nearest cover in sight; searches when there is none (ACT-04).</summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            if (c.InCover(a)) { c.Stay(); return; }                     // stay once inside
            var cover = c.NearestCover(a);
            if (cover == null) { c.Search(); return; }                  // ENV-12: no cover module → search
            if (run) c.RunTo(cover.Value.Point, stopAt: 0f);
            else c.WalkTo(cover.Value.Point, stopAt: 0f);
        }

        /// <summary>True when the cover sense (<c>relevantSense</c>, reference <c>"Cover"</c>) reports cover in sight (CTRL-10).</summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token(relevantSense);
            return t != null && t != "none";
        }
    }
}
