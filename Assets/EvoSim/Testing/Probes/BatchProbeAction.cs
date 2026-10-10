using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only action that wanders and records the group sizes the act phase plans through ActAll (ARCH-12, 20 §10).</summary>
    public class BatchProbeAction : AnimalAction
    {
        public readonly List<int> Groups = new List<int>();

        protected override string DefaultDescription => "wander (a test probe)";

        public override void Act(Animal a, ActContext c) => c.Wander();

        public override void ActAll(IReadOnlyList<Animal> group, ActContext c, Span<ActPlan> plans)
        {
            Groups.Add(group.Count);
            base.ActAll(group, c, plans);
        }
    }
}
