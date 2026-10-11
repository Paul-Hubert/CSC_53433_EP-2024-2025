using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §14: the kinematic locomotion that refuses ground steeper than a limit and charges energy for every
    /// meter climbed (MOVE-02 still holds: it only moves the animal through Move).
    /// </summary>
    /// <remarks>
    /// <para><b>What it adds.</b> A species that avoids steep slopes and pays to climb: on the same terrain, a species with it
    /// and a kinematic one move differently.</para>
    /// <para><b>Mechanism.</b> A subclass of a reference module (MOVE-07): it overrides <see cref="Move"/> to change the intent
    /// (stay when the step ahead is too steep) and then lets <see cref="KinematicLocomotion"/> do the moving, and overrides
    /// <see cref="ExtraCost"/>, which the metabolism adds to the energy of the move (MOVE-06).</para>
    /// <para><b>In a scene.</b> Replace the species' <c>KinematicLocomotion</c> component by this one: a species has exactly
    /// one locomotion (V-13). It matters only on a ground with heights (the terrain ground).</para>
    /// </remarks>
    public class SlopeLocomotion : KinematicLocomotion
    {
        [SerializeField, Range(0f, 90f), Tooltip("Steeper ground is refused, in degrees (reference 35).")]
        float maxSlopeDegrees = 35f;
        [SerializeField, Min(0f), Tooltip("Energy per meter climbed (reference 0.3).")]
        float energyPerMeterClimbed = 0.3f;

        /// <summary>The steepest slope the animal walks onto, in degrees.</summary>
        public float MaxSlopeDegrees => maxSlopeDegrees;

        /// <summary>The energy one meter of climbing costs, in points per meter of height gained.</summary>
        public float EnergyPerMeterClimbed => energyPerMeterClimbed;

        /// <summary>
        /// Looks at the slope from the animal to one meter ahead along the intent; steeper than the limit, the animal stays.
        /// Otherwise it moves as the kinematic locomotion does.
        /// </summary>
        /// <inheritdoc/>
        public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
        {
            if (!intent.IsStay && c.Ground != null)
            {
                Vector3 ahead = a.Position + intent.Direction;
                if (c.Ground.SlopeDegrees(a.Position, ahead) > maxSlopeDegrees) intent = intent.Stay();   // too steep: don't go
            }
            return base.Move(a, intent, staminaBudget, c);
        }

        /// <summary>The height gained times <see cref="EnergyPerMeterClimbed"/>; going down or on the flat costs nothing extra.</summary>
        /// <inheritdoc/>
        public override float ExtraCost(Animal a, Vector3 from, Vector3 to) => Mathf.Max(0f, to.y - from.y) * energyPerMeterClimbed;

        /// <summary>Sets the locomotion from code (tests).</summary>
        /// <param name="maxDegrees">The steepest slope allowed, in degrees.</param>
        /// <param name="perMeterClimbed">The energy per meter climbed, in points.</param>
        public void Configure(float maxDegrees, float perMeterClimbed)
        {
            maxSlopeDegrees = maxDegrees;
            energyPerMeterClimbed = perMeterClimbed;
        }
    }
}
