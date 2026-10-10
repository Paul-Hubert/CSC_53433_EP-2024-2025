namespace EvoSim
{
    /// <summary>Maps a batch of queries to probability rows over the species' actions (DEC-11).</summary>
    public abstract class Brain : WorldService
    {
        /// <summary>Part of memo and cache keys (DEC-30, DEC-33).</summary>
        public abstract string Id { get; }

        /// <summary>The most actions a species may have with this brain (DEC-14).</summary>
        public virtual int MaxActions => int.MaxValue;

        /// <summary>Whether queries may carry attachments (SENSE-41).</summary>
        public virtual bool AcceptsAttachments => false;
    }
}
