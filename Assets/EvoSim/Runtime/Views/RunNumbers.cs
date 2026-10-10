using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// What the overlay shows of a run, read from the World's own counters (13 §4–5) and nothing else: tick, state,
    /// population, births, deaths, kills, mutations, model calls. Read only; a test compares it with the event log.
    /// </summary>
    public sealed class RunNumbers
    {
        public struct SpeciesLine
        {
            public string Name;
            public int Living, Births, Deaths, Kills, Eggs, MaxGeneration;
        }

        public int Tick;
        public RunState State;
        /// <summary>What the tick waits for ("JEV: 12 queries", "the mutator: 2 eggs"), or null.</summary>
        public string WaitingFor;
        public readonly List<SpeciesLine> Species = new List<SpeciesLine>();
        public int MutationsOk, MutationsFailed, MutationAttempts;
        public int BrainCalls, MutatorCalls, CacheHits, MemoHits, Decisions;
        public double BrainSeconds;

        /// <summary>Fills the numbers from the World as it is now.</summary>
        public RunNumbers Read(World w)
        {
            Tick = w.Tick;
            State = w.State;
            WaitingFor = Waiting(w);
            Species.Clear();
            Decisions = MemoHits = 0;
            BrainSeconds = 0;
            var eggs = w.Service<EggSystem>();
            foreach (var s in w.AllSpecies)
            {
                int living = 0;
                foreach (var a in s.Animals) if (!a.IsGone) living++;
                var c = s.Counters;
                Species.Add(new SpeciesLine
                {
                    Name = s.DisplayName, Living = living, Births = c.Births, Deaths = c.DeathsTotal, Kills = c.Kills,
                    Eggs = eggs != null ? eggs.CountOf(s) : 0, MaxGeneration = c.MaxGeneration,
                });
                Decisions += c.Decisions;
                MemoHits += c.MemoHits;
                BrainSeconds += c.BrainMilliseconds / 1000.0;
            }
            MutationsOk = w.Mutations.Successes;
            MutationsFailed = w.Mutations.Failures;
            MutationAttempts = w.Mutations.Attempts;
            BrainCalls = w.Decisions.ModelCalls;
            MutatorCalls = w.Mutations.ModelCalls;
            CacheHits = w.Decisions.CacheHits + w.Mutations.CacheHits;
            return this;
        }

        /// <summary>The brains or the mutator the current tick waits for, with how many queries or eggs.</summary>
        public static string Waiting(World w)
        {
            var phase = w.WaitingFor;
            if (phase == null) return null;
            if (phase is ChooseActionsPhase)
            {
                int queries = 0;
                foreach (var a in w.Decisions.Answers) if (!a.IsDone || !a.Pending.IsDone) queries += a.Count;
                var names = new List<string>();
                foreach (var d in w.Decisions.Due)
                    if (d.Brain != null && !names.Contains(d.Brain.name)) names.Add(d.Brain.name);
                return $"{(names.Count > 0 ? string.Join(", ", names) : "the brain")}: {queries} queries";
            }
            if (phase is HatchPhase)
            {
                int eggs = 0;
                var system = w.Service<EggSystem>();
                if (system != null)
                    foreach (var e in system.Entities) if (e.HatchTick <= w.Tick && !e.IsComplete) eggs++;
                return $"the mutator: {eggs} eggs";
            }
            return phase.PhaseName;
        }
    }
}
