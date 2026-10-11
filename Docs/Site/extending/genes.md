---
uid: extend-genes
---

# Genes

Three changes need no code: a **free text gene** (a temperament the brain reads), a **number gene** that lets evolution
tune a trait, and a shared **allele pool** of founder sentences. Write a **gene kind** only when a value must be stored
or expressed in a new way: a number the brain reads as words, a choice among a few options, the weights of a small
network (GENE-07). How genes work is explained in [Genes and mutation](../guides/genes-and-mutation.md).

## A free text gene

A free gene belongs to no action; it appears in the genes block under its own label.

1. Under the species, outside its `Actions` group (a `Genes` group is a good place), create an object named
   `temperament` and add a **Text Gene**.
2. Tick **Free**. Leave **Label** empty to use the object's name.
3. Leave **Pool** empty and fill **Founders**: *"Be bold."*, *"Be careful."*. Keep **Has Neutral**: the pool also gets
   *"No preference."*.
4. Press Play. The species inspector's **Genes** foldout lists `prey.temperament`, and its **Prompt preview** shows the
   line `- temperament: "Be bold."` in the genes block.

The species' `LlmMutation` (with an empty **Only These**) mutates it like any other text gene. A text gene that is neither
under an action nor free is treated as free, with a warning (V-06). Adding a gene changes the species' signature
(SPEC-03): accept the new one with the fix V-12 offers.

## A number gene

A <xref:EvoSim.NumberGene> sets one trait at birth, and a <xref:EvoSim.GaussianMutation> moves it a little at each
mutation.

1. Select the species and choose *GameObject ▸ EvoSim ▸ Add Gene ▸ Stamina gene*. You get a `StaminaGene` object in the
   species' `Genes` group, with a Number Gene labelled `stamina` (**Trait** `stamina.max`, **Min** 20, **Max** 120,
   **Founders** 45, 60, 75) and a Gaussian Mutation (**Sigma** 5).
2. For another trait, change **Trait** to a name a module of the species declares: the inspector's **Stats and traits**
   foldout lists them (`stamina.max`, `stamina.regen`, `speed.walk`, `speed.run`, `vision`, `maturity`…). Tick
   **Multiply Default** to scale the trait's default instead of replacing it: a `speed.run` gene with range 0.8 to 1.2
   and founders 0.9, 1, 1.1 makes some animals 10 % faster.
3. Set **Sigma** in the gene's own unit. A Gaussian Mutation with an empty **Only These** is the species' default for
   every number gene; for a gene on another scale, add a second Gaussian Mutation and list the gene in its **Only These**
   (MUT-02).
4. Give the trait a **cost**, or the gene drifts to whichever end helps (09 §5, V-47): on the species' `Metabolism`, set
   **Cost Trait** to `stamina.max` and **Cost Per Unit** to 0.005, so that 60 stamina costs 0.3 energy a tick.

`GaussianMutation` accepts any number gene that implements <xref:EvoSim.INumberRange> (**Min** and **Max**), so your own
number gene kinds work with it too.

## An allele pool

An <xref:EvoSim.AllelePool> is an asset of founder and contrast sentences that several text genes share, for example the
same `follow` sentences in two species.

1. *Assets ▸ Create ▸ EvoSim ▸ Allele Pool*.
2. Fill **Founders** (at most 12 words each, plain words, no numbers), and **Contrast Pro** and **Contrast Anti**, the
   two sentences brain tests use (GENE-23).
3. Drag the asset into the **Pool** field of each text gene. Its founders replace the gene's own list; its contrast
   sentences do too, when not empty. The neutral allele stays a setting of each gene.

The reference pools are in `Assets/EvoSim/Data/AllelePools` (`prey_eat.asset`…). Founder pools are frozen for an
experiment (GENE-21): changing one starts a new experiment, and its new sentences make new prompts that the brain and
mutator caches have never seen.

## A new gene kind

Derive from <xref:EvoSim.Gene>. *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Gene kind* writes a number gene template into
`Assets/Student`, with a test in `Assets/Student/Tests`.

| Member | | What it does, and when it is called |
|---|---|---|
| [Kind](xref:EvoSim.Gene.Kind) | must implement | `Text` or `Number`. It decides which operators accept the gene: `LlmMutation` takes text, `GaussianMutation` takes numbers with a range. |
| [FounderPool](xref:EvoSim.Gene.FounderPool) | must implement | The values founders draw from, each with its origin (`founder`, `neutral`). Read at the start of the run, registered before anything else. |
| [Express](xref:EvoSim.Gene.Express(EvoSim.AlleleValue,EvoSim.Expression)) | must implement | Turns a value into a trait (`e.SetTrait`) or a genes-block line (`e.PromptGene`). Called at birth, and each time a decision's prompt is written. |
| [ReadByBrain](xref:EvoSim.Gene.ReadByBrain) | may override | True: the gene enters the genes block and the brain key. Default: text genes only. |
| [Check](xref:EvoSim.Gene.Check(EvoSim.AlleleValue)) | may override | Why a value is not allowed, or null. Every mutant is checked at hatching, whatever made it. |
| [Decimals](xref:EvoSim.Gene.Decimals) | may override | The precision at which two numbers are the same allele (default 4). |
| [MayMutate](xref:EvoSim.Gene.MayMutate(EvoSim.Allele)) | may override | False keeps an allele from mutating, as `TextGene` does for the neutral allele (MUT-22). |
| `Initialize`, `Validate` | may override | Reset caches; report bad settings before the run. |

A complete example: a number in [0, 1] that the brain reads as a word.

```csharp
using System.Collections.Generic;
using EvoSim;
using UnityEngine;

/// <summary>A boldness in [0, 1] that the brain reads as "Be careful.", "Be balanced." or "Be bold.".</summary>
public class BoldnessGene : Gene, INumberRange
{
    [SerializeField, Tooltip("Founder values, in [0, 1].")]
    List<float> founders = new List<float> { 0.2f, 0.5f, 0.8f };

    public float Min => 0f;
    public float Max => 1f;
    public override AlleleKind Kind => AlleleKind.Number;
    public override bool ReadByBrain => true;          // it enters the genes block
    public override int Decimals => 2;                 // 0.501 and 0.502 are one allele

    public override IReadOnlyList<FounderAllele> FounderPool
    {
        get
        {
            var pool = new List<FounderAllele>();
            foreach (var f in founders) pool.Add(new FounderAllele(AlleleValue.OfNumber(f), "founder"));
            return pool;
        }
    }

    public override void Express(AlleleValue value, Expression e) =>
        e.PromptGene(Label, value.Number < 0.33f ? "Be careful." : value.Number < 0.67f ? "Be balanced." : "Be bold.");

    public override string Check(AlleleValue value) =>
        value.Number < Min || value.Number > Max ? $"{value.Number} outside [0, 1]" : null;
}
```

Add the component under the species, on an object named `boldness`: its label, and so its locus id `prey.boldness`, comes
from the object's name unless you set **Label**. A Gaussian Mutation with **Sigma** 5 would throw the value to 0 or 1 at
every mutation, so add a Gaussian Mutation with **Sigma** 0.1 and list this gene in its **Only These**.

Only <xref:EvoSim.TextGene> and its subclasses are bound to actions. To make a text gene kind that binds, derive from
`TextGene`; a text kind derived from `Gene` should return `SentenceGuards.Check(value.Text)` from `Check`.

## Rules your gene kind keeps

- **Settings only.** One component serves every animal of the species: nothing per animal in its fields.
- **A frozen pool** (GENE-21). `FounderPool` returns the same values in the same order in every run, built from the
  settings alone. It is never empty: the reference kinds report an empty pool as V-41 in `Validate`; do the same.
- **Express through the expression only** (GENE-03, GENE-04). It runs at birth with an animal, and for prompts without
  one: change nothing else, draw no random number, give the same output for the same value.
- **Check is the contract with every operator.** A mutant it refuses leaves the inherited allele (MUT-04). Text founders
  and mutants pass the sentence guards (GENE-22).
- **A label unique in the species**, so the locus id is unique in the world (GENE-11).

## Test it

There is no conformance suite for gene kinds, but the operators' suite (T-MUT) mutates a number gene and a text gene and
checks the results against the gene's `Check`. Test your expression the way T-GENE-01 does:

```csharp
using System.Collections.Generic;
using EvoSim;
using EvoSim.Testing;
using NUnit.Framework;

public class BoldnessGeneTests : WorldFixture
{
    [Test]
    public void BoldValuesSayBeBold()
    {
        var w = New().Flat(20, 20).Species("prey", s => s.Population(2).Action<RestAction>("rest").Module<BoldnessGene>()).Build();
        var sp = w.FindSpecies("prey");
        var lines = new List<KeyValuePair<string, string>>();
        sp.Module<BoldnessGene>().Express(AlleleValue.OfNumber(0.9f), new Expression(null, sp.Declarations, lines));
        Assert.AreEqual("Be bold.", lines[0].Value);
    }
}
```

See [Testing your module](testing-your-module.md) for the test assembly and the runner.

## Read next

- The reference kinds: <xref:EvoSim.TextGene> and <xref:EvoSim.NumberGene>; the API: <xref:EvoSim.Expression>,
  <xref:EvoSim.AlleleValue>, <xref:EvoSim.SentenceGuards>.
- How your gene's values change: [Mutation](mutation.md).
- The contract: [09 — Genes, alleles and genomes](../spec/09-genes-alleles-and-genomes.md).
