namespace EvoSim.Testing
{
    /// <summary>A test-only module that names a companion it needs (V-26).</summary>
    [RequiresModule(typeof(Energy))]
    public class NeedsEnergyProbe : SpeciesModule
    {
    }
}
