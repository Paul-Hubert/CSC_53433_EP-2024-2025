namespace EvoSim.Testing
{
    /// <summary>A test-only view that remembers what OnShow told it.</summary>
    public class ShowProbeView : AnimalView
    {
        /// <summary>The calls to <see cref="OnShow"/> so far.</summary>
        public int Shown;
        /// <summary>The id of the animal last shown; -1 before any.</summary>
        public int LastShownId = -1;

        /// <summary>Counts the call and keeps the animal's id.</summary>
        /// <inheritdoc/>
        public override void OnShow(Animal a)
        {
            Shown++;
            LastShownId = a.Id;
        }
    }
}
