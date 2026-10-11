using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only action that wanders and records the group sizes the act phase plans through ActAll (ARCH-12, 20 §10).</summary>
    public class BatchProbeAction : AnimalAction
    {
        /// <summary>The size of each group the act phase passed to <see cref="ActAll"/>, in order.</summary>
        public readonly List<int> Groups = new List<int>();

        /// <summary>"wander (a test probe)".</summary>
        protected override string DefaultDescription => "wander (a test probe)";

        /// <summary>Wanders (MOVE-05).</summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c) => c.Wander();

        /// <summary>Records the group's size, then plans each animal as the base class does.</summary>
        /// <inheritdoc/>
        public override void ActAll(IReadOnlyList<Animal> group, ActContext c, Span<ActPlan> plans)
        {
            Groups.Add(group.Count);
            base.ActAll(group, c, plans);
        }
    }
}
