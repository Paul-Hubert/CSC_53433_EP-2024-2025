namespace EvoSim
{
    /// <summary>While a species has fewer animals than its floor, a newcomer arrives (POP-03); it decides at the next tick (TICK-05).</summary>
    /// <remarks>
    /// Tenth phase of the reference tick, after Environment and before Record: the state it leaves is the one the next
    /// tick's decisions see. Species without a <see cref="FloorRule"/> are skipped. Newcomers come from
    /// <see cref="World.AddNewcomer"/>, which draws their genome and position from the species' <c>founders</c> stream and
    /// records an <c>immigrant</c> event.
    /// </remarks>
    public class FloorPhase : TickPhase
    {
        /// <summary>Adds newcomers to every species below its floor, species in the World's order.</summary>
        /// <inheritdoc/>
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
