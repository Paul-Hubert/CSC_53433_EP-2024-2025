using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What an action may do in one tick (ACT-06): ask questions about what the animal could sense, and say where it
    /// wants to go (an intent) and what to do on arrival (an interaction). It can't move or change any animal itself.
    /// </summary>
    /// <remarks>
    /// <para>The act phase owns one context and reuses it for every animal. For each animal it calls <see cref="Begin"/>,
    /// then the chosen action's <see cref="AnimalAction.Act"/>, then reads <see cref="Plan"/>. Afterwards the species'
    /// <see cref="Locomotion"/> moves the animal, and the interaction applies only if the animal is within reach after
    /// moving (ACT-10).</para>
    /// <para>The queries (<c>Nearest…</c>, <see cref="InCover"/>, <see cref="IsReady"/>) only return what the animal's
    /// senses could report: within its vision, never itself, never a killed animal, never an animal hidden from it in
    /// cover (ACT-05). In the sequential act orders they see the state left by the animals that acted before; in the
    /// simultaneous order, the state at the start of the act phase (ACT-31).</para>
    /// <para>The intent methods (<c>WalkTo</c>, <c>RunTo</c>, <c>RunAwayFrom</c>, <c>WalkAwayFrom</c>, <see cref="Stay"/>,
    /// <see cref="Wander"/>, <see cref="Search"/>) replace each other: the last one called is the plan. The two
    /// <c>OnArrival</c> methods set the interaction the same way.</para>
    /// </remarks>
    /// <example>
    /// The grazing part of the reference <see cref="EatAction"/>:
    /// <code>
    /// public override void Act(Animal a, ActContext c)
    /// {
    ///     var food = c.NearestResource(a);                      // within vision, diet-checked
    ///     if (food == null) { c.Search(); return; }             // nothing in sight: search (ACT-04)
    ///     c.WalkTo(food.Value.Position, stopAt: 0f);            // MOVE-04
    ///     c.OnArrival(Interaction.Graze(food.Value));           // applied after moving, within reach (ACT-10)
    /// }
    /// </code>
    /// </example>
    public sealed class ActContext
    {
        static readonly float[] TurnAngles = { -90f, -45f, 45f, 90f };   // the turns of a wandering animal, in degrees (MOVE-05)

        Animal current;   // the animal being planned, set by Begin

        /// <summary>The world being simulated: its species, services, queries and random streams.</summary>
        public World World { get; }
        /// <summary>The world's ground (heights, walkable points, water), or null in a world without one.</summary>
        public Ground Ground => World.Ground;
        /// <summary>The current tick of the world.</summary>
        public int Tick => World.Tick;
        /// <summary>
        /// Within this distance of a food item the animal grazes it, in meters (ACT-10; reference 0.5). The act phase
        /// copies its own graze reach here; an action reads it to tell that the animal already stands on its food.
        /// </summary>
        public float GrazeReach { get; set; } = 0.5f;

        /// <summary>The intent planned so far for the current animal: a stay until the action calls an intent method.</summary>
        public Intent Intent { get; private set; }
        /// <summary>The interaction planned so far for the current animal: none until the action calls <c>OnArrival</c>.</summary>
        public Interaction Interaction { get; private set; }

        /// <summary>
        /// Creates a context for a world. The act phase makes one and reuses it for every animal; a test, or a student's
        /// own act phase, can make its own.
        /// </summary>
        /// <param name="world">The world whose ground, queries and random streams the context uses.</param>
        public ActContext(World world) { World = world; }

        /// <summary>Starts planning for an animal: stay and no interaction until the action says otherwise.</summary>
        /// <param name="a">The animal about to act.</param>
        /// <remarks>
        /// <see cref="AnimalAction.ActAll"/> calls it before each <see cref="AnimalAction.Act"/>, and the act phase calls
        /// it for busy animals. Call it yourself only when you drive an action from your own code, such as a test.
        /// </remarks>
        public void Begin(Animal a)
        {
            current = a;
            Intent = Intent.StayAt(a.Position);
            Interaction = default;
            TargetAnimal = null;
        }

        /// <summary>
        /// The animal the intent is about (followed, fled, chased), or null. The act phase records it as the animal's
        /// <see cref="Animal.TargetId"/>, for the inspector and for senses such as the sample <c>ChasedSense</c>.
        /// </summary>
        public Animal TargetAnimal { get; private set; }

        /// <summary>What was planned for the current animal: its intent, interaction and target animal.</summary>
        public ActPlan Plan => new ActPlan(Intent, Interaction, TargetAnimal);

        // ---- Queries: only what the animal's senses could report (ACT-05) ----

        /// <summary>The animal's vision, in meters (SENSE-22): its <c>vision</c> trait, or 20 m for a species without one.</summary>
        /// <param name="a">The animal that looks.</param>
        /// <returns>The radius within which the queries find things, in meters.</returns>
        public float Vision(Animal a) => World.Queries.Vision(a);

        /// <summary>
        /// The nearest visible animal of a set (threats, prey, kin, listed), within vision: never itself, never a killed
        /// animal, never one hidden from it in cover (ACT-05).
        /// </summary>
        /// <param name="a">The animal that looks.</param>
        /// <param name="set">
        /// Whom to look for: the species' threats, prey or kin, derived from the food web (SPEC-12), or the listed species.
        /// </param>
        /// <param name="listed">
        /// The species to look among when <paramref name="set"/> is <see cref="AnimalSet.Listed"/>; ignored otherwise.
        /// </param>
        /// <returns>The animal found and its distance in meters, or null when none is in sight.</returns>
        /// <example>
        /// The reference <see cref="FleeAction"/>:
        /// <code>
        /// var threat = c.NearestAnimal(a, AnimalSet.Threats);
        /// if (threat == null) { c.Search(); return; }
        /// c.RunAwayFrom(threat.Value.Animal);
        /// </code>
        /// </example>
        public AnimalHit? NearestAnimal(Animal a, AnimalSet set, IReadOnlyList<Species> listed = null) =>
            World.Queries.NearestAnimal(a, set, listed);

        /// <summary>
        /// The nearest visible kin that is ready to mate (REPRO-01), within vision and within the partner range of the
        /// species' kin sense that reports readiness: the action never targets what the senses can't report (ACT-05).
        /// </summary>
        /// <param name="a">The animal that looks for a partner.</param>
        /// <returns>The partner and its distance in meters, or null when none is in sight.</returns>
        public AnimalHit? NearestReadyPartner(Animal a) => World.Queries.NearestReadyKin(a);

        /// <summary>
        /// The nearest item of a layer the diet grazes, within vision; the diet and the layer's <see cref="Edible"/> are
        /// checked (SPEC-13).
        /// </summary>
        /// <param name="a">The animal that looks for food.</param>
        /// <param name="layerName">A layer's name, or empty for every layer the diet grazes.</param>
        /// <returns>The item (its layer, cell, position and distance), or null when none is in sight.</returns>
        public ResourceItem? NearestResource(Animal a, string layerName = "") =>
            World.Queries.NearestResource(a, layerName, Vision(a));

        /// <summary>
        /// The nearest carcass this animal may eat from, within vision: its diet scavenges that species' carcasses,
        /// portions are left, it is not the killer and it has not eaten from it yet.
        /// </summary>
        /// <param name="a">The animal that looks for a carcass.</param>
        /// <returns>The carcass and its distance in meters, or null when none is in sight.</returns>
        public EntityHit<Carcass>? NearestCarcass(Animal a) => World.Queries.NearestCarcass(a, Vision(a));

        /// <summary>The nearest egg (of a species, or any) the diet eats (REPRO-23), within vision.</summary>
        /// <param name="a">The animal that looks for eggs.</param>
        /// <param name="of">Only eggs of this species, or null for every species whose eggs the diet eats.</param>
        /// <returns>The egg and its distance in meters, or null when none is in sight.</returns>
        public EntityHit<Egg>? NearestEgg(Animal a, Species of = null) => World.Queries.NearestEgg(a, of, Vision(a));
        /// <summary>
        /// The nearest entity of a kind of your own (fruit, nests…) within the animal's vision, across every entity system
        /// of that type; used-up entities are skipped and ties go to the lower id (SPACE-07, SPACE-08).
        /// </summary>
        /// <typeparam name="T">The entity class, a subclass of <see cref="Entity"/>.</typeparam>
        /// <param name="a">The animal that looks.</param>
        /// <param name="filter">
        /// Receives an entity and the looking animal and returns true to keep it; null keeps every entity. No diet is
        /// checked for you: check it here if the entity is food.
        /// </param>
        /// <returns>The entity and its distance in meters, or null when none is in sight.</returns>
        public EntityHit<T>? NearestEntity<T>(Animal a, System.Func<T, Animal, bool> filter = null) where T : Entity =>
            World.Queries.NearestEntity(a, Vision(a), filter);

        /// <summary>The nearest cover within vision (or a given radius); distance 0 when the animal stands in cover (ENV-10).</summary>
        /// <param name="a">The animal that looks for cover.</param>
        /// <param name="radius">The search radius in meters; negative (the default) uses the animal's vision.</param>
        /// <returns>A point in cover and its distance, or null when none is in reach or the world has no cover module (ENV-12).</returns>
        public CoverHit? NearestCover(Animal a, float radius = -1f) =>
            World.Queries.NearestCover(a, radius >= 0f ? radius : Vision(a));

        /// <summary>True when the animal stands in cover of any cover module (ENV-10).</summary>
        /// <param name="a">The animal to check, usually the one that acts.</param>
        /// <returns>True when its position is in cover; always false in a world without cover (ENV-12).</returns>
        public bool InCover(Animal a) => World.Queries.InCover(a);

        /// <summary>Whether another animal is ready to mate (REPRO-01), by its species' mating rule; never without one.</summary>
        /// <param name="other">Any animal, usually one a query found.</param>
        /// <returns>True when it is ready to mate.</returns>
        public bool IsReady(Animal other) => World.Queries.IsReady(other);

        // ---- Intents (ACT-03) ----

        /// <summary>
        /// Walks toward a point and stops at a distance from it (MOVE-04): the locomotion never overshoots the stop
        /// distance. Already within it, the animal stays.
        /// </summary>
        /// <param name="target">The point to reach.</param>
        /// <param name="stopAt">How far from the point to stop, in meters; 0 goes onto it.</param>
        public void WalkTo(Vector3 target, float stopAt = 0f) => Toward(target, stopAt, false);
        /// <summary>
        /// Runs toward a point at the species' running speed and stops at a distance from it (MOVE-04). Each meter costs
        /// the same stamina as walking (ANIM-21); running only covers more meters per tick.
        /// </summary>
        /// <param name="target">The point to reach.</param>
        /// <param name="stopAt">How far from the point to stop, in meters; 0 goes onto it.</param>
        public void RunTo(Vector3 target, float stopAt = 0f) => Toward(target, stopAt, true);

        /// <summary>Plans a straight intent from the current animal toward a point, measured with the world distance (SPACE-02).</summary>
        void Toward(Vector3 target, float stopAt, bool run)
        {
            float d = World.Distance(current.Position, target);
            Intent = Intent.Toward(current.Position, target, stopAt, run, d);
        }

        /// <summary>Walks toward an animal (follow, mate), stopping at a distance, and records it as the target animal.</summary>
        /// <param name="target">The animal to reach; its position now is the point walked to.</param>
        /// <param name="stopAt">How far from it to stop, in meters.</param>
        public void WalkTo(Animal target, float stopAt) { WalkTo(target.Position, stopAt); TargetAnimal = target; }

        /// <summary>Runs toward an animal (hunt), stopping at a distance, and records it as the target animal.</summary>
        /// <param name="target">The animal to reach; its position now is the point run to.</param>
        /// <param name="stopAt">How far from it to stop, in meters.</param>
        public void RunTo(Animal target, float stopAt) { RunTo(target.Position, stopAt); TargetAnimal = target; }

        /// <summary>Runs straight away from an animal (flee) and records it as the target animal.</summary>
        /// <param name="threat">The animal to run from.</param>
        public void RunAwayFrom(Animal threat) { RunAwayFrom(threat.Position); TargetAnimal = threat; }

        /// <summary>
        /// Runs straight away from a point, as far as speed and stamina allow. Standing on the point itself, the animal
        /// runs along its heading.
        /// </summary>
        /// <param name="threat">The point to run from.</param>
        public void RunAwayFrom(Vector3 threat) => Intent = Intent.AwayFrom(current.Position, threat, true, HeadingDirection(current.Heading));
        /// <summary>Walks straight away from a point, as far as speed and stamina allow.</summary>
        /// <param name="threat">The point to walk from.</param>
        public void WalkAwayFrom(Vector3 threat) => Intent = Intent.AwayFrom(current.Position, threat, false, HeadingDirection(current.Heading));
        /// <summary>Stays where it is this tick (rest, stay in cover, already on the food).</summary>
        public void Stay() => Intent = Intent.StayAt(current.Position);

        /// <summary>
        /// A persistent random walk (MOVE-05): keep the heading; with probability <see cref="Locomotion.WanderTurnP"/>
        /// turn by ±45° or ±90°; blocked, pick a new random heading. Walks. Draws from the species' <c>actions</c> stream.
        /// </summary>
        /// <remarks>
        /// It turns the current animal's own heading. Without a locomotion it turns with probability 0.25. When the
        /// action has nothing to act on, call <see cref="Search"/> instead, so that the search is counted (ACT-04).
        /// </remarks>
        public void Wander()
        {
            var a = current;
            var loco = a.Species.Module<Locomotion>();
            var rng = World.Random.For(a.Species, "actions");
            float turnP = loco != null ? loco.WanderTurnP : 0.25f;
            if (rng.Chance(turnP)) a.Heading = Normalize(a.Heading + TurnAngles[rng.Range(0, 4)]);
            float step = loco != null ? Mathf.Max(0.5f, a.Trait(loco.WalkSpeed)) : 1f;   // one stride ahead decides "blocked"
            var ground = Ground;
            for (int tries = 0; ground != null && tries < 8; tries++)
            {
                if (ground.Clear(a.Position, a.Position + HeadingDirection(a.Heading) * step)) break;
                a.Heading = rng.Range(0f, 360f);                                     // blocked: a new random heading
            }
            var dir = HeadingDirection(a.Heading);
            Intent = new Intent(dir, false, float.PositiveInfinity, a.Position + dir, wandering: true);
        }

        /// <summary>Nothing to act on in sight: wander, counted as searching once per decision (ACT-04).</summary>
        /// <remarks>
        /// The first call after a decision sets the animal's <see cref="Animal.Searching"/> flag and adds one to its
        /// species' <see cref="SpeciesCounters.Invalid"/> count (the prototype's "invalid"); the next decision clears the
        /// flag. Every action that can find nothing should call it rather than <see cref="Wander"/>.
        /// </remarks>
        public void Search()
        {
            if (!current.Searching)
            {
                current.Searching = true;
                current.Species.Counters.Invalid++;
            }
            Wander();
        }

        /// <summary>
        /// Sets what to do on arrival: graze, strike, scavenge or eat an egg. It applies after moving, in the same tick,
        /// only when the animal is within reach and the diet allows it (ACT-10, SPEC-13).
        /// </summary>
        /// <param name="i">
        /// An interaction built with <see cref="EvoSim.Interaction.Graze"/>, <see cref="EvoSim.Interaction.Strike"/>,
        /// <see cref="EvoSim.Interaction.Scavenge"/> or <see cref="EvoSim.Interaction.EatEgg"/>.
        /// </param>
        public void OnArrival(Interaction i) => Interaction = i;

        /// <summary>
        /// Sets a custom interaction (22 §3). It applies after moving when the animal is within its
        /// <see cref="IInteraction.Reach"/> of the point the intent was heading to (<see cref="EvoSim.Intent.Target"/>).
        /// </summary>
        /// <param name="custom">The contact effect of your own.</param>
        public void OnArrival(IInteraction custom) => Interaction = Interaction.Of(custom);

        /// <summary>The unit direction of a heading in degrees (0 = +z, 90 = +x).</summary>
        /// <param name="degrees">A heading, in degrees clockwise from +z when seen from above.</param>
        /// <returns>A unit vector on the horizontal plane.</returns>
        public static Vector3 HeadingDirection(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        /// <summary>Brings an angle into [0, 360) degrees.</summary>
        static float Normalize(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }
    }

    /// <summary>
    /// One animal's plan for a tick (<see cref="ActContext.Plan"/>): what <see cref="AnimalAction.ActAll"/> writes for each
    /// animal of its group, and what the act phase then moves and applies.
    /// </summary>
    public readonly struct ActPlan
    {
        /// <summary>Where the animal wants to go this tick.</summary>
        public readonly Intent Intent;
        /// <summary>What to do on arrival, or none.</summary>
        public readonly Interaction Interaction;
        /// <summary>The animal the plan is about (followed, fled, chased), or null.</summary>
        public readonly Animal TargetAnimal;

        /// <summary>Creates a plan.</summary>
        /// <param name="intent">Where the animal wants to go this tick.</param>
        /// <param name="interaction">What to do on arrival, or <see cref="EvoSim.Interaction.None"/>.</param>
        /// <param name="targetAnimal">The animal the plan is about, or null.</param>
        public ActPlan(Intent intent, Interaction interaction, Animal targetAnimal)
        {
            Intent = intent; Interaction = interaction; TargetAnimal = targetAnimal;
        }

        /// <summary>Stay where it is, no interaction (busy animals, animals without an action).</summary>
        /// <param name="a">The animal that stays.</param>
        /// <returns>A plan with a stay intent at the animal's position and no interaction.</returns>
        public static ActPlan Stay(Animal a) => new ActPlan(Intent.StayAt(a.Position), default, null);
    }
}
