using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What a kill leaves (03 §3): a few portions that other eaters may eat, one portion each, never the killer. It rots
    /// away when its lifetime runs out, eaten or not, and goes as soon as its last portion is eaten (ENV-22).
    /// </summary>
    /// <remarks>
    /// <para><see cref="CarcassSystem.Create"/> makes one when a strike kills, and <see cref="CarcassSystem.EatPortion"/>
    /// feeds an eater from it. The system reuses removed carcasses, so don't keep a reference to one after the
    /// environment phase removed it.</para>
    /// </remarks>
    public class Carcass : Entity
    {
        /// <inheritdoc/>
        public override string Kind => "carcass";
        /// <summary>The species of the killed animal: diets name its carcasses "carcass:&lt;species&gt;".</summary>
        public Species Of;
        /// <summary>The id of the animal that made the kill, which may not eat from it; -1 when unknown.</summary>
        public int KillerId = -1;
        /// <summary>Portions still to eat; at 0 the carcass is used up.</summary>
        public int PortionsLeft;
        /// <summary>Energy points one portion gives, before the eater's diet scale (the killed species' <see cref="Edible.carcassEnergy"/>).</summary>
        public float EnergyPerPortion;
        /// <summary>Ids of the animals that ate a portion already (each eater at most one).</summary>
        public readonly List<int> EatenBy = new List<int>();

        /// <summary>
        /// Checks whether an animal may eat a portion now: portions are left, it is not the killer, and it has not eaten
        /// from this carcass yet (06 §4, 03 §3). The diet is checked elsewhere, by the interaction.
        /// </summary>
        /// <param name="a">The animal that wants to eat.</param>
        /// <returns>True when it may eat a portion; false when the carcass is used up, it made the kill, or it ate here already.</returns>
        public bool CanBeEatenBy(Animal a) =>
            !UsedUp && PortionsLeft > 0 && a.Id != KillerId && !EatenBy.Contains(a.Id);
    }
}
