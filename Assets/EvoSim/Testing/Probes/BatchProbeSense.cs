using System;
using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only sense that records the group sizes the sense phase reads through ReadAll (ARCH-12, 20 §10).</summary>
    public class BatchProbeSense : ProbeSense
    {
        public readonly List<int> Groups = new List<int>();

        public override void ReadAll(IReadOnlyList<Animal> group, SenseContext s, Span<int> tokens)
        {
            Groups.Add(group.Count);
            base.ReadAll(group, s, tokens);
        }
    }
}
