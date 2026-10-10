namespace EvoSim.Testing
{
    /// <summary>A test-only view that remembers what OnShow told it.</summary>
    public class ShowProbeView : AnimalView
    {
        public int Shown;
        public int LastShownId = -1;

        public override void OnShow(Animal a)
        {
            Shown++;
            LastShownId = a.Id;
        }
    }
}
