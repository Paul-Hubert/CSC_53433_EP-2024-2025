using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test-only brain with memory: not memoizable, so every deciding animal's query reaches it with its id (even
    /// animals in the same situation), and it answers uniform rows.
    /// </summary>
    public class MemoryBrain : Brain
    {
        /// <summary>The id of the animal behind each query received, in order.</summary>
        public readonly List<int> AnimalIds = new List<int>();

        /// <summary>"memory".</summary>
        public override string Id => "memory";

        /// <summary>False: no memo, so each animal gets its own query.</summary>
        public override bool Memoizable => false;

        /// <summary>Records each query's animal id and answers a uniform row at once.</summary>
        /// <inheritdoc/>
        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            var rows = new float[batch.Count][];
            for (int i = 0; i < batch.Count; i++)
            {
                AnimalIds.Add(batch[i].AnimalId);
                rows[i] = Uniform(batch[i].Species.Actions.Count);
            }
            return BrainAnswer.Now(rows);
        }
    }
}
