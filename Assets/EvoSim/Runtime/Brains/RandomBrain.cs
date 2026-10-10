using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Uniform over the species' actions, whatever the genes and the situation: the null model (08 §6).</summary>
    public class RandomBrain : Brain
    {
        public override string Id => "random";

        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            var rows = new float[batch.Count][];
            for (int i = 0; i < batch.Count; i++) rows[i] = Uniform(batch[i].Species.Actions.Count);
            return BrainAnswer.Now(rows);
        }
    }
}
