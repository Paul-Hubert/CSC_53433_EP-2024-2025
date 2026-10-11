---
uid: extend-environment
---

# Food, cover, entities and ground

Four kinds of world services make up the environment, and each is a base class you can derive from. Add a
**resource layer** for food that sits on cells and grows back (berries, mushrooms), a **cover** for hiding places, an
**entity system** for objects that appear, move or rot (fruit, nests), or a **ground** for a different world shape.
Making something edible needs no code at all: see [Who eats what](#who-eats-what). For how the reference modules behave,
read [The environment](../guides/environment.md) first.

All four go under the World, outside any species: a layer, a cover or an entity system on its own GameObject under
`Environment`, the ground on the World's `Ground` GameObject. There is no script template for them: create a C# file in
`Assets/Student` and derive from the base class.

## A resource layer

<xref:EvoSim.ResourceLayer> holds edible items. Senses and actions find items through the eater's diet, and the act
phase consumes them when an animal grazes.

| Member | | What it does, when it is called |
|---|---|---|
| `Count` | must implement | how many items the layer holds now (ENV-01) |
| [Nearest](xref:EvoSim.ResourceLayer.Nearest(UnityEngine.Vector3,System.Single,EvoSim.ResourceItem@)) | must implement | the nearest item within a radius, as a <xref:EvoSim.ResourceItem>; called by senses and actions |
| [Consume](xref:EvoSim.ResourceLayer.Consume(EvoSim.ResourceItem)) | must implement | removes the item at once; false if it is gone already (ENV-02); called when an animal grazes |
| [Regrow](xref:EvoSim.ResourceLayer.Regrow(EvoSim.RandomStream)) | may override | once per tick, in the environment phase, with the layer's own stream (ENV-03); nothing by default |
| `StreamName` | may override | the name of that stream; the layer's name by default |
| `WithinReach` | may override | the nearest item within reach; `Nearest` by default |
| `Initialize` | may override | build empty storage: the ground is ready, the cover may not be yet |
| `Begin` | may override | place the first items: every module is ready, cover included |

`LayerName` is the GameObject's name, and `Edible` the <xref:EvoSim.Edible> on the same GameObject.

### Example: berry bushes

Bushes grow in some cells of a 4 m grid, each with one berry that grows back. It reuses <xref:EvoSim.CellGrid>, the
cell grid of the reference <xref:EvoSim.FoodGrid>, which finds the nearest marked cell for you.

```csharp
using EvoSim;
using UnityEngine;

public class BerryBushes : ResourceLayer
{
    [SerializeField, Range(0f, 1f), Tooltip("Share of walkable cells with a bush.")] float bushShare = 0.2f;
    [SerializeField, Range(0f, 1f), Tooltip("Chance per tick that a bush regrows its berry.")] float regrowP = 0.01f;

    CellGrid grid;
    bool[] bush, berry;     // per cell: a bush grows here; its berry is ripe
    int count;

    public override int Count => count;

    public override void Initialize()
    {
        grid = new CellGrid(World.Ground.Bounds, 4f);
        bush = new bool[grid.Count];
        berry = new bool[grid.Count];
        count = 0;
    }

    public override void Begin()                                      // the map: from the world seed (RAND-04)
    {
        var rng = World.Random.Get("world/" + ServiceName);
        for (int c = 0; c < grid.Count; c++)
            if (World.Ground.IsWalkable(grid.Center(c)) && rng.NextDouble() < bushShare) { bush[c] = berry[c] = true; count++; }
    }

    public override bool Nearest(Vector3 p, float radius, out ResourceItem item)
    {
        int c = grid.Nearest(berry, p, radius, World.Ground, out float d);   // world distance, ties by cell index
        item = c >= 0 ? new ResourceItem(this, c, World.Ground.OnGround(grid.Center(c)), d) : default;
        return c >= 0;
    }

    public override bool Consume(ResourceItem item)
    {
        if (item.Layer != this || item.Cell < 0 || !berry[item.Cell]) return false;   // gone already (ENV-02)
        berry[item.Cell] = false;
        count--;
        return true;
    }

    public override void Regrow(RandomStream rng)                       // the layer's own stream, in cell order
    {
        for (int c = 0; c < grid.Count; c++)
            if (bush[c] && !berry[c] && rng.NextDouble() < regrowP) { berry[c] = true; count++; }
    }
}
```

To use it:

1. Add a GameObject `Berries` under the World's `Environment`, with `BerryBushes` and an `Edible` (`Energy`: the energy
   points of one berry).
2. On each species that eats berries, add a diet entry `Berries`.
3. Point the eater's actions and senses at it: an <xref:EvoSim.EatAction> with `Target` `Berries` (or empty for every
   layer the diet grazes), a <xref:EvoSim.NearestResourceSense> with `Layer Name` `Berries` if the brain should know.

### Rules

- **Consume once** (ENV-02): `Consume` removes the item before it returns true, and returns false for an item already
  eaten. The act order decides who gets a contested item.
- **Change only in your phase** (ENV-03): the items change only in `Regrow` and `Consume`. `Regrow` draws only from
  the stream it is given, in a fixed order: loop over cells, never over a `Dictionary` (RAND-05).
- **The map from the world seed** (RAND-04): draw the first items in `Begin` from `World.Random.Get("world/" + ServiceName)`,
  so runs that share a world seed share the map.
- **Nearest by the world distance** (SPACE-02), ties broken by a fixed key such as the cell index (SPACE-08).
  <xref:EvoSim.CellGrid>'s `Nearest` does both.
- **Items on walkable ground**, so animals can reach them.
- **A unique name**: diets, senses and the stream use the GameObject's name. It must differ from the other layers and
  must not be `world`, `act-order` or contain `/` (V-18).

## A cover

<xref:EvoSim.Cover> answers where animals can hide. The World combines every cover: an animal is hidden when any cover
hides it, and the hide action runs to the nearest cover point of all of them.

| Member | | What it does |
|---|---|---|
| [InCover](xref:EvoSim.Cover.InCover(UnityEngine.Vector3)) | must implement | whether a point is in cover (ENV-10) |
| [NearestCover](xref:EvoSim.Cover.NearestCover(UnityEngine.Vector3,System.Single,UnityEngine.Vector3@,System.Single@)) | must implement | the nearest point in cover within a radius, and its distance; the point itself, at 0, when already in cover |
| [Hides](xref:EvoSim.Cover.Hides(EvoSim.Species,EvoSim.Species)) | may override | whether species `hidden` is hidden from species `from`: from its threats by default, limited by `Hiding Species` |
| [IsHiddenFrom](xref:EvoSim.Cover.IsHiddenFrom(EvoSim.Animal,EvoSim.Species)) | may override | `Hides` and `InCover` at the animal's position |

A complete cover, from EvoSim's test probes, where a band of the world is in cover:

```csharp
using EvoSim;
using UnityEngine;

public class SideCover : Cover
{
    public float MinX = float.NegativeInfinity, MaxX = float.PositiveInfinity;

    public override bool InCover(Vector3 p) => p.x >= MinX && p.x <= MaxX;

    public override bool NearestCover(Vector3 p, float radius, out Vector3 point, out float distance)
    {
        point = new Vector3(Mathf.Clamp(p.x, MinX, MaxX), p.y, p.z);
        distance = Mathf.Abs(point.x - p.x);
        return distance <= radius;
    }
}
```

To hide every species from every other one, not only from its threats:
`public override bool Hides(Species hidden, Species from) => from != hidden;`

**Rules.** The same point gets the same answer for the whole run, or the answer changes only in a phase. Build a map in
`Initialize` from `World.Random.Get("world/" + ServiceName)` and draw nothing afterwards (RAND-04). Return a walkable
point from `NearestCover`, measured with the world distance. `Hides` gives the same answer for the same two species all
run long, since senses, actions and strikes all ask it (ENV-11).

## An entity kind

An <xref:EvoSim.Entity> is an object with a position that is not an animal. Its <xref:EvoSim.EntitySystem%601> keeps
the entities of one kind in creation order and finds them like animals (ENV-20).

| Member | | What it does, when it is called |
|---|---|---|
| `Entity.Kind`, `EntitySystem<T>.Kind` | must implement | the kind's name, e.g. `"apple"` |
| `Add(e)` | call | adds an entity: it gets its id and creation tick |
| `Nearest(p, radius, viewer, filter, out found, out distance)` | call | the nearest entity passing the filter, ties by id; used-up ones are skipped |
| `Move(e, t)` | may override | moves one entity, each tick in the environment phase (ENV-21); nothing by default |
| `OnRemoved(e, reason, t)` | may override | after an entity is removed, `"used up"` or `"expired"` |
| `UpdateEntities(t)` | may override, call base | the environment phase's call: `Move` each entity, count lifetimes down, remove used-up and expired ones (ENV-22) |
| `Initialize` | may override, call base | empties the list for a new run |

An entity's `LifetimeLeft` counts down once per tick (-1 = never), and setting `UsedUp` removes it in this tick's
environment phase.

### Example: apples that fall and rot

```csharp
using EvoSim;
using UnityEngine;

public class Apple : Entity
{
    public override string Kind => "apple";
}

public class AppleSystem : EntitySystem<Apple>
{
    [SerializeField, Range(0f, 1f), Tooltip("Chance per tick that an apple falls.")] float fallP = 0.2f;
    [SerializeField, Min(1), Tooltip("Ticks before an apple rots.")] int rotTicks = 200;

    public override string Kind => "apple";

    public override void UpdateEntities(TickContext t)
    {
        base.UpdateEntities(t);                                   // the old ones age and go first (ENV-22)
        var rng = t.Stream(ServiceName);                          // its own stream (RAND-02)
        if (rng.Chance(fallP))
            Add(new Apple { Position = World.Ground.RandomWalkable(rng), LifetimeLeft = rotTicks });
    }
}
```

Put `AppleSystem` on a GameObject `Apples` under `Environment`. Senses find apples with
`s.NearestEntity<Apple>(a, vision)` on the <xref:EvoSim.SenseContext>, actions with `c.NearestEntity<Apple>(a)` on the
<xref:EvoSim.ActContext> (within the animal's vision). The run views show one grey cube per apple: the
<xref:EvoSim.EntityMarkers> read every entity kind through `EntityAt`, which `EntitySystem<T>` provides.

An apple is eaten through an action with an interaction of your own ([An action](action.md#interactions)). The
interaction marks the apple used up first, so a second animal reaching it in the same tick gets nothing:

```csharp
using EvoSim;

public class EatAppleAction : AnimalAction
{
    protected override string DefaultDescription => "go to the nearest visible apple and eat it";

    public override void Act(Animal a, ActContext c)
    {
        var apple = c.NearestEntity<Apple>(a);                  // within the animal's vision
        if (apple == null) { c.Search(); return; }
        c.WalkTo(apple.Value.Position, 0f);
        c.OnArrival(new EatApple(apple.Value.Entity));
    }

    sealed class EatApple : IInteraction
    {
        readonly Apple apple;
        public EatApple(Apple a) { apple = a; }
        public float Reach => 0.5f;
        public void Apply(Animal a, InteractionContext c)
        {
            if (apple.UsedUp) return;                           // eaten by an animal that acted earlier (ENV-02)
            apple.UsedUp = true;                                // removed in this tick's environment phase
            c.Feed(a, 20f);
        }
    }
}
```

**Rules.** Entities change only in phases and interactions, never from the views (ENV-21, SPACE-12). Keep them on
walkable ground. Draw random numbers only from a named stream of `t` (RAND-02). Don't add or remove entities from
`OnRemoved`: the list is being compacted.

## A ground

<xref:EvoSim.Ground> gives every point its height and walkability, and owns the world's distance (SPACE-02, SPACE-04).

| Member | | What it does, when it is called |
|---|---|---|
| `Bounds` | must implement | the world rectangle (x, z), half-open; layers and covers lay their grids over it |
| [Height](xref:EvoSim.Ground.Height(UnityEngine.Vector3)) | must implement | the ground's height; animals and items follow it (SPACE-06) |
| [IsWalkable](xref:EvoSim.Ground.IsWalkable(UnityEngine.Vector3)) | must implement | whether an animal may stand there; checked after every move |
| [Distance](xref:EvoSim.Ground.Distance(UnityEngine.Vector3,UnityEngine.Vector3)) | may override | the world distance; every query uses it through `World.Distance` |
| `IsWater`, `NearestWater` | may override | water, for the food bonus, water senses and drinking |
| `SlopeDegrees` | may override | the slope between two points, for locomotions |
| `SlideTo`, `SideStep` | may override | how a straight move is blocked, and how a stuck animal is freed |
| `RandomWalkable` | may override | where founders and newcomers appear |

The easiest start is a subclass of <xref:EvoSim.FlatGround>. This flat ground with a pond comes from EvoSim's test
probes:

```csharp
using EvoSim;
using UnityEngine;

public class PondGround : FlatGround
{
    public Vector3 PondCenter = new Vector3(10f, 0f, 10f);
    public float PondRadius = 3f;

    public override bool IsWater(Vector3 p) => Flat(p, PondCenter) < PondRadius;
    public override bool IsWalkable(Vector3 p) => base.IsWalkable(p) && !IsWater(p);

    public override WaterHit? NearestWater(Vector3 p, float radius)
    {
        float d = Mathf.Max(0f, Flat(p, PondCenter) - PondRadius);
        if (d > radius) return null;
        var dir = new Vector3(p.x - PondCenter.x, 0f, p.z - PondCenter.z);
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
        dir.Normalize();
        return new WaterHit(PondCenter + dir * (PondRadius - 0.01f), PondCenter + dir * (PondRadius + 0.01f), d);
    }

    static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
}
```

Replace the `FlatGround` on the World's `Ground` GameObject with it: a World uses one ground.

**Rules.**

- `IsWalkable` is false outside `Bounds`: nothing ever leaves the rectangle (SPACE-03).
- The answers don't change during a run: layers and covers read them once, at initialisation.
- All walkable ground is connected, or animals get trapped in pockets (SPACE-05).
- `Distance` is never shorter than the straight horizontal distance: the cell grids' nearest-cell search relies on it.
- `SideStep` and `RandomWalkable` draw only from the stream they are given (RAND-02).
- The ground is initialised before every other service: don't look up layers or covers in its `Initialize`.

## Who eats what

Whether something is eaten is decided on both sides (SPEC-10, SPEC-13): the eaten thing carries an enabled
<xref:EvoSim.Edible>, which says how much energy it gives, and the eater's <xref:EvoSim.Diet> lists it. The form of the
**diet entry** decides how it is eaten:

| Diet entry | Eaten | The Edible goes on | Energy per meal |
|---|---|---|---|
| a layer's name: `Grass` | grazed, an item within `Graze Reach` (0.5 m) | the layer's GameObject | `Energy` per item |
| a species' id or display name: `prey` | struck within `Strike Reach` (1 m), killing with the hunter's `kill.chance` | the GameObject that holds the <xref:EvoSim.Species> | `Energy` per kill |
| `carcass:<species>`: `carcass:prey` | scavenged within `Scavenge Reach` (1 m), one portion per eater, never by the killer | the killed species' GameObject, with `Carcass Portions` above 0; the world needs a <xref:EvoSim.CarcassSystem> | `Carcass Energy` per portion |
| `egg:<species>`: `egg:prey` | eaten whole within `Graze Reach` | the <xref:EvoSim.EggSystem>'s GameObject | `Energy` per egg |

The energy is multiplied by the entry's `Energy Scale`, then passed through
[Diet.EnergyFrom](xref:EvoSim.Diet.EnergyFrom(EvoSim.Animal,EvoSim.EdibleTarget,System.Single)), which a subclass of
`Diet` can override. Reaches are settings of the act phase. The Edible's `Method` field is only a label that the
species inspector shows. Validation reports an entry that names nothing (V-20) and a target without an Edible (V-24,
with a fix). Your own entity kinds, such as the apples above, are eaten through an interaction instead.

### Recipe: make eggs edible

No code (22 §15):

1. Give the laying species an <xref:EvoSim.Incubation> with `Ticks` above 0, for example 20. With 0, eggs hatch in the
   tick they are laid and nothing can reach them.
2. Add an `Edible` to the World's `Eggs` GameObject, the one with the `EggSystem`, with `Energy` 15.
3. On the egg eater, add the diet entry `egg:prey`.
4. Give it a way to find eggs: an `EatAction` whose `Target` is `egg:prey`, and, if its brain should know, a
   <xref:EvoSim.NearestEntitySense> with `Kind` `egg`, `Of Species` `prey` and the label `Eggs`.

An eaten egg never hatches and is recorded as `egg_lost` with the reason `eaten` (REPRO-23).

### Carcasses

What a kill leaves is set on the **killed** species' Edible: `Carcass Portions` (reference 2, 0 = no carcass),
`Carcass Energy` per portion (30) and `Carcass Ticks` before it rots (100). The hunter's diet lists `carcass:prey`. The
carcass system's `Prompt Rule` is the line its prompt gets, `{portions}` replaced by the number of portions.

On every kill, the act phase calls
[CarcassSystem.Create](xref:EvoSim.CarcassSystem.Create(EvoSim.Animal,System.Int32)) on the World's first carcass
system. It is virtual: override it to change what a kill leaves, and keep the base call, which fills the carcass in and
adds it. The system reuses removed carcasses, so an override of `OnRemoved` must call the base too.

```csharp
public class LeanCarcasses : CarcassSystem
{
    public override Carcass Create(Animal killed, int killerId)
    {
        var c = base.Create(killed, killerId);
        if (c != null && killed.Age < 100) c.PortionsLeft = 1;     // a young animal feeds one eater
        return c;
    }
}
```

## Test it

Every `ResourceLayer` class in the project, yours included, runs through the **T-ENV layer template** of the
conformance suite (*Window ▸ General ▸ Test Runner*, EditMode, `ConformanceTests` ▸ `LayerConformance`). It puts your
layer on a 30 × 30 m world as `Grass`, with an Edible, and checks that it starts with items, that `Nearest` finds one
within the radius, and that an item is consumed once. A layer that starts empty fails it.

For your own checks, build a small world in code ([Testing your module](testing-your-module.md)):

```csharp
using EvoSim;
using EvoSim.Testing;
using NUnit.Framework;

public class BerryBushesTests : WorldFixture
{
    [Test]
    public void BerriesAreEatenOnceAndGrowBack()
    {
        var w = New().Flat(40, 40).Phase<EnvironmentPhase>().Service<BerryBushes>("Berries").Build();
        var bushes = w.Service<ResourceLayer>("Berries");
        int start = bushes.Count;
        Assert.IsTrue(bushes.Nearest(Place.At(20, 20), 100f, out var berry));
        Assert.IsTrue(bushes.Consume(berry));
        Assert.IsFalse(bushes.Consume(berry), "eaten once (ENV-02)");
        w.Advance(1000);
        Assert.AreEqual(start, bushes.Count, "the berry grew back");
    }
}
```

`WorldBuilder.Ground<T>(width, depth)` builds a world on a `FlatGround` subclass of yours. The tests in
`Assets/EvoSim/Tests/EditMode/Environment/EnvironmentTests.cs` check covers, carcasses and a replaced distance against
brute-force searches: copy the one closest to your module.

## Read next

- Reference modules: <xref:EvoSim.FoodGrid>, <xref:EvoSim.CoverLayer>, <xref:EvoSim.CarcassSystem>,
  <xref:EvoSim.EggSystem>, <xref:EvoSim.TerrainGround>.
- [The samples](samples.md): seasons that slow the grass, water senses and drinking on a ground with water.
- [A sense](sense.md) and [An action or an interaction](action.md): let animals see and use what you added.
- [Phases, services and world hooks](world-modules.md): services, `Initialize` and `Begin`.
- Specification: [03 — Environment](../spec/03-environment.md).
