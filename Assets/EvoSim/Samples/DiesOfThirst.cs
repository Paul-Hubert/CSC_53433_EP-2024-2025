using System.Collections.Generic;

namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §1: thirst grows in the death phase, and an animal at the deadly level dies of "thirst".</summary>
    /// <remarks>
    /// <para><b>What it adds.</b> A cause of death, "thirst", and the growth of the <see cref="Thirst"/> stat: every tick, in
    /// the death phase, each animal's thirst grows by <see cref="Thirst.PerTick"/>; at <see cref="Thirst.Deadly"/> the
    /// animal dies. One small rule does both jobs, so thirst ages with the animal.</para>
    /// <para><b>Mechanism.</b> A <see cref="DeathRule"/>: <see cref="Causes"/> names its causes (one statistics column each)
    /// and <see cref="CauseOfDeath"/> returns a cause or null. <c>[RequiresModule(typeof(Thirst))]</c> names the module it
    /// needs: validation offers to add it, and the conformance suites add it before testing.</para>
    /// <para><b>In a scene.</b> Add it on a child GameObject of the species, with a <see cref="Thirst"/>.</para>
    /// </remarks>
    [RequiresModule(typeof(Thirst))]
    public class DiesOfThirst : DeathRule
    {
        static readonly IReadOnlyList<string> causes = System.Array.AsReadOnly(new[] { "thirst" });   // shared, so read-only (ARCH-07)
        Thirst thirst;   // the species' thirst module, found in Initialize; null without one

        /// <summary>"thirst".</summary>
        public override IEnumerable<string> Causes => causes;

        /// <summary>Finds the species' <see cref="Thirst"/> module.</summary>
        public override void Initialize() => thirst = Species.Module<Thirst>();

        /// <summary>
        /// Adds a tick of thirst, then returns "thirst" when the deadly level is reached, else null (no thirst module: null).
        /// </summary>
        /// <inheritdoc/>
        public override string CauseOfDeath(Animal a)
        {
            if (thirst == null) return null;
            a[thirst.Value] += thirst.PerTick;                       // ages with the animal, in the death phase
            return a[thirst.Value] >= thirst.Deadly ? "thirst" : null;
        }

        /// <summary>Reports an error (V-20) when the species has no <see cref="Thirst"/> module.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Thirst>() == null) report.Error("V-20", this, "DiesOfThirst needs a Thirst module.");
        }
    }
}
