namespace EvoSim
{
    // Deaths: one record per death (ANIM-42), whatever removed the animal.
    public partial class World
    {
        /// <summary>
        /// Marks the animal gone and records its death once (ANIM-42): cause, age, generation, meals, offspring, and the
        /// killer for a kill. It leaves the species' list at the end of the phase.
        /// </summary>
        public void RecordDeath(Animal a, string cause)
        {
            if (a.IsGone) return;
            a.IsGone = true;
            var s = a.Species;
            s.Counters.CountDeath(cause, a.Age);
            var e = new SimEvent("death", Tick, a.Id, s.Id)
                .With("cause", cause)
                .With("age", a.Age)
                .With("gen", a.Generation)
                .With("food", a.Meals)
                .With("offspring", a.Offspring);
            if (a.Killed && a.KilledBy >= 0) e.With("killer", a.KilledBy);
            Events.Record(e);
            s.OnDied(a, cause);
        }
    }
}
