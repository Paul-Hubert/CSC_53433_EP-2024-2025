namespace EvoSim
{
    /// <summary>Death by starvation: energy ≤ 0 at the death check (ANIM-40).</summary>
    public class Starvation : DeathRule
    {
        Energy energy;

        public override void Initialize() => energy = Species.Module<Energy>();

        public override System.Collections.Generic.IEnumerable<string> Causes => new[] { "starvation" };

        public override string CauseOfDeath(Animal a) => energy != null && a[energy.Value] <= 0f ? "starvation" : null;

        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null) report.Warning("V-20", this, "Starvation needs an Energy module.");
        }
    }
}
