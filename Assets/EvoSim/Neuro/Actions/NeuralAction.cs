using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// What the neural species' actions share (plan M16 D5, D6, §5.5): they steer without a target, then plan at most one
    /// contact interaction on what lies within reach of where the step ends, and every one of them breeds when the animal
    /// is ready.
    /// </summary>
    /// <remarks>
    /// <para>A subclass plans its move in <see cref="Move"/>, only through the <see cref="ActContext"/> (ACT-06): it never sets
    /// the heading or the position. The act phase turns the heading toward the move; a blocked step leaves it unchanged and
    /// the locomotion's side step unsticks it (MOVE-04).</para>
    /// <para>Contacts (<see cref="Contacts"/>): after the move, strike the nearest prey, else scavenge the nearest edible
    /// carcass, else graze the nearest item, each within its reach of the step's end (ACT-03, ACT-05); the interaction
    /// resolver applies it only within reach (ACT-10). Reading the world draws no random number.</para>
    /// <para><see cref="Mates"/> is on by default: an animal breeds whenever it is ready, alone (asexual, REPRO-04) or with a
    /// ready kin in reach (sexual, REPRO-02).</para>
    /// </remarks>
    public abstract class NeuralAction : AnimalAction
    {
        [SerializeField, Tooltip("The contacts this action may make after its move: strike before scavenge before graze (ACT-03).")]
        NeuralContacts contacts = NeuralContacts.Graze;
        [SerializeField, Tooltip("On: the animal breeds during this action when it is ready (REPRO-02, REPRO-04; plan D5).")]
        bool mates = true;

        Locomotion locomotion;
        Diet diet;

        /// <summary>The contacts this action may make.</summary>
        public NeuralContacts Contacts => contacts;
        /// <inheritdoc/>
        public override bool Mates => mates;
        /// <inheritdoc/>
        public override bool Hunts => (contacts & NeuralContacts.Strike) != 0;
        /// <inheritdoc/>
        public override bool SelfDirected => true;

        /// <summary>Sets the contacts and breeding from code (templates, tests), as the Inspector does.</summary>
        /// <param name="contactSet">The contacts.</param>
        /// <param name="breeds">Whether the animal breeds during this action.</param>
        public void SetContacts(NeuralContacts contactSet, bool breeds = true)
        {
            contacts = contactSet;
            mates = breeds;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            locomotion = Species.Module<Locomotion>();
            diet = Species.Module<Diet>();
        }

        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            Move(a, c);
            Contact(a, c);
        }

        /// <summary>Plans this action's move through the context: a step, a turn, or staying.</summary>
        /// <param name="a">The animal.</param>
        /// <param name="c">The context to plan through.</param>
        protected abstract void Move(Animal a, ActContext c);

        /// <summary>The animal's speed, in meters per tick: its walk or run trait.</summary>
        /// <param name="a">The animal.</param>
        /// <param name="run">True for the run speed.</param>
        /// <returns>The speed, 0 without a locomotion.</returns>
        protected float SpeedOf(Animal a, bool run) =>
            locomotion == null ? 0f : a.Trait(run ? locomotion.RunSpeed : locomotion.WalkSpeed);

        /// <summary>Plans a step of <paramref name="distance"/> meters toward the heading plus <paramref name="turn"/> degrees.</summary>
        /// <param name="a">The animal.</param>
        /// <param name="c">The context.</param>
        /// <param name="turn">Degrees added to the heading, clockwise.</param>
        /// <param name="distance">How far, in meters.</param>
        /// <param name="run">True to run.</param>
        protected static void Step(Animal a, ActContext c, float turn, float distance, bool run)
        {
            var target = a.Position + ActContext.HeadingDirection(a.Heading + turn) * distance;
            if (run) c.RunTo(target);
            else c.WalkTo(target);
        }

        /// <summary>Where the planned step ends, at most a tick's move along the intent.</summary>
        Vector3 StepEnd(Animal a, ActContext c)
        {
            var intent = c.Intent;
            if (intent.IsStay) return a.Position;
            return a.Position + intent.Direction * Mathf.Min(SpeedOf(a, intent.Run), intent.MaxDistance);
        }

        /// <summary>Plans one contact on the nearest candidate within reach of the step's end: strike, scavenge, then graze.</summary>
        void Contact(Animal a, ActContext c)
        {
            if (contacts == NeuralContacts.None || diet == null) return;
            var end = StepEnd(a, c);
            var act = World.Phase<ActPhase>();
            if ((contacts & NeuralContacts.Strike) != 0)
            {
                float reach = act != null ? act.StrikeReach : 1f;
                var prey = World.PreyOf(Species);
                if (prey.Count > 0 && World.Space.Nearest(a, end, reach, prey, (x, viewer) => !viewer.Species.World.Queries.IsHiddenFrom(x, viewer.Species), out var hit))
                {
                    c.OnArrival(Interaction.Strike(hit.Animal));
                    return;
                }
            }
            if ((contacts & NeuralContacts.Scavenge) != 0)
            {
                float reach = act != null ? act.ScavengeReach : 1f;
                var carcasses = World.Service<CarcassSystem>();
                if (carcasses != null && carcasses.Nearest(end, reach, a,
                        (x, viewer) => x.CanBeEatenBy(viewer) && viewer.Species.Module<Diet>()?.Scavenging(x.Of) != null, out var carcass, out _))
                {
                    c.OnArrival(Interaction.Scavenge(carcass));
                    return;
                }
            }
            if ((contacts & NeuralContacts.Graze) != 0)
            {
                float reach = act != null ? act.GrazeReach : c.GrazeReach;
                ResourceItem? best = null;
                var targets = diet.Resolved;
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (t.Kind != EdibleTargetKind.Layer || t.Edible == null || t.Layer == null) continue;
                    if (t.Layer.Nearest(end, reach, out var item) && (best == null || item.Distance < best.Value.Distance - 1e-6f)) best = item;
                }
                if (best != null) c.OnArrival(Interaction.Graze(best.Value));
            }
        }

        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (diet == null) diet = Species != null ? Species.Module<Diet>() : null;
            if ((contacts & NeuralContacts.Strike) != 0 && (diet == null || !diet.Strikes))
                report.Warning("V-75", this, $"Action '{Name}' may strike, but the diet of '{Species.DisplayName}' strikes no species.");
            if ((contacts & NeuralContacts.Scavenge) != 0 && (diet == null || !diet.Has(EdibleTargetKind.Carcass)))
                report.Warning("V-75", this, $"Action '{Name}' may scavenge, but the diet of '{Species.DisplayName}' eats no carcass.");
            if ((contacts & NeuralContacts.Graze) != 0 && (diet == null || !diet.Has(EdibleTargetKind.Layer)))
                report.Warning("V-75", this, $"Action '{Name}' may graze, but the diet of '{Species.DisplayName}' grazes no layer.");
        }
    }
}
