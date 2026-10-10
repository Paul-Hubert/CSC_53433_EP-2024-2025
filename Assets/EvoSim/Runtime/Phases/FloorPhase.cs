namespace EvoSim
{
    /// <summary>While a species has fewer animals than its floor, a newcomer arrives (POP-03); it decides at the next tick (TICK-05).</summary>
    public class FloorPhase : TickPhase
    {
        public override void Run(TickContext t)
        {
            foreach (var s in World.AllSpecies)
            {
                var floor = s.Module<FloorRule>();
                if (floor == null) continue;
                int living = 0;
                foreach (var a in s.Animals) if (!a.IsGone) living++;
                for (; living < floor.Floor; living++) World.AddNewcomer(s);
            }
        }
    }
}
