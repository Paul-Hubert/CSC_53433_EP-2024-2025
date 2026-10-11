---
uid: extend-mutation
---

# Mutation

There are three ways to change how genes mutate:

- a **mutation deck**, to change what the mutator model is asked to do with a sentence (no code);
- a **mutation operator**, to change how values change: a rule without a model, a new number rule, your own model;
- a **mutator client**, to send the same prompts to another server: see
  [An HTTP brain or mutator server](http-brains.md).

How mutation works from conception to hatching is explained in [Genes and mutation](../guides/genes-and-mutation.md).

## A mutation deck

The deck is the list of instructions the <xref:EvoSim.LlmMutation> draws from. Each mutation draws one per try and sends
it to the mutator with the sentence.

1. *Assets ▸ Create ▸ EvoSim ▸ Mutation Deck* asks where to save a `.txt` file and writes a copy of the reference deck
   `mutate_v4.txt` into it.
2. Edit it: one instruction per line. Blank lines and lines starting with `#` are ignored (MUT-10). Short instructions
   about the rule work best, such as *"Change when this rule applies."*.
3. Drag the file into the **Deck** field of the species' **Llm Mutation** (in the reference scenes, on the species'
   `Mutation` object).

The same component has three more fields: **Context Line**, the one fixed line before the instruction (no line break, no
`{`: V-49); **Tries**, 5 instructions and seeds per mutation before the gene stays unchanged; **Max Words**, 12, the
longest mutant (a text gene's own **Max Words** wins when smaller).

> [!NOTE]
> A mutant records its instruction's index in the deck (`llm#3`, and `"prompt": 3` in the birth event). Reordering the
> lines changes what those numbers mean. New instructions make new prompts, which the answer cache has never seen.

## A mutation operator

Derive from <xref:EvoSim.MutationOperator>. *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Mutation operator* writes a template
into `Assets/Student` (a number operator that adds ±1), with a test in `Assets/Student/Tests`.

| Member | | What it does, and when it is called |
|---|---|---|
| [Accepts](xref:EvoSim.MutationOperator.Accepts(EvoSim.Gene)) | must implement | Whether it can change this gene, usually by its `Kind`. Asked for each locus at each conception (MUT-02), and by the validator (V-48). |
| [StartMutation](xref:EvoSim.MutationOperator.StartMutation(EvoSim.Gene,EvoSim.Allele,EvoSim.RandomStream)) | must implement | Draws every random choice from `rng`, then returns a job. Called in the breed phase, once per mutating locus of each baby. |
| [UsesMutatorService](xref:EvoSim.MutationOperator.UsesMutatorService) | may override | True when the work goes to the <xref:EvoSim.MutatorService> (see below). |
| `Validate` | may override | Call `base.Validate(report)` first: it checks the rate (V-35). |
| **Rate**, **Only These** | Inspector | The probability per gene per baby (reference 0.03); the genes it is configured for. |

`StartMutation` returns a <xref:EvoSim.MutationJob>:

| Factory | Use it when |
|---|---|
| [MutationJob.Done(text, op)](xref:EvoSim.MutationJob.Done(System.String,System.String)), [Done(number, op)](xref:EvoSim.MutationJob.Done(System.Single,System.String)) | the new value is ready at once; `op` is the short detail recorded with the allele, e.g. `ladder+1` |
| [MutationJob.Failed(reason)](xref:EvoSim.MutationJob.Failed(System.String)) | there is nothing to change: the gene keeps its allele (MUT-04) |
| [MutationJob.Running(pending)](xref:EvoSim.MutationJob.Running(EvoSim.Pending)) | the work goes on in the background (see below) |

A complete example, the sample `Assets/EvoSim/Samples/IntensityLadder.cs` (<xref:EvoSim.Samples.IntensityLadder>): it
moves the sentence's intensity word one step along a ladder, without any model (MUT-21).

```csharp
using System;
using EvoSim;
using UnityEngine;

public class IntensityLadder : MutationOperator
{
    static readonly string[] Ladder = { "never", "rarely", "sometimes", "often", "always" };

    public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;

    public override MutationJob StartMutation(Gene g, Allele parent, RandomStream rng)
    {
        var words = parent.Text.Split(' ');
        int i = -1, k = -1;
        for (int w = 0; w < words.Length && i < 0; w++)
        {
            k = Array.IndexOf(Ladder, words[w].ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':'));
            if (k >= 0) i = w;
        }
        if (i < 0) return MutationJob.Failed("no intensity word");
        int step = rng.NextBool() ? 1 : -1;
        string next = Ladder[Mathf.Clamp(k + step, 0, Ladder.Length - 1)];
        string original = words[i];
        string trail = original.Substring(original.TrimEnd('.', ',', '!', '?', ';', ':').Length);
        if (char.IsUpper(original[0])) next = char.ToUpperInvariant(next[0]) + next.Substring(1);
        words[i] = next + trail;
        return MutationJob.Done(string.Join(" ", words), step > 0 ? "ladder+1" : "ladder-1");
    }
}
```

Put the component under the species, for example on its `Mutation` object, and list the genes it should mutate in its
**Only These**. Exactly one operator applies to a locus (MUT-02): the first that accepts the gene and lists it, else the
first that accepts it and lists no gene. With an empty **Only These** next to the reference `LlmMutation`, the one found
first in the hierarchy would be the default for every text gene.

## Rules your operator keeps

- **Blind** (CORE-07). It reads the gene's settings and the parent allele, nothing else: no animal, no other gene, no
  world state, no clock. Selection alone decides what is good.
- **Draw now, from the given stream** (MUT-30). Every random choice comes from `rng`, inside `StartMutation`, as many
  draws whatever happens later. The same stream state gives the same job, so a seed replays the run.
- **Guards hold.** At hatching the gene's [Check](xref:EvoSim.Gene.Check(EvoSim.AlleleValue)) judges every result: a
  refused value (a 13-word sentence, a number outside the range) leaves the inherited allele and counts as `not allowed`.
  A result equal to the parent counts as `unchanged` (MUT-04).
- **A stable operator detail.** The `op` string is recorded with every mutant (GENE-12): keep it short and meaningful.

## An asynchronous operator

A model takes time to answer. A job may finish after `StartMutation` returns: the egg then waits until its genome is
complete (MUT-32).

**Through the mutator service.** The World's <xref:EvoSim.MutatorService> sends prompts to the mutator server in batches,
caches the answers, cleans and checks them (MUT-12) and redraws rejected ones (MUT-13). Draw each try's instruction and
seed, then [Submit](xref:EvoSim.MutatorService.Submit(System.String,System.Int32[],System.Int64[],System.String,System.Collections.Generic.IReadOnlyList{System.String},System.Int32))
them. Return true from `UsesMutatorService`, so the client records the model's identity in the run info.

```csharp
using System.Collections.Generic;
using EvoSim;

public class OppositeMutation : MutationOperator
{
    static readonly List<string> Deck = new List<string> { "Make this rule say the opposite." };

    public override bool Accepts(Gene g) => g.Kind == AlleleKind.Text;
    public override bool UsesMutatorService => true;

    public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng)
    {
        var instruction = new int[3];                                          // every try uses instruction 0
        var seeds = new long[3];
        for (int i = 0; i < 3; i++) seeds[i] = rng.NextUInt() & 0x7FFFFFFF;    // drawn now, used or not (MUT-30)
        var service = World.Service<MutatorService>();
        if (service == null) return MutationJob.Failed("no mutator");
        return service.Submit(parent.Text, instruction, seeds, "The sentence below is a rule that a wild animal follows.", Deck, 12);
    }
}
```

**With your own background work.** Return `MutationJob.Running` with a <xref:EvoSim.Pending> that tells when the work is
done, and complete or fail the job as the work's last step:

```csharp
public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng)
{
    bool up = rng.NextBool();                          // every draw now, on the main thread (MUT-30)
    string text = parent.Text;                         // copy what the work needs: no Unity object in the task
    var job = MutationJob.Running(null);
    job.Pending = Pending.Of(System.Threading.Tasks.Task.Run(() =>
    {
        string mutant = text.Replace("often", up ? "always" : "rarely");   // stands for slow work
        if (mutant == text) job.Fail("no often");
        else job.Complete(AlleleValue.OfText(mutant), up ? "often+1" : "often-1");
    }));
    return job;
}
```

The result must depend only on the parent and the draws, never on timing: then the Freeze and Responsive wait modes, and
a replay, give the same genes. Never touch Unity objects or the world from the background thread.

## Test it

Your operator enters the T-MUT conformance suite by itself (*Window ▸ General ▸ Test Runner*, EditMode,
`ConformanceTests` ▸ `MutationOperatorConformance`). It mutates a text gene and a number gene twice with the same seed and
checks that the operator accepts at least one of them, that both jobs finish with the same outcome and the same value,
and that the value passes the gene's `Check`. For a test of your own, copy `IntensityLadderSteps` in
`Assets/EvoSim/Tests/EditMode/Samples/SampleTests.cs`; see [Testing your module](testing-your-module.md).

## Read next

- The reference operators: <xref:EvoSim.LlmMutation>, <xref:EvoSim.GaussianMutation>; rewrite `LlmMutation` from its stub
  in [the teaching path](exercises.md).
- The prompt, cleaning and guards as pure functions: <xref:EvoSim.MutationText>, <xref:EvoSim.SentenceGuards>.
- New kinds of values to mutate: [Genes](genes.md). Another server: [An HTTP brain or mutator server](http-brains.md).
- The contract: [10 — Mutation](../spec/10-mutation.md).
