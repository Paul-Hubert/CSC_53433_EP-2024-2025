namespace EvoSim
{
    /// <summary>
    /// The contact an action asks for on arrival (ACT-03, ACT-10): graze an item, strike an animal, eat a carcass
    /// portion or an egg, or a custom one. A small value, so planning allocates nothing.
    /// </summary>
    /// <remarks>
    /// Build one with a static method (<see cref="Graze"/>, <see cref="Strike"/>, <see cref="Scavenge"/>,
    /// <see cref="EatEgg"/>, <see cref="Of"/>) and pass it to <see cref="ActContext.OnArrival(Interaction)"/>. The
    /// <see cref="InteractionResolver"/> applies it after the animal moved, if the target is within reach, still there,
    /// and allowed by the eater's diet (SPEC-13).
    /// </remarks>
    public readonly struct Interaction
    {
        /// <summary>Which contact effect; <see cref="InteractionKind.None"/> when the action asked for nothing.</summary>
        public readonly InteractionKind Kind;
        /// <summary>The food item to graze (<see cref="InteractionKind.Graze"/>).</summary>
        public readonly ResourceItem Item;
        /// <summary>The animal to strike (<see cref="InteractionKind.Strike"/>), or null.</summary>
        public readonly Animal Target;
        /// <summary>The carcass to eat a portion of (<see cref="InteractionKind.Scavenge"/>), or null.</summary>
        public readonly Carcass Carcass;
        /// <summary>The egg to eat (<see cref="InteractionKind.EatEgg"/>), or null.</summary>
        public readonly Egg Egg;
        /// <summary>The student's effect (<see cref="InteractionKind.Custom"/>), or null.</summary>
        public readonly IInteraction Custom;

        /// <summary>Builds an interaction; actions use the static methods instead.</summary>
        Interaction(InteractionKind kind, ResourceItem item = default, Animal target = null, Carcass carcass = null,
                    Egg egg = null, IInteraction custom = null)
        {
            Kind = kind; Item = item; Target = target; Carcass = carcass; Egg = egg; Custom = custom;
        }

        /// <summary>No interaction: nothing happens on arrival.</summary>
        public static Interaction None => default;
        /// <summary>Consume the item and gain its energy (ACT-11): the edible's energy times the diet's scale. No digestion.</summary>
        /// <param name="item">
        /// An item found by <see cref="ActContext.NearestResource"/>; grazed if within the graze reach
        /// (reference 0.5 m).
        /// </param>
        /// <returns>The interaction to pass to <see cref="ActContext.OnArrival(Interaction)"/>.</returns>
        public static Interaction Graze(ResourceItem item) => new Interaction(InteractionKind.Graze, item: item);
        /// <summary>
        /// Try to kill the animal (ACT-11, ACT-12): with the hunter's kill chance it dies, a carcass may appear, and the
        /// hunter gains its energy and becomes busy digesting. A hunter strikes at most once per tick.
        /// </summary>
        /// <param name="target">
        /// The prey, usually found by <see cref="ActContext.NearestAnimal"/>; struck if within the strike reach
        /// (reference 1 m).
        /// </param>
        /// <returns>The interaction to pass to <see cref="ActContext.OnArrival(Interaction)"/>.</returns>
        public static Interaction Strike(Animal target) => new Interaction(InteractionKind.Strike, target: target);
        /// <summary>Eat one portion of the carcass (ACT-11): the eater gains the portion's energy and becomes busy digesting.</summary>
        /// <param name="carcass">
        /// A carcass found by <see cref="ActContext.NearestCarcass"/>; eaten from if within the scavenge reach
        /// (reference 1 m).
        /// </param>
        /// <returns>The interaction to pass to <see cref="ActContext.OnArrival(Interaction)"/>.</returns>
        public static Interaction Scavenge(Carcass carcass) => new Interaction(InteractionKind.Scavenge, carcass: carcass);
        /// <summary>Eat the egg (REPRO-23): it never hatches and is recorded as lost, "eaten". No digestion.</summary>
        /// <param name="egg">
        /// An egg found by <see cref="ActContext.NearestEgg"/>; eaten if within the graze reach (reference 0.5 m).
        /// </param>
        /// <returns>The interaction to pass to <see cref="ActContext.OnArrival(Interaction)"/>.</returns>
        public static Interaction EatEgg(Egg egg) => new Interaction(InteractionKind.EatEgg, egg: egg);
        /// <summary>A student's interaction (22 §3); <see cref="ActContext.OnArrival(IInteraction)"/> wraps it for you.</summary>
        /// <param name="custom">The contact effect, applied within its own <see cref="IInteraction.Reach"/>.</param>
        /// <returns>The interaction to pass to <see cref="ActContext.OnArrival(Interaction)"/>.</returns>
        public static Interaction Of(IInteraction custom) => new Interaction(InteractionKind.Custom, custom: custom);

        /// <summary>True when no interaction was asked for.</summary>
        public bool IsNone => Kind == InteractionKind.None;
    }
}
