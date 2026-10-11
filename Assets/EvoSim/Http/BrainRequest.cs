using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// One decision query as an HTTP brain sends it: its place in the batch, the text for the model and the species'
    /// action names. <see cref="HttpBrain.Ask"/> builds it on the main thread; <see cref="HttpBrain.AnswerAsync"/> reads it
    /// on the thread pool, so it holds plain values only, no Unity object.
    /// </summary>
    public sealed class BrainRequest
    {
        /// <summary>The query's index in the batch, from 0: the row of the <see cref="BrainAnswer"/> to fill.</summary>
        public int Index { get; }
        /// <summary>The text sent to the model, from <see cref="HttpBrain.TextFor"/> (the full prompt by default).</summary>
        public string Text { get; }
        /// <summary>The species' actions, in its action order (the answer's row order, DEC-11).</summary>
        public IReadOnlyList<string> Actions { get; }
        /// <summary>The id of the deciding animal's species.</summary>
        public string SpeciesId { get; }

        /// <summary>Makes the request for one query; <see cref="HttpBrain.Ask"/> makes one per query of a batch.</summary>
        /// <param name="index">The query's index in the batch, from 0.</param>
        /// <param name="text">The text for the model.</param>
        /// <param name="actions">The species' action names, in action order.</param>
        /// <param name="speciesId">The species' id.</param>
        public BrainRequest(int index, string text, IReadOnlyList<string> actions, string speciesId)
        {
            Index = index;
            Text = text;
            Actions = actions;
            SpeciesId = speciesId;
        }
    }
}
