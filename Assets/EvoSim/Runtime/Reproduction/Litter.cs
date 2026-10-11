using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Litter size and the energy babies cost (REPRO-10, REPRO-11): k uniform in [min, max], cut so that no parent pays
    /// more than it has, never below min; each baby starts with childEnergy, paid by the parents in equal shares.
    /// </summary>
    /// <remarks>
    /// <para>The breed phase finds it by type (<c>Species.Module&lt;Litter&gt;()</c>), so a replacement derives from it. At
    /// each conception it calls <see cref="Size"/>; each parent then pays <see cref="ChildEnergy"/> / (number of parents)
    /// × k energy (REPRO-11), and each baby hatches with <see cref="ChildEnergy"/>. The Block cap rule may cut k further.
    /// Without a Litter, a conception makes one baby and costs nothing (V-26 warns).</para>
    /// <para>The reference <see cref="Size"/>, step by step (the exercise MyLitter rewrites it):</para>
    /// <list type="number">
    /// <item><description>k = <c>rng.Range(Min, Max + 1)</c>: uniform in [Min, Max], one draw;</description></item>
    /// <item><description>share = ChildEnergy / number of parents;</description></item>
    /// <item><description>affordable = the smallest floor(energy / share) over the parents (no limit when share is 0);</description></item>
    /// <item><description>the result is max(Min, min(k, affordable)): what the poorest parent can pay, but never below Min,
    /// so a parent can end with zero or less energy.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// One baby per conception whatever the energy, as in the prototype before 2026-10-09 (setting Min = Max = 1 does the same):
    /// <code>
    /// public class OneBaby : Litter
    /// {
    ///     public override int Size(RandomStream rng, float[] parentEnergies) => 1;
    /// }
    /// </code>
    /// </example>
    public class Litter : SpeciesModule
    {
        [SerializeField, Min(1), Tooltip("Smallest litter (reference 2).")]
        int min = 2;
        [SerializeField, Min(1), Tooltip("Largest litter (reference 4).")]
        int max = 4;
        [SerializeField, Min(0f), Tooltip("Energy of each baby, paid by the parents (reference 40).")]
        float childEnergy = 40f;

        /// <summary>The smallest litter, in babies (reference 2).</summary>
        public int Min => min;
        /// <summary>The largest litter, in babies (reference 4).</summary>
        public int Max => max;
        /// <summary>The energy each baby starts with, in points, paid by its parents (reference 40).</summary>
        public virtual float ChildEnergy => childEnergy;

        /// <summary>
        /// The litter size for these parents' energies (REPRO-10): drawn, then cut to what they can pay, never below min.
        /// </summary>
        /// <param name="rng">The species' litter stream: draw only from it.</param>
        /// <param name="parentEnergies">Each parent's energy now, in points: two values, or one for a single parent.</param>
        /// <returns>The number of babies, at least <see cref="Min"/>.</returns>
        public virtual int Size(RandomStream rng, float[] parentEnergies)
        {
            int k = rng.Range(min, max + 1);
            float share = childEnergy / parentEnergies.Length;
            int affordable = int.MaxValue;
            if (share > 0f)                                          // free babies: energy sets no limit
                foreach (var e in parentEnergies) affordable = Mathf.Min(affordable, Mathf.FloorToInt(e / share));
            return Mathf.Max(min, Mathf.Min(k, affordable));
        }

        /// <summary>Sets the litter from code (WorldBuilder, tests).</summary>
        /// <param name="smallest">The smallest litter, at least 1.</param>
        /// <param name="largest">The largest litter, at least <paramref name="smallest"/>.</param>
        /// <param name="energy">The energy of each baby, in points.</param>
        public void Configure(int smallest, int largest, float energy)
        {
            min = smallest;
            max = largest;
            childEnergy = energy;
        }

        /// <summary>
        /// Reports a bad range (V-33), and a smallest litter that costs a parent more than the mate energy (V-34).
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (min < 1 || min > max) report.Error("V-33", this, $"Litter [{min}, {max}]: min must be at least 1 and at most max.");
            var rule = Species.Module<MatingRule>();
            if (rule != null && childEnergy * min / (rule.Sexual ? 2f : 1f) > rule.MateEnergy)
                report.Warning("V-34", this, "A minimal litter costs a parent more than the mate energy: parents can fall below zero (REPRO-10).");
        }
    }
}
