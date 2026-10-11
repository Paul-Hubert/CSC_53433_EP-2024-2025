namespace EvoSim
{
    /// <summary>
    /// A sense that casts rays (line of sight, terrain ahead): it asks for its rays first; the sense phase casts the
    /// rays of all deciding animals in one batch, then each sense reads its results (SENSE-30). It must give the same
    /// tokens as evaluating it one animal at a time (SENSE-31).
    /// </summary>
    /// <remarks>
    /// <para>Implement <see cref="RequestRays"/> to add the animal's rays, in a fixed order, and <see cref="Sense.Read"/> to
    /// read the k-th one back with <see cref="RayBatch.Hit"/> through <see cref="SenseContext.Rays"/>. Nothing else
    /// changes: place it under a species like any sense.</para>
    /// <para>The rays never hit the views' layer, so what an animal senses never depends on rendering (RAND-11). Read
    /// outside the sense phase, with a context whose rays were not cast, every ray reads as a miss.</para>
    /// </remarks>
    /// <example>
    /// The test sense <c>ObstacleSense</c>: one ray along the heading, "blocked" if it hits a collider within 5 m.
    /// <code>
    /// public class ObstacleSense : BatchedSense
    /// {
    ///     readonly List&lt;string&gt; tokens = new List&lt;string&gt; { "clear", "blocked" };
    ///     public override IReadOnlyList&lt;string&gt; Tokens => tokens;
    ///     protected override string DefaultLabel => "Ahead";
    ///
    ///     public override void RequestRays(Animal a, RayBatch rays) =>
    ///         rays.Add(a.Position + Vector3.up * 0.5f, ActContext.HeadingDirection(a.Heading), 5f);
    ///
    ///     public override int Read(Animal a, SenseContext s) => s.Rays.Hit(a, this, 0, out _) ? 1 : 0;
    /// }
    /// </code>
    /// </example>
    public abstract class BatchedSense : Sense
    {
        /// <summary>
        /// Asks for this animal's rays with <see cref="RayBatch.Add"/>; read their results in Read through
        /// <see cref="SenseContext.Rays"/>.
        /// </summary>
        /// <param name="a">The animal that decides this tick.</param>
        /// <param name="rays">The batch to add rays to; the first ray this call adds is number 0 for this animal and sense.</param>
        /// <remarks>Called by <see cref="SenseContext.CastRays"/>, before any sense reads. Ask for the same rays for the
        /// same state, in the same order: <see cref="Sense.Read"/> finds them by their number.</remarks>
        public abstract void RequestRays(Animal a, RayBatch rays);
    }
}
