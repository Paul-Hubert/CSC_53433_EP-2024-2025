namespace EvoSim.Neuro
{
    /// <summary>
    /// What stopped a ray of a <see cref="RayFanSense"/> (plan M16 §5.4). The value order breaks ties: at equal distance the
    /// lower value wins, then the lower id (SPACE-08), so animals come before items, and cover and the ground come last.
    /// </summary>
    public enum RayHitKind
    {
        /// <summary>An animal of a species whose diet strikes the viewer's species (the food web).</summary>
        Threat,
        /// <summary>An animal of a species the viewer's diet strikes.</summary>
        Prey,
        /// <summary>An animal of the viewer's species, ready to mate; only when this kind is enabled, else <see cref="Kin"/>.</summary>
        ReadyKin,
        /// <summary>An animal of the viewer's species.</summary>
        Kin,
        /// <summary>Any other animal.</summary>
        OtherAnimal,
        /// <summary>A carcass, edible or not.</summary>
        Carcass,
        /// <summary>An entity of another kind (eggs, fruit…); off in the templates.</summary>
        Entity,
        /// <summary>An item of a resource layer the viewer's diet grazes.</summary>
        Food,
        /// <summary>An item of any other resource layer; off in the templates.</summary>
        Item,
        /// <summary>The edge of a cover patch, seen from outside it.</summary>
        Cover,
        /// <summary>Water; only when this kind is enabled, else <see cref="Obstacle"/>.</summary>
        Water,
        /// <summary>The world's edge, water (unless split out) or ground that can't be walked on.</summary>
        Obstacle,
    }
}
