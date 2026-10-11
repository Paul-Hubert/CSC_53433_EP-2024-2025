---
uid: documenting-your-code
---

# Writing documentation comments

Every type and every member in EvoSim carries a `///` documentation comment. Your editor shows it when you hover over a
name, and the [API reference](../api/index.md) of this site is built from it. Document your own modules the same way:
the next reader may be you, three weeks later.

This page is the house style. A checker, the [doc audit](#check-your-comments), enforces the parts a program can check.

## What gets a comment

| Element | Needs |
|---|---|
| Every type (class, struct, interface, enum, delegate), public or not | a `<summary>` |
| Every member (method, property, constructor, event, operator, indexer, enum member, constant), public or not | a `<summary>` |
| Every public or protected method, constructor, delegate or indexer | a `<param>` for each parameter, a `<returns>` for a returned value, a `<typeparam>` for each type parameter |
| A public or protected field | a `<summary>` |
| A private field | a `<summary>`, **or** a `[Tooltip]` (the Inspector shows it), **or** a `//` comment at the end of its line or on the line just above, **or** nothing when it only backs a property of the same name (`animals` behind `Animals`) |
| An override whose base text fits | `/// <inheritdoc/>` |
| An override that does something particular | its own `<summary>` plus `/// <inheritdoc/>`, which keeps the base's parameters and returns |
| A test method | its `[Test, Description("…")]` (no `///` needed); the test class and its helpers get a `<summary>` |
| A partial type (`World`) | a `<summary>` in its main file only; each other part starts with a `//` line saying what that file holds |

Inside method bodies, write a `//` comment where a step is not obvious from the code: why it is done, which rule it keeps,
what would break without it. Don't narrate code that reads well by itself.

## How to write a summary

**One or two sentences** that say what the thing is for, in the words of the simulation, not of the code.

- Types and properties: a noun phrase. *The species' brain, or the World's default (DEC-13).*
- Methods: a verb in the third person. *Reads one animal at a decision and returns one of its tokens (SENSE-03).*
- Booleans: *True when …*; say what false means if it is not the plain opposite.
- Say what `null`, empty, `-1` or `0` mean: *… or null when nothing is in sight.*
- Give the unit of every quantity: meters, ticks, points (energy, stamina), seconds, *per tick*.
- Say **when** it is called and **by whom** for hooks and lifecycle methods: *Called by the World once per run, after
  every module declared its stats and before founders spawn.*
- For something a student overrides, say **what it must keep**: the same tokens for the same animal, random draws only
  from the stream it is given, no change to the world from inside `Act`…

Avoid: *This method…*, *Gets or sets…*, *Helper*, repeating the name (*The species.* for `Species`), the history of the
code (*fixed in M13.4* belongs to the implementation log), and *we*.

Write plain English for second-year students: short sentences, common words, one idea per sentence. The first time a
page uses a term of the glossary (allele, locus, tick, phase…), link it or explain it.

### Rule ids and spec references

The [specification](../spec/README.md) numbers its rules. Name the rule a piece of code keeps, in parentheses:
*(SENSE-01)*, *(DEC-30, DEC-33)*. Sections of a spec document are written *(07 §4)*, validation messages *V-08*, tests
*T-ACT-09*. The site turns rule ids into links to the specification.

## Tags

| Tag | Use it for |
|---|---|
| `<summary>` | always; one or two sentences |
| `<param name="x">` | what the argument is and what values it takes: *The animal that decides.* |
| `<returns>` | what comes back, including the "nothing" case: *The nearest prey within vision, or null.* |
| `<typeparam name="T">` | what the type parameter stands for: *The kind of module to find.* |
| `<remarks>` | anything longer: lifecycle, rules to keep, how to extend, performance. Use `<para>` for paragraphs and `<list type="bullet"><item><description>…</description></item></list>` for lists |
| `<example>` + `<code>` | on base classes students derive from and on the most used methods: a short, real example, taken from EvoSim or its samples |
| `<see cref="…"/>` | a link to a type or member the reader may want to open next: `<see cref="ActContext.Search"/>`, `<see cref="Species.Module{T}"/>` |
| `<paramref name="x"/>` | a parameter named in the text |
| `<c>…</c>` | a literal value, a file name, an Inspector field or code inline: `<c>"none"</c>`, `<c>stats.csv</c>` |
| `<see langword="null"/>` | `null`, `true`, `false` when they are values (plain "null" is fine in a sentence) |
| `<inheritdoc/>` | take the base type's or interface's text |
| `<exception cref="…">` | an exception the caller should expect and handle |

XML rules: write `<` as `&lt;`, `>` as `&gt;` and `&` as `&amp;`, also inside `<code>`. Generic types in a `cref` take
braces: `<see cref="EntitySystem{T}"/>`. The compiler checks every `cref`: a wrong one is reported by the audit.

Keep lines under 140 characters; wrap long comments on several `///` lines.

## Examples

A base class students derive from:

```cs
/// <summary>
/// Reads one animal and the world and returns one token from a fixed list (SENSE-01): what the brain is told about one
/// aspect of the situation, such as the nearest food or the energy level.
/// </summary>
/// <remarks>
/// <para>Derive from it, declare every token in <see cref="Tokens"/>, implement <see cref="Read"/>, and place the component
/// under a species: its depth-first place among the species' senses is its place in the situation text.</para>
/// <para>A sense only reads. It sees the world as it was at the start of the tick (SENSE-04) and draws no random number.</para>
/// </remarks>
/// <example>
/// <code>
/// public class HungerSense : Sense
/// {
///     static readonly string[] tokens = { "fed", "hungry" };
///     Energy energy;
///     public override IReadOnlyList&lt;string&gt; Tokens => tokens;
///     public override void Initialize() => energy = Species.Module&lt;Energy&gt;();
///     public override int Read(Animal a, SenseContext s) => a[energy.Value] &lt; 30f ? 1 : 0;
/// }
/// </code>
/// </example>
public abstract class Sense : SpeciesModule
```

A method with parameters:

```cs
/// <summary>Reads one animal at a decision (SENSE-03).</summary>
/// <param name="a">The animal that decides.</param>
/// <param name="s">The world as it was at the start of the tick: nearest animals, food, cover, distances.</param>
/// <returns>An index into <see cref="Tokens"/>.</returns>
public abstract int Read(Animal a, SenseContext s);
```

An override that does something particular, keeping the base's parameters:

```cs
/// <summary>Walks to the nearest visible food the diet allows and grazes it on arrival; searches when none is in sight (ACT-04).</summary>
/// <inheritdoc/>
public override void Act(Animal a, ActContext c)
```

An override that changes nothing in the contract:

```cs
/// <inheritdoc/>
public override bool IsRelevant(ObservationView o) => Seen(o.Token(relevantSense));
```

Private state:

```cs
[SerializeField, Tooltip("A layer name; empty = every layer the diet grazes.")]
string target = "";            // the Tooltip documents it
Species eggsOf;                // whose eggs this action eats, when its target is "egg:<species>"
readonly List<Animal> animals = new List<Animal>();   // backs Animals: nothing more to say
```

## Check your comments

The doc audit compiles the EvoSim assemblies with Unity's own compiler and lists what is missing or malformed:

```powershell
powershell -File Docs\Site\tools\audit.ps1                                  # everything
powershell -File Docs\Site\tools\audit.ps1 --only Runtime/Senses --max 50   # one folder (relative to Assets/EvoSim)
```

| Code | Meaning |
|---|---|
| `CS1591` | a public or protected type or member has no doc comment |
| `DOC01` | a non-public type or member has no doc comment (fields: see the table above) |
| `DOC02` | a public or protected member's comment lacks a `<param>`, `<typeparam>` or `<returns>` |
| `CS1570` | the comment is not well-formed XML (an unescaped `<` or `&`?) |
| `CS1572`, `CS1573` | a `<param>` names no parameter, or a parameter has none |
| `CS1574`, `CS1580`, `CS1584` | a `cref` points to nothing, or is badly written |
| `CS1587` | a `///` comment sits on nothing (use `//` there) |

`--same-code GITREF` checks that only comments and spaces changed since a git revision: use it after a pass that should
not touch the code.

The site itself is built by `Docs/Site/build.ps1` (see `Docs/Site/README.md`).
