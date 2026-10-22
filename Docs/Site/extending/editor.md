---
uid: extend-editor
---

# The editor

Your module can come with its own editor tools: a window that shows what it does in a running world, an inspector that
shows computed facts next to its fields, validation messages with fix buttons, a menu item that adds it to a species.
Choose a **validation message** to catch a configuration mistake before Play; it works in the editor, in batch runs and
in tests. Choose a **window or inspector** to look at a running world; it exists only in the editor.

## The hooks

| Hook | Where | What it does, when it runs |
|---|---|---|
| `Validate(ValidationReport)` on a <xref:EvoSim.SpeciesModule> or a <xref:EvoSim.WorldModule> | your module, in `Assets/Student` | reports configuration problems; called by <xref:EvoSim.World.Prepare(EvoSim.ValidationReport)> in the inspectors, before Play, and when a run starts |
| <xref:EvoSim.ValidationFix> | inside `Validate` | a button next to the message: a label and the change it makes |
| Unity's `EditorWindow` | an editor-only assembly | a window, opened from a menu item |
| Unity's `UnityEditor.Editor` with `[CustomEditor(typeof(…))]` | an editor-only assembly | replaces the inspector of your component |
| Unity's `[MenuItem("…")]` | an editor-only assembly | a menu item, such as your own *Add Action* entry |

## Where editor code lives

Editor code uses `UnityEditor`, which players lack, so it lives in an editor-only assembly (EDIT-03); in the runtime
`Student` assembly it would break the player build. The repository already has one,
`Assets/Student/Editor/Student.Editor.asmdef`, made for the scene builder of the
[worked example](worked-example-alarm-calls.md):

```json
{
    "name": "Student.Editor",
    "references": ["Student", "EvoSim.Runtime", "EvoSim.Editor"],
    "includePlatforms": ["Editor"],
    "autoReferenced": false
}
```

Put your windows, inspectors and menus in that folder: they see your modules, EvoSim and its editor helpers. If your
editor code uses the samples, add `"EvoSim.Samples"` to `references`.

## A window

This window lists the living animals of each species with their energy. It uses <xref:EvoSim.Editor.EditorWorlds> to
find the world the other EvoSim windows show: the one of the selection, else the first running one.

```csharp
using EvoSim;
using EvoSim.Editor;
using UnityEditor;
using UnityEngine;

public class EnergyListWindow : EditorWindow
{
    Vector2 scroll;

    [MenuItem("Window/EvoSim/Student/Energy List")]
    static void Open() => GetWindow<EnergyListWindow>("Energy").Show();

    void OnInspectorUpdate() { if (Application.isPlaying) Repaint(); }   // about ten times a second

    void OnGUI()
    {
        var w = EditorWorlds.Current();
        if (w == null || !w.IsInitialized) { EditorGUILayout.HelpBox("Enter Play mode in a scene with a World.", MessageType.Info); return; }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var s in w.AllSpecies)
        {
            var energy = s.Module<Energy>();
            if (energy == null) continue;
            EditorGUILayout.LabelField($"{s.DisplayName}: {s.Animals.Count} living", EditorStyles.boldLabel);
            foreach (var a in s.Animals)
                if (!a.IsGone) EditorGUILayout.LabelField($"#{a.Id}", $"{a[energy.Value]:0} energy");
        }
        EditorGUILayout.EndScrollView();
    }
}
```

Open it with *Window ▸ EvoSim ▸ Student ▸ Energy List*, then press Play. Other helpers:
[`EditorWorlds.SelectedAnimal`](xref:EvoSim.Editor.EditorWorlds.SelectedAnimal(EvoSim.World@)) gives the animal whose
body is selected, and [`EditorWorlds.ActionColor`](xref:EvoSim.Editor.EditorWorlds.ActionColor(System.Int32)) the colour
every EvoSim window uses for an action.

## An inspector

An inspector of your own can show what the fields mean, not only what they are (EDIT-04). Write a class
`FearInspector : UnityEditor.Editor` with `[CustomEditor(typeof(Fear))]` (`Fear` is the module of the next section).
In `OnInspectorGUI`, call `DrawDefaultInspector()` to keep the fields, then draw facts computed from `(Fear)target`:
in Play mode, for example, the mean of `a[fear.Value]` over `fear.Species.Animals`. Override `RequiresConstantRepaint`
to return `Application.isPlaying` so the numbers follow the run. Write `UnityEditor.Editor` in full, as EvoSim's own
inspectors do: inside a namespace under `EvoSim`, `Editor` alone means the `EvoSim.Editor` namespace.

## Validation messages with fixes

A module checks its own settings in `Validate`. Each problem is one message: an id, the object at fault, a sentence that
names the rule, and a fix when there is an obvious one. This stat module reports a start above its maximum, with a
**Clamp** button, and notes when no sense lets the brain read it:

```csharp
using System.Linq;
using EvoSim;
using UnityEngine;

/// <summary>A fear stat on every animal of the species, from <c>start</c> at birth up to <c>max</c>.</summary>
public class Fear : SpeciesModule
{
    [SerializeField, Min(0f), Tooltip("Fear at birth.")] float start = 0f;
    [SerializeField, Min(1f), Tooltip("The most fear an animal can feel.")] float max = 100f;

    public StatId Value { get; private set; }
    public float Start => start;

    public override void Declare(SpeciesBuilder b) => Value = b.DeclareStat("fear", StatStart.Fixed(start), 0f, max);

    public override void Validate(ValidationReport report)
    {
        if (start > max)
            report.Warning("V-36", this, $"Fear starts at {start}, above its maximum {max}.",
                           new ValidationFix("Clamp", () => start = max));
        if (!Species.ModulesOf<LevelSense>().Any(s => s.Stat == "fear"))
            report.Info("Fear-01", this, "No LevelSense reads the stat 'fear': the brain never hears about it.");
    }

    public void Configure(float startFear, float maxFear) { start = startFear; max = maxFear; }
}
```

Select the World and press *Validate*: the message reads `V-36  Fear starts at 150, above its maximum 100.`, with
*Select* and *Clamp* next to it. *Clamp* runs the fix on your component, marks it changed and validates again. An error
(`report.Error`) also stops Play ([Editor tooling](../guides/editor-tooling.md#validation)).

Rules for `Validate`:

- **Only read.** It runs in the editor and at the start of every run. Only the fix changes the scene, when clicked.
- **Name the object at fault**, usually `this`: *Select* and the Console use it (EDIT-02).
- **Error** when the run cannot be right (it blocks Play), **Warning** when it probably runs not as meant, **Info** for
  a fact worth knowing.
- **The id is any short string.** Reuse a catalogue id (21 §2) when your check is one of its kinds.
- **A fix captures what it needs** in local variables, not loop variables. A fix that changes the component's own
  fields, as above, is saved with the scene.

More in [Species modules](species-modules.md#validation); to require another module, `[RequiresModule]` checks it for
you ([dependencies](species-modules.md#dependencies)).

## Your own *Add* menu item

*GameObject ▸ EvoSim ▸ Add Action* and *Add Sense* list fixed items. Each one copies a prefab from
`Assets/EvoSim/Modules` with <xref:EvoSim.Editor.WorldFactory.AddModule(UnityEngine.GameObject,System.String,System.String)>.
To offer your own module the same way:

1. **Make the prefab.** Create an empty GameObject, add your action component and a <xref:EvoSim.TextGene>, and fill
   the gene's **Founders** (or set its **Pool**). For a sense, name the GameObject after its label and add the sense.
2. **Save it** by dragging it into `Assets/EvoSim/Modules` (an action: `Drink.prefab`), `Assets/EvoSim/Modules/Senses`
   or `Assets/EvoSim/Modules/Genes`. Delete the scene object.
3. **Add a menu item** in your editor assembly (with the `using` lines of the window above):

```csharp
static class MyAddMenus
{
    const string Item = "GameObject/EvoSim/Add Action/Drink (mine)";

    [MenuItem(Item, false, 20)]
    static void AddDrink()
    {
        var species = Selection.activeGameObject.GetComponentInParent<Species>(true);
        var go = WorldFactory.AddModule(species.gameObject, "Actions", "Drink");   // under the species' Actions group
        Undo.RegisterCreatedObjectUndo(go, "Add Drink");
        Selection.activeObject = go;
    }

    [MenuItem(Item, true)]   // enabled only when a species, or an object under it, is selected
    static bool CanAddDrink() => Selection.activeGameObject != null
                                 && Selection.activeGameObject.GetComponentInParent<Species>(true) != null;
}
```

The kind is `"Actions"`, `"Senses"` or `"Genes"`; the module is the prefab's file name without `.prefab`. An action is
renamed in lower case (`drink`): its GameObject's name is its name in the prompt. You can also skip the menu and drag
your prefab onto the species' `Actions` or `Senses` object.

## Test it

Test validation with a world built in code, in a <xref:EvoSim.Testing.WorldFixture> class with `using System.Linq;`
([Testing your module](testing-your-module.md)):

```csharp
[Test, Description("A start above the maximum is reported (V-36), and Clamp fixes it")]
public void StartAboveMaximum()
{
    var w = New().Flat(20, 20)
        .Species("prey", s => s.Population(0).Action<RestAction>("rest").Module<Fear>(f => f.Configure(150f, 100f)))
        .BuildUninitialized();
    var report = new ValidationReport();
    w.Prepare(report);
    report.Messages.First(m => m.Id == "V-36").Fix.Apply();
    Assert.AreEqual(100f, w.FindSpecies("prey").Module<Fear>().Start);
}
```

Windows and inspectors have no test suite: open them in Play mode and look.

## The API for tools

Your editor code can call EvoSim's own tools, all static classes of the `EvoSim.Editor` assembly. [Running and
building](../guides/running-and-building.md) shows `Batch` and `RealTimeRuns` at work, in the editor and in batch mode.

| Class | Use it to |
|---|---|
| <xref:EvoSim.Editor.EditorWorlds> | find the open scenes' worlds, the current world, the selected animal, the action colours |
| <xref:EvoSim.Editor.ValidationPanel>, <xref:EvoSim.Editor.PlayModeGate> | validate a world and draw its messages with *Select* and fix buttons; list the errors that would block Play |
| <xref:EvoSim.Editor.PromptPreview> | build the exact prompt a brain receives for a genome and a situation |
| <xref:EvoSim.Editor.WorldFactory> | make a world, a species, or add a module prefab, exactly as the menus do |
| <xref:EvoSim.Editor.ScriptTemplates>, <xref:EvoSim.Editor.Exercises> | write a script and its test from a template; swap reference modules for exercise stubs and back |
| <xref:EvoSim.Editor.ReferenceScenes>, <xref:EvoSim.Editor.RunViews> | the reference folders and decision period, the reference content; add the run views to a scene |
| <xref:EvoSim.Batch> | run a scenario headless (`Run`, `RunScenarioNamed`, `RunTier`), or any action on a scene (`WithScene`) |
| <xref:EvoSim.Editor.RealTimeRuns> | headless runs with files, Play-mode runs with player arguments, run levels L0–L2 |
| <xref:EvoSim.Integrity>, <xref:EvoSim.IntegrityGates> | the brain and mutator checks B-01 to B-11, each written as an <xref:EvoSim.IntegrityReport> |
| <xref:EvoSim.Editor.VisualPlayerBuild> | build the Windows player |

## Read next

- [Editor tooling](../guides/editor-tooling.md): every inspector, window and menu EvoSim already has.
- <xref:EvoSim.Editor.EcologyMonitorWindow>, <xref:EvoSim.Editor.AnimalInspectorWindow>: short windows to copy from.
- [21 — Editor tooling](../spec/21-editor-tooling.md): the validation catalogue and the rules EDIT-01 to EDIT-05.
