using System.Collections.Generic;

namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §1: thirst grows in the death phase, and an animal at the deadly level dies of "thirst".</summary>
    [RequiresModule(typeof(Thirst))]
    public class DiesOfThirst : DeathRule
    {
        static readonly IReadOnlyList<string> causes = System.Array.AsReadOnly(new[] { "thirst" });   // shared, so read-only (ARCH-07)
        Thirst thirst;

        public override IEnumerable<string> Causes => causes;

        public override void Initialize() => thirst = Species.Module<Thirst>();

        public override string CauseOfDeath(Animal a)
        {
            if (thirst == null) return null;
            a[thirst.Value] += thirst.PerTick;                       // ages with the animal, in the death phase
            return a[thirst.Value] >= thirst.Deadly ? "thirst" : null;
        }

        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Thirst>() == null) report.Error("V-20", this, "DiesOfThirst needs a Thirst module.");
        }
    }
}
