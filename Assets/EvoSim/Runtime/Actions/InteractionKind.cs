namespace EvoSim
{
    /// <summary>The contact effects of 07 §3 (ACT-11), plus custom ones.</summary>
    public enum InteractionKind
    {
        /// <summary>No interaction: nothing happens on arrival.</summary>
        None,
        /// <summary>Consume an item of a resource layer and gain its energy.</summary>
        Graze,
        /// <summary>Try to kill an animal; on success eat it and digest.</summary>
        Strike,
        /// <summary>Eat one portion of a carcass and digest.</summary>
        Scavenge,
        /// <summary>Eat an egg, which then never hatches (REPRO-23).</summary>
        EatEgg,
        /// <summary>A student's effect, an <see cref="IInteraction"/> (22 §3).</summary>
        Custom
    }
}
