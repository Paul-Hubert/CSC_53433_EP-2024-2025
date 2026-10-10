using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// JEV choice (08 §6): a distilled decision model behind vLLM. One /v1/completions request per batch with the list
    /// of texts, max_tokens 1, the option letters as the only allowed tokens and their log-probabilities; each row is
    /// softmax((log-prob + head bias) / T) with the head and T of the pinned revision. Options always in the species'
    /// action order; at most 16 (DEC-14); inputs above the training length are counted, not cut.
    /// </summary>
    public class JevBrain : HttpBrain
    {
        public const string Letters = "ABCDEFGHIJKLMNOP";
        public const string DefaultQuestion = "Which action does this animal take now?";

        /// <summary>The question asked for one species (the prototype asked predators "Which action does this predator take now?").</summary>
        [Serializable]
        public class SpeciesQuestion
        {
            [Tooltip("The species id.")]
            public string species = "";
            [Tooltip("The question line for that species.")]
            public string question = DefaultQuestion;
        }

        [SerializeField, Tooltip("The LoRA module name given to vLLM (--lora-modules jev-decision=…).")]
        string servedModel = "jev-decision";
        [SerializeField, Tooltip("The model repository: autotrust/JEV-9B (the GGUF files are the bare backbone, without head).")]
        string repo = "autotrust/JEV-9B";
        [SerializeField, Tooltip("The pinned revision the head and calibration files come from; part of cache keys (DEC-33).")]
        string revision = "b63f651ce8ed64481d3f5e73ecdb05f740042f01";
        [SerializeField, Tooltip("adapter_vllm/decision_head.json at the pinned revision (Data/Models/JEV-9B).")]
        TextAsset decisionHead;
        [SerializeField, Tooltip("calibration.json at the pinned revision (Data/Models/JEV-9B).")]
        TextAsset calibration;
        [SerializeField, Min(1), Tooltip("The model's training length in tokens (1 024); longer inputs are counted, not cut.")]
        int maxTokens = 1024;
        [SerializeField, Range(1, 256), Tooltip("Texts per request; a larger batch is split into several requests (reference 64).")]
        int maxTextsPerRequest = 64;
        [SerializeField, Tooltip("The question per species; a species not listed gets \"Which action does this animal take now?\".")]
        List<SpeciesQuestion> questions = new List<SpeciesQuestion>();

        JevHead head;
        int tooLong;

        public JevBrain()
        {
            host = "http://localhost:8000";
            timeoutSeconds = 60f;
        }

        public override string Id => "jev";
        public override int MaxActions => Letters.Length;
        public override int MaxPromptTokens => maxTokens;
        public override string AnswerInstruction => "";
        public override string ModelIdentity => $"{repo}@{(revision.Length > 12 ? revision.Substring(0, 12) : revision)}:{servedModel}";
        public override string Mode => "choice:bare-v1:" + Hashing.Sha256Hex(QuestionsKey(), 8);
        /// <summary>Inputs above the training length so far (B-06).</summary>
        public int TooLong => tooLong;
        public JevHead Head => head ?? (head = LoadHead());

        public void SetModelFiles(TextAsset headFile, TextAsset calibrationFile)
        {
            decisionHead = headFile;
            calibration = calibrationFile;
            head = null;
        }

        public void SetQuestion(string speciesId, string question)
        {
            questions.RemoveAll(q => q.species == speciesId);
            questions.Add(new SpeciesQuestion { species = speciesId, question = question });
        }

        public string Question(Species s)
        {
            foreach (var q in questions) if (q.species == s.Id && !string.IsNullOrEmpty(q.question)) return q.question;
            return DefaultQuestion;
        }

        public override void Initialize()
        {
            base.Initialize();
            head = null;
            tooLong = 0;
        }

        JevHead LoadHead() => JevHead.Parse(decisionHead != null ? decisionHead.text : null, calibration != null ? calibration.text : null);

        /// <summary>The model's input (template bare-v1): the species prompt without the answer instruction, the question, the options.</summary>
        public static string Text(string state, string question, IReadOnlyList<string> options)
        {
            var sb = new StringBuilder();
            sb.Append("[kind] choice\n[state] ").Append(state).Append("\n[question] ").Append(question).Append("\n[options]\n");
            for (int i = 0; i < options.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(Letters[i]).Append(") ").Append(options[i]);
            }
            return sb.Append("\n[decision]:").ToString();
        }

        public override string TextFor(DecisionQuery q)
        {
            if (head == null) head = LoadHead();                                        // main thread: TextAsset.text
            string text = Text(q.Prompt(this).Trim(), Question(q.Species), ActionNames(q.Species));
            if (Brain.EstimateTokens(text) > maxTokens) Interlocked.Increment(ref tooLong);
            return text;
        }

        protected override async Task AnswerAsync(HttpCaller c, IReadOnlyList<BrainRequest> requests, BrainAnswer answer)
        {
            var h = head ?? throw new InvalidOperationException("the JEV head isn't loaded");
            var groups = new SortedDictionary<int, List<BrainRequest>>();                 // option count → requests (allowed ids differ)
            foreach (var r in requests)
            {
                if (!groups.TryGetValue(r.Actions.Count, out var list)) groups[r.Actions.Count] = list = new List<BrainRequest>();
                list.Add(r);
            }
            var sends = new List<Task>();
            foreach (var g in groups)
                for (int start = 0; start < g.Value.Count; start += maxTextsPerRequest)
                    sends.Add(Send(c, h, g.Value.GetRange(start, Math.Min(maxTextsPerRequest, g.Value.Count - start)), g.Key, answer));
            await Task.WhenAll(sends).ConfigureAwait(false);
        }

        /// <summary>The request body for some texts with n options (also used by the golden test).</summary>
        public JObject Body(IReadOnlyList<string> texts, int n) => Body(Head, texts, n);

        JObject Body(JevHead h, IReadOnlyList<string> texts, int n)
        {
            var prompts = new JArray();
            foreach (var t in texts) prompts.Add(t);
            return new JObject
            {
                ["model"] = servedModel,
                ["prompt"] = prompts,
                ["max_tokens"] = 1,
                ["temperature"] = 1.0,
                ["logprobs"] = n,
                ["allowed_token_ids"] = new JArray(h.ChoiceIds(n)),
                ["add_special_tokens"] = false,
                ["return_tokens_as_token_ids"] = true,
            };
        }

        async Task Send(HttpCaller c, JevHead h, List<BrainRequest> chunk, int n, BrainAnswer answer)
        {
            try
            {
                var texts = new List<string>(chunk.Count);
                foreach (var r in chunk) texts.Add(r.Text);
                var response = await c.PostAsync("/v1/completions", Body(h, texts, n)).ConfigureAwait(false);
                var choices = response["choices"] as JArray ?? throw new FormatException("no choices in the answer");
                var filled = new bool[chunk.Count];
                for (int k = 0; k < choices.Count; k++)
                {
                    var choice = choices[k];
                    int index = (int?)choice["index"] ?? k;
                    if (index < 0 || index >= chunk.Count) continue;
                    var top = choice["logprobs"]?["top_logprobs"]?[0] as JObject;
                    if (top == null) continue;
                    var lp = new Dictionary<int, double>();
                    foreach (var p in top.Properties()) lp[TokenId(p.Name)] = (double)p.Value;
                    answer.SetRow(chunk[index].Index, h.Row(lp, n));
                    filled[index] = true;
                }
                for (int k = 0; k < chunk.Count; k++) if (!filled[k]) answer.Fail(chunk[k].Index, "no log-probabilities for this text");
            }
            catch (Exception e)
            {
                foreach (var r in chunk) answer.Fail(r.Index, e.Message);
            }
        }

        /// <summary>"token_id:3721" → 3721 (vLLM with return_tokens_as_token_ids).</summary>
        static int TokenId(string key)
        {
            int colon = key.LastIndexOf(':');
            return int.TryParse(colon >= 0 ? key.Substring(colon + 1) : key, out int id) ? id : -1;
        }

        string QuestionsKey()
        {
            var sb = new StringBuilder();
            foreach (var q in questions) sb.Append(q.species).Append('=').Append(q.question).Append('\n');
            return sb.ToString();
        }

        protected override string HealthPath => "/v1/models";

        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            try { LoadHead(); }
            catch (Exception e) { report.Error("V-20", this, $"JEV needs its decision head and calibration files: {e.Message}."); }
        }
    }
}
