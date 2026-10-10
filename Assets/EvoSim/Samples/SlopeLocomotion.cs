using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §14: the kinematic locomotion that refuses ground steeper than a limit and charges energy for every
    /// meter climbed (MOVE-02 still holds: it only moves the animal through Move).
    /// </summary>
    public class SlopeLocomotion : KinematicLocomotion
    {
        [SerializeField, Range(0f, 90f), Tooltip("Steeper ground is refused, in degrees (reference 35).")]
        float maxSlopeDegrees = 35f;
        [SerializeField, Min(0f), Tooltip("Energy per meter climbed (reference 0.3).")]
        float energyPerMeterClimbed = 0.3f;

        public float MaxSlopeDegrees => maxSlopeDegrees;
        public float EnergyPerMeterClimbed => energyPerMeterClimbed;

        public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
        {
            if (!intent.IsStay && c.Ground != null)
            {
                Vector3 ahead = a.Position + intent.Direction;
                if (c.Ground.SlopeDegrees(a.Position, ahead) > maxSlopeDegrees) intent = intent.Stay();   // too steep: don't go
            }
            return base.Move(a, intent, staminaBudget, c);
        }

        public override float ExtraCost(Animal a, Vector3 from, Vector3 to) => Mathf.Max(0f, to.y - from.y) * energyPerMeterClimbed;

        public void Configure(float maxDegrees, float perMeterClimbed)
        {
            maxSlopeDegrees = maxDegrees;
            energyPerMeterClimbed = perMeterClimbed;
        }
    }
}
