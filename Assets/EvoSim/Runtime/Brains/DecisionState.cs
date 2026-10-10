using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>The decisions of the current tick, the brain answers they wait for, and the run's memo (08 §1–§4).</summary>
    public sealed class DecisionState
    {
        readonly List<Decision> due = new List<Decision>();
        readonly List<Decision> pool = new List<Decision>();
        readonly List<BrainAnswer> answers = new List<BrainAnswer>();
        readonly List<Pending> pendings = new List<Pending>();

        /// <summary>The animals deciding this tick, in species order then id order.</summary>
        public IReadOnlyList<Decision> Due => due;
        public DecisionMemo Memo { get; } = new DecisionMemo();
        public IReadOnlyList<BrainAnswer> Answers => answers;
        /// <summary>The tick the due list belongs to.</summary>
        public int Tick { get; internal set; } = -1;
        /// <summary>Model calls made by brains this run (cache and memo hits excluded).</summary>
        public int ModelCalls { get; internal set; }
        public int CacheHits { get; internal set; }

        /// <summary>Done when every brain answer of this tick is in (TICK-03).</summary>
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
        public Decision Add(Animal a)
        {
            Decision d;
            if (pool.Count > due.Count) d = pool[due.Count];
            else { d = new Decision(); pool.Add(d); }
            d.Reset(a);
            due.Add(d);
            return d;
        }

        /// <summary>An answer this tick waits for (a student's own ask phase calls it).</summary>
        public void AddAnswer(BrainAnswer a) => answers.Add(a);

        /// <summary>Starts a tick's decisions.</summary>
        public void Clear(int tick)
        {
            due.Clear();
            answers.Clear();
            Tick = tick;
        }

        internal void Reset()
        {
            Clear(-1);
            Memo.Clear();
            ModelCalls = 0;
            CacheHits = 0;
        }
    }
}
