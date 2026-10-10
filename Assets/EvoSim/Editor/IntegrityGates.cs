using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The brain and mutator gates of 32 §2 (B-02…B-10), measured as the prototype measured them (e1_sensitivity):
    /// genomes from texts (founders, control sentences, the neutral genome, contrast pairs, single edits) × the
    /// reference situations (15 §2), through the scene's real prompt and brain. Queries are built from texts only:
    /// nothing is registered in the world. Each writes a report (IntegrityReport).
    /// </summary>
    public static class IntegrityGates
    {
        public const double SignAccuracy = 0.85, MeanDeltaP = 0.25, MiMargin = 2.0, LocalityRatio = 0.5, Rho = 0.3;

        /// <summary>The action a gene's contrast pair should move and the situation tag where it is relevant (prototype RELEVANT_TAG).</summary>
        static readonly Dictionary<string, string> RelevantTag = new Dictionary<string, string>
        {
            { "eat", "food_present" }, { "flee", "predator_present" }, { "hide", "predator_present" }, { "follow", "animal_present" },
            { "rest", "any" }, { "mate", "animal_near" }, { "hunt", "prey_present" },
        };

        /// <summary>A species' situations with their tags.</summary>
        sealed class Situations
        {
            public readonly List<Observation> Observations = new List<Observation>();
            public readonly List<List<string>> Tags = new List<List<string>>();
        }

        static Situations Reference(Species s, int keep = 0)
        {
            var set = s.Senses.Any(x => x is NearestAnimalSense n && n.Targets == AnimalSet.Prey)
                ? ObservationSet.PredatorReference()
                : ObservationSet.FromJsonLines(File.ReadAllText(Path.Combine(Application.dataPath, "EvoSim/Data/Observations/prey_observations_v2.jsonl")));
            var all = new Situations();
            foreach (var row in set.Rows)
            {
                var o = set.ToObservation(row, s);
                if (o == null) continue;
                all.Observations.Add(o);
                all.Tags.Add(new List<string>(row.Tags));
            }
            if (keep <= 0 || keep >= all.Observations.Count) return all;
            // Like the prototype's pick_obs: every tag represented (up to 3 each), then the rest in order, deterministically.
            var chosen = new List<int>();
            foreach (var tag in all.Tags.SelectMany(t => t).Distinct().OrderBy(t => t, StringComparer.Ordinal))
                foreach (int i in Enumerable.Range(0, all.Tags.Count).Where(i => all.Tags[i].Contains(tag)).Take(3))
                    if (!chosen.Contains(i)) chosen.Add(i);
            for (int i = 0; chosen.Count < keep && i < all.Observations.Count; i++) if (!chosen.Contains(i)) chosen.Add(i);
            var picked = new Situations();
            foreach (int i in chosen.Take(keep).OrderBy(i => i)) { picked.Observations.Add(all.Observations[i]); picked.Tags.Add(all.Tags[i]); }
            return picked;
        }

        // ---- Genomes as texts (locus order of the brain-read genes) ----

        static List<TextGene> TextGenes(Species s) => s.Genes.Where(g => g.ReadByBrain).OfType<TextGene>().ToList();

        static string[] Neutral(List<TextGene> genes) => genes.Select(g => g.Neutral).ToArray();

        static List<string[]> Founders(List<TextGene> genes, int n, RandomStream rng)
        {
            var list = new List<string[]>();
            for (int tries = 0; list.Count < n && tries < n * 20; tries++)
            {
                var g = genes.Select(x => x.Founders[rng.Range(0, x.Founders.Count)]).ToArray();
                if (!list.Any(o => o.SequenceEqual(g))) list.Add(g);
            }
            return list;
        }

        static List<string[]> Controls(List<TextGene> genes, IReadOnlyList<string> sentences, int n, RandomStream rng) =>
            Enumerable.Range(0, n).Select(_ => genes.Select(x => sentences[rng.Range(0, sentences.Count)]).ToArray()).ToList();

        static DecisionQuery Query(Species s, List<TextGene> genes, string[] texts, Observation o, TextStyle style)
        {
            var list = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < genes.Count; i++) list.Add(new KeyValuePair<string, string>(genes[i].Label, texts[i]));
            return new DecisionQuery(s, list, o, s.Describe(o, style), style, Hashing.Sha256Hex(s.Id + "\n" + string.Join("\n", texts), 16));
        }

        /// <summary>P[g][o][a] for every genome × situation; failed rows are counted and left uniform.</summary>
        static float[][][] Ask(Brain brain, Species s, List<TextGene> genes, List<string[]> genomes, Situations sit, TextStyle style, ref int calls, ref int failures)
        {
            var queries = new List<DecisionQuery>();
            foreach (var g in genomes) foreach (var o in sit.Observations) queries.Add(Query(s, genes, g, o, style));
            var rows = new float[queries.Count][];
            for (int start = 0; start < queries.Count; start += 256)
            {
                var batch = queries.GetRange(start, Math.Min(256, queries.Count - start));
                var answer = brain.Ask(batch);
                answer.Pending.Wait();
                calls += answer.ModelCalls;
                for (int i = 0; i < batch.Count; i++)
                {
                    if (answer.Failed(i)) { failures++; rows[start + i] = Brain.Uniform(s.Actions.Count); }
                    else rows[start + i] = answer.Row(i);
                }
            }
            var P = new float[genomes.Count][][];
            for (int g = 0; g < genomes.Count; g++)
            {
                P[g] = new float[sit.Observations.Count][];
                for (int o = 0; o < sit.Observations.Count; o++) P[g][o] = rows[g * sit.Observations.Count + o];
            }
            return P;
        }

        // ---- Metrics (prototype promptevo/metrics.py) ----

        static double Entropy(IReadOnlyList<double> p) { double h = 0; foreach (var x in p) if (x > 0) h -= x * Math.Log(x, 2); return h; }
        static double Entropy(float[] p) => Entropy(p.Select(x => (double)x).ToList());

        /// <summary>MI_G = mean over situations of H(mean over genomes p) − mean over genomes H(p).</summary>
        public static double MiGenome(float[][][] P)
        {
            int G = P.Length, O = P[0].Length, A = P[0][0].Length;
            double total = 0;
            for (int o = 0; o < O; o++)
            {
                var mean = new double[A];
                double hMean = 0;
                for (int g = 0; g < G; g++) { for (int a = 0; a < A; a++) mean[a] += P[g][o][a] / (double)G; hMean += Entropy(P[g][o]) / G; }
                total += Entropy(mean) - hMean;
            }
            return total / O;
        }

        /// <summary>MI_O = mean over genomes of H(mean over situations p) − mean over situations H(p).</summary>
        public static double MiObs(float[][][] P)
        {
            int G = P.Length, O = P[0].Length, A = P[0][0].Length;
            double total = 0;
            for (int g = 0; g < G; g++)
            {
                var mean = new double[A];
                double hMean = 0;
                for (int o = 0; o < O; o++) { for (int a = 0; a < A; a++) mean[a] += P[g][o][a] / (double)O; hMean += Entropy(P[g][o]) / O; }
                total += Entropy(mean) - hMean;
            }
            return total / G;
        }

        static double Jsd(float[] p, float[] q)
        {
            var m = new double[p.Length];
            for (int i = 0; i < p.Length; i++) m[i] = 0.5 * (p[i] + q[i]);
            return Entropy(m) - 0.5 * (Entropy(p) + Entropy(q));
        }

        /// <summary>d(g, g') = mean over situations of JSD(p(.|o,g), p(.|o,g')).</summary>
        static double Distance(float[][] a, float[][] b) => Enumerable.Range(0, a.Length).Average(o => Jsd(a[o], b[o]));

        static double TextDistance(string[] a, string[] b)
        {
            string x = string.Join(" | ", a), y = string.Join(" | ", b);
            int[,] d = new int[x.Length + 1, y.Length + 1];                                    // 1 − similarity, edit-distance based
            for (int i = 0; i <= x.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= y.Length; j++) d[0, j] = j;
            for (int i = 1; i <= x.Length; i++)
                for (int j = 1; j <= y.Length; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (x[i - 1] == y[j - 1] ? 0 : 1));
            return (double)d[x.Length, y.Length] / Math.Max(1, Math.Max(x.Length, y.Length));
        }

        static double Spearman(IList<double> x, IList<double> y)
        {
            double[] Rank(IList<double> v)
            {
                var order = Enumerable.Range(0, v.Count).OrderBy(i => v[i]).ToArray();
                var r = new double[v.Count];
                for (int k = 0; k < order.Length;)
                {
                    int end = k;
                    while (end + 1 < order.Length && v[order[end + 1]] == v[order[k]]) end++;
                    for (int t = k; t <= end; t++) r[order[t]] = (k + end) / 2.0 + 1;
                    k = end + 1;
                }
                return r;
            }
            var rx = Rank(x); var ry = Rank(y);
            double mx = rx.Average(), my = ry.Average(), sxy = 0, sxx = 0, syy = 0;
            for (int i = 0; i < rx.Length; i++) { sxy += (rx[i] - mx) * (ry[i] - my); sxx += (rx[i] - mx) * (rx[i] - mx); syy += (ry[i] - my) * (ry[i] - my); }
            return sxx == 0 || syy == 0 ? double.NaN : sxy / Math.Sqrt(sxx * syy);
        }

        // ---- The gates ----

        delegate void Body(World w, Brain brain, IntegrityReport r);

        static IntegrityReport Run(string id, string title, bool blocking, string scene, string brainName, Body body)
        {
            var report = new IntegrityReport(id, title, blocking);
            Batch.WithScene(scene, w =>
            {
                var brain = w.GetComponentsInChildren<Brain>(true).FirstOrDefault(b => b.gameObject.name == brainName || b.Id == brainName);
                if (brain == null) { report.Fail($"no brain named {brainName}"); return ""; }
                w.DefaultBrain = brain;
                w.NoMutation = true;
                w.WaitMode = WaitMode.Freeze;
                w.StartOnPlay = false;
                foreach (var rec in w.GetComponentsInChildren<RunRecorder>(true)) rec.WriteFiles = false;
                if (!w.Initialize()) { report.Fail("validation: " + w.LastReport); return ""; }
                report.Facts["scene"] = scene;
                report.Facts["brain"] = brain.Id;
                report.Facts["model"] = brain.ModelIdentity;
                report.Facts["mode"] = brain.Mode;
                foreach (var s in w.AllSpecies) report.Facts["prompt " + s.Id] = s.Prompt.Id;
                body(w, brain, report);
                return "";
            });
            report.Write();
            return report;
        }

        /// <summary>B-02 G1 directed: every contrast pair (pro / anti, neutral elsewhere) × the relevant situations → sign accuracy and mean ΔP.</summary>
        public static IntegrityReport Directed(string scene, string brainName) => Run("B-02", "G1 directed", false, scene, brainName, (w, brain, r) =>
        {
            int calls = 0, failures = 0, signs = 0, cells = 0;
            var means = new List<double>();
            foreach (var s in w.AllSpecies)
            {
                var genes = TextGenes(s);
                var sit = Reference(s);
                var neutral = Neutral(genes);
                for (int i = 0; i < genes.Count; i++)
                {
                    var gene = genes[i];
                    if (string.IsNullOrEmpty(gene.ContrastPro) || string.IsNullOrEmpty(gene.ContrastAnti)) continue;
                    var action = s.Actions.FirstOrDefault(a => a.Gene == gene);
                    if (action == null || !RelevantTag.TryGetValue(action.Name, out var tag)) continue;
                    var pro = (string[])neutral.Clone(); pro[i] = gene.ContrastPro;
                    var anti = (string[])neutral.Clone(); anti[i] = gene.ContrastAnti;
                    var relevant = Enumerable.Range(0, sit.Observations.Count).Where(o => sit.Tags[o].Contains(tag)).ToList();
                    if (relevant.Count == 0) continue;
                    var subset = new Situations();
                    foreach (int o in relevant) { subset.Observations.Add(sit.Observations[o]); subset.Tags.Add(sit.Tags[o]); }
                    if (action is HideAction) WithCoverInSight(s, subset);                // the reference set has no cover (decision 50)
                    var P = Ask(brain, s, genes, new List<string[]> { pro, anti }, subset, w.TextStyle, ref calls, ref failures);
                    int a = action.Index, positive = 0;
                    double sum = 0;
                    for (int o = 0; o < relevant.Count; o++)
                    {
                        double dp = P[0][o][a] - P[1][o][a];
                        sum += dp;
                        if (dp > 0) positive++;
                    }
                    signs += positive; cells += relevant.Count; means.Add(sum / relevant.Count);
                    var row = r.Row();
                    row["species"] = s.Id; row["locus"] = gene.Label; row["n"] = relevant.Count;
                    row["mean_dp"] = Math.Round(sum / relevant.Count, 3); row["sign_acc"] = Math.Round((double)positive / relevant.Count, 2);
                }
            }
            double acc = cells > 0 ? (double)signs / cells : 0, mean = means.Count > 0 ? means.Average() : 0;
            r.Facts["sign_accuracy"] = Math.Round(acc, 3); r.Facts["mean_dp"] = Math.Round(mean, 3); r.Facts["calls"] = calls; r.Facts["failures"] = failures;
            if (acc < SignAccuracy) r.Fail($"sign accuracy {acc:0.00} < {SignAccuracy}");
            if (mean < MeanDeltaP) r.Fail($"mean ΔP {mean:0.000} < {MeanDeltaP}");
        });

        /// <summary>
        /// The prototype's 48 situations have no cover sense (every row says "none"), so a hide gene could never matter
        /// there: its relevant situations get cover "close" (1-4 m), the Unity prey's own sense.
        /// </summary>
        static void WithCoverInSight(Species s, Situations sit)
        {
            int index = -1;
            for (int i = 0; i < s.Senses.Count; i++) if (s.Senses[i] is NearestCoverSense) index = i;
            if (index < 0) return;
            var tokens = s.Senses[index].Tokens;
            int close = -1;
            for (int t = 0; t < tokens.Count; t++) if (tokens[t] == "close") close = t;
            if (close < 0) return;
            for (int k = 0; k < sit.Observations.Count; k++)
            {
                var o = sit.Observations[k];
                var values = new int[o.Count];
                for (int i = 0; i < values.Length; i++) values[i] = i == index ? close : o[i];
                sit.Observations[k] = new Observation(values);
            }
        }

        /// <summary>B-03 G2 information: 10 founder genomes, 10 random-text genomes, the neutral genome × 24 situations.</summary>
        public static IntegrityReport Information(string scene, string brainName) => Run("B-03", "G2 information", false, scene, brainName, (w, brain, r) =>
        {
            int calls = 0, failures = 0;
            var rng = new RandomStream(1234, "gate");
            bool ok = true;
            foreach (var s in w.AllSpecies)
            {
                var genes = TextGenes(s);
                var sit = Reference(s, 24);
                var founders = Founders(genes, 10, rng);
                var controls = Controls(genes, w.ControlSentences, 10, rng);
                var F = Ask(brain, s, genes, founders, sit, w.TextStyle, ref calls, ref failures);
                var C = Ask(brain, s, genes, controls, sit, w.TextStyle, ref calls, ref failures);
                var N = Ask(brain, s, genes, new List<string[]> { Neutral(genes) }, sit, w.TextStyle, ref calls, ref failures)[0];
                double miF = MiGenome(F), miC = MiGenome(C), miO = MiObs(F);
                double gib = C.Average(c => Distance(c, N)), fn = F.Average(f => Distance(f, N));
                bool pass = miF >= MiMargin * Math.Max(miC, 1e-3);
                ok &= pass;
                var row = r.Row();
                row["species"] = s.Id; row["mi_g_founders"] = Math.Round(miF, 4); row["mi_g_random"] = Math.Round(miC, 4);
                row["mi_o_founders"] = Math.Round(miO, 4); row["random_to_neutral"] = Math.Round(gib, 4); row["founder_to_neutral"] = Math.Round(fn, 4);
                row["gate"] = pass ? "pass" : "fail";
            }
            r.Facts["calls"] = calls; r.Facts["failures"] = failures;
            if (!ok) r.Fail("MI_G(founders) < 2 × MI_G(random text) for some species (a gate known to fail for JEV and gemma, owner decision)");
        });

        /// <summary>B-04 G3 locality: 10 parents × 4 single mutations by the scene's mutator; locality ratio ≤ 0.5, Spearman ρ ≥ 0.3.</summary>
        public static IntegrityReport Locality(string scene, string brainName) => Run("B-04", "G3 locality", false, scene, brainName, (w, brain, r) =>
        {
            int calls = 0, failures = 0, mutatorCalls = 0;
            var s = w.AllSpecies[0];
            var genes = TextGenes(s);
            var sit = Reference(s, 24);
            var rng = new RandomStream(4321, "gate");
            var parents = Founders(genes, 10, rng);
            var client = w.GetComponentInChildren<MutatorClient>(true);
            var service = w.GetComponentInChildren<MutatorService>(true);
            var op = s.GetComponentInChildren<LlmMutation>(true);
            if (client == null || service == null || op == null) { r.Fail("needs the scene's mutator service, client and LLM mutation"); return; }
            var edits = new List<(int parent, string[] child)>();
            var requests = new List<(int parent, int locus, MutatorRequest request)>();
            for (int p = 0; p < parents.Count; p++)
                for (int k = 0; k < 4; k++)
                {
                    int locus = rng.Range(0, genes.Count);
                    string prompt = MutationText.Prompt(op.ContextLine, op.Instructions[rng.Range(0, op.Instructions.Count)], parents[p][locus]);
                    requests.Add((p, locus, new MutatorRequest(prompt, rng.NextUInt() & 0x7FFFFFFF, service.Temperature, service.Model)));
                }
            var reply = client.Send(requests.Select(x => x.request).ToList());
            reply.Pending.Wait();
            mutatorCalls = reply.ModelCalls;
            for (int i = 0; i < requests.Count; i++)
            {
                string answer = reply.Answer(i);
                if (answer == null) continue;
                string cleaned = MutationText.Clean(answer);
                if (MutationText.Reject(cleaned, parents[requests[i].parent][requests[i].locus], op.MaxWords) != null) continue;
                var child = (string[])parents[requests[i].parent].Clone();
                child[requests[i].locus] = cleaned;
                edits.Add((requests[i].parent, child));
            }
            var P = Ask(brain, s, genes, parents, sit, w.TextStyle, ref calls, ref failures);
            var E = Ask(brain, s, genes, edits.Select(e => e.child).ToList(), sit, w.TextStyle, ref calls, ref failures);
            var editD = edits.Select((e, i) => Distance(P[e.parent], E[i])).ToList();
            var editT = edits.Select(e => TextDistance(parents[e.parent], e.child)).ToList();
            var unrelD = new List<double>(); var unrelT = new List<double>();
            for (int i = 0; i < parents.Count; i++)
                for (int j = i + 1; j < parents.Count; j++) { unrelD.Add(Distance(P[i], P[j])); unrelT.Add(TextDistance(parents[i], parents[j])); }
            double Median(List<double> v) { var o = v.OrderBy(x => x).ToList(); return o.Count == 0 ? double.NaN : o.Count % 2 == 1 ? o[o.Count / 2] : 0.5 * (o[o.Count / 2 - 1] + o[o.Count / 2]); }
            double ratio = Median(editD) / Median(unrelD);
            double rho = Spearman(editT.Concat(unrelT).ToList(), editD.Concat(unrelD).ToList());
            r.Facts["edits"] = edits.Count; r.Facts["locality_ratio"] = Math.Round(ratio, 3); r.Facts["spearman_rho"] = Math.Round(rho, 3);
            r.Facts["calls"] = calls; r.Facts["mutator_calls"] = mutatorCalls; r.Facts["failures"] = failures;
            if (!(ratio <= LocalityRatio)) r.Fail($"locality ratio {ratio:0.00} > {LocalityRatio}");
            if (!(rho >= Rho)) r.Fail($"Spearman ρ {rho:0.00} < {Rho}");
        });

        /// <summary>B-05 option order (JEV): the same queries with the options reversed → the share of changed top answers (reported).</summary>
        public static IntegrityReport OptionOrder(string scene, string brainName) => Run("B-05", "Option order", false, scene, brainName, (w, brain, r) =>
        {
            if (!(brain is JevBrain jev)) { r.Fail("a multiple-choice brain (JEV) is needed"); return; }
            var head = jev.Head;                                                          // read the TextAssets on the main thread
            var s = w.AllSpecies[0];
            var genes = TextGenes(s);
            var sit = Reference(s, 20);
            var founders = Founders(genes, 5, new RandomStream(55, "gate"));
            var names = HttpBrain.ActionNames(s);
            int n = names.Length, changed = 0, total = 0;
            var texts = new List<string>(); var reversed = new List<string>();
            foreach (var g in founders)
                foreach (var o in sit.Observations)
                {
                    var q = Query(s, genes, g, o, w.TextStyle);
                    string state = q.Prompt(jev).Trim();
                    texts.Add(JevBrain.Text(state, jev.Question(s), names));
                    reversed.Add(JevBrain.Text(state, jev.Question(s), names.Reverse().ToArray()));
                }
            float[][] Rows(List<string> list)
            {
                var body = jev.Body(list, n);                                             // built on the main thread
                return System.Threading.Tasks.Task.Run(async () =>
            {
                var res = await jev.Caller.PostAsync("/v1/completions", body).ConfigureAwait(false);
                var rows = new float[list.Count][];
                foreach (var choice in (JArray)res["choices"])
                {
                    var lp = new Dictionary<int, double>();
                    foreach (var p in ((JObject)choice["logprobs"]["top_logprobs"][0]).Properties()) lp[int.Parse(p.Name.Substring(p.Name.LastIndexOf(':') + 1))] = (double)p.Value;
                    rows[(int)choice["index"]] = head.Row(lp, n);
                }
                return rows;
            }).GetAwaiter().GetResult();
            }
            var A = new List<float[]>(); var B = new List<float[]>();
            for (int i = 0; i < texts.Count; i += 64) { A.AddRange(Rows(texts.GetRange(i, Math.Min(64, texts.Count - i)))); B.AddRange(Rows(reversed.GetRange(i, Math.Min(64, reversed.Count - i)))); }
            for (int i = 0; i < A.Count; i++)
            {
                int topA = Array.IndexOf(A[i], A[i].Max());
                int topB = n - 1 - Array.IndexOf(B[i], B[i].Max());                         // reversed options map back
                total++;
                if (topA != topB) changed++;
            }
            r.Facts["queries"] = total; r.Facts["calls"] = 2 * total; r.Facts["changed_top_share"] = Math.Round((double)changed / Math.Max(1, total), 3);
            r.Facts["note"] = "requests always use the species' action order; the model card reports 11.5 % on 16 options";
        });

        /// <summary>B-06 prompt length: the longest founder genome with the longest situation, for every species, under the brain's limit.</summary>
        public static IntegrityReport PromptLength(string scene, string brainName) => Run("B-06", "Prompt length", false, scene, brainName, (w, brain, r) =>
        {
            foreach (var s in w.AllSpecies)
            {
                var genes = TextGenes(s);
                var longest = genes.Select(g => g.Founders.OrderByDescending(f => f.Length).First()).ToArray();
                var sit = Reference(s);
                var o = sit.Observations.OrderByDescending(x => s.Describe(x, w.TextStyle).Length).First();
                var q = Query(s, genes, longest, o, w.TextStyle);
                string text = brain is HttpBrain http ? http.TextFor(q) : q.Prompt(brain);
                int tokens = Brain.EstimateTokens(text);
                var row = r.Row();
                row["species"] = s.Id; row["tokens_estimated"] = tokens; row["limit"] = brain.MaxPromptTokens;
                if (brain.MaxPromptTokens > 0 && tokens > brain.MaxPromptTokens) r.Fail($"{s.Id}: about {tokens} tokens > {brain.MaxPromptTokens}");
            }
            if (brain is JevBrain jev) r.Facts["too_long"] = jev.TooLong;
        });

        /// <summary>B-07 points sanity (Ollama points brain): shares of answers summing to 100 and of all-zero answers, by genome kind.</summary>
        public static IntegrityReport PointsSanity(string scene, string brainName, bool cpuOnly) => Run("B-07", "Points sanity", false, scene, brainName, (w, brain, r) =>
        {
            if (!(brain is OllamaPointsBrain points)) { r.Fail("an Ollama points brain is needed"); return; }
            points.CpuOnly = cpuOnly;
            var s = w.AllSpecies[0];
            var genes = TextGenes(s);
            var sit = Reference(s, 3);
            var rng = new RandomStream(77, "gate");
            var kinds = new Dictionary<string, List<string[]>>
            {
                { "founders", Founders(genes, 2, rng) }, { "random text", Controls(genes, w.ControlSentences, 2, rng) }, { "neutral", new List<string[]> { Neutral(genes) } },
            };
            var actions = HttpBrain.ActionNames(s);
            foreach (var kind in kinds.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                int hundred = 0, zero = 0, n = 0, failed = 0;
                foreach (var g in kind.Value)
                    foreach (var o in sit.Observations)
                    {
                        var q = Query(s, genes, g, o, w.TextStyle);
                        try
                        {
                            var res = System.Threading.Tasks.Task.Run(() => points.Caller.PostAsync("/api/chat", points.Body(q.Prompt(points), actions))).GetAwaiter().GetResult();
                            var answer = JObject.Parse((string)res["message"]["content"]);
                            long sum = actions.Sum(a => answer[a] != null && answer[a].Type == JTokenType.Integer ? (long)answer[a] : 0);
                            n++;
                            if (sum == 100) hundred++;
                            if (sum == 0) zero++;
                        }
                        catch (Exception) { failed++; }
                    }
                var row = r.Row();
                row["genomes"] = kind.Key; row["answers"] = n; row["sum_100"] = n > 0 ? Math.Round((double)hundred / n, 2) : 0; row["all_zero"] = zero; row["failed"] = failed;
                if (kind.Key == "founders" && zero > 0) r.Fail("all-zero answers on founder genes");
            }
            r.Facts["cpu_only"] = cpuOnly;
        });

        /// <summary>B-08 names: the predator named "wolf" or "shadow"; the prey gene "Run from any wolf you see." → P(flee) lower for "shadow".</summary>
        public static IntegrityReport Names(string scene, string brainName)
        {
            double Flee(string predatorName, out int calls)
            {
                calls = 0;
                double mean = 0;
                int n = 0, c = 0, f = 0;
                Batch.WithScene(scene, w =>
                {
                    var brain = w.GetComponentsInChildren<Brain>(true).First(b => b.gameObject.name == brainName || b.Id == brainName);
                    w.DefaultBrain = brain;
                    w.NoMutation = true;
                    foreach (var rec in w.GetComponentsInChildren<RunRecorder>(true)) rec.WriteFiles = false;
                    FieldPath.Set(w, "Predator/Species/displayName", predatorName, out _);
                    if (!w.Initialize()) return "";
                    var s = w.FindSpecies("prey");
                    var genes = TextGenes(s);
                    var texts = Neutral(genes);
                    int flee = genes.FindIndex(g => g.Label == "flee");
                    texts[flee] = "Run from any wolf you see.";
                    var sit = Reference(s);
                    var near = new Situations();
                    for (int i = 0; i < sit.Observations.Count; i++)
                        if (sit.Tags[i].Contains("predator_present")) { near.Observations.Add(sit.Observations[i]); near.Tags.Add(sit.Tags[i]); }
                    var P = Ask(brain, s, genes, new List<string[]> { texts }, near, w.TextStyle, ref c, ref f);
                    int a = s.FindAction("flee").Index;
                    mean = P[0].Average(row => row[a]);
                    n = near.Observations.Count;
                    return "";
                });
                calls = c;
                return mean;
            }
            var report = new IntegrityReport("B-08", "Names", false);
            double wolf = Flee("wolf", out int c1), shadow = Flee("shadow", out int c2);
            report.Facts["scene"] = scene; report.Facts["brain"] = brainName;
            report.Facts["p_flee_wolf"] = Math.Round(wolf, 3); report.Facts["p_flee_shadow"] = Math.Round(shadow, 3); report.Facts["calls"] = c1 + c2;
            if (!(shadow < wolf)) report.Fail("P(flee) isn't lower when the threat is called \"shadow\": the brain ignores the name");
            report.Write();
            return report;
        }

        /// <summary>B-09 repeatability: the same 50 queries twice (no cache) → identical rows; differences above 1e-3 reported.</summary>
        public static IntegrityReport Repeatability(string scene, string brainName) => Run("B-09", "Repeatability", false, scene, brainName, (w, brain, r) =>
        {
            int calls = 0, failures = 0, differing = 0;
            double worst = 0;
            var s = w.AllSpecies[0];
            var genes = TextGenes(s);
            var sit = Reference(s, 10);
            var genomes = Founders(genes, 5, new RandomStream(99, "gate"));
            var A = Ask(brain, s, genes, genomes, sit, w.TextStyle, ref calls, ref failures);
            var B = Ask(brain, s, genes, genomes, sit, w.TextStyle, ref calls, ref failures);
            for (int g = 0; g < A.Length; g++)
                for (int o = 0; o < A[g].Length; o++)
                {
                    double d = A[g][o].Zip(B[g][o], (x, y) => Math.Abs(x - y)).Max();
                    worst = Math.Max(worst, d);
                    if (d > 1e-3) differing++;
                }
            r.Facts["queries"] = genomes.Count * sit.Observations.Count; r.Facts["calls"] = calls; r.Facts["failures"] = failures;
            r.Facts["rows_differing_above_1e-3"] = differing; r.Facts["largest_difference"] = Math.Round(worst, 6);
            if (!(brain is JevBrain) && differing > 0) r.Fail("answers differ between two identical runs (temperature 0 and a seed should repeat)");
        });

        /// <summary>The judge prompt of 32 §2 (the prototype's judge_sense_v1), used by B-10.</summary>
        public const string JudgePrompt =
            "Animals in a simulation act on short English rules called genes. Each gene sits in a slot. The slot \"{slot}\" is about {topic}.\n\n" +
            "The gene in the slot \"{slot}\" is: \"{text}\"\n\n" +
            "Does this gene still give the animal a usable rule about {topic}? A usable rule is understandable and could guide what the animal does, " +
            "even if it is odd or unwise. Nonsense, or a sentence about something else, is not a usable rule.\n\nAnswer yes or no.";

        /// <summary>The slot topics of the prototype (gene_timeline.TOPIC); "hide" is the Unity prey's addition.</summary>
        static readonly Dictionary<string, string> Topics = new Dictionary<string, string>
        {
            { "eat", "when and how to eat" }, { "flee", "when to run away" }, { "hide", "when to hide in cover" },
            { "follow", "when to follow other animals" }, { "rest", "when to rest" }, { "mate", "when to look for a partner" },
            { "predator.hunt", "when and how to hunt prey" }, { "predator.follow", "when to follow other predators" },
        };

        static string Topic(string species, string slot) =>
            Topics.TryGetValue(species + "." + slot, out var t) || Topics.TryGetValue(slot, out t) ? t : "when to " + slot;

        /// <summary>
        /// B-10 mutator: every founder sentence through the deck once (guards pass ≥ 70 %), then 10 mutations in a row
        /// without selection; a judge model says whether each gene is still a usable rule (≥ 80 % after one mutation).
        /// </summary>
        public static IntegrityReport MutatorQuality(string scene, string judgeModel, int chain = 10) => Run("B-10", "Mutator", false, scene, "Random", (w, brain, r) =>
        {
            var service = w.GetComponentInChildren<MutatorService>(true);
            var client = w.GetComponentInChildren<MutatorClient>(true);
            var judge = w.GetComponentInChildren<OllamaPointsBrain>(true);
            if (service == null || client == null) { r.Fail("needs the scene's mutator service and client"); return; }
            var rng = new RandomStream(1010, "gate");
            int firstOk = 0, firstTotal = 0, mutatorCalls = 0, judgeCalls = 0;
            var current = new List<(string species, TextGene gene, LlmMutation op, string text)>();
            foreach (var s in w.AllSpecies)
            {
                var op = s.GetComponentInChildren<LlmMutation>(true);
                if (op == null) continue;
                foreach (var g in TextGenes(s)) foreach (var f in g.Founders) current.Add((s.Id, g, op, f));
            }
            var usableAfter = new Dictionary<int, double>();
            for (int step = 1; step <= chain; step++)
            {
                var requests = current.Select(c => new MutatorRequest(MutationText.Prompt(c.op.ContextLine, c.op.Instructions[rng.Range(0, c.op.Instructions.Count)], c.text),
                                                                      rng.NextUInt() & 0x7FFFFFFF, service.Temperature, service.Model)).ToList();
                var reply = client.Send(requests);
                reply.Pending.Wait();
                mutatorCalls += requests.Count;
                for (int i = 0; i < current.Count; i++)
                {
                    string answer = reply.Answer(i);
                    string cleaned = answer != null ? MutationText.Clean(answer) : null;
                    bool ok = cleaned != null && MutationText.Reject(cleaned, current[i].text, current[i].op.MaxWords) == null;
                    if (step == 1) { firstTotal++; if (ok) firstOk++; }
                    if (ok) current[i] = (current[i].species, current[i].gene, current[i].op, cleaned);
                }
                if ((step == 1 || step == chain) && judge != null && !string.IsNullOrEmpty(judgeModel))
                {
                    int usable = 0;
                    foreach (var c in current)
                    {
                        string topic = Topic(c.species, c.gene.Label);
                        string prompt = JudgePrompt.Replace("{slot}", c.gene.Label).Replace("{topic}", topic).Replace("{text}", c.text);
                        var body = new JObject
                        {
                            ["model"] = judgeModel, ["stream"] = false, ["think"] = false,
                            ["messages"] = new JArray(new JObject { ["role"] = "user", ["content"] = prompt }),
                            ["format"] = JObject.Parse("{\"type\": \"object\", \"properties\": {\"answer\": {\"type\": \"string\", \"enum\": [\"yes\", \"no\"]}}, \"required\": [\"answer\"]}"),
                            ["options"] = new JObject { ["temperature"] = 0, ["seed"] = 0, ["num_gpu"] = 0, ["num_ctx"] = 1024 },
                        };
                        try
                        {
                            var res = System.Threading.Tasks.Task.Run(() => judge.Caller.PostAsync("/api/chat", body)).GetAwaiter().GetResult();
                            judgeCalls++;
                            if ((string)JObject.Parse((string)res["message"]["content"] ?? "{}")["answer"] == "yes") usable++;
                        }
                        catch (Exception) { }
                    }
                    usableAfter[step] = (double)usable / Math.Max(1, current.Count);
                }
            }
            double first = (double)firstOk / Math.Max(1, firstTotal);
            r.Facts["sentences"] = current.Count; r.Facts["first_answers_passing_guards"] = Math.Round(first, 3);
            r.Facts["mutator"] = client is OllamaMutatorClient ollama
                ? service.Model + "@" + (System.Threading.Tasks.Task.Run(() => OllamaTags.DigestAsync(ollama.Caller, service.Model)).GetAwaiter().GetResult() ?? "unknown")
                : client.ModelIdentity; r.Facts["mutator_calls"] = mutatorCalls; r.Facts["judge"] = judgeModel; r.Facts["judge_calls"] = judgeCalls;
            foreach (var kv in usableAfter) r.Facts[$"usable_after_{kv.Key}"] = Math.Round(kv.Value, 3);
            foreach (var c in current.Take(12)) { var row = r.Row(); row["species"] = c.species; row["slot"] = c.gene.Label; row["after_chain"] = c.text; }
            if (first < 0.7) r.Fail($"guards pass {first:P0} of first answers < 70 %");
            if (usableAfter.TryGetValue(1, out var u1) && u1 < 0.8) r.Fail($"usable after one mutation {u1:P0} < 80 %");
        });
    }
}
