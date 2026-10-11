namespace EvoSim
{
    /// <summary>
    /// A custom contact effect (22 §3, drink): applied after moving when the animal is within <see cref="Reach"/> of the
    /// point its action walked or ran to (ACT-10).
    /// </summary>
    /// <remarks>
    /// <para>An action asks for one with <see cref="ActContext.OnArrival(IInteraction)"/>, usually as a small private
    /// class inside the action. The act phase moves the animal, then the interaction resolver calls <see cref="Apply"/>
    /// once, in the same tick, if the animal ended within <see cref="Reach"/> of the point its intent was heading to
    /// (<see cref="Intent.Target"/>; its own position for a stay).</para>
    /// <para>What <see cref="Apply"/> must keep: it is the only place where an action's effect on animals happens
    /// (ACT-06); it draws random numbers only from <see cref="InteractionContext.Stream"/>; and nothing checks a diet for
    /// it, so check the <see cref="Diet"/> yourself when the effect is a meal.</para>
    /// </remarks>
    /// <example>
    /// The sample <c>DrinkAction</c> (22 §3) walks to the shore of the nearest water and resets thirst there:
    /// <code>
    /// public override void Act(Animal a, ActContext c)
    /// {
    ///     var water = c.Ground != null ? c.Ground.NearestWater(a.Position, c.Vision(a)) : null;
    ///     if (water == null) { c.Search(); return; }
    ///     c.WalkTo(water.Value.Shore, 0f);
    ///     c.OnArrival(new Drink(thirst));
    /// }
    ///
    /// sealed class Drink : IInteraction
    /// {
    ///     readonly Thirst thirst;
    ///     public Drink(Thirst t) { thirst = t; }
    ///     public float Reach =&gt; 0.5f;
    ///     public void Apply(Animal a, InteractionContext c) { if (thirst != null) a[thirst.Value] = 0f; }
    /// }
    /// </code>
    /// </example>
    public interface IInteraction
    {
        /// <summary>
        /// How close the animal must end, after moving, to the point its intent was heading to, in meters.
        /// <see cref="float.PositiveInfinity"/> applies the effect wherever the animal ends up.
        /// </summary>
        float Reach { get; }
        /// <summary>
        /// Applies the effect (ACT-10). Called by the act phase's <see cref="InteractionResolver"/>, once, right after the
        /// actor moved, only when it is within <see cref="Reach"/> and was not killed earlier in the tick.
        /// </summary>
        /// <param name="actor">The animal whose action asked for the interaction.</param>
        /// <param name="c">
        /// What the effect may use: the world, the ground, <see cref="InteractionContext.Feed"/> and the random
        /// streams.
        /// </param>
        void Apply(Animal actor, InteractionContext c);
    }
}
