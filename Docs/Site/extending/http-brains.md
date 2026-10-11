---
uid: extend-http-brains
---

# An HTTP brain or mutator server

Derive from <xref:EvoSim.HttpBrain> to decide with a model behind a web API: a chat server, a classifier, a service of
your own. The base class sends the requests off the main thread, with time-outs, retries and a cap on parallel requests;
you only build each request and read each answer. A brain that needs no server is a plain <xref:EvoSim.Brain>
([A brain](brain.md)). To mutate genes with another server, derive from <xref:EvoSim.HttpMutatorClient>; for a mutator
without a server, from <xref:EvoSim.MutatorClient>.

## The reference servers

| Component | Server | Default **Host** | Calls | Reference settings |
|---|---|---|---|---|
| <xref:EvoSim.JevBrain> | vLLM serving JEV-9B with its LoRA module `jev-decision` | `http://localhost:8000` | `POST /v1/completions`, `GET /v1/models` | time-out 60 s, 4 in flight, 64 texts per request |
| <xref:EvoSim.OllamaPointsBrain> | Ollama | `http://localhost:11434` | `POST /api/chat`, `GET /api/tags` | `gemma4:12b`, context 4 096 tokens, seed 0, 2 in flight |
| <xref:EvoSim.OllamaMutatorClient> | Ollama, the model on the CPU | `http://localhost:11434` | `POST /api/chat`, `GET /api/tags` | the mutator service's model (`gemma4:26b`, temperature 1.2), context 1 024 tokens, 2 in flight |
| <xref:EvoSim.OpenAIMutatorClient> | an OpenAI-compatible server (vLLM) | `http://localhost:8001` | `POST /v1/chat/completions`, `GET /v1/models` | answers of at most 64 tokens, thinking off |

The `GET` path is the connection test: *Window ▸ EvoSim ▸ Brain Health* has a **Test connection** button for each
brain and mutator client (V-60), and shows their tries, failures and latencies during a run.

## Client settings

Every HTTP brain and mutator client has these Inspector fields; <xref:EvoSim.HttpCaller> applies them to each request.

| Field | What it does | HTTP brains | Mutator clients |
|---|---|---|---|
| **Host** | the server's address | see above | see above |
| **Api Key Variable** | the name of the environment variable that holds the API key; empty for none | empty | empty |
| **Timeout Seconds** | the time-out of one try, in seconds | 120 (JEV 60) | 60 |
| **Tries** | tries per request, 1 to 10 | 3 | 3 |
| **Backoff Seconds** | the wait before the second try; the waits between tries are Backoff × 1, 2, 4… | 1 | 1 |
| **Max Parallel** | requests in flight at once; the others wait their turn | 4 (Ollama brain 2) | 2 |

A try that times out, gets no answer, or gets the status 408, 429 or 5xx is tried again after its wait: with the
reference values, 1 s then 2 s. Any other error status fails at once. A request that still fails after its last try
fails its query (DEC-42).

## An HTTP brain

| Step | Thread | Done by |
|---|---|---|
| The *Ask brains* phase sends the queries the memo and the cache can't answer, one batch per brain (DEC-15) | main | the World |
| Each query becomes a <xref:EvoSim.BrainRequest>: index, text, action names | main | [Ask](xref:EvoSim.HttpBrain.Ask(System.Collections.Generic.IReadOnlyList{EvoSim.DecisionQuery})), your `TextFor` |
| The requests are posted, the answers read, each row set or failed | thread pool | your `AnswerAsync` |
| The answer is completed; a request left without a row fails | thread pool | <xref:EvoSim.HttpBrain> |
| The tick waits (*Freeze* blocks the main thread, *Responsive* keeps Unity drawing, SPACE-14), stores the rows in the memo and the cache, draws the actions | main | the *Choose actions* phase |

What you write and what you use:

| Member | You | What it does, when it is called |
|---|---|---|
| [AnswerAsync](xref:EvoSim.HttpBrain.AnswerAsync(EvoSim.HttpCaller,System.Collections.Generic.IReadOnlyList{EvoSim.BrainRequest},EvoSim.BrainAnswer)) | must implement | Once per batch, on the thread pool. Post with `c.PostAsync`, then call `answer.SetRow(r.Index, row)` or `answer.Fail(r.Index, reason)` for every request. Don't call `Complete` |
| <xref:EvoSim.HttpBrain.HealthPath> | must implement | the path the connection test reads |
| <xref:EvoSim.Brain.Id> | must implement | the brain's name in memo and cache keys |
| <xref:EvoSim.Brain.ModelIdentity>, <xref:EvoSim.Brain.Mode> | should override | the model and how you use it: part of the cache key (below) |
| <xref:EvoSim.Brain.AnswerInstruction> | may override | the prompt's last line, fixed by the brain because its parsing depends on it (PROMPT-07) |
| [TextFor](xref:EvoSim.HttpBrain.TextFor(EvoSim.DecisionQuery)) | may override | the text sent for a query, built on the main thread; by default the full prompt |
| <xref:EvoSim.Brain.MaxActions>, <xref:EvoSim.Brain.MaxPromptTokens>, <xref:EvoSim.Brain.CanMixSpecies>; `Initialize`, `Begin`, `Validate` | may override (call the base for the last three) | limits checked before the run (DEC-13, DEC-14); the base drops its client at Initialize and checks **Host** (V-20) and keys (V-61) |
| <xref:EvoSim.HttpBrain.Caller>, <xref:EvoSim.HttpBrain.Stats>, <xref:EvoSim.HttpBrain.Transport>, [ConfigureClient](xref:EvoSim.HttpBrain.ConfigureClient(System.String,System.Single,System.Int32,System.Single,System.Int32,System.String)) | use | the client, its counts, a fake server for tests, the settings from code |

To make a row, use <xref:EvoSim.PointsAnswer> for points answers (DEC-41), or
[Brain.Normalize](xref:EvoSim.Brain.Normalize(System.Double[])) for any non-negative weights.

### Example: points from an OpenAI-compatible server

A brain modelled on <xref:EvoSim.OllamaPointsBrain>, for a server such as llama.cpp (port 8080) or vLLM:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvoSim;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>Points mode on an OpenAI-compatible server: one chat call per query.</summary>
public class ChatPointsBrain : HttpBrain
{
    [SerializeField, Tooltip("The model's name on the server.")] string model = "my-model";

    public ChatPointsBrain() { host = "http://localhost:8080"; maxParallel = 2; }

    public override string Id => "chat-points";
    public override string Mode => "points-v1";
    public override string ModelIdentity => model;
    public override string AnswerInstruction => OllamaPointsBrain.Instruction;
    protected override string HealthPath => "/v1/models";

    protected override async Task AnswerAsync(HttpCaller c, IReadOnlyList<BrainRequest> requests, BrainAnswer answer)
    {
        var calls = new List<Task>(requests.Count);
        foreach (var r in requests) calls.Add(One(c, r, answer));
        await Task.WhenAll(calls).ConfigureAwait(false);
    }

    async Task One(HttpCaller c, BrainRequest r, BrainAnswer answer)
    {
        try
        {
            var body = new JObject
            {
                ["model"] = model, ["temperature"] = 0, ["seed"] = 0,
                ["messages"] = new JArray(new JObject { ["role"] = "user", ["content"] = r.Text }),
                ["response_format"] = new JObject { ["type"] = "json_object" },
            };
            var response = await c.PostAsync("/v1/chat/completions", body).ConfigureAwait(false);
            var row = PointsAnswer.ToRow((string)response["choices"]?[0]?["message"]?["content"], r.Actions, out var problem);
            if (row == null) answer.Fail(r.Index, problem);
            else answer.SetRow(r.Index, row);
        }
        catch (Exception e) { answer.Fail(r.Index, e.Message); }
    }
}
```

Put the file in `Assets/Student`: *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Brain* writes a plain brain and its test there;
change its base class to `HttpBrain`. Add the component to a GameObject under the World's *Brains* object, then pick it
in a species' **Brain** field or the World's **Default Brain**.

### The cache key

The answer cache replays rows without calling the model (DEC-33). Its key
([AnswerCache.KeyFor](xref:EvoSim.AnswerCache.KeyFor(EvoSim.Brain,EvoSim.DecisionQuery,System.String))) joins the
brain's `Id`, `ModelIdentity` and `Mode` with the prompt's id, the text style, the genes and the situation. So
`ModelIdentity` must change with the model: give its name and its revision or digest when the server tells them (JEV:
`autotrust/JEV-9B@b63f651ce8ed:jev-decision`; Ollama: `gemma4:12b@<digest>`). `Mode` must change when you change how a
prompt becomes a request or an answer a row: put a version in it (`points-v1`). Anything that changes answers and is
missing from the key makes a replay return old answers.

### Strict by default, keys from the environment

An HTTP brain starts with **Strict** and **Use Cache** on. When a query still fails after its tries, a strict brain
stops the run cleanly: every file is written and the run can be resumed (DEC-40). With **Strict** off, the query gets a
uniform row instead, counted as a failure and never cached (DEC-34).

Never type an API key in the Inspector. Put it in an environment variable of the environment Unity (or the player)
starts from, and write the variable's name in **Api Key Variable**, for example `OLLAMA_API_KEY`. The key is read at
each call and sent as a bearer token; it is never stored, logged or written (OUT-04). Validation reports any serialized
field that looks like a key (V-61).

## A mutator client

The <xref:EvoSim.MutatorService> sends each round of mutation prompts that its cache can't answer to the World's
mutator client, then checks and caches the answers (MUT-14). An <xref:EvoSim.HttpMutatorClient> posts one chat call per
prompt from the thread pool; `Body` and `Content` run there.

| Member | You | What it does |
|---|---|---|
| <xref:EvoSim.HttpMutatorClient.ChatPath>, <xref:EvoSim.HttpMutatorClient.HealthPath> | must implement | the chat path and the connection test's path |
| [Body](xref:EvoSim.HttpMutatorClient.Body(EvoSim.MutatorRequest)) | must implement | the request body for one <xref:EvoSim.MutatorRequest>: its `Prompt`, `Model`, `Seed`, `Temperature` |
| [Content](xref:EvoSim.HttpMutatorClient.Content(Newtonsoft.Json.Linq.JToken)) | must implement | the answer text in a response, or null: the request then fails |
| <xref:EvoSim.MutatorClient.ModelIdentity> | should override | the model, recorded with every mutant allele (GENE-12) |
| [UserMessage](xref:EvoSim.HttpMutatorClient.UserMessage(System.String)) | use | the `messages` array: the prompt as the only user message |

**The body carries the prompt and nothing else** (CORE-07, MUT-11): one user message, no system message, nothing about
the animal. Send the request's own seed and temperature: the cache's key holds them (MUT-14). The integrity check B-11
reads the bodies a real run sends and accepts only `model`, `messages`, `seed`, `temperature`, `max_tokens`, `options`,
`stream`, `think` and `chat_template_kwargs`.

```csharp
using EvoSim;
using Newtonsoft.Json.Linq;

/// <summary>A mutator behind a llama.cpp server (OpenAI-style chat).</summary>
public class LlamaCppMutatorClient : HttpMutatorClient
{
    public LlamaCppMutatorClient() { host = "http://localhost:8080"; }

    public override string ModelIdentity =>
        World != null && World.Service<MutatorService>() != null ? World.Service<MutatorService>().Model : "";
    protected override string ChatPath => "/v1/chat/completions";
    protected override string HealthPath => "/health";

    public override JObject Body(MutatorRequest r) => new JObject
    {
        ["model"] = r.Model, ["messages"] = UserMessage(r.Prompt), ["max_tokens"] = 64,
        ["seed"] = r.Seed, ["temperature"] = HttpCaller.Number(r.Temperature),
    };

    protected override string Content(JToken response) => (string)response["choices"]?[0]?["message"]?["content"];
}
```

In the reference scenes the client sits on the *Mutator client* GameObject under the World's *Brains* object, next to
the *Mutator* service: put yours there in place of <xref:EvoSim.OllamaMutatorClient>. The model's name and the
temperature are the service's fields.

A mutator without a server derives from <xref:EvoSim.MutatorClient>: its
[Send](xref:EvoSim.MutatorClient.Send(System.Collections.Generic.IReadOnlyList{EvoSim.MutatorRequest})) returns a
<xref:EvoSim.MutatorReply> filled with `SetAnswer` or `Fail`, then `Complete`. Read <xref:EvoSim.Testing.FakeMutator>.

## Rules your brain or client must keep

1. **No Unity object off the main thread**: read Unity objects in `TextFor` or `Initialize`, keep plain values.
2. **Await with `ConfigureAwait(false)`**: in *Freeze* mode an await that needs the main thread never resumes.
3. **A row or a failure for every request** (DEC-11): one entry per action, in action order, each ≥ 0, summing to 1.
4. **The same query gives the same row** (DEC-12): temperature 0 and a fixed seed.
5. Keys only from the environment (OUT-04), a complete cache key (DEC-33), a blind mutator (CORE-07).

## Test it

Tests never call a model: they give the brain a <xref:EvoSim.Testing.FakeHttpTransport> through
<xref:EvoSim.HttpBrain.Transport>. Its `Respond` function answers each request; `FailWith`, `Hang` and `DelayMs` make it
fail, time out or answer late; `Requests` and `MaxInFlight` show what it received.

```csharp
using EvoSim;
using EvoSim.Testing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class ChatPointsBrainTests : WorldFixture
{
    [Test]
    public void AnswersFromAFakeServer()
    {
        var choice = new JObject { ["message"] = new JObject { ["content"] = "{\"eat\": 80, \"flee\": 20}" } };
        var fake = new FakeHttpTransport { Respond = (method, path, body) => new JObject { ["choices"] = new JArray(choice) } };
        var w = New().Lab1(prey: 6, predators: 2, randomBrain: false)
            .DefaultBrain<ChatPointsBrain>(b => { b.Transport = fake; b.ConfigureClient("http://fake.test", 5f, 1, 0f, 2); })
            .Build();
        w.Advance(8);
        Assert.AreEqual(8, w.Tick, "no failure stopped the strict brain");
        Assert.Greater(fake.Count, 0, "the brain called its server");
    }
}
```

The test uses Newtonsoft's `JObject`: add `Newtonsoft.Json.dll` to the **Assembly References** of
`Assets/Student/Tests/Student.Tests.asmdef`. More in `Assets/EvoSim/Tests/EditMode/Http`: `HttpBrainTests.cs`
(formats, retries, time-outs, strict stops, the cache, both wait modes) and `HttpMutatorTests.cs`.

## Read next

<xref:EvoSim.OllamaPointsBrain>, then <xref:EvoSim.JevBrain> (batched requests, log-probabilities);
<xref:EvoSim.OllamaMutatorClient>, <xref:EvoSim.OpenAIMutatorClient>; [A brain](brain.md), [Mutation](mutation.md),
[Decisions and brains](../guides/brains-and-decisions.md); the specification's
[08 — Decisions, brains and prompts](../spec/08-decisions-brains-and-prompts.md) and [10 — Mutation](../spec/10-mutation.md).
