namespace EvoSim
{
    /// <summary>
    /// Waits for the brains (TICK-03), stores fresh answers in the memo and the cache (never a failure, DEC-34), and
    /// draws each deciding animal's action with its species' sampling stream after the temperature (DEC-20, RAND-03).
    /// A failed answer stops a strict run cleanly; otherwise it becomes a uniform row and is counted (DEC-40).
    /// </summary>
    public class ChooseActionsPhase : TickPhase
    {
        AnswerCache cache;

        public override void Initialize() => cache = World.Service<AnswerCache>();

        public override Pending WaitsFor(TickContext t) => World.Decisions.Pending;

        public override void Run(TickContext t)
        {
            var state = World.Decisions;
            foreach (var d in state.Due)
            {
                if (d.Answer == null)
                {
                    // DEC-30: a cached answer joins the memo where a live one would, so a replay counts like the live run (13 §4).
                    if (d.Source == "cache") state.Memo.Store(d.MemoKey, d.Row);
                    continue;
                }
                state.ModelCalls += d.AnswerIndex == 0 ? d.Answer.ModelCalls : 0;
                d.Animal.Species.Counters.BrainMilliseconds += d.Answer.Milliseconds / d.Answer.Count;   // each query's share (backend_s)
                var row = d.Answer.Row(d.AnswerIndex);
                string problem = d.Answer.Failed(d.AnswerIndex) ? d.Answer.Error(d.AnswerIndex)
                               : BrainAnswer.Check(row, d.Animal.Species.Actions.Count);
                if (problem != null)
                {
                    if (d.Brain.Strict)
                    {
                        World.Stop($"brain failure: {d.Brain.Id} for {d.Animal.Species.Id}: {problem}");   // DEC-40, RAND-20
                        return;
                    }
                    d.Failed = true;
                    d.Error = problem;
                    continue;
                }
                d.Row = row;
                state.Memo.Store(d.MemoKey, row);                                                   // DEC-30
                if (d.CacheKey != null && cache != null) cache.StoreRow(d.Brain.Id, d.CacheKey, row); // DEC-33
            }

            foreach (var d in state.Due)
            {
                var a = d.Animal;
                var species = a.Species;
                if (d.SameAs != null) { d.Row = d.SameAs.Row; d.Failed = d.SameAs.Failed; }       // DEC-32
                if (d.Row == null || d.Failed)
                {
                    d.Row = Brain.Uniform(species.Actions.Count);                                  // DEC-40, not cached
                    if (!d.Failed) d.Failed = true;
                    species.Counters.Failures++;
                }
                var p = Sampling.Temper(d.Row, World.SamplingTemperature);                         // DEC-20
                a.Action = t.Stream(species, "sampling").Choose(p);                                // RAND-03
                a.Searching = false;                                                                // DEC-03
                a.BredThisPeriod = false;
                a.LastObservation = d.Observation;                                                  // DEC-21
                a.LastSituation = d.Situation;
                a.LastProbabilities = d.Row;
                a.LastDecisionTick = t.Tick;
                if (a.Action >= 0 && a.Action < species.Counters.ActionCounts.Length) species.Counters.ActionCounts[a.Action]++;
            }
        }

        public override void Validate(ValidationReport report)
        {
            var phases = World.Phases;
            int me = -1, ask = -1;
            for (int i = 0; i < phases.Count; i++)
            {
                if (phases[i] == this) me = i;
                if (phases[i] is AskBrainsPhase) ask = i;
            }
            if (ask < 0 || ask > me) report.Error("V-08", this, "Choose actions must come after Ask brains.", PlaceAfter(World.Phase<AskBrainsPhase>()));
        }
    }
}
