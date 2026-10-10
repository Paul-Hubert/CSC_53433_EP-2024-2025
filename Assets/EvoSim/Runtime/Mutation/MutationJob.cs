using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// One mutation of one gene of one baby (10 §1): its choices are drawn at conception (MUT-30); the result may
    /// come later, from the mutator (MUT-32). Succeeded → a new value; failed → the inherited allele stays (MUT-04).
    /// </summary>
    public sealed class MutationJob
    {
        readonly List<string> rejections = new List<string>();

        public bool IsDone { get; private set; }
        public bool Succeeded { get; private set; }
        public AlleleValue Result { get; private set; }
        /// <summary>The operator detail recorded with the allele: "llm#3", "gauss"… (GENE-12).</summary>
        public string Operator { get; private set; }
        public string Model { get; private set; }
        public long? Seed { get; private set; }
        public string FailureReason { get; private set; }
        /// <summary>Model calls made for this job, cached answers excluded (MUT-05).</summary>
        public int ModelCalls { get; set; }
        /// <summary>Why each rejected try was rejected (MUT-05, MUT-13).</summary>
        public IReadOnlyList<string> Rejections => rejections;
        /// <summary>What the world waits for while the job runs (SPACE-13); null when done at once.</summary>
        public Pending Pending { get; set; }

        /// <summary>A job finished at once with a value.</summary>
        public static MutationJob Done(AlleleValue value, string op, string model = null, long? seed = null)
        {
            var j = new MutationJob();
            j.Complete(value, op, model, seed);
            return j;
        }

        public static MutationJob Done(float value, string op) => Done(AlleleValue.OfNumber(value), op);
        public static MutationJob Done(string text, string op) => Done(AlleleValue.OfText(text), op);

        /// <summary>A job that failed: the gene doesn't mutate (MUT-04).</summary>
        public static MutationJob Failed(string reason)
        {
            var j = new MutationJob();
            j.Fail(reason);
            return j;
        }

        /// <summary>A job still running; the mutator completes it.</summary>
        public static MutationJob Running(Pending pending) => new MutationJob { Pending = pending };

        public void Complete(AlleleValue value, string op, string model = null, long? seed = null)
        {
            Result = value;
            Operator = op;
            Model = model;
            Seed = seed;
            Succeeded = true;
            IsDone = true;
        }

        public void Fail(string reason)
        {
            FailureReason = reason;
            Succeeded = false;
            IsDone = true;
        }

        public void Reject(string reason) => rejections.Add(reason);
    }
}
