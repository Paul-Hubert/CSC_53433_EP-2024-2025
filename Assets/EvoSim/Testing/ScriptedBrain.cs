using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test brain (30 §2): answers from a policy written in the test, or from given vectors per (species,
    /// observation); counts calls and batches; can answer N Advance calls late, fail, or refuse to mix species.
    /// Test code only: the system ships no rule-based brain (owner decision).
    /// </summary>
    public class ScriptedBrain : Brain
    {
        public string BrainId = "scripted";
        /// <summary>The row for a query; null = uniform.</summary>
        public Func<DecisionQuery, float[]> Policy;
        /// <summary>Rows by "species|observation key", used before the policy.</summary>
        public readonly Dictionary<string, float[]> Vectors = new Dictionary<string, float[]>();
        /// <summary>A failure message for a query, or null.</summary>
        public Func<DecisionQuery, string> FailWhen;
        public int DelayCalls;
        public bool MixesSpecies = true;
        public int ActionLimit = int.MaxValue;
        public int TokenLimit;
        public bool TakesAttachments;
        public string Instruction = "Answer with the probabilities.";

        public int Calls, Batches;
        public readonly List<int> BatchSizes = new List<int>();
        public readonly List<DecisionQuery> Seen = new List<DecisionQuery>();

        public override string Id => BrainId;
        public override int MaxActions => ActionLimit;
        public override int MaxPromptTokens => TokenLimit;
        public override bool AcceptsAttachments => TakesAttachments;
        public override bool CanMixSpecies => MixesSpecies;
        public override string AnswerInstruction => Instruction;

        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            Batches++;
            Calls += batch.Count;
            BatchSizes.Add(batch.Count);
            var answer = DelayCalls > 0 ? BrainAnswer.Later(batch.Count, new AdvancePending(World, DelayCalls)) : BrainAnswer.Later(batch.Count);
            for (int i = 0; i < batch.Count; i++)
            {
                var q = batch[i];
                Seen.Add(q);
                string fail = FailWhen?.Invoke(q);
                if (fail != null) { answer.Fail(i, fail); continue; }
                if (!Vectors.TryGetValue(q.Species.Id + "|" + q.Observation.Key, out var row))
                    row = Policy?.Invoke(q) ?? Uniform(q.Species.Actions.Count);
                answer.SetRow(i, row);
            }
            answer.ModelCalls = batch.Count;
            answer.Complete();
            return answer;
        }

        /// <summary>A row with all the weight on one action (by name), the rest spread by epsilon.</summary>
        public static float[] Prefer(Species s, string action, float epsilon = 0f)
        {
            int n = s.Actions.Count;
            var row = new float[n];
            int k = s.FindAction(action)?.Index ?? 0;
            for (int i = 0; i < n; i++) row[i] = i == k ? 1f - epsilon * (n - 1) : epsilon;
            return row;
        }

        /// <summary>The token a query's observation has for a sense label, or null.</summary>
        public static string Token(DecisionQuery q, string label) => new ObservationView(q.Species, q.Observation).Token(label);
    }
}
