namespace EvoSim
{
    /// <summary>
    /// The contact an action asks for on arrival (ACT-03, ACT-10): graze an item, strike an animal, eat a carcass
    /// portion or an egg, or a custom one. A small value, so planning allocates nothing.
    /// </summary>
    public readonly struct Interaction
    {
        public readonly InteractionKind Kind;
        public readonly ResourceItem Item;
        public readonly Animal Target;
        public readonly Carcass Carcass;
        public readonly Egg Egg;
        public readonly IInteraction Custom;

        Interaction(InteractionKind kind, ResourceItem item = default, Animal target = null, Carcass carcass = null,
                    Egg egg = null, IInteraction custom = null)
        {
            Kind = kind; Item = item; Target = target; Carcass = carcass; Egg = egg; Custom = custom;
        }

        public static Interaction None => default;
        /// <summary>Consume the item and gain its energy (ACT-11).</summary>
        public static Interaction Graze(ResourceItem item) => new Interaction(InteractionKind.Graze, item: item);
        /// <summary>Try to kill the animal (ACT-11, ACT-12).</summary>
        public static Interaction Strike(Animal target) => new Interaction(InteractionKind.Strike, target: target);
        /// <summary>Eat one portion of the carcass (ACT-11).</summary>
        public static Interaction Scavenge(Carcass carcass) => new Interaction(InteractionKind.Scavenge, carcass: carcass);
        /// <summary>Eat the egg (REPRO-23).</summary>
        public static Interaction EatEgg(Egg egg) => new Interaction(InteractionKind.EatEgg, egg: egg);
        /// <summary>A student's interaction (22 §3).</summary>
        public static Interaction Of(IInteraction custom) => new Interaction(InteractionKind.Custom, custom: custom);

        public bool IsNone => Kind == InteractionKind.None;
    }
}
