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
    public class MutatorService : WorldService
    {
        [SerializeField, Tooltip("The mutator model (reference qwen3.5:0.8b on the CPU).")]
        string model = "qwen3.5:0.8b";
        [SerializeField, Min(0f), Tooltip("Sampling temperature of the mutator (reference 1.2).")]
        float temperature = 1.2f;
        [SerializeField, Tooltip("Off (reference): an unavailable mutator leaves the gene unchanged (MUT-04). On: the run stops.")]
        bool strict;
        [SerializeField, Tooltip("Store and reuse the mutator's answers (MUT-14).")]
        bool useCache = true;

        sealed class Job
        {
            public MutationJob Result;
            public string Parent, Context;
            public IReadOnlyList<string> Instructions;
            public int[] Instruction;
            public long[] Seed;
            public int MaxWords, Next;
        }

        readonly List<Job> queue = new List<Job>();
        readonly List<Job> inFlight = new List<Job>();
        readonly List<MutatorRequest> requests = new List<MutatorRequest>();
        MutatorReply reply;
        MutatorClient client;
        AnswerCache cache;
        int outstanding;

        public string Model => model;
        public float Temperature => temperature;
        public bool Strict { get => strict; set => strict = value; }
        /// <summary>Batches sent so far, with their sizes (T-MUT-11).</summary>
        public List<int> BatchSizes { get; } = new List<int>();
        public bool HasClient => client != null;

        public override void Initialize()
        {
            client = World.Service<MutatorClient>();
            cache = useCache ? World.Service<AnswerCache>() : null;
            queue.Clear(); inFlight.Clear(); BatchSizes.Clear();
            reply = null;
            outstanding = 0;
        }

        public void Configure(string modelName, float temp, bool cached = true)
        {
            model = modelName;
            temperature = temp;
            useCache = cached;
        }

        /// <summary>Starts a job whose tries are already drawn (MUT-30); its first try waits for the next Flush.</summary>
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

        MutatorRequest RequestFor(Job j) =>
            new MutatorRequest(MutationText.Prompt(j.Context, j.Instructions[j.Instruction[j.Next]], j.Parent), j.Seed[j.Next], temperature, model);

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

        void Answer(Job j, string answer, string error, bool fromCache)
        {
            if (answer == null)
            {
                if (strict) { World.Stop("mutator failure: " + error); }
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

        void Finish(Job j, string reason)
        {
            j.Result.Fail(reason);
            outstanding--;
        }

        /// <summary>What a phase waits for: done when these jobs are; waiting pumps and blocks on the batch out (Freeze).</summary>
        public Pending PendingFor(IReadOnlyList<MutationJob> jobs) => new JobsPending(this, jobs);

        sealed class JobsPending : Pending
        {
            readonly MutatorService service;
            readonly MutationJob[] jobs;

            public JobsPending(MutatorService service, IReadOnlyList<MutationJob> jobs)
            {
                this.service = service;
                this.jobs = new MutationJob[jobs.Count];
                for (int i = 0; i < jobs.Count; i++) this.jobs[i] = jobs[i];
            }

            public override bool IsDone
            {
                get
                {
                    service.Pump();
                    foreach (var j in jobs) if (!j.IsDone) return false;
                    return true;
                }
            }

            public override void Wait()
            {
                for (int guard = 0; guard < 100000 && !IsDone; guard++)
                {
                    if (service.reply == null) { service.Flush(); if (service.reply == null) break; }
                    service.reply.Pending.Wait();
                }
            }
        }
    }
}
