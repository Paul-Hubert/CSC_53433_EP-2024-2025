using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Builds each deciding animal's query (DEC-10), answers it from the memo (DEC-30) or the answer cache (DEC-33),
    /// sends the rest as one batch per brain — per species for brains that can't mix species — with identical keys
    /// sent once (DEC-13, DEC-15, DEC-32). Nothing in the world changes here (CORE-06).
    /// </summary>
    public class AskBrainsPhase : TickPhase
    {
        readonly Dictionary<string, Decision> firstWithKey = new Dictionary<string, Decision>();
        readonly List<Brain> brains = new List<Brain>();
        readonly List<List<Decision>> batches = new List<List<Decision>>();
        AnswerCache cache;

        public override void Initialize() => cache = World.Service<AnswerCache>();

        public override void Run(TickContext t)
        {
            var state = World.Decisions;
            var sense = World.Phase<SensePhase>()?.Context ?? new SenseContext(World);
            firstWithKey.Clear();
            brains.Clear();
            foreach (var b in batches) b.Clear();

            foreach (var d in state.Due)
            {
                var a = d.Animal;
                var species = a.Species;
                d.Brain = species.Brain;
                species.Counters.Decisions++;
                var genome = QueryGenome(a, t);
                var attachments = SensePhase.Attachments(a, sense, out string attachmentKey, out bool memoable);
                d.Query = new DecisionQuery(species, DecisionQuery.BrainGenes(species, genome), d.Observation, d.Situation, World.TextStyle,
                                            genome != null ? genome.BrainKey : species.Id, attachments);
                d.Query.AnimalId = a.Id;
                if (d.Brain != null && !d.Brain.Memoizable) memoable = false;              // a brain with memory: no memo, no sharing, no cache
                d.MemoKey = memoable ? DecisionMemo.Key(species, d.Brain, d.Query.GenomeKey, d.Observation) + (attachmentKey.Length > 0 ? "|" + attachmentKey : "") : null;

                if (state.Memo.TryGet(d.MemoKey, out var row)) { d.Row = row; d.Source = "memo"; species.Counters.MemoHits++; continue; }
                species.Counters.BrainQueries++;
                if (d.MemoKey != null && firstWithKey.TryGetValue(d.MemoKey, out var same)) { d.SameAs = same; d.Source = "same"; continue; }
                if (d.MemoKey != null) firstWithKey[d.MemoKey] = d;
                if (d.Brain != null && d.Brain.UsesCache && cache != null && memoable)
                {
                    d.CacheKey = AnswerCache.KeyFor(d.Brain, d.Query, attachmentKey);
                    if (cache.TryGetRow(d.Brain.Id, d.CacheKey, out row)) { d.Row = row; d.Source = "cache"; state.CacheHits++; continue; }
                }
                if (d.Brain == null) { d.Failed = true; d.Error = "no brain"; continue; }
                BatchFor(d.Brain, species).Add(d);
            }

            for (int i = 0; i < brains.Count; i++)
            {
                var batch = batches[i];
                if (batch.Count == 0) continue;
                var queries = new DecisionQuery[batch.Count];
                for (int k = 0; k < batch.Count; k++) queries[k] = batch[k].Query;
                var answer = brains[i].Ask(queries);
                for (int k = 0; k < batch.Count; k++) { batch[k].Answer = answer; batch[k].AnswerIndex = k; batch[k].Source = "brain"; }
                state.AddAnswer(answer);
            }
        }

        /// <summary>The genome the brain reads: the animal's own, or another living animal's under the shuffled control (CTRL-01/02).</summary>
        Genome QueryGenome(Animal a, TickContext t)
        {
            if (!World.ShuffledGenes) return a.Genome;
            var living = a.Species.Animals;
            if (living.Count <= 1) return a.Genome;
            var rng = t.Stream(a.Species, "sampling");
            int i = rng.Range(0, living.Count - 1);
            var other = living[i] == a ? living[living.Count - 1] : living[i];          // another animal, uniformly
            return other.Genome;
        }

        List<Decision> BatchFor(Brain b, Species s)
        {
            for (int i = 0; i < brains.Count; i++)
            {
                if (brains[i] != b) continue;
                var batch = batches[i];
                if (b.CanMixSpecies || batch.Count == 0 || batch[0].Animal.Species == s) return batch;
            }
            brains.Add(b);
            if (batches.Count < brains.Count) batches.Add(new List<Decision>());
            return batches[brains.Count - 1];
        }

        public override void Validate(ValidationReport report)
        {
            int me = IndexOf(this), sense = IndexOf(World.Phase<SensePhase>());
            if (sense < 0 || sense > me) report.Error("V-08", this, "Ask brains must come after the Sense phase.", PlaceAfter(World.Phase<SensePhase>()));
            foreach (var s in World.AllSpecies)
            {
                var brain = s.Brain;
                if (brain == null) { report.Error("V-10", s, $"Species '{s.DisplayName}' has no brain and the World has no default brain.", PickBrain(s)); continue; }
                if (!IsService(brain))
                {
                    report.Error("V-10", s, $"The brain '{brain.name}' of '{s.DisplayName}' is disabled, inactive, under a species or in another World: " +
                                            "it would be used without being set up or checked (ARCH-06, DEC-13).", PickBrain(s));
                    continue;
                }
                if (s.Actions.Count > brain.MaxActions)
                    report.Error("V-11", s, $"'{s.DisplayName}' has {s.Actions.Count} actions; {brain.Id} takes at most {brain.MaxActions} (DEC-14).");
                if (brain.MaxPromptTokens > 0 && s.Prompt != null)
                {
                    int tokens = Brain.EstimateTokens(LongestPrompt(s, brain));
                    if (tokens > brain.MaxPromptTokens)
                        report.Error("V-11", s, $"The longest prompt of '{s.DisplayName}' is about {tokens} tokens; {brain.Id} takes {brain.MaxPromptTokens} (DEC-14).");
                }
                foreach (var sense2 in s.Senses)
                    if (sense2.HasAttachments && !brain.AcceptsAttachments)
                        report.Error("V-51", sense2, $"Sense '{sense2.Label}' gives attachments and {brain.Id} can't read them (SENSE-41).");
            }
        }

        bool IsService(Brain b)
        {
            foreach (var svc in World.Services) if (svc == b) return true;
            return false;
        }

        /// <summary>V-10's fix: the World's first enabled brain for this species (or as the default), if there is one.</summary>
        ValidationFix PickBrain(Species s)
        {
            var first = World.Service<Brain>();
            if (first == null) return null;
            var world = World;
            return s.OwnBrain != null ? new ValidationFix($"Use {first.name}", () => s.SetBrain(first))
                                      : new ValidationFix($"Use {first.name} as the default", () => world.DefaultBrain = first);
        }

        /// <summary>The longest founder genome with the longest situation (B-06, PROMPT-06).</summary>
        static string LongestPrompt(Species s, Brain brain)
        {
            var genes = new List<KeyValuePair<string, string>>();
            foreach (var g in s.Genes)
            {
                if (!g.ReadByBrain) continue;
                string longest = "";
                foreach (var f in g.FounderPool) if (f.Value.ToString().Length > longest.Length) longest = f.Value.ToString();
                genes.Add(new KeyValuePair<string, string>(g.Label, longest));
            }
            var sb = new System.Text.StringBuilder();
            foreach (var sense in s.Senses)
            {
                string longest = "";
                for (int t = 0; t < sense.Tokens.Count; t++)
                    foreach (var style in new[] { TextStyle.V1, TextStyle.V2 })
                    {
                        string text = sense.Write(t, style);
                        if (text.Length > longest.Length) longest = text;
                    }
                sb.Append(longest).Append(' ');
            }
            var q = new DecisionQuery(s, genes, new Observation(new int[s.Senses.Count]), sb.ToString().TrimEnd(), TextStyle.V1, "");
            return q.Prompt(brain);
        }

        int IndexOf(TickPhase p)
        {
            var list = World.Phases;
            for (int i = 0; i < list.Count; i++) if (list[i] == p) return i;
            return -1;
        }
    }
}
