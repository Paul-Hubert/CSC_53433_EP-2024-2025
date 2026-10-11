using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Runs LLM mutations (10 §2, §4): every try's instruction and seed are drawn at conception (MUT-30); requests are
    /// batched by round — all first tries together, then the redraws of the rejected ones (MUT-31); answers are
    /// cached by model, prompt, seed and temperature (MUT-14). It advances only when pumped on the main thread, so
    /// the Freeze and Responsive wait modes give the same results.
    /// </summary>
    /// <remarks>
    /// <para>A world service, one per world. It reaches the model through the World's <see cref="MutatorClient"/> and
    /// stores answers in the <see cref="AnswerCache"/> service when caching is on.</para>
    /// <para>A tick, in order:</para>
    /// <list type="number">
    /// <item><description>in the breed phase, <see cref="LlmMutation"/> calls <see cref="Submit"/> for each mutating text
    /// gene; a first try already in the cache is answered at once;</description></item>
    /// <item><description>at the end of the breed phase, <see cref="Flush"/> sends every queued try as one batch;</description></item>
    /// <item><description>in the hatch phase, <see cref="Pump"/> reads a finished batch: each answer is cleaned and checked
    /// (MUT-12); an accepted answer completes its job, a rejected one queues the job's next try (MUT-13), and the next
    /// round goes out at once;</description></item>
    /// <item><description>when a due egg's jobs are not done, the hatch phase waits on <see cref="PendingFor"/>
    /// (MUT-32).</description></item>
    /// </list>
    /// </remarks>
    public class MutatorService : WorldService
    {
        [SerializeField, Tooltip("The mutator model (reference gemma4:26b on the CPU, owner 2026-10-10; qwen3.5:0.8b is the small, faster choice).")]
        string model = "gemma4:26b";
        [SerializeField, Min(0f), Tooltip("Sampling temperature of the mutator (reference 1.2).")]
        float temperature = 1.2f;
        [SerializeField, Tooltip("Off (reference): an unavailable mutator leaves the gene unchanged (MUT-04). On: the run also stops, at the end of that tick.")]
        bool strict;
        [SerializeField, Tooltip("Store and reuse the mutator's answers (MUT-14).")]
        bool useCache = true;

        /// <summary>One LLM mutation in progress: its job, the prompt's parts, and every try drawn at conception.</summary>
        sealed class Job
        {
            /// <summary>The job handed to the operator, completed or failed here.</summary>
            public MutationJob Result;
            /// <summary>The parent sentence, and the fixed context line.</summary>
            public string Parent, Context;
            /// <summary>The deck's instructions.</summary>
            public IReadOnlyList<string> Instructions;
            /// <summary>For each try, the index of its instruction in the deck.</summary>
            public int[] Instruction;
            /// <summary>For each try, its seed.</summary>
            public long[] Seed;
            /// <summary>The longest mutant allowed, in words; and the try being asked, from 0.</summary>
            public int MaxWords, Next;
        }

        readonly List<Job> queue = new List<Job>();                   // jobs whose current try waits for the next batch
        readonly List<Job> inFlight = new List<Job>();                // jobs of the batch out, in request order
        readonly List<MutatorRequest> requests = new List<MutatorRequest>();   // reused for each batch sent
        MutatorReply reply;                                           // the batch out, or null when none is
        MutatorClient client;                                         // the World's mutator client, or null
        AnswerCache cache;                                            // the World's answer cache, or null when caching is off
        int outstanding;                                              // jobs submitted and not finished yet

        /// <summary>The mutator model's name, sent with every request and recorded with mutants (reference gemma4:26b).</summary>
        public string Model => model;
        /// <summary>The sampling temperature of the mutator (reference 1.2).</summary>
        public float Temperature => temperature;
        /// <summary>
        /// True: a failed mutator call also stops the run, at the end of the tick. False (reference): the gene stays
        /// unchanged and the run goes on (MUT-04).
        /// </summary>
        public bool Strict { get => strict; set => strict = value; }
        /// <summary>Batches sent so far, with their sizes (T-MUT-11).</summary>
        public List<int> BatchSizes { get; } = new List<int>();
        /// <summary>True when the World has a mutator client; without one every LLM mutation fails ("no mutator").</summary>
        public bool HasClient => client != null;

        /// <summary>Finds the client and the cache and empties the queues, at the start of a run.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            client = World.Service<MutatorClient>();
            cache = useCache ? World.Service<AnswerCache>() : null;
            queue.Clear(); inFlight.Clear(); BatchSizes.Clear();
            reply = null;
            outstanding = 0;
        }

        /// <summary>Sets the service from code (WorldBuilder, tests).</summary>
        /// <param name="modelName">The mutator model's name.</param>
        /// <param name="temp">The sampling temperature.</param>
        /// <param name="cached">Whether answers are stored and reused (MUT-14).</param>
        public void Configure(string modelName, float temp, bool cached = true)
        {
            model = modelName;
            temperature = temp;
            useCache = cached;
        }

        /// <summary>Starts a job whose tries are already drawn (MUT-30); its first try waits for the next Flush.</summary>
        /// <param name="parent">The sentence to change.</param>
        /// <param name="instructionPerTry">For each try, in order, the index of its instruction in <paramref name="instructions"/>.</param>
        /// <param name="seedPerTry">For each try, its seed; same length as <paramref name="instructionPerTry"/>.</param>
        /// <param name="contextLine">The fixed first line of the prompt, or empty.</param>
        /// <param name="instructions">The deck's instructions.</param>
        /// <param name="maxWords">The longest mutant allowed, in words.</param>
        /// <returns>The job; it is done when an answer passes the guards or every try is rejected.</returns>
        /// <remarks>Called by <see cref="LlmMutation.StartMutation"/>, or by a student's operator that uses the mutator.</remarks>
        public MutationJob Submit(string parent, int[] instructionPerTry, long[] seedPerTry, string contextLine,
                                  IReadOnlyList<string> instructions, int maxWords)
        {
            var job = new Job
            {
                Result = new MutationJob(), Parent = parent, Context = contextLine, Instructions = instructions,
                Instruction = instructionPerTry, Seed = seedPerTry, MaxWords = maxWords,
            };
            outstanding++;
            Enqueue(job);
            return job.Result;
        }

        /// <summary>The request for a job's current try: its prompt and seed, with this service's temperature and model.</summary>
        MutatorRequest RequestFor(Job j) =>
            new MutatorRequest(MutationText.Prompt(j.Context, j.Instructions[j.Instruction[j.Next]], j.Parent), j.Seed[j.Next], temperature, model);

        /// <summary>
        /// Answers a job's current try from the cache, or queues it for the next batch; fails the job when there is no client.
        /// </summary>
        void Enqueue(Job j)
        {
            var request = RequestFor(j);
            if (cache != null && cache.TryGetText("mutator-" + model, request.CacheKey, out var cached))
            {
                World.Mutations.CacheHits++;
                Answer(j, cached, null, fromCache: true);
                return;
            }
            if (client == null) { Finish(j, "no mutator"); return; }
            queue.Add(j);
        }

        /// <summary>Sends the queued tries as one batch, if no batch is out (MUT-31).</summary>
        /// <remarks>Called at the end of the breed phase, and by <see cref="Pump"/> for the next round.</remarks>
        public void Flush()
        {
            if (reply != null || queue.Count == 0) return;
            inFlight.Clear();
            inFlight.AddRange(queue);
            queue.Clear();
            requests.Clear();
            foreach (var j in inFlight) requests.Add(RequestFor(j));
            BatchSizes.Add(requests.Count);
            reply = client.Send(requests);
        }

        /// <summary>Processes a finished batch, queues and sends the next round; true when no job is left.</summary>
        /// <returns>True when every submitted job is done.</returns>
        /// <remarks>Called on the main thread by the hatch phase each tick, and by the pending of <see cref="PendingFor"/>.</remarks>
        public bool Pump()
        {
            if (reply != null && reply.Pending.IsDone)
            {
                var done = reply;
                reply = null;
                World.Mutations.ModelCalls += done.ModelCalls > 0 ? done.ModelCalls : done.Count;
                var batch = inFlight.ToArray();
                inFlight.Clear();
                for (int i = 0; i < batch.Length; i++) Answer(batch[i], done.Answer(i), done.Error(i), fromCache: false);
            }
            Flush();
            return outstanding == 0;
        }

        /// <summary>
        /// Handles the answer to a job's current try: a failed call fails the job (MUT-04); an answer is cached, cleaned and
        /// checked (MUT-12), then completes the job or is rejected, and the job's next try is queued (MUT-13).
        /// </summary>
        void Answer(Job j, string answer, string error, bool fromCache)
        {
            if (answer == null)
            {
                // Strict: the run stops at the tick boundary; this tick still ends with the gene unchanged (RAND-20).
                if (strict) World.RequestStop("mutator failure: " + error);
                Finish(j, "mutator unavailable: " + error);
                return;
            }
            var request = RequestFor(j);
            if (!fromCache && cache != null) cache.StoreText("mutator-" + model, request.CacheKey, answer);
            string cleaned = MutationText.Clean(answer);
            string why = MutationText.Reject(cleaned, j.Parent, j.MaxWords);
            if (why == null)
            {
                j.Result.Complete(AlleleValue.OfText(cleaned), "llm#" + j.Instruction[j.Next], model, j.Seed[j.Next]);
                outstanding--;
                return;
            }
            j.Result.Reject(why);
            World.Mutations.Reject(why);
            j.Next++;
            if (j.Next < j.Seed.Length) Enqueue(j);                                // MUT-13: a new instruction and seed
            else Finish(j, "every try rejected");
        }

        /// <summary>Ends a job without a value: the gene keeps its allele (MUT-04).</summary>
        void Finish(Job j, string reason)
        {
            j.Result.Fail(reason);
            outstanding--;
        }

        /// <summary>What a phase waits for: done when these jobs are; waiting pumps and blocks on the batch out (Freeze).</summary>
        /// <param name="jobs">Jobs this service runs, e.g. the unfinished mutations of the eggs due to hatch.</param>
        /// <returns>A pending that is done when every one of the jobs is done.</returns>
        public Pending PendingFor(IReadOnlyList<MutationJob> jobs) => new JobsPending(this, jobs);

        /// <summary>The pending of <see cref="PendingFor"/>: checking it pumps the service.</summary>
        sealed class JobsPending : Pending
        {
            readonly MutatorService service;                          // the service to pump
            readonly MutationJob[] jobs;                              // a copy of the jobs waited for

            /// <summary>Waits for a copy of the list of jobs.</summary>
            public JobsPending(MutatorService service, IReadOnlyList<MutationJob> jobs)
            {
                this.service = service;
                this.jobs = new MutationJob[jobs.Count];
                for (int i = 0; i < jobs.Count; i++) this.jobs[i] = jobs[i];
            }

            /// <summary>Pumps the service, then true when every job is done.</summary>
            /// <inheritdoc/>
            public override bool IsDone
            {
                get
                {
                    service.Pump();
                    foreach (var j in jobs) if (!j.IsDone) return false;
                    return true;
                }
            }

            /// <summary>Blocks until done (Freeze): waits for the batch out, round after round, sending one when none is out.</summary>
            /// <inheritdoc/>
            public override void Wait()
            {
                for (int guard = 0; guard < 100000 && !IsDone; guard++)        // a bound on rounds: a broken client can't hang the editor
                {
                    if (service.reply == null) { service.Flush(); if (service.reply == null) break; }   // nothing left to send
                    service.reply.Pending.Wait();
                }
            }
        }
    }
}
