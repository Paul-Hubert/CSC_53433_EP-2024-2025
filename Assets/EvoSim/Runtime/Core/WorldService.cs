namespace EvoSim
{
    /// <summary>Something phases and modules use: ground, layers, brains, recorder. Found with World.Service&lt;T&gt;().</summary>
    public abstract class WorldService : WorldModule
    {
        /// <summary>The name other modules use to find this service (ARCH-08): its GameObject's name.</summary>
        public virtual string ServiceName => gameObject.name;

        /// <summary>Rule lines this service adds to one species' prompt (PROMPT-01), e.g. the carcass line.</summary>
        public virtual void WritePromptRules(Species species, PromptWriter w) { }
    }
}
