using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only brain with memory: not memoizable, so every deciding animal's query reaches it with its id.</summary>
    public class MemoryBrain : Brain
    {
        public readonly List<int> AnimalIds = new List<int>();

        public override string Id => "memory";
        public override bool Memoizable => false;

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
