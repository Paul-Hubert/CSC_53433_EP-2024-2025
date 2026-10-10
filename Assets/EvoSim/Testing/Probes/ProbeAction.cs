namespace EvoSim.Testing
{
    /// <summary>A test-only action defined outside the runtime assembly (T-CORE-02): it counts its calls and stays.</summary>
    public class ProbeAction : AnimalAction
    {
        protected override string DefaultDescription => "do nothing (a test probe)";

        public int Calls;
        /// <summary>Calls for an animal already killed this tick (must stay 0, ANIM-41).</summary>
        public int CallsWhileKilled;

        public override void Act(Animal a, ActContext c)
        {
            Calls++;
            if (a.Killed) CallsWhileKilled++;
        }
    }
}
