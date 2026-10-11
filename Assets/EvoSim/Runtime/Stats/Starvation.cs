namespace EvoSim
{
    /// <summary>Death by starvation: energy ≤ 0 at the death check (ANIM-40).</summary>
    /// <remarks>
    /// Teaching-path exercise 3. It finds the species' <see cref="Energy"/> module once, in <see cref="Initialize"/>,
    /// and names the cause <c>"starvation"</c> for every animal whose energy is at or below 0. A species without an
    /// Energy module never starves (V-26 warns).
    /// </remarks>
    public class Starvation : DeathRule
    {
        Energy energy;      // the species' energy module, found in Initialize; null: no animal starves

        /// <summary>Finds the species' <see cref="Energy"/> module.</summary>
        /// <inheritdoc/>
        public override void Initialize() => energy = Species.Module<Energy>();

        /// <summary>One cause: <c>"starvation"</c>.</summary>
        public override System.Collections.Generic.IEnumerable<string> Causes => new[] { "starvation" };

        /// <summary><c>"starvation"</c> when the animal's energy is at or below 0, else null.</summary>
        /// <inheritdoc/>
        public override string CauseOfDeath(Animal a) => energy != null && a[energy.Value] <= 0f ? "starvation" : null;

        /// <summary>Warns when the species has no Energy module (V-26), with a fix that adds one.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null) report.Warning("V-26", this, "Starvation needs an Energy module.", BreedPhase.AddModule<Energy>(Species, "Energy"));
        }
    }
}
