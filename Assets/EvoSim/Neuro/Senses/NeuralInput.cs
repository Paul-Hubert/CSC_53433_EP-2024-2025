namespace EvoSim.Neuro
{
    /// <summary>
    /// The attachment an <see cref="INeuralSense"/> adds to a query (SENSE-40; plan M16 §5.4): the index of the sense in its
    /// species' sense order and its values. The brain maps attachments by <see cref="SenseIndex"/>, never by position, since
    /// the sense phase skips null attachments.
    /// </summary>
    public sealed class NeuralInput
    {
        /// <summary>The sense's index in <see cref="Species.Senses"/>.</summary>
        public int SenseIndex { get; }
        /// <summary>The values, <see cref="INeuralSense.NeuralWidth"/> of them; owned by this attachment, not changed after.</summary>
        public float[] Values { get; }

        /// <summary>Makes an attachment.</summary>
        /// <param name="senseIndex">The sense's index in its species' sense order.</param>
        /// <param name="values">The values; kept, not copied.</param>
        public NeuralInput(int senseIndex, float[] values) { SenseIndex = senseIndex; Values = values; }
    }
}
