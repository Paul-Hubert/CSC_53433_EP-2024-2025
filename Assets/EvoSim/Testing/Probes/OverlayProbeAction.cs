using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test-only overlay (ACT-07): done in passing, it plans a walk that must be ignored and a custom interaction that
    /// counts the times it applied, per animal.
    /// </summary>
    /// <example>
    /// <code>
    /// var w = New().Flat(40, 40).Phase&lt;ActPhase&gt;()
    ///     .Species("prey", s =&gt; s.PreyBody().Action&lt;OverlayProbeAction&gt;("mark").Action&lt;RestAction&gt;("rest"))
    ///     .Build();
    /// </code>
    /// </example>
    public class OverlayProbeAction : AnimalAction
    {
        /// <summary>How many ticks it acts (<see cref="AnimalAction.OverlayTicks"/>); 0 = until the next decision.</summary>
        public int Ticks = 1;
        /// <summary>Claim to mate, for the validation test (V-27).</summary>
        public bool ClaimsToMate;
        /// <summary>Claim to hunt, for the validation test (V-27).</summary>
        public bool ClaimsToHunt;
        /// <summary>The interaction's reach, in meters, measured from the point the ignored intent names.</summary>
        public float Reach = float.PositiveInfinity;
        /// <summary>The times its interaction applied, all animals together.</summary>
        public int Applied;
        /// <summary>The id of the last animal it applied to, or -1.</summary>
        public int LastActor = -1;

        /// <summary>"leave a mark in passing (a test probe)".</summary>
        protected override string DefaultDescription => "leave a mark in passing (a test probe)";

        /// <summary>True: done in passing.</summary>
        public override bool Overlay => true;
        /// <summary><see cref="Ticks"/>.</summary>
        public override int OverlayTicks => Ticks;
        /// <summary><see cref="ClaimsToMate"/>.</summary>
        public override bool Mates => ClaimsToMate;
        /// <summary><see cref="ClaimsToHunt"/>.</summary>
        public override bool Hunts => ClaimsToHunt;

        /// <summary>Plans a walk 10 m east (the act phase must ignore it) and the counting interaction.</summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            c.WalkTo(a.Position + new Vector3(10f, 0f, 0f));
            c.OnArrival(new Mark(this));
        }

        /// <summary>The counting interaction.</summary>
        sealed class Mark : IInteraction
        {
            readonly OverlayProbeAction owner;   // the probe whose counters it updates

            /// <summary>An interaction for the probe <paramref name="o"/>.</summary>
            public Mark(OverlayProbeAction o) { owner = o; }

            /// <summary>The probe's <see cref="OverlayProbeAction.Reach"/>.</summary>
            public float Reach => owner.Reach;

            /// <summary>Counts one application and remembers the actor.</summary>
            public void Apply(Animal actor, InteractionContext c)
            {
                owner.Applied++;
                owner.LastActor = actor.Id;
            }
        }
    }
}
