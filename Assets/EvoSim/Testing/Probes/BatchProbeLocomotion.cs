using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only locomotion that records the group sizes the act phase moves through MoveAll (ARCH-12, 20 §10).</summary>
    public class BatchProbeLocomotion : KinematicLocomotion
    {
        public readonly List<int> Groups = new List<int>();

        public override void MoveAll(IReadOnlyList<Animal> group, IReadOnlyList<Intent> intents, MoveContext c, Span<float> moved)
        {
            Groups.Add(group.Count);
            base.MoveAll(group, intents, c, moved);
        }
    }
}
