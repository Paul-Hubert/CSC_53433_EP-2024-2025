using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// What an action may do in one tick (ACT-06): ask questions about what the animal could sense, and say where it
    /// wants to go (an intent) and what to do on arrival (an interaction). It can't move or change any animal itself.
    /// </summary>
    public sealed class ActContext
    {
        static readonly float[] TurnAngles = { -90f, -45f, 45f, 90f };

        Animal current;

        public World World { get; }
        public Ground Ground => World.Ground;
        public int Tick => World.Tick;
        /// <summary>Within this distance of a food item the animal grazes it (ACT-10).</summary>
        public float GrazeReach { get; set; } = 0.5f;

        /// <summary>The intent planned so far for the current animal (stay by default).</summary>
        public Intent Intent { get; private set; }
        /// <summary>The interaction planned so far (none by default).</summary>
        public Interaction Interaction { get; private set; }

        /// <summary>One context per act phase (or per test); it is reused for every animal.</summary>
        public ActContext(World world) { World = world; }

        /// <summary>Starts planning for an animal: stay and no interaction until the action says otherwise.</summary>
        public void Begin(Animal a)
        {
            current = a;
            Intent = Intent.StayAt(a.Position);
            Interaction = default;
            TargetAnimal = null;
        }

        /// <summary>The animal the intent is about (followed, fled, chased), or null; recorded for the inspector.</summary>
        public Animal TargetAnimal { get; private set; }

        /// <summary>What was planned for the current animal: its intent, interaction and target animal.</summary>
        public ActPlan Plan => new ActPlan(Intent, Interaction, TargetAnimal);

        // ---- Queries: only what the animal's senses could report (ACT-05) ----

        /// <summary>The animal's vision, in meters (SENSE-22).</summary>
        public float Vision(Animal a) => World.Queries.Vision(a);

        /// <summary>The nearest visible animal of a set (threats, prey, kin, listed), within vision.</summary>
        public AnimalHit? NearestAnimal(Animal a, AnimalSet set, IReadOnlyList<Species> listed = null) =>
            World.Queries.NearestAnimal(a, set, listed);

        /// <summary>The nearest visible kin that is ready to mate, within vision.</summary>
        public AnimalHit? NearestReadyPartner(Animal a) => World.Queries.NearestReadyKin(a);

        /// <summary>The nearest item of a layer the diet grazes (empty name: any such layer), within vision.</summary>
        public ResourceItem? NearestResource(Animal a, string layerName = "") =>
            World.Queries.NearestResource(a, layerName, Vision(a));

        /// <summary>The nearest carcass this animal may eat from, within vision.</summary>
        public EntityHit<Carcass>? NearestCarcass(Animal a) => World.Queries.NearestCarcass(a, Vision(a));

        /// <summary>The nearest egg (of a species, or any) the diet eats, within vision.</summary>
        public EntityHit<Egg>? NearestEgg(Animal a, Species of = null) => World.Queries.NearestEgg(a, of, Vision(a));
        /// <summary>The nearest entity of a kind of your own within the animal's vision (see WorldQueries.NearestEntity).</summary>
        public EntityHit<T>? NearestEntity<T>(Animal a, System.Func<T, Animal, bool> filter = null) where T : Entity =>
            World.Queries.NearestEntity(a, Vision(a), filter);

        /// <summary>The nearest cover within vision (or a given radius); distance 0 when in cover.</summary>
        public CoverHit? NearestCover(Animal a, float radius = -1f) =>
            World.Queries.NearestCover(a, radius >= 0f ? radius : Vision(a));

        public bool InCover(Animal a) => World.Queries.InCover(a);

        /// <summary>Whether another animal is ready to mate (REPRO-01).</summary>
        public bool IsReady(Animal other) => World.Queries.IsReady(other);

        // ---- Intents (ACT-03) ----

        public void WalkTo(Vector3 target, float stopAt = 0f) => Toward(target, stopAt, false);
        public void RunTo(Vector3 target, float stopAt = 0f) => Toward(target, stopAt, true);

        void Toward(Vector3 target, float stopAt, bool run)
        {
            float d = World.Distance(current.Position, target);
            Intent = Intent.Toward(current.Position, target, stopAt, run, d);
        }

        /// <summary>Walk toward an animal (follow, mate), stopping at a distance.</summary>
        public void WalkTo(Animal target, float stopAt) { WalkTo(target.Position, stopAt); TargetAnimal = target; }

        /// <summary>Run toward an animal (hunt), stopping at a distance.</summary>
        public void RunTo(Animal target, float stopAt) { RunTo(target.Position, stopAt); TargetAnimal = target; }

        /// <summary>Run away from an animal (flee).</summary>
        public void RunAwayFrom(Animal threat) { RunAwayFrom(threat.Position); TargetAnimal = threat; }

        public void RunAwayFrom(Vector3 threat) => Intent = Intent.AwayFrom(current.Position, threat, true, HeadingDirection(current.Heading));
        public void WalkAwayFrom(Vector3 threat) => Intent = Intent.AwayFrom(current.Position, threat, false, HeadingDirection(current.Heading));
        public void Stay() => Intent = Intent.StayAt(current.Position);

        /// <summary>
        /// A persistent random walk (MOVE-05): keep the heading; with probability wanderTurnP turn by ±45° or ±90°;
        /// blocked, pick a new random heading. Walks. Draws from the species' actions stream.
        /// </summary>
        public void Wander()
        {
            var a = current;
            var loco = a.Species.Module<Locomotion>();
            var rng = World.Random.For(a.Species, "actions");
            float turnP = loco != null ? loco.WanderTurnP : 0.25f;
            if (rng.Chance(turnP)) a.Heading = Normalize(a.Heading + TurnAngles[rng.Range(0, 4)]);
            float step = loco != null ? Mathf.Max(0.5f, a.Trait(loco.WalkSpeed)) : 1f;
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
        public void Search()
        {
            if (!current.Searching)
            {
                current.Searching = true;
                current.Species.Counters.Invalid++;
            }
            Wander();
        }

        /// <summary>What to do when within reach after moving (ACT-10).</summary>
        public void OnArrival(Interaction i) => Interaction = i;

        /// <summary>A custom interaction (22 §3), applied within its reach of the point walked or run to.</summary>
        public void OnArrival(IInteraction custom) => Interaction = Interaction.Of(custom);

        /// <summary>The unit direction of a heading in degrees (0 = +z, 90 = +x).</summary>
        public static Vector3 HeadingDirection(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        static float Normalize(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }
    }

    /// <summary>One animal's plan for a tick (ActContext.Plan): the batch entry point AnimalAction.ActAll writes these.</summary>
    public readonly struct ActPlan
    {
        public readonly Intent Intent;
        public readonly Interaction Interaction;
        public readonly Animal TargetAnimal;

        public ActPlan(Intent intent, Interaction interaction, Animal targetAnimal)
        {
            Intent = intent; Interaction = interaction; TargetAnimal = targetAnimal;
        }

        /// <summary>Stay where it is, no interaction (busy animals, animals without an action).</summary>
        public static ActPlan Stay(Animal a) => new ActPlan(Intent.StayAt(a.Position), default, null);
    }
}
