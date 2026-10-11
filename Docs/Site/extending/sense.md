---
uid: extend-sense
---

# A sense

A sense tells the brain one new thing about the situation: whether water is near, whether a threat is chasing the
animal, what lies ahead. Derive from <xref:EvoSim.Sense> for a few tokens of your own, from <xref:EvoSim.BandedSense>
for "how far is the nearest X", and from <xref:EvoSim.BatchedSense> when you need raycasts. To sense a stat as low,
medium or high (thirst, fear), you need no code: add a <xref:EvoSim.LevelSense> and set its **Stat** and thresholds.
How senses, observations and situation texts fit together is in [Senses and prompts](../guides/senses-and-prompts.md).

## The base class

| Member | | What it does, and when it is called |
|---|---|---|
| [Tokens](xref:EvoSim.Sense.Tokens) | must implement | every token the sense can return, "none" included, fixed before the run |
| [Read](xref:EvoSim.Sense.Read(EvoSim.Animal,EvoSim.SenseContext)) | must implement | returns the animal's token, as an index into `Tokens`; called by the sense phase for each deciding animal |
| [Write](xref:EvoSim.Sense.Write(System.Int32,EvoSim.TextStyle)) | may override | the text of a token in V1 or V2; the default writes `Label: token.` in both |
| [DefaultLabel](xref:EvoSim.Sense.DefaultLabel) | may override | the label when the Inspector's **Label** is empty; by default the class name without "Sense" |
| [ReadAll](xref:EvoSim.Sense.ReadAll(System.Collections.Generic.IReadOnlyList{EvoSim.Animal},EvoSim.SenseContext,System.Span{System.Int32})) | may override | reads all the deciding animals of the species at once; the default calls `Read` for each |
| [HasAttachments](xref:EvoSim.Sense.HasAttachments), [Attachment](xref:EvoSim.Sense.Attachment(EvoSim.Animal,EvoSim.SenseContext)), [AttachmentKey](xref:EvoSim.Sense.AttachmentKey(System.Object)) | may override | an image or other object for the query ([Attachments](#attachments)) |
| [Declare](xref:EvoSim.SpeciesModule.Declare(EvoSim.SpeciesBuilder)) | may override | declares the traits the sense needs ("vision", "maturity"), before any animal exists |
| [Initialize](xref:EvoSim.SpeciesModule.Initialize) | may override | finds stats and other modules, builds the token list |
| [Validate](xref:EvoSim.SpeciesModule.Validate(EvoSim.ValidationReport)) | may override | reports configuration problems before the run |

`Read` receives a <xref:EvoSim.SenseContext>. It is the only way to look at the world: `Vision`, `NearestAnimal`,
`NearestResource`, `NearestCarcass`, `NearestEgg`, `NearestEntity<T>`, `NearestCover`, `InCover`, `IsReady`,
`CurrentTarget`, `Ground` and `Rays`. Its queries leave out animals hidden from the reader and animals killed this tick.

## A complete example

The sample <xref:EvoSim.Samples.ChasedSense> (`Assets/EvoSim/Samples/ChasedSense.cs`) says whether the nearest threat is
hunting this animal. It lets a cannibal tell a hungry neighbour from a peaceful one.

```csharp
using System.Collections.Generic;

namespace EvoSim.Samples
{
    public class ChasedSense : Sense
    {
        static readonly IReadOnlyList<string> tokens = System.Array.AsReadOnly(new[] { "no", "yes" });

        public override IReadOnlyList<string> Tokens => tokens;
        protected override string DefaultLabel => "Chased";

        public override int Read(Animal a, SenseContext s)
        {
            var threat = s.NearestAnimal(a, AnimalSet.Threats, s.Vision(a));
            if (threat == null) return 0;
            var t = threat.Value.Animal;
            bool hunting = t.CurrentAction != null && t.CurrentAction.Hunts;
            return hunting && s.CurrentTarget(t) == a ? 1 : 0;   // the hunter's target this tick
        }

        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 1 ? "Something is chasing me." : "Nothing is chasing me.")
                                  : $"{Label}: {tokens[token]}.";
    }
}
```

**Where to put it.**

1. *Assets ▸ Create ▸ EvoSim ▸ Script ▸ Sense* writes `Assets/Student/<Name>.cs`, a sense that compiles, and its test
   `Assets/Student/Tests/<Name>Tests.cs`.
2. Add the component under the species, in its `Senses` group, on a child GameObject named after its label, as the
   reference species do. Any place under the species works.
3. The order matters. The senses are taken depth-first: the n-th sense found gives the n-th token of the observation and
   the n-th fragment of the situation text. The species inspector lists them *in situation-text order*.
4. Adding, removing or moving a sense changes the species' signature. Validation warns until you accept it (V-12),
   because results and cached answers change too.

If your sense reads a module of your own (a `Thirst` stat), put `[RequiresModule(typeof(Thirst))]` on the class:
validation then checks that the species has it (V-26), and the test suites add it for you.

## Distances: BandedSense

A <xref:EvoSim.BandedSense> does most of the work for "how far is the nearest X". It declares the "vision" trait, builds
the tokens from its **Bands** (here, adjacent, close, medium, far, none), writes both styles with ranges in meters, and
checks the bands (V-30). You write `Read`, and return
[TokenFor](xref:EvoSim.BandedSense.TokenFor(System.Single,System.Single,System.Boolean)) or
[NoneToken](xref:EvoSim.BandedSense.NoneToken). The sample <xref:EvoSim.Samples.NearestWaterSense>:

```csharp
public class NearestWaterSense : BandedSense
{
    protected override bool HasHere => true;                               // a "here" token, at the shore
    protected override string DefaultLabel => "Water";
    protected override string HereSentence() => "I am at the water's edge.";   // V2 text of "here"

    public override int Read(Animal a, SenseContext s)
    {
        if (s.Ground == null) return NoneToken;
        float vision = s.Vision(a);
        var water = s.Ground.NearestWater(a.Position, vision);
        return water == null ? NoneToken : TokenFor(water.Value.Distance, vision, here: water.Value.Distance <= 0.5f);
    }
}
```

| Member | Use |
|---|---|
| [Vision](xref:EvoSim.BandedSense.Vision) | the "vision" trait: `a.Trait(Vision)` is the animal's vision in meters (or `s.Vision(a)`) |
| [HasHere](xref:EvoSim.BandedSense.HasHere), [HereSentence](xref:EvoSim.BandedSense.HereSentence) | add a "here" token, and its V2 sentence |
| [Noun](xref:EvoSim.BandedSense.Noun) | the noun of V2 texts ("The nearest water is…"); the **Noun** field, or the label in lower case |
| [BuildTokens](xref:EvoSim.BandedSense.BuildTokens) | override to add tokens per band, as <xref:EvoSim.NearestAnimalSense> does for readiness |

If you override `Declare`, `Initialize` or `Validate`, call the base method.

## Raycasts: BatchedSense

A <xref:EvoSim.BatchedSense> asks for its rays first, in
[RequestRays](xref:EvoSim.BatchedSense.RequestRays(EvoSim.Animal,EvoSim.RayBatch)). The sense phase casts the rays of
every deciding animal in one batch (SENSE-30); then `Read` gets the k-th ray back from
[SenseContext.Rays](xref:EvoSim.SenseContext.Rays). The test sense <xref:EvoSim.Testing.ObstacleSense> casts one ray
along the heading:

```csharp
public override void RequestRays(Animal a, RayBatch rays) =>
    rays.Add(a.Position + Vector3.up * 0.5f, ActContext.HeadingDirection(a.Heading), 5f);   // ray 0

public override int Read(Animal a, SenseContext s) => s.Rays.Hit(a, this, 0, out _) ? 1 : 0;
```

Ask for the same rays in the same order for the same state: `Read` finds them by number. The tokens must equal those
of rays cast one at a time (SENSE-31); <xref:EvoSim.RayBatch.Immediate> casts them that way for a test.

## Attachments

A sense can attach an object to the query, such as a small camera image (SENSE-40). Override
[HasAttachments](xref:EvoSim.Sense.HasAttachments) to return true and
[Attachment](xref:EvoSim.Sense.Attachment(EvoSim.Animal,EvoSim.SenseContext)) to return the object; the ask-brains phase
calls it for each query. A query with an attachment is memoised and cached only if
[AttachmentKey](xref:EvoSim.Sense.AttachmentKey(System.Object)) gives a key for it, such as a coarse summary of the
image (SENSE-41). Only a brain whose <xref:EvoSim.Brain.AcceptsAttachments> is true can serve the species (V-51); no
reference brain is. The test sense <xref:EvoSim.Testing.CameraProbeSense> shows the three members.

## Rules your sense keeps

1. **A fixed list of tokens** (SENSE-01): build it in a field or in `Initialize`, never during the run. Include the
   "nothing" token. Keep it short: each token multiplies the species' observation space (SENSE-05, V-50).
2. **Same state, same token** (SENSE-03): draw no random number and read no clock.
3. **Only the state at the start of the tick** (SENSE-04): read the world through the <xref:EvoSim.SenseContext> you
   receive. Keep settings in the module, nothing per animal.
4. **Hidden and killed things are never sensed** (SENSE-06): find animals, food and cover with the context's queries,
   which leave them out.
5. **A sense only reads.** It never changes an animal or the world.
6. **One text per token** (SENSE-10): `Write` depends on the token and the style only, never on the animal. Every token
   has a non-empty text in both styles. Distances say "meters" (SENSE-12, SENSE-13).
7. **A label of its own** within the species (V-17): the brain tells senses apart by label.
8. **The batch gives the same tokens**: a `ReadAll` override, or a batched sense, gives exactly what `Read` would give
   for each animal alone (SENSE-31).

## Test it

Your sense runs through the **sense conformance suite**, `ConformanceTests.SenseConformance`, as soon as it compiles.
The suite finds every sense class of the EvoSim assemblies and of `Assets/Student`, puts it under the prey of a small
ecology with the random brain, and checks that:

- the token list is not empty, and no token is empty;
- `Write` gives a non-empty text for every token and both styles, the same text each time;
- over 60 ticks, every observation holds a valid index for your sense.

Run it from *Window ▸ General ▸ Test Runner*, **EditMode** tab: expand `EvoSim.Tests.EditMode` ▸ `EvoSim.Tests` ▸
`ConformanceTests` ▸ `SenseConformance` and run the case named after your class. The failure message names the rule.

Then test what your sense means. The sample test of `ChasedSense` (`Assets/EvoSim/Tests/EditMode/Samples/SampleTests.cs`,
shortened here) places animals, runs a tick and reads the sense:

```csharp
[Test]
public void ChasedSense()
{
    var w = New().Ecology(40f, 0f, 0f, regrow: 0f, prey: s => s.Sense<ChasedSense>()).Phase<ActPhase>().Build();
    var prey = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
    var hunter = Place.Animal(w.FindSpecies("predator"), Place.At(15, 10));
    prey.Choose("rest");
    hunter.Choose("hunt");
    var sense = w.FindSpecies("prey").Module<ChasedSense>();
    Assert.AreEqual("no", sense.Tokens[sense.Read(prey, new SenseContext(w))], "nobody hunted yet");
    w.Advance(1);
    w.Space.Rebuild();                                   // animals moved: refresh the spatial index
    Assert.AreEqual("yes", sense.Tokens[sense.Read(prey, new SenseContext(w))]);
    Assert.AreEqual("Something is chasing me.", sense.Write(1, TextStyle.V2));
}
```

The test class derives from <xref:EvoSim.Testing.WorldFixture>. More in [Testing your module](testing-your-module.md).

## Read next

- The reference senses, written like yours: <xref:EvoSim.LevelSense>, <xref:EvoSim.AgeSense>,
  <xref:EvoSim.NearestResourceSense>, <xref:EvoSim.NearestCoverSense>, <xref:EvoSim.NearestEntitySense>,
  <xref:EvoSim.NearestAnimalSense>.
- Rewrite `LevelSense` (exercise 2) or `NearestAnimalSense` (exercise 5) from their stubs: [the teaching path](exercises.md).
- The samples <xref:EvoSim.Samples.ChasedSense> and <xref:EvoSim.Samples.NearestWaterSense>: [The samples](samples.md).
- An action reads your tokens by label in `IsRelevant`: [An action](action.md).
- Rule lines in the prompt for your module: [Species modules](species-modules.md).
- Specification: [06 — Senses and observations](../spec/06-senses-and-observations.md).
