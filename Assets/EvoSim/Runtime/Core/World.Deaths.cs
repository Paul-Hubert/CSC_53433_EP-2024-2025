namespace EvoSim
{
    // Deaths: one record per death (ANIM-42), whatever removed the animal.
    public partial class World
    {
        /// <summary>
        /// Marks the animal gone and records its death once (ANIM-42): cause, age, generation, meals, offspring, and the
        /// killer for a kill. It leaves the species' list at the end of the phase.
        /// </summary>
        /// <param name="a">The animal that dies; nothing happens when it is already gone.</param>
        /// <param name="cause">The cause, as events and the statistics name it: <c>"killed"</c>, <c>"starvation"</c>,
        /// <c>"old_age"</c>, <c>"migrated"</c>, or a cause of your own (it gets its <c>deaths_&lt;cause&gt;</c> column).</param>
        /// <remarks>
        /// <para>After the <c>death</c> event, it calls the species' <see cref="Species.OnDied"/>, then each of its
        /// modules' <see cref="SpeciesModule.OnDied"/>, then each world module's <see cref="WorldModule.OnDied"/>.</para>
        /// <para>The animal stays in <see cref="Species.Animals"/> until the phase that killed it calls
        /// <see cref="Species.RemoveGone"/>: a death phase of your own must call it too.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// World.RecordDeath(a, "lightning");   // a death of your own, in your own phase
        /// a.Species.RemoveGone();
        /// </code>
        /// </example>
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
            foreach (var m in s.Modules) m.OnDied(a, cause);
            foreach (var m in modules) m.OnDied(a, cause);
        }

        /// <summary>Tells the species' modules, then the World's, that an animal joined the living (in hierarchy order).</summary>
        internal void Born(Animal a)
        {
            foreach (var m in a.Species.Modules) m.OnBorn(a);
            foreach (var m in modules) m.OnBorn(a);
        }
    }
}
