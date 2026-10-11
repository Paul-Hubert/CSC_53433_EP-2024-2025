using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only sense that records the group sizes the sense phase reads through ReadAll (ARCH-12, 20 §10).</summary>
    public class BatchProbeSense : ProbeSense
    {
        /// <summary>The size of each group the sense phase passed to <see cref="ReadAll"/>, in order.</summary>
        public readonly List<int> Groups = new List<int>();

        /// <summary>Records the group's size, then reads each animal as the base class does.</summary>
        /// <inheritdoc/>
        public override void ReadAll(IReadOnlyList<Animal> group, SenseContext s, Span<int> tokens)
        {
            Groups.Add(group.Count);
            base.ReadAll(group, s, tokens);
        }
    }
}
