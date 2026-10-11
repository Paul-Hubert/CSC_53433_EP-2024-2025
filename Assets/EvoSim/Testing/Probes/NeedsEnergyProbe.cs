namespace EvoSim.Testing
{
    /// <summary>
    /// A test-only module that names a companion it needs with <see cref="RequiresModuleAttribute"/>: a species without an
    /// <see cref="Energy"/> module gets the warning V-26, whose fix adds one.
    /// </summary>
    [RequiresModule(typeof(Energy))]
    public class NeedsEnergyProbe : SpeciesModule
    {
    }
}
