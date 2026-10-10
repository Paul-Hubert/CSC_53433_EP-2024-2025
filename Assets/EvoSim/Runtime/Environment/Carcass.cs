using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>What a kill leaves: portions that other eaters may eat, one each, never the killer (03 §3).</summary>
    public class Carcass : Entity
    {
        public override string Kind => "carcass";
        /// <summary>The species of the killed animal: diets name it "carcass:&lt;species&gt;".</summary>
        public Species Of;
        public int KillerId = -1;
        public int PortionsLeft;
        public float EnergyPerPortion;
        /// <summary>Animals that ate a portion already (each eater at most one).</summary>
        public readonly List<int> EatenBy = new List<int>();

        /// <summary>Portions left, not its killer, not eaten from by it yet (06 §4, 03 §3).</summary>
        public bool CanBeEatenBy(Animal a) =>
            !UsedUp && PortionsLeft > 0 && a.Id != KillerId && !EatenBy.Contains(a.Id);
    }
}
