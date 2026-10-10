namespace EvoSim
{
    /// <summary>One animal deciding this tick: what it sensed, what its brain was asked and answered (08 §1–§5).</summary>
    public sealed class Decision
    {
        public Animal Animal;
        public Observation Observation;
        public string Situation;
        public DecisionQuery Query;
        public Brain Brain;
        /// <summary>The memo key (DEC-30), or null when an attachment has no cache key (SENSE-41).</summary>
        public string MemoKey;
        public string CacheKey;
        /// <summary>The answer row once known.</summary>
        public float[] Row;
        public bool Failed;
        public string Error;
        /// <summary>Where the row came from: memo, cache, brain, or a duplicate of another decision this tick (DEC-32).</summary>
        public string Source;
        /// <summary>The decision of this tick with the same key whose answer this one shares (DEC-32), or null.</summary>
        public Decision SameAs;
        /// <summary>The batch answer that holds this decision's row, and its index in it.</summary>
        public BrainAnswer Answer;
        public int AnswerIndex = -1;

        internal void Reset(Animal a)
        {
            Animal = a;
            Observation = null; Situation = null; Query = null; Brain = null;
            MemoKey = CacheKey = null; Row = null; Failed = false; Error = null; Source = null; SameAs = null;
            Answer = null; AnswerIndex = -1;
        }
    }
}
