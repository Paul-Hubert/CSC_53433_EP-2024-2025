namespace EvoSim.Testing
{
    /// <summary>A test-only action defined outside the runtime assembly (T-CORE-02): it counts its calls.</summary>
    public class ProbeAction : AnimalAction
    {
        public int Calls;
        public override void Act(Animal a, ActContext c) => Calls++;
    }
}
