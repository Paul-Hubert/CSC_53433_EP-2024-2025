using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Litter size and the energy babies cost (REPRO-10, REPRO-11): k uniform in [min, max], cut so that no parent pays
    /// more than it has, never below min; each baby starts with childEnergy, paid by the parents in equal shares.
    /// </summary>
    public class Litter : SpeciesModule
    {
        [SerializeField, Min(1), Tooltip("Smallest litter (reference 2).")]
        int min = 2;
        [SerializeField, Min(1), Tooltip("Largest litter (reference 4).")]
        int max = 4;
        [SerializeField, Min(0f), Tooltip("Energy of each baby, paid by the parents (reference 40).")]
        float childEnergy = 40f;

        public int Min => min;
        public int Max => max;
        public float ChildEnergy => childEnergy;

        /// <summary>The litter size for these parents' energies (REPRO-10): drawn, then cut to what they can pay, never below min.</summary>
        public int Size(RandomStream rng, float[] parentEnergies)
        {
            int k = rng.Range(min, max + 1);
            float share = childEnergy / parentEnergies.Length;
            int affordable = int.MaxValue;
            if (share > 0f)
                foreach (var e in parentEnergies) affordable = Mathf.Min(affordable, Mathf.FloorToInt(e / share));
            return Mathf.Max(min, Mathf.Min(k, affordable));
        }

        public void Configure(int smallest, int largest, float energy)
        {
            min = smallest;
            max = largest;
            childEnergy = energy;
        }

        public override void Validate(ValidationReport report)
        {
            if (min < 1 || min > max) report.Error("V-33", this, $"Litter [{min}, {max}]: min must be at least 1 and at most max.");
            var rule = Species.Module<MatingRule>();
            if (rule != null && childEnergy * min / (rule.Sexual ? 2f : 1f) > rule.MateEnergy)
                report.Warning("V-34", this, "A minimal litter costs a parent more than the mate energy: parents can fall below zero (REPRO-10).");
        }
    }
}
