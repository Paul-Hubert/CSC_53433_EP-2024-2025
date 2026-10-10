using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>One query as an HTTP brain sends it, built on the main thread: its index in the batch, the text, the action names.</summary>
    public sealed class BrainRequest
    {
        public int Index { get; }
        public string Text { get; }
        /// <summary>The species' actions, in its action order (the answer's row order, DEC-11).</summary>
        public IReadOnlyList<string> Actions { get; }
        public string SpeciesId { get; }

        public BrainRequest(int index, string text, IReadOnlyList<string> actions, string speciesId)
        {
            Index = index;
            Text = text;
            Actions = actions;
            SpeciesId = speciesId;
        }
    }
}
