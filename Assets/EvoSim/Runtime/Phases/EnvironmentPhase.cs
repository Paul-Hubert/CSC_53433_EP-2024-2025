namespace EvoSim
{
    /// <summary>Resource layers regrow, each with its own stream; entities move, rot or vanish (TICK-04 #9, ENV-03, ENV-22).</summary>
    /// <remarks>
    /// <para>Ninth phase of the reference tick, after Migration and before Floor. It visits the World's services in
    /// hierarchy order: each <see cref="ResourceLayer"/> regrows with the stream named by its
    /// <see cref="ResourceLayer.StreamName"/>, and each <see cref="EntityKind"/> updates its entities. Nothing else in the
    /// world changes here.</para>
    /// <para>A phase of one's own placed before this one can change how the layers grow this tick, as the seasons sample
    /// does (22 §9).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Recipe 22 §9 (Samples/SeasonsPhase, cut down): a phase before Environment that slows the grass in winter.
    /// public class SeasonsPhase : TickPhase
    /// {
    ///     [SerializeField] int yearLength = 2000;
    ///     [SerializeField] float winterRegrowFactor = 0.2f;
    ///     [SerializeField] string layer = "Grass";
    ///     FoodGrid food;
    ///
    ///     public override void Initialize() =&gt; food = World.Service&lt;FoodGrid&gt;(layer);
    ///
    ///     public override void Run(TickContext t)
    ///     {
    ///         bool winter = t.Tick % yearLength &gt;= yearLength / 2;
    ///         if (food != null) food.RegrowFactor = winter ? winterRegrowFactor : 1f;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class EnvironmentPhase : TickPhase
    {
        /// <summary>Regrows every resource layer and updates every entity kind, in the order of the World's services.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            var services = World.Services;
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] is ResourceLayer layer) layer.Regrow(t.Stream(layer.StreamName));
                else if (services[i] is EntityKind kind) kind.UpdateEntities(t);
            }
        }
    }
}
