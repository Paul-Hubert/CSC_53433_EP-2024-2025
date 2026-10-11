---
uid: extend-views
---

# Views

A view shows an animal; it never changes it. Write an <xref:EvoSim.AnimalView> subclass to colour, scale or animate the
animals from their state, put it on a body prefab, and give the prefab to the species' <xref:EvoSim.Body>. This page
also shows how to make your own entities visible and how to add a panel of your own. If what you want must change the
run (an animal that reacts to something), it is not a view: write a module instead ([Extending EvoSim](index.md)).
How the views work at run time is in [Views and real time](../guides/views-and-real-time.md).

## The base class

The World places every view in its `LateUpdate`, then calls `OnShow`. A view has nothing else to do.

| Member | You | What it does, when it is called |
|---|---|---|
| [OnShow(Animal)](xref:EvoSim.AnimalView.OnShow(EvoSim.Animal)) | override | Every frame the view is shown, on the main thread, right after the World placed it. Its argument is the view's animal, as the last tick left it. Also called while the run is paused, or waits for a brain in the *Responsive* mode. The base does nothing |
| <xref:EvoSim.AnimalView.AnimalId> | read | the id of the animal shown, or -1 while the view waits in its pool |
| `Awake` | may write | Unity's, called once when the pool makes the view: create your `MaterialPropertyBlock`, find child renderers |

What a view usually reads from its animal (every member of <xref:EvoSim.Animal> is readable):

| Read | Gives |
|---|---|
| `a.CurrentAction`, `a.Searching` | the current action (`.Name` is its name, e.g. *flee*), or null; true while it searches |
| `a.Generation`, `a.Age` | its generation (founders are 0) and its age in ticks |
| [a\[stat\]](xref:EvoSim.Animal.Item(EvoSim.StatId)) | a stat's value; the id comes from its module, e.g. `a.Species.Module<Energy>().Value` |
| `a.IsBusy`, `a.BusyReason` | true while it can't act, and why (*digesting* after a meal) |
| `a.LastProbabilities` | the probabilities of its last decision, in action order |
| `a.Species`, `a.Genome` | its species and its genes |

## Example: colour by generation

A view that paints the body blue for the founders, shading to red at generation 20 and later.

```csharp
using EvoSim;
using UnityEngine;

/// <summary>Colours the body by generation: founders blue, generation 20 and later red.</summary>
public class GenerationView : AnimalView
{
    static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField, Tooltip("The body's renderer.")]
    Renderer body;
    [SerializeField, Min(1), Tooltip("The generation shown fully red.")]
    int reddest = 20;

    MaterialPropertyBlock block;      // sets the colour without copying the material
    int shown = -1;                   // the generation the body shows now

    void Awake() => block = new MaterialPropertyBlock();

    public override void OnShow(Animal a)
    {
        if (body == null || a.Generation == shown) return;   // nothing changed: no update
        shown = a.Generation;
        block.SetColor(ColorId, Color.Lerp(Color.blue, Color.red, Mathf.Clamp01(a.Generation / (float)reddest)));
        body.SetPropertyBlock(block);
    }
}
```

Put the class in `Assets/Student` (there is no script template for views; a new C# script file will do). Compare with
what is shown rather than remembering the animal: a pooled view is reused for another animal when its own dies.
<xref:EvoSim.ActionView> is the reference view to read next: it updates two renderers, each only when its colour changed.

## Make a body prefab

1. Duplicate `Assets/EvoSim/Modules/Bodies/PreyBody.prefab` into `Assets/Student`, or build your own. The root's pivot is
   the animal's feet: the World sets the root at the ground's height. The reference body is 1 m high and looks along
   +z, with a dark nose at the front, because the World turns the view toward where the animal moves.
2. Put your view on the **root**. A root holds one <xref:EvoSim.AnimalView> only: remove the <xref:EvoSim.ActionView> of
   the copy first. Fill your view's fields (the body's renderer above).
3. Leave out colliders. The pool disables every collider and moves every part to layer 2 anyway.
4. Select the species, and on its **Body** set **Prefab** to your prefab and **Size** in meters: the prefab's scale is
   multiplied by it (prey 0.6, predator 0.9). **Prewarm** is the number of views made when the run starts (0 = the
   initial population). With no prefab, the species shows capsules of the **Color** field.

A prefab with no <xref:EvoSim.AnimalView> on its root gets a plain one: it follows its animal and shows nothing more.

## Rules your view must keep

1. **Read only** (SPACE-12). Never write a stat, an action, a position or a genome, and call nothing that changes the
   World. The run must give the same events hash with **Show Views** on or off (RAND-11).
2. **No simulation random streams** (RAND-01). Drawing from the World's streams would shift every later draw of the
   run. A visual effect that needs chance (a wobble, a blink) uses `Time` or Unity's own `Random`, which the simulation
   never reads.
3. **Main thread, every frame, every animal.** Keep `OnShow` cheap: find components in `Awake`, change a renderer only
   when its value changed, and use a `MaterialPropertyBlock`; `renderer.material` copies the material for each view.
4. **Layer 2 and no colliders.** The pool puts the view on *Ignore Raycast*
   (<xref:EvoSim.ViewPool.IgnoreRaycastLayer>), which every sense ray leaves out, and disables its colliders. Don't
   change a layer or enable a collider at run time: a sense could then hit your view.
5. **Don't move the root.** The World sets its position and rotation every frame, and the pool scaled it by **Size**.
   Animate child objects instead.

## Make your entities visible

<xref:EvoSim.EntityMarkers> draws one marker per entity of every <xref:EvoSim.EntityKind> under the World. It reads
them through <xref:EvoSim.EntityKind.EntityCount> and [EntityAt](xref:EvoSim.EntityKind.EntityAt(System.Int32)).

- A system derived from <xref:EvoSim.EntitySystem%601> implements both: your entities appear with no code, as grey
  0.3 m cubes (the **Other** colour of EntityMarkers). Carcasses and eggs have their own shapes.
- A class derived from <xref:EvoSim.EntityKind> directly must override `EntityAt`. Its default returns null, and an
  entity it doesn't return is not drawn.
- For a look of your own, write a run view like EntityMarkers: a component outside the World that, in `LateUpdate`,
  reads `World.Service<FruitSystem>().Entities` and places pooled objects without colliders, on
  <xref:EvoSim.ViewPool.IgnoreRaycastLayer>.

## A panel of your own

<xref:EvoSim.RunNumbers> reads everything the overlay shows from the World's counters. A panel of your own can read it
too. Give the camera a <xref:EvoSim.WorldCamera.Blocks> test so that clicks on your panel don't pick an animal; keep the
overlay's test in it.

```csharp
using EvoSim;
using UnityEngine;

/// <summary>A panel at the bottom left: living animals and highest generation per species.</summary>
public class PopulationPanel : MonoBehaviour
{
    readonly RunNumbers numbers = new RunNumbers();
    World world;
    WorldCamera cam;
    RunOverlay overlay;
    Rect panel = new Rect(8f, 0f, 260f, 0f);       // in GUI coordinates: origin at the top left

    void Update()
    {
        if (world == null) world = FindAnyObjectByType<World>();
        if (overlay == null) overlay = FindAnyObjectByType<RunOverlay>();
        if (cam == null && (cam = FindAnyObjectByType<WorldCamera>()) != null) cam.Blocks = Covers;
        if (world != null && world.IsInitialized) numbers.Read(world);
    }

    // Screen points have their origin at the bottom left.
    bool Covers(Vector2 p) =>
        panel.Contains(new Vector2(p.x, Screen.height - p.y)) || (overlay != null && overlay.Covers(p));

    void OnGUI()
    {
        if (world == null || !world.IsInitialized) return;
        panel.height = 10f + 22f * numbers.Species.Count;
        panel.y = Screen.height - panel.height - 8f;
        GUILayout.BeginArea(panel, GUI.skin.box);
        foreach (var s in numbers.Species) GUILayout.Label($"{s.Name}: {s.Living} living, generation {s.MaxGeneration}");
        GUILayout.EndArea();
    }
}
```

Put it on the scene's *Run views* object. It only reads, like every run view.

## Test it

A view needs frames, so check it in Play mode:

1. Run the scene with your body and note the events hash (see [Determinism and replay](../guides/determinism.md)).
2. Run it again with the same seed and **Show Views** off on the World. The hash must be the same.

EvoSim's own play-mode tests do this in `Assets/EvoSim/Tests/PlayMode/ViewPlayTests.cs`: `ViewsNeverChangeTheRun`
(T-SPACE-06) compares hashes with views on, off and uninterpolated, and `ViewsHearTheirAnimal` checks that a view of one's
own (<xref:EvoSim.Testing.ShowProbeView>) hears `OnShow` with its own animal every frame.

## Read next

- <xref:EvoSim.ActionView>: the reference view. <xref:EvoSim.ActionColors>: the action colours, to reuse.
- <xref:EvoSim.EntityMarkers>, <xref:EvoSim.GroundView>, <xref:EvoSim.RunOverlay>: the run views, all read-only.
- [Food, cover, entities and ground](environment.md): entity systems of your own.
- Specification: [02 — Space and time](../spec/02-space-and-time.md) (SPACE-12).
