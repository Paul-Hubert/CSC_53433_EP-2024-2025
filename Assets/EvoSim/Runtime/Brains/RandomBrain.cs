using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Uniform over the species' actions, whatever the genes and the situation: the null model (08 §6).</summary>
    /// <remarks>
    /// <para>The reference of teaching-path exercise 13 (<c>MyRandomBrain</c>, 22 §0). What it does: its
    /// <see cref="Id"/> is "random"; <see cref="Ask"/> makes one row per query, in query order, with one entry per action of
    /// that query's species, each 1/n (<see cref="Brain.Uniform"/>), and answers at once with <see cref="BrainAnswer.Now"/>.
    /// It reads neither the genes nor the situation, and keeps every default (memoizable, any number of actions, species
    /// mixed in one batch, no cache).</para>
    /// <para>Random hunters can't feed themselves: the gap between this brain and a real one shows that the brain matters.
    /// It is also the brain of fast tests.</para>
    /// </remarks>
    public class RandomBrain : Brain
    {
        /// <summary>"random".</summary>
        /// <inheritdoc/>
        public override string Id => "random";

        /// <summary>One uniform row per query, over that query's species' actions, answered at once.</summary>
        /// <inheritdoc/>
        public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)
        {
            var rows = new float[batch.Count][];
            for (int i = 0; i < batch.Count; i++) rows[i] = Uniform(batch[i].Species.Actions.Count);
            return BrainAnswer.Now(rows);
        }
    }
}
