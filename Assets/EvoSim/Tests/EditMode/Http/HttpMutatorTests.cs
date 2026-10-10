using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>Mutator clients against fake transports (10 §2, B-11 offline): what the model receives, caching, failures.</summary>
    public class HttpMutatorTests : WorldFixture
    {
        const string Context = "The sentence below is a rule that a wild animal follows.";

        World Breeding<T>(FakeHttpTransport fake, string cache = null, string name = "mutator") where T : HttpMutatorClient
        {
            var b = New(name: name).Flat(20, 20).Food(0f, 0f).Service<EggSystem>("Eggs")
                .Service<MutatorService>("Mutator", m => m.Configure("qwen3.5:0.8b", 1.2f, cached: cache != null))
                .Service<T>("client", c => { c.Transport = fake; c.ConfigureClient("http://mutator.test", 5f, 3, 0f, 2); })
                .Phase<BreedPhase>().Phase<HatchPhase>()
                .Species("prey", s => s.PreyBody().Action<MateAction>("mate", founders: new[] { "Mate often." }).LifeRules(100, 0).LlmMutation(1f));
            if (cache != null) b.Service<AnswerCache>("Answer cache", c => c.SetFolder(cache));
            var w = b.Build();
            var sp = w.AllSpecies[0];
            foreach (var x in new[] { 10f, 10.5f }) { var a = Place.Animal(sp, Place.At(x, 10)); a.Age = 200; a.SetStat("energy", 100); a.Choose("mate"); }
            return w;
        }

        static FakeHttpTransport Ollama(string answer = "Mate very often.") => new FakeHttpTransport
        {
            Respond = (method, path, body) => path == "/api/tags"
                ? new JObject { ["models"] = new JArray(new JObject { ["name"] = "qwen3.5:0.8b", ["digest"] = "feedfacecafe0001" }) }
                : FakeHttpTransport.Chat(answer),
        };

        [Test, Description("T-MUT-06 and B-11 offline (MUT-11, CORE-07): the Ollama mutator receives one user message with exactly the context line, one deck instruction, the quoted sentence and the reply line; seed, temperature 1.2, CPU, num_ctx 1 024, thinking off; nothing else")]
        public void OllamaMutatorRequest()
        {
            var fake = Ollama();
            var w = Breeding<OllamaMutatorClient>(fake);
            w.Advance(1);
            var chats = fake.Requests.Where(r => r.Path == "/api/chat").ToList();
            Assert.Greater(chats.Count, 0);
            Assert.AreEqual(w.Mutations.ModelCalls, chats.Count);
            var deck = MutationText.Instructions(Lab.Deck.text);
            foreach (var (method, _, body, _) in chats)
            {
                Assert.AreEqual("POST", method);
                CollectionAssert.AreEqual(new[] { "messages", "model", "options", "stream", "think" }, body.Properties().Select(p => p.Name).OrderBy(n => n).ToArray(), "no other field");
                Assert.AreEqual("qwen3.5:0.8b", (string)body["model"]);
                Assert.AreEqual(1, ((JArray)body["messages"]).Count);
                Assert.AreEqual("user", (string)body["messages"][0]["role"]);
                string prompt = FakeHttpTransport.Prompt(body);
                var m = Regex.Match(prompt, "^" + Regex.Escape(Context) + "\n(.+)\n\n\"Mate often\\.\"\n\nReply with the new sentence only\\.$");
                Assert.IsTrue(m.Success, prompt);
                CollectionAssert.Contains(deck, m.Groups[1].Value, "one instruction from the deck");
                Assert.AreEqual(MutationText.Prompt(Context, m.Groups[1].Value, "Mate often."), prompt);
                Assert.AreEqual(0, (int)body["options"]["num_gpu"]);
                Assert.AreEqual(1024, (int)body["options"]["num_ctx"]);
                Assert.AreEqual(JTokenType.Integer, body["options"]["seed"].Type);
                StringAssert.Contains("\"temperature\":1.2}", body["options"].ToString(Newtonsoft.Json.Formatting.None));
                Assert.IsFalse((bool)body["think"]);
                Assert.IsFalse((bool)body["stream"]);
            }
            var babies = w.AllSpecies[0].Animals.Where(a => a.Generation > 0).ToList();
            Assert.Greater(babies.Count, 0);
            Assert.IsTrue(babies.All(b => b.Genome[0].Text == "Mate very often."), "the cleaned answer became the gene");
        }

        [Test, Description("T-MUT-08 over HTTP (MUT-14): the same run twice with the answer cache → the second makes no mutator call and gives the same genomes")]
        public void MutatorCacheOverHttp()
        {
            string cache = Path.Combine(Application.temporaryCachePath, "evosim-http-mutator-cache");
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
            var first = Ollama();
            var w1 = Breeding<OllamaMutatorClient>(first, cache, "m1");
            w1.Advance(1);
            int calls = first.Requests.Count(r => r.Path == "/api/chat");
            Assert.Greater(calls, 0);
            var again = Ollama("Something else entirely.");
            var w2 = Breeding<OllamaMutatorClient>(again, cache, "m2");
            w2.Advance(1);
            Assert.AreEqual(0, again.Requests.Count(r => r.Path == "/api/chat"), "answered from the cache");
            Assert.AreEqual(w1.Events.Hash, w2.Events.Hash);
        }

        [Test, Description("MUT-04, DEC-42: a mutator failing every call (non-strict, reference) → the genes don't mutate, failures are counted, the run goes on")]
        public void FailingMutator()
        {
            var fake = new FakeHttpTransport { FailWith = (path, body) => path == "/api/chat" ? 500 : 0 };
            var w = Breeding<OllamaMutatorClient>(fake);
            w.Advance(2);
            Assert.AreEqual(2, w.Tick);
            Assert.Greater(w.Mutations.Failures, 0);
            Assert.IsTrue(w.AllSpecies[0].Animals.Where(a => a.Generation > 0).All(b => b.Genome[0].Text == "Mate often."));
            var stats = w.GetComponentInChildren<OllamaMutatorClient>().Stats;
            int chats = fake.Requests.Count(r => r.Path == "/api/chat");
            Assert.AreEqual(chats, stats.Tries);
            Assert.AreEqual(chats, 3 * stats.Failures, "each request tried 3 times");
            Assert.AreEqual(0, stats.Latencies.Count);
        }

        [Test, Description("10 §2: the OpenAI-compatible mutator (vLLM) sends the prompt, seed, temperature and thinking off; reads choices[0].message.content")]
        public void OpenAIMutatorRequest()
        {
            var fake = new FakeHttpTransport
            {
                Respond = (method, path, body) => new JObject
                {
                    ["choices"] = new JArray(new JObject { ["message"] = new JObject { ["role"] = "assistant", ["content"] = "\"Mate rarely.\"" } }),
                },
            };
            var w = Breeding<OpenAIMutatorClient>(fake);
            w.Advance(1);
            var chats = fake.Requests.Where(r => r.Path == "/v1/chat/completions").ToList();
            Assert.Greater(chats.Count, 0);
            var body = chats[0].Body;
            CollectionAssert.AreEqual(new[] { "chat_template_kwargs", "max_tokens", "messages", "model", "seed", "temperature" },
                                      body.Properties().Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.IsFalse((bool)body["chat_template_kwargs"]["enable_thinking"]);
            Assert.AreEqual(64, (int)body["max_tokens"]);
            Assert.IsTrue(w.AllSpecies[0].Animals.Where(a => a.Generation > 0).All(b => b.Genome[0].Text == "Mate rarely."), "quotes cleaned (MUT-12)");
        }
    }
}
