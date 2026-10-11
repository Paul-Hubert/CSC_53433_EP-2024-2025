namespace EvoSim
{
    /// <summary>Something phases and modules use: ground, layers, brains, recorder. Found with World.Service&lt;T&gt;().</summary>
    /// <remarks>
    /// <para>Derive from it (or from a service kind: <see cref="Ground"/>, <see cref="ResourceLayer"/>, <see cref="Brain"/>…)
    /// and place the component under the World, outside every species. Other modules find it by type with
    /// <see cref="World.Service{T}()"/>, or by type and name with <see cref="World.Service{T}(string)"/>: never by a scene
    /// reference, so prefabs can be shared (ARCH-08).</para>
    /// <para>Services are initialised before the species' modules and the phases, in hierarchy order, the ground first.
    /// See <see cref="WorldModule"/> for every hook.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Ground ground;
    /// public override void Initialize() => ground = World.Service&lt;Ground&gt;();
    /// </code>
    /// </example>
    public abstract class WorldService : WorldModule
    {
        /// <summary>The name other modules use to find this service (ARCH-08): its GameObject's name.</summary>
        /// <remarks>Layers and brains are found by name, so two of them may not share one (V-18).</remarks>
        public virtual string ServiceName => gameObject.name;

        /// <summary>Rule lines this service adds to one species' prompt (PROMPT-01), e.g. a line about a layer that species uses.</summary>
        /// <param name="species">The species whose prompt is being built.</param>
        /// <param name="w">The prompt writer: add lines with its <c>Rule</c> method.</param>
        /// <remarks>Called while each species' prompt template is built, after the species' own modules wrote theirs.
        /// Write nothing for a species the service doesn't concern.</remarks>
        public virtual void WritePromptRules(Species species, PromptWriter w) { }
    }
}
