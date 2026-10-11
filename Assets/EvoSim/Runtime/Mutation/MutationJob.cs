using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// One mutation of one gene of one baby (10 §1): its choices are drawn at conception (MUT-30); the result may
    /// come later, from the mutator (MUT-32). Succeeded → a new value; failed → the inherited allele stays (MUT-04).
    /// </summary>
    /// <remarks>
    /// <para><see cref="MutationOperator.StartMutation"/> returns one. An operator that answers at once returns
    /// <see cref="Done(AlleleValue, string, string, long?)"/> or <see cref="Failed"/>. One that works in the background
    /// returns <see cref="Running"/> with a <see cref="EvoSim.Pending"/> the hatch phase can wait for, and later calls
    /// <see cref="Complete"/> or <see cref="Fail"/>; jobs of the <see cref="MutatorService"/> have no pending of their own:
    /// the hatch phase waits on the service.</para>
    /// <para>At hatching a successful job's <see cref="Result"/> is checked by the gene, then registered as a mutant with
    /// <see cref="Operator"/>, <see cref="Model"/> and <see cref="Seed"/> (MUT-03, GENE-12).</para>
    /// </remarks>
    public sealed class MutationJob
    {
        readonly List<string> rejections = new List<string>();

        /// <summary>True once the job has a value or has failed.</summary>
        public bool IsDone { get; private set; }
        /// <summary>True when the job ended with a value in <see cref="Result"/>.</summary>
        public bool Succeeded { get; private set; }
        /// <summary>The new value; meaningful only when <see cref="Succeeded"/>.</summary>
        public AlleleValue Result { get; private set; }
        /// <summary>The operator detail recorded with the allele: "llm#3", "gauss"… (GENE-12).</summary>
        public string Operator { get; private set; }
        /// <summary>The name of the model that wrote the value, or null for an operator without a model.</summary>
        public string Model { get; private set; }
        /// <summary>The seed of the model call that wrote the value, or null.</summary>
        public long? Seed { get; private set; }
        /// <summary>Why the job failed (<c>"no mutator"</c>, <c>"every try rejected"</c>…), or null.</summary>
        public string FailureReason { get; private set; }
        /// <summary>Model calls made for this job, cached answers excluded (MUT-05).</summary>
        public int ModelCalls { get; set; }
        /// <summary>Why each rejected try was rejected (MUT-05, MUT-13).</summary>
        public IReadOnlyList<string> Rejections => rejections;
        /// <summary>
        /// What the world waits for while the job runs (SPACE-13); null when done at once, and for the jobs of the
        /// <see cref="MutatorService"/>, which the hatch phase waits on through the service.
        /// </summary>
        public Pending Pending { get; set; }

        /// <summary>A job finished at once with a value.</summary>
        /// <param name="value">The new value.</param>
        /// <param name="op">The operator detail recorded with the allele, e.g. <c>gauss</c>.</param>
        /// <param name="model">The model that wrote it, or null.</param>
        /// <param name="seed">The model call's seed, or null.</param>
        /// <returns>A done, successful job.</returns>
        public static MutationJob Done(AlleleValue value, string op, string model = null, long? seed = null)
        {
            var j = new MutationJob();
            j.Complete(value, op, model, seed);
            return j;
        }

        /// <summary>A job finished at once with a number.</summary>
        /// <param name="value">The new number.</param>
        /// <param name="op">The operator detail recorded with the allele, e.g. <c>gauss</c>.</param>
        /// <returns>A done, successful job.</returns>
        public static MutationJob Done(float value, string op) => Done(AlleleValue.OfNumber(value), op);
        /// <summary>A job finished at once with a sentence.</summary>
        /// <param name="text">The new sentence.</param>
        /// <param name="op">The operator detail recorded with the allele, e.g. <c>ladder+1</c>.</param>
        /// <returns>A done, successful job.</returns>
        public static MutationJob Done(string text, string op) => Done(AlleleValue.OfText(text), op);

        /// <summary>A job that failed: the gene doesn't mutate (MUT-04).</summary>
        /// <param name="reason">Why, e.g. <c>"no intensity word"</c>.</param>
        /// <returns>A done, failed job.</returns>
        public static MutationJob Failed(string reason)
        {
            var j = new MutationJob();
            j.Fail(reason);
            return j;
        }

        /// <summary>A job still running, for an operator that works in the background: the operator completes it later.</summary>
        /// <param name="pending">What the hatch phase waits for until the operator calls <see cref="Complete"/> or
        /// <see cref="Fail"/> (SPACE-13).</param>
        /// <returns>A running job.</returns>
        public static MutationJob Running(Pending pending) => new MutationJob { Pending = pending };

        /// <summary>Ends the job with a value.</summary>
        /// <param name="value">The new value.</param>
        /// <param name="op">The operator detail recorded with the allele.</param>
        /// <param name="model">The model that wrote it, or null.</param>
        /// <param name="seed">The model call's seed, or null.</param>
        public void Complete(AlleleValue value, string op, string model = null, long? seed = null)
        {
            Result = value;
            Operator = op;
            Model = model;
            Seed = seed;
            Succeeded = true;
            IsDone = true;
        }

        /// <summary>Ends the job without a value: the inherited allele stays (MUT-04).</summary>
        /// <param name="reason">Why it failed.</param>
        public void Fail(string reason)
        {
            FailureReason = reason;
            Succeeded = false;
            IsDone = true;
        }

        /// <summary>Records why one try was rejected (MUT-13); the job goes on with its next try, if any.</summary>
        /// <param name="reason">Why, e.g. <c>"invalid"</c> or <c>"unchanged"</c>.</param>
        public void Reject(string reason) => rejections.Add(reason);
    }
}
