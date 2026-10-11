using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test brain (30 §2): answers from a policy written in the test, or from given rows per (species, observation);
    /// counts calls and batches; can answer N Advance calls late, fail, or refuse to mix species.
    /// Test code only: the system ships no rule-based brain (owner decision).
    /// </summary>
    /// <remarks>
    /// <para>For each query it answers, in this order: a failure when <see cref="FailWhen"/> gives a message; the row in
    /// <see cref="Vectors"/> for the query's species and observation key; the row <see cref="Policy"/> returns; a uniform row.</para>
    /// <para>Add it with <see cref="WorldBuilder.DefaultBrain{T}"/> and keep a reference from the configure callback, or find it
    /// with <c>w.GetComponentInChildren&lt;ScriptedBrain&gt;()</c>. Every query it saw is in <see cref="Seen"/>.</para>
    /// </remarks>
    /// <example>
    /// Every animal chooses to mate, whatever it senses:
    /// <code>
    /// var w = New(name: "baby age").Ecology(40f, 0f, 0f, regrow: 0f, prey: s =&gt; s.LifeRules(40, 0)).ReferencePhases()
    ///     .DefaultBrain&lt;ScriptedBrain&gt;(b =&gt; b.Policy = q =&gt; ScriptedBrain.Prefer(q.Species, "mate")).Build();
    /// </code>
    /// A policy that reads the situation (the scripted prey of R-01): flee a close threat, eat food in sight, mate with a
    /// near partner, else rest.
    /// <code>
    /// static float[] PreyPolicy(DecisionQuery q)
    /// {
    ///     if (q.Species.Id != "prey") return Brain.Uniform(q.Species.Actions.Count);
    ///     string threat = ScriptedBrain.Token(q, "Predator"), food = ScriptedBrain.Token(q, "Food");
    ///     string kin = ScriptedBrain.Token(q, "Animal");
    ///     string action = threat == "adjacent" || threat == "close" ? "flee"
    ///                   : food != null &amp;&amp; food != "none" ? "eat"
    ///                   : kin != null &amp;&amp; kin.EndsWith(", ready") ? "mate" : "rest";
    ///     return ScriptedBrain.Prefer(q.Species, action, 0.02f);
    /// }
    ///
    /// var w = New(seed).Lab1(prey: 30, predators: 5, randomBrain: false)
    ///     .DefaultBrain&lt;ScriptedBrain&gt;(x =&gt; x.Policy = PreyPolicy).Build();
    /// </code>
    /// </example>
    public class ScriptedBrain : Brain
    {
        /// <summary>The brain's id (part of memo and cache keys, DEC-30); give two scripted brains in one world different ids.</summary>
        public string BrainId = "scripted";
        /// <summary>The row for a query, one probability per action of its species; returning null gives a uniform row.</summary>
        public Func<DecisionQuery, float[]> Policy;
        /// <summary>Fixed rows by <c>"species id|observation key"</c>, used before <see cref="Policy"/>.</summary>
        public readonly Dictionary<string, float[]> Vectors = new Dictionary<string, float[]>();
        /// <summary>A failure message for a query, or null to answer it (DEC-34, DEC-40).</summary>
        public Func<DecisionQuery, string> FailWhen;
        /// <summary>Answers arrive this many <see cref="World.Advance"/> calls late (T-SPACE-07); 0 = at once.</summary>
        public int DelayCalls;
        /// <summary>False: the brain can't mix species, so the World sends it one batch per species (DEC-13).</summary>
        public bool MixesSpecies = true;
        /// <summary>The most actions a species may have with this brain (DEC-14); no limit by default.</summary>
        public int ActionLimit = int.MaxValue;
        /// <summary>The longest prompt it takes, in estimated tokens (DEC-14); 0 = no limit.</summary>
        public int TokenLimit;
        /// <summary>True: queries may carry attachments, such as a camera sense's image (SENSE-41).</summary>
        public bool TakesAttachments;
        /// <summary>The answer instruction that ends the prompt (PROMPT-07).</summary>
        public string Instruction = "Answer with the probabilities.";

        /// <summary>The queries received so far, failed ones included (<see cref="Calls"/>), and the batches (<see cref="Batches"/>).</summary>
        public int Calls, Batches;
        /// <summary>The size of each batch received, in order.</summary>
        public readonly List<int> BatchSizes = new List<int>();
        /// <summary>Every query received, in order.</summary>
        public readonly List<DecisionQuery> Seen = new List<DecisionQuery>();

        /// <inheritdoc/>
        public override string Id => BrainId;
        /// <inheritdoc/>
        public override int MaxActions => ActionLimit;
        /// <inheritdoc/>
        public override int MaxPromptTokens => TokenLimit;
        /// <inheritdoc/>
        public override bool AcceptsAttachments => TakesAttachments;
        /// <inheritdoc/>
        public override bool CanMixSpecies => MixesSpecies;
        /// <inheritdoc/>
        public override string AnswerInstruction => Instruction;

        /// <summary>
        /// Records the batch and answers each query from <see cref="FailWhen"/>, <see cref="Vectors"/>, <see cref="Policy"/>
        /// or a uniform row, at once or <see cref="DelayCalls"/> Advance calls later; each query counts as one model call.
        /// </summary>
        /// <inheritdoc/>
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

        /// <summary>A row that puts the weight on one action, by name, and <paramref name="epsilon"/> on each other action.</summary>
        /// <param name="s">The species, whose action order the row follows.</param>
        /// <param name="action">The preferred action's name; an unknown name prefers the first action.</param>
        /// <param name="epsilon">The probability of each other action; 0 = the preferred action always.</param>
        /// <returns>A row of the species' length that sums to 1.</returns>
        public static float[] Prefer(Species s, string action, float epsilon = 0f)
        {
            int n = s.Actions.Count;
            var row = new float[n];
            int k = s.FindAction(action)?.Index ?? 0;
            for (int i = 0; i < n; i++) row[i] = i == k ? 1f - epsilon * (n - 1) : epsilon;
            return row;
        }

        /// <summary>The token a query's observation has for one sense, to write a policy that reads the situation.</summary>
        /// <param name="q">The query.</param>
        /// <param name="label">The sense's label, as in the situation text ("Food", "Predator", "Energy"…).</param>
        /// <returns>The token ("none", "close", "low"…), or null when the species has no sense of that label.</returns>
        public static string Token(DecisionQuery q, string label) => new ObservationView(q.Species, q.Observation).Token(label);
    }
}
