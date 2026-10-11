using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>
    /// Scenario S18 (ENV-21): walk to the nearest carcass in sight, then drag it 1 m per tick toward the nearest cover.
    /// The carcass moves in the act phase through an interaction, never from the visuals.
    /// </summary>
    /// <remarks>
    /// <para><b>What it adds.</b> An action, "drag", for a species that eats carcasses: it fetches the nearest carcass it may
    /// eat from and carries it, at walking speed, into the nearest cover in sight, where hunters can't see it. No carcass in
    /// sight: it searches; no cover in sight: it stays by the carcass.</para>
    /// <para><b>Mechanism.</b> An <see cref="AnimalAction"/> with a custom <see cref="IInteraction"/> that changes an entity: an
    /// entity's position may change, but only in a phase (ENV-21). The interaction's reach is unlimited, so it applies every
    /// tick after the animal moved. It is not one of the recipes of 22: it writes out the S18 scenario of 31.</para>
    /// <para><b>In a scene.</b> Add it on a new GameObject named "drag" under the actions of a species whose diet lists
    /// carcasses (the predator), with a <see cref="TextGene"/> on the same GameObject. <see cref="IsRelevant"/> reads the
    /// "Carcass" sense.</para>
    /// </remarks>
    public class DragAction : AnimalAction
    {
        [SerializeField, Min(0.1f), Tooltip("How close the animal must be to grab the carcass, in meters (reference 1).")]
        float grabReach = 1f;

        /// <summary>"drag the nearest carcass toward cover".</summary>
        protected override string DefaultDescription => "drag the nearest carcass toward cover";

        /// <summary>
        /// No carcass in sight: searches (ACT-04). Farther than the grab reach: walks to it. Within reach: walks toward the
        /// nearest cover in sight and drags the carcass along (stays when no cover is in sight).
        /// </summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            var carcass = c.NearestCarcass(a);
            if (carcass == null) { c.Search(); return; }
            if (carcass.Value.Distance > grabReach) { c.WalkTo(carcass.Value.Position, 0f); return; }
            var cover = c.NearestCover(a);
            if (cover == null) { c.Stay(); return; }
            c.WalkTo(cover.Value.Point, 0f);
            c.OnArrival(new Drag(carcass.Value.Entity));
        }

        /// <summary>True when the "Carcass" sense reports a carcass in sight (CTRL-10); false without that sense.</summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token("Carcass");
            return t != null && t != "none";
        }

        /// <summary>After moving, the carcass follows the animal (it was within reach before the move).</summary>
        sealed class Drag : IInteraction
        {
            readonly Carcass carcass;   // the carcass being dragged

            /// <summary>Drags <paramref name="c"/>.</summary>
            public Drag(Carcass c) { carcass = c; }

            /// <summary>Unlimited: the drag applies wherever the animal ended its move.</summary>
            public float Reach => float.PositiveInfinity;

            /// <summary>Puts the carcass at the animal's position, unless it was eaten up meanwhile.</summary>
            public void Apply(Animal a, InteractionContext c)
            {
                if (!carcass.UsedUp) carcass.Position = a.Position;
            }
        }
    }
}
