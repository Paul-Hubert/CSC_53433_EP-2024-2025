using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only locomotion that records the group sizes the act phase moves through MoveAll (ARCH-12, 20 §10).</summary>
    public class BatchProbeLocomotion : KinematicLocomotion
    {
        /// <summary>The size of each group the act phase passed to <see cref="MoveAll"/>, in order.</summary>
        public readonly List<int> Groups = new List<int>();

        /// <summary>Records the group's size, then moves each animal as the kinematic locomotion does.</summary>
        /// <inheritdoc/>
        public override void MoveAll(IReadOnlyList<Animal> group, IReadOnlyList<Intent> intents, MoveContext c, Span<float> moved)
        {
            Groups.Add(group.Count);
            base.MoveAll(group, intents, c, moved);
        }
    }
}
