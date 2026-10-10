using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Every living animal acts, in the act order (ACT-30): its action plans an intent and an interaction, its species'
    /// locomotion moves it, interactions apply within reach, then its metabolism is paid and busy counters drop
    /// (TICK-04 #4, ANIM-20, ANIM-30).
    /// </summary>
    public class ActPhase : TickPhase
    {
        [SerializeField, Tooltip("Species in turn (reference), all mixed, or simultaneous (ACT-30).")]
        ActOrder order = ActOrder.SpeciesInTurn;
        [SerializeField, Min(0f), Tooltip("Graze reach: an item within this distance is eaten, in meters (reference 0.5; ACT-10).")]
        float grazeReach = 0.5f;
        [SerializeField, Min(0f), Tooltip("Strike reach, in meters (reference 1; ACT-10).")]
        float strikeReach = 1f;
        [SerializeField, Min(0f), Tooltip("Scavenge reach, in meters (reference 1; ACT-10).")]
        float scavengeReach = 1f;

        readonly List<Animal> actors = new List<Animal>();
        Intent[] intents = new Intent[16];
        Interaction[] interactions = new Interaction[16];
        bool[] busyAtStart = new bool[16];
        float[] moved = new float[16];
        Vector3[] from = new Vector3[16];
        int[] order2 = new int[16];
        ActContext act;
        MoveContext move;
        InteractionResolver resolver;

        public ActOrder Order { get => order; set => order = value; }
        public float GrazeReach => grazeReach;
        public float StrikeReach => strikeReach;
        public float ScavengeReach => scavengeReach;

        public override void Initialize()
        {
            act = new ActContext(World) { GrazeReach = grazeReach };
            move = new MoveContext(World);
            resolver = new InteractionResolver(World) { GrazeReach = grazeReach, StrikeReach = strikeReach, ScavengeReach = scavengeReach };
            int most = 0;                                                            // room for every animal the caps allow (R-07)
            foreach (var s in World.AllSpecies)
            {
                var cap = s.Module<CapRule>();
                most += cap != null ? cap.Cap : 2 * s.InitialPopulation;
            }
            if (actors.Capacity < most) actors.Capacity = most;
            if (order != ActOrder.SpeciesInTurn) Ensure(most);
        }

        public override void Run(TickContext t)
        {
            World.Space.Rebuild();
            var rng = t.Stream(RandomStreams.ActOrderStream);                  // ACT-32
            var all = World.AllSpecies;                                          // index loops: no enumerator per tick (R-07)
            for (int si = 0; si < all.Count; si++)
            {
                var animals = all[si].Animals;
                for (int i = 0; i < animals.Count; i++) { animals[i].StruckThisTick = false; animals[i].MetersMoved = 0f; }
            }

            switch (order)
            {
                case ActOrder.SpeciesInTurn:
                    for (int si = 0; si < all.Count; si++)
                    {
                        Living(all[si], actors);
                        rng.Shuffle(actors);
                        ActInTurn();
                    }
                    break;
                case ActOrder.AllMixed:
                    actors.Clear();
                    for (int si = 0; si < all.Count; si++) Append(all[si], actors);
                    rng.Shuffle(actors);
                    ActInTurn();
                    break;
                default:
                    actors.Clear();
                    for (int si = 0; si < all.Count; si++) Append(all[si], actors);
                    ActTogether(rng);
                    break;
            }
        }

        /// <summary>Sequential orders: each animal acts on the state left by those before it (ACT-31).</summary>
        void ActInTurn()
        {
            for (int k = 0; k < actors.Count; k++)
            {
                var a = actors[k];
                if (a.Killed || a.IsGone) continue;                                  // killed earlier this tick (ANIM-41)
                Ensure(1);
                Plan(a, 0);
                Move(a, 0);
                if (!interactions[0].IsNone) resolver.Apply(a, interactions[0], intents[0].Target);
                Settle(a, 0);
            }
        }

        /// <summary>
        /// Simultaneous: every intent from the start state, each action planning its animals as a group; each species'
        /// locomotion moves its animals as a group; interactions in a fresh random order (ACT-31, ARCH-12).
        /// </summary>
        void ActTogether(RandomStream rng)
        {
            int n = actors.Count;
            Ensure(n);
            PlanTogether(n);
            MoveTogether(n);
            for (int i = 0; i < n; i++) order2[i] = i;
            for (int i = n - 1; i > 0; i--) { int j = rng.Range(0, i + 1); (order2[i], order2[j]) = (order2[j], order2[i]); }
            for (int k = 0; k < n; k++)
            {
                int i = order2[k];
                if (!interactions[i].IsNone) resolver.Apply(actors[i], interactions[i], intents[i].Target);
            }
            for (int i = 0; i < n; i++)
                if (!actors[i].Killed) Settle(actors[i], i);
        }

        // ---- The batch entry points (20 §10, ARCH-12): actions plan and locomotions move groups of animals ----

        readonly Animal[] one = new Animal[1];
        readonly ActPlan[] onePlan = new ActPlan[1];
        readonly Intent[] oneIntent = new Intent[1];
        readonly List<object> groupOwners = new List<object>();                     // an action or a locomotion, in order of first use
        readonly List<List<int>> groupMembers = new List<List<int>>();
        readonly List<Animal> groupAnimals = new List<Animal>();
        ActPlan[] groupPlans = new ActPlan[16];
        Intent[] groupIntents = new Intent[16];
        float[] groupMoved = new float[16];

        /// <summary>The animal's action gives an intent and an interaction; busy animals and animals without an action stay.</summary>
        void Plan(Animal a, int i)
        {
            Count(a, i);
            var action = a.CurrentAction;
            if (a.IsBusy || action == null) { act.Begin(a); Apply(a, i, ActPlan.Stay(a)); return; }   // ANIM-30: busy animals don't act
            one[0] = a;
            action.ActAll(one, act, onePlan);
            Apply(a, i, onePlan[0]);
        }

        void PlanTogether(int n)
        {
            ClearGroups();
            for (int i = 0; i < n; i++)
            {
                var a = actors[i];
                Count(a, i);
                var action = a.CurrentAction;
                if (a.IsBusy || action == null) Apply(a, i, ActPlan.Stay(a));
                else GroupOf(action).Add(i);
            }
            for (int g = 0; g < groupOwners.Count; g++)
            {
                var members = Gather(g);
                if (groupPlans.Length < members.Count) groupPlans = new ActPlan[Math.Max(members.Count, groupPlans.Length * 2)];
                ((AnimalAction)groupOwners[g]).ActAll(groupAnimals, act, new Span<ActPlan>(groupPlans, 0, members.Count));
                for (int k = 0; k < members.Count; k++) Apply(actors[members[k]], members[k], groupPlans[k]);
            }
        }

        void Count(Animal a, int i)
        {
            var counters = a.Species.Counters;
            counters.AnimalTicks++;
            var stamina = a.Species.Module<Stamina>();
            if (stamina != null && stamina.Budget(a) < Units.Epsilon) counters.Exhausted++;
            busyAtStart[i] = a.IsBusy;
        }

        void Apply(Animal a, int i, ActPlan plan)
        {
            intents[i] = plan.Intent;
            interactions[i] = plan.Interaction;
            var target = interactions[i].Kind == InteractionKind.Strike ? interactions[i].Target : plan.TargetAnimal;
            a.TargetId = a.IsBusy || target == null ? -1 : target.Id;
            a.TargetPoint = intents[i].Target;
        }

        void MoveTogether(int n)
        {
            ClearGroups();
            for (int i = 0; i < n; i++)
            {
                from[i] = actors[i].Position;
                moved[i] = 0f;
                var loco = actors[i].Species.Module<Locomotion>();
                if (loco != null && !intents[i].IsStay) GroupOf(loco).Add(i);
            }
            for (int g = 0; g < groupOwners.Count; g++)
            {
                var members = Gather(g);
                int m = members.Count;
                if (groupIntents.Length < m) { groupIntents = new Intent[Math.Max(m, groupIntents.Length * 2)]; groupMoved = new float[groupIntents.Length]; }
                for (int k = 0; k < m; k++) groupIntents[k] = intents[members[k]];
                var loco = (Locomotion)groupOwners[g];
                loco.MoveAll(groupAnimals, new ArraySegment<Intent>(groupIntents, 0, m), move, new Span<float>(groupMoved, 0, m));
                for (int k = 0; k < m; k++)
                {
                    moved[members[k]] = groupMoved[k];
                    AfterMove(actors[members[k]], members[k], loco);
                }
            }
        }

        void ClearGroups()
        {
            foreach (var list in groupMembers) list.Clear();
            groupOwners.Clear();
        }

        List<int> GroupOf(object owner)
        {
            for (int g = 0; g < groupOwners.Count; g++) if (groupOwners[g] == owner) return groupMembers[g];
            groupOwners.Add(owner);
            if (groupMembers.Count < groupOwners.Count) groupMembers.Add(new List<int>());
            return groupMembers[groupOwners.Count - 1];
        }

        List<int> Gather(int g)
        {
            var members = groupMembers[g];
            groupAnimals.Clear();
            for (int k = 0; k < members.Count; k++) groupAnimals.Add(actors[members[k]]);
            return members;
        }

        /// <summary>The species' locomotion moves it; whatever it does is the result (MOVE-01), never off walkable ground (MOVE-02).</summary>
        void Move(Animal a, int i)
        {
            from[i] = a.Position;
            moved[i] = 0f;
            var loco = a.Species.Module<Locomotion>();
            if (loco == null || intents[i].IsStay) return;
            one[0] = a;
            oneIntent[0] = intents[i];
            loco.MoveAll(one, oneIntent, move, new Span<float>(moved, i, 1));
            AfterMove(a, i, loco);
        }

        void AfterMove(Animal a, int i, Locomotion loco)
        {
            var ground = World.Ground;
            if (ground != null && !ground.IsWalkable(a.Position))
            {
                Debug.LogWarning($"EvoSim: {loco.GetType().Name} left {a} on non-walkable ground; moved back (MOVE-02).", loco);
                a.Position = from[i];
                moved[i] = 0f;
            }
            a.MetersMoved = moved[i];
            if (moved[i] > Units.Epsilon)
            {
                World.Space.Moved(a);
                if (!intents[i].Wandering)
                {
                    var d = a.Position - from[i];
                    a.Heading = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                }
            }
        }

        /// <summary>Metabolism and stamina (ANIM-20, ANIM-21), then the busy countdown (ANIM-30).</summary>
        void Settle(Animal a, int i)
        {
            var loco = a.Species.Module<Locomotion>();
            float extra = loco != null && moved[i] > Units.Epsilon ? loco.ExtraCost(a, from[i], a.Position) : 0f;
            var metabolism = a.Species.Module<Metabolism>();
            if (metabolism != null) metabolism.Settle(a, moved[i], extra, busyAtStart[i]);
            else a.Species.Module<Stamina>()?.Settle(a, moved[i], a.Species.Module<Energy>());
            if (busyAtStart[i] && a.BusyTicks > 0 && --a.BusyTicks == 0)
            {
                a.Action = -1;                                                   // decides at the start of the next tick
                a.BusyReason = null;
            }
        }

        static void Living(Species s, List<Animal> result)
        {
            result.Clear();
            Append(s, result);
        }

        static void Append(Species s, List<Animal> result)
        {
            var animals = s.Animals;
            for (int i = 0; i < animals.Count; i++)
                if (!animals[i].Killed && !animals[i].IsGone) result.Add(animals[i]);
        }

        void Ensure(int n)
        {
            if (intents.Length >= n) return;
            int size = Math.Max(n, intents.Length * 2);
            Array.Resize(ref intents, size);
            Array.Resize(ref interactions, size);
            Array.Resize(ref busyAtStart, size);
            Array.Resize(ref moved, size);
            Array.Resize(ref from, size);
            Array.Resize(ref order2, size);
        }

        public override void Validate(ValidationReport report)
        {
            foreach (var s in World.AllSpecies)
            {
                int n = 0;
                foreach (var l in s.ModulesOf<Locomotion>()) n++;
                if (n != 1)
                {
                    var sp = s;
                    report.Error("V-13", s, $"Species '{s.DisplayName}' has {n} locomotion components; it needs exactly one (MOVE-01).",
                                 n == 0 ? new ValidationFix("Add KinematicLocomotion", () => sp.gameObject.AddComponent<KinematicLocomotion>()) : null);
                }
            }
        }
    }
}
