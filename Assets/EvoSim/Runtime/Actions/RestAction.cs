namespace EvoSim
{
    /// <summary>Stay still to catch your breath and save energy (07 §4). Never searches; the metabolism charges its rest cost.</summary>
    /// <remarks>
    /// The reference rest action, and teaching-path exercise 1: <see cref="Act"/> only calls
    /// <see cref="ActContext.Stay"/>. Because <see cref="Rests"/> is true, a still tick costs the metabolism's rest cost
    /// (reference 0.2) instead of its base cost (0.7), and standing still lets stamina recover (ANIM-21).
    /// </remarks>
    public class RestAction : AnimalAction
    {
        /// <inheritdoc/>
        protected override string DefaultDescription => "stay still to catch your breath and save energy";

        /// <summary>True: a still tick with this action costs the metabolism's rest cost (05 §4).</summary>
        public override bool Rests => true;

        /// <summary>Stays where it is, every tick.</summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c) => c.Stay();
    }
}
