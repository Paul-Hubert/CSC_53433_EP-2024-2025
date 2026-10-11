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
    /// <remarks>
    /// <para>The reference brain. The server is vLLM on <c>http://localhost:8000</c>, serving the model
    /// <c>autotrust/JEV-9B</c> with its LoRA module under the name <c>jev-decision</c>. The decision head and the
    /// calibration come from two files of the pinned revision, given as TextAssets (Data/Models/JEV-9B); the model's GGUF
    /// files are the bare backbone, without that head.</para>
    /// <para>The text of a query follows the template bare-v1 (<see cref="Text"/>): the species prompt without its answer
    /// instruction, a question line (<see cref="Question"/>) and the options A, B, C… Requests go by option count (the
    /// allowed tokens differ), with at most <c>maxTextsPerRequest</c> texts each. The model identity (repository,
    /// revision, served name) and the mode (template and questions) are part of the answer cache's keys (DEC-33).</para>
    /// </remarks>
    public class JevBrain : HttpBrain
    {
        /// <summary>
        /// The option letters, A to P: action i is shown as the i-th letter, so a species has at most 16 actions (DEC-14).
        /// </summary>
        public const string Letters = "ABCDEFGHIJKLMNOP";
        /// <summary>The question line of a species that has no question of its own.</summary>
        public const string DefaultQuestion = "Which action does this animal take now?";

        /// <summary>
        /// The question asked for one species (the prototype asked predators "Which action does this predator take now?").
        /// </summary>
        [Serializable]
        public class SpeciesQuestion
        {
            /// <summary>The species' id.</summary>
            [Tooltip("The species id.")]
            public string species = "";
            /// <summary>The question line for that species.</summary>
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

        /// <summary>Sets JEV's defaults: the vLLM server on <c>http://localhost:8000</c>, a time-out of 60 s per try.</summary>
        public JevBrain()
        {
            host = "http://localhost:8000";
            timeoutSeconds = 60f;
        }

        /// <summary><c>jev</c>.</summary>
        /// <inheritdoc/>
        public override string Id => "jev";
        /// <summary>16: one option letter per action (DEC-14).</summary>
        /// <inheritdoc/>
        public override int MaxActions => Letters.Length;
        /// <summary>
        /// The model's training length, in tokens (reference 1 024): validation checks the longest prompt against it (V-11);
        /// a longer text met during a run is sent whole and counted in <see cref="TooLong"/>.
        /// </summary>
        /// <inheritdoc/>
        public override int MaxPromptTokens => maxTokens;
        /// <summary>Empty: JEV reads a question and lettered options instead of an answer instruction.</summary>
        /// <inheritdoc/>
        public override string AnswerInstruction => "";
        /// <summary><c>repository@revision:served name</c>, the revision cut to 12 characters (part of cache keys, DEC-33).</summary>
        /// <inheritdoc/>
        public override string ModelIdentity => $"{repo}@{(revision.Length > 12 ? revision.Substring(0, 12) : revision)}:{servedModel}";
        /// <summary>
        /// <c>choice:bare-v1:</c> and a hash of the species' questions: changing a question changes the cache keys (DEC-33).
        /// </summary>
        /// <inheritdoc/>
        public override string Mode => "choice:bare-v1:" + Hashing.Sha256Hex(QuestionsKey(), 8);
        /// <summary>Texts above the training length sent so far in this run (B-06); they are counted, not cut.</summary>
        public int TooLong => tooLong;
        /// <summary>The decision head and calibration, read from the two files on first use (main thread: TextAsset.text).</summary>
        /// <exception cref="ArgumentException">A file is missing, or lacks the choice slots or the choice temperature.</exception>
        public JevHead Head => head ?? (head = LoadHead());

        /// <summary>
        /// Sets the head and calibration files from code (tests, scenes built by script); the head is read again on next use.
        /// </summary>
        /// <param name="headFile"><c>adapter_vllm/decision_head.json</c> of the pinned revision.</param>
        /// <param name="calibrationFile"><c>calibration.json</c> of the pinned revision.</param>
        public void SetModelFiles(TextAsset headFile, TextAsset calibrationFile)
        {
            decisionHead = headFile;
            calibration = calibrationFile;
            head = null;
        }

        /// <summary>Sets the question line of one species, replacing the one it had.</summary>
        /// <param name="speciesId">The species' id.</param>
        /// <param name="question">The question line, such as "Which action does this predator take now?".</param>
        public void SetQuestion(string speciesId, string question)
        {
            questions.RemoveAll(q => q.species == speciesId);
            questions.Add(new SpeciesQuestion { species = speciesId, question = question });
        }

        /// <summary>Gives the question line asked for a species.</summary>
        /// <param name="s">The species.</param>
        /// <returns>The species' own question, or <see cref="DefaultQuestion"/> when it has none.</returns>
        public string Question(Species s)
        {
            foreach (var q in questions) if (q.species == s.Id && !string.IsNullOrEmpty(q.question)) return q.question;
            return DefaultQuestion;
        }

        /// <summary>Also forgets the head, so it is read again from the files, and sets the too-long count to 0.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            head = null;
            tooLong = 0;
        }

        /// <summary>Reads the head and calibration from the two TextAssets (main thread only).</summary>
        JevHead LoadHead() => JevHead.Parse(decisionHead != null ? decisionHead.text : null, calibration != null ? calibration.text : null);

        /// <summary>
        /// The model's input (template bare-v1): the species prompt without the answer instruction, the question, the options.
        /// </summary>
        /// <remarks>
        /// The text, for a species with the actions eat, flee and rest:
        /// <code>
        /// [kind] choice
        /// [state] &lt;the state&gt;
        /// [question] Which action does this animal take now?
        /// [options]
        /// A) eat
        /// B) flee
        /// C) rest
        /// [decision]:
        /// </code>
        /// </remarks>
        /// <param name="state">The species prompt with genes and situation, without the answer instruction.</param>
        /// <param name="question">The question line.</param>
        /// <param name="options">The action names, in the species' action order (at most 16).</param>
        /// <returns>The text the model reads.</returns>
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

        /// <summary>
        /// Builds the bare-v1 text from the trimmed prompt, the species' question and its action names (<see cref="Text"/>),
        /// and counts it in <see cref="TooLong"/> when it is above the training length. Also loads the head (main thread).
        /// </summary>
        /// <inheritdoc/>
        public override string TextFor(DecisionQuery q)
        {
            if (head == null) head = LoadHead();                                        // main thread: TextAsset.text
            string text = Text(q.Prompt(this).Trim(), Question(q.Species), ActionNames(q.Species));
            if (Brain.EstimateTokens(text) > maxTokens) Interlocked.Increment(ref tooLong);
            return text;
        }

        /// <summary>
        /// Groups the requests by option count, since the allowed tokens differ, and sends each group in requests of at most
        /// <c>maxTextsPerRequest</c> texts, all at once.
        /// </summary>
        /// <inheritdoc/>
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
        /// <remarks>Call it on the main thread the first time: it may read the head from its files.</remarks>
        /// <param name="texts">The model's inputs, from <see cref="Text"/>.</param>
        /// <param name="n">The number of options of every text (1 to 16).</param>
        /// <returns>The /v1/completions body: one token among the first n option letters, with their log-probabilities.</returns>
        public JObject Body(IReadOnlyList<string> texts, int n) => Body(Head, texts, n);

        /// <summary>The /v1/completions body for some texts with n options, with a head already loaded.</summary>
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

        /// <summary>
        /// Posts one chunk of texts with n options and fills each text's row from its options' log-probabilities; a text
        /// without them, or a failed call, fails its rows (thread pool).
        /// </summary>
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
                    int index = (int?)choice["index"] ?? k;                             // the text's place in this chunk
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

        /// <summary>The species' questions as one text, hashed into <see cref="Mode"/>.</summary>
        string QuestionsKey()
        {
            var sb = new StringBuilder();
            foreach (var q in questions) sb.Append(q.species).Append('=').Append(q.question).Append('\n');
            return sb.ToString();
        }

        /// <summary><c>/v1/models</c>.</summary>
        /// <inheritdoc/>
        protected override string HealthPath => "/v1/models";

        /// <summary>Also checks that the head and calibration files are set and readable (V-20).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            try { LoadHead(); }
            catch (Exception e) { report.Error("V-20", this, $"JEV needs its decision head and calibration files: {e.Message}."); }
        }
    }
}
