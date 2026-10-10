namespace EvoSim
{
    /// <summary>How something edible is eaten (SPEC-10).</summary>
    public enum EatMethod
    {
        /// <summary>Consume an item within reach, gain its energy.</summary>
        Graze,
        /// <summary>Try to kill an animal within reach; on success gain its energy and maybe leave a carcass.</summary>
        Strike,
        /// <summary>Eat one portion of a carcass (or any entity with portions) within reach.</summary>
        Scavenge
    }
}
