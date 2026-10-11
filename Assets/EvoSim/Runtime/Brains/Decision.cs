namespace EvoSim
{
    /// <summary>One animal deciding this tick: what it sensed, what its brain was asked and answered (08 §1–§5).</summary>
    /// <remarks>
    /// The decision phases fill it in turn: the sense phase sets <see cref="Animal"/>, <see cref="Observation"/> and
    /// <see cref="Situation"/>; the ask-brains phase the <see cref="Query"/>, the keys, and the row when the memo or the
    /// cache has it, else the <see cref="Answer"/> to wait for; the choose-actions phase the final <see cref="Row"/> or the
    /// failure. Objects are pooled and reused from tick to tick: don't keep one beyond its tick.
    /// </remarks>
    public sealed class Decision
    {
        /// <summary>The animal that decides.</summary>
        public Animal Animal;

        /// <summary>Its sense tokens at the start of the tick (SENSE-02).</summary>
        public Observation Observation;

        /// <summary>The observation as text, in the world's style (SENSE-10).</summary>
        public string Situation;

        /// <summary>What its brain is asked (DEC-10), built by the ask-brains phase.</summary>
        public DecisionQuery Query;

        /// <summary>The brain of its species (its own, or the World's default), or null when there is none.</summary>
        public Brain Brain;

        /// <summary>
        /// The memo key (DEC-30), or null when an attachment has no cache key (SENSE-41) or the brain is not memoizable: then
        /// the decision is never memoised, shared or cached.
        /// </summary>
        public string MemoKey;

        /// <summary>The answer cache key (DEC-33), or null when the brain doesn't use the cache or the decision can't be cached.</summary>
        public string CacheKey;

        /// <summary>The answer row once known.</summary>
        public float[] Row;

        /// <summary>
        /// True when the brain failed, its row was malformed, or there was no brain: the animal then draws from a uniform
        /// row, and nothing is stored (DEC-34, DEC-40).
        /// </summary>
        public bool Failed;

        /// <summary>Why the decision failed ("no brain", the brain's message, "sums to 0.9"…), or null.</summary>
        public string Error;

        /// <summary>
        /// Where the row came from: "memo", "cache", "brain", or "same" for a duplicate of another decision this tick
        /// (DEC-32); null when there was no brain.
        /// </summary>
        public string Source;

        /// <summary>The decision of this tick with the same key whose answer this one shares (DEC-32), or null.</summary>
        public Decision SameAs;

        /// <summary>The batch answer that holds this decision's row, and its index in it.</summary>
        public BrainAnswer Answer;

        /// <summary>This decision's index in <see cref="Answer"/>, or -1 when it was not sent to the brain.</summary>
        public int AnswerIndex = -1;

        /// <summary>Clears everything for a new tick and sets the deciding animal.</summary>
        internal void Reset(Animal a)
        {
            Animal = a;
            Observation = null; Situation = null; Query = null; Brain = null;
            MemoKey = CacheKey = null; Row = null; Failed = false; Error = null; Source = null; SameAs = null;
            Answer = null; AnswerIndex = -1;
        }
    }
}
