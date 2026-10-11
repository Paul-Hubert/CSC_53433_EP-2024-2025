using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>The decisions of the current tick, the brain answers they wait for, and the run's memo (08 §1–§4).</summary>
    /// <remarks>
    /// The World holds one (<c>World.Decisions</c>), shared by the three decision phases: the sense phase clears it and adds
    /// the deciding animals, the ask-brains phase adds the brain answers, and the choose-actions phase waits for
    /// <see cref="Pending"/>, then reads <see cref="Due"/>. A student who replaces one of these phases uses the same calls.
    /// </remarks>
    public sealed class DecisionState
    {
        readonly List<Decision> due = new List<Decision>();             // this tick's decisions, a prefix of the pool
        readonly List<Decision> pool = new List<Decision>();            // Decision objects reused from tick to tick
        readonly List<BrainAnswer> answers = new List<BrainAnswer>();   // this tick's batch answers
        readonly List<Pending> pendings = new List<Pending>();          // scratch list for Pending

        /// <summary>The animals deciding this tick, in species order then id order.</summary>
        public IReadOnlyList<Decision> Due => due;

        /// <summary>The run's decision memo (DEC-30), cleared when a run starts.</summary>
        public DecisionMemo Memo { get; } = new DecisionMemo();

        /// <summary>The batch answers this tick waits for, one per brain batch.</summary>
        public IReadOnlyList<BrainAnswer> Answers => answers;

        /// <summary>The tick the due list belongs to.</summary>
        public int Tick { get; internal set; } = -1;

        /// <summary>Model calls made by brains this run (cache and memo hits excluded).</summary>
        public int ModelCalls { get; internal set; }

        /// <summary>Decisions answered from the answer cache this run (DEC-33).</summary>
        public int CacheHits { get; internal set; }

        /// <summary>Done when every brain answer of this tick is in (TICK-03).</summary>
        /// <remarks>Null when nothing is pending: every answer was given at once, or came from the memo or the cache. The
        /// choose-actions phase returns it from its <c>WaitsFor</c>.</remarks>
        public Pending Pending
        {
            get
            {
                pendings.Clear();
                foreach (var a in answers) if (!a.IsDone || !a.Pending.IsDone) pendings.Add(a.Pending);
                return pendings.Count == 0 ? null : Pending.All(pendings.ToArray());
            }
        }

        /// <summary>Adds a deciding animal for this tick (a student's own sense phase calls it).</summary>
        /// <param name="a">The animal that decides.</param>
        /// <returns>Its decision, cleared, to fill with the observation and the situation text.</returns>
        public Decision Add(Animal a)
        {
            Decision d;
            if (pool.Count > due.Count) d = pool[due.Count];
            else { d = new Decision(); pool.Add(d); }
            d.Reset(a);
            due.Add(d);
            return d;
        }

        /// <summary>Adds an answer this tick waits for (a student's own ask phase calls it).</summary>
        /// <param name="a">The answer a brain's <see cref="Brain.Ask"/> returned.</param>
        public void AddAnswer(BrainAnswer a) => answers.Add(a);

        /// <summary>Starts a tick's decisions: empties the due list and the answers; the memo stays.</summary>
        /// <param name="tick">The tick that starts.</param>
        public void Clear(int tick)
        {
            due.Clear();
            answers.Clear();
            Tick = tick;
        }

        /// <summary>Starts a run: clears the tick's decisions, the memo and the counters.</summary>
        internal void Reset()
        {
            Clear(-1);
            Memo.Clear();
            ModelCalls = 0;
            CacheHits = 0;
        }
    }
}
