namespace EvoSim
{
    /// <summary>
    /// Waits for the brains (TICK-03), stores fresh answers in the memo and the cache (never a failure, DEC-34), and
    /// draws each deciding animal's action with its species' sampling stream after the temperature (DEC-20, RAND-03).
    /// A failed answer stops a strict run cleanly; otherwise it becomes a uniform row and is counted (DEC-40).
    /// </summary>
    /// <remarks>
    /// <para>Third phase of the reference tick, after Ask brains (V-08) and before Act. It is the phase the tick waits at
    /// while the brains think: in the Freeze wait mode the main thread blocks, in the Responsive mode the tick resumes here
    /// on a later frame (SPACE-14). Either way the result is the same.</para>
    /// <para>An animal that shares another's key (DEC-32) takes that animal's row. Every deciding animal gets a new action,
    /// its searching and bred-this-period flags are cleared (DEC-03), and its observation, situation and probabilities are
    /// kept for the inspector (DEC-21).</para>
    /// <para>When the draw picks an overlay (ACT-07), a second draw from the same stream picks the carrier among the other
    /// actions, in proportion to their probabilities (DEC-20). Species without overlays draw exactly once.</para>
    /// </remarks>
    public class ChooseActionsPhase : TickPhase
    {
        AnswerCache cache;      // the World's answer cache, or null: fresh answers are then kept in the memo only
        float[] rest = new float[0];   // the tempered row with the overlays set to 0: the carrier's weights (ACT-07)

        /// <summary>Finds the World's answer cache, if any.</summary>
        /// <inheritdoc/>
        public override void Initialize() => cache = World.Service<AnswerCache>();

        /// <summary>The brains' answers to this tick's batches that are not in yet, or null when all are in (or nothing was sent).</summary>
        /// <inheritdoc/>
        public override Pending WaitsFor(TickContext t) => World.Decisions.Pending;

        /// <summary>Stores the answers, then draws the action of every deciding animal.</summary>
        /// <remarks>A strict brain's failure stops the World before any action is drawn, so the tick changes nothing (RAND-20).</remarks>
        /// <inheritdoc/>
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
                var sampling = t.Stream(species, "sampling");
                a.Action = sampling.Choose(p);                                                     // RAND-03
                a.EndOverlay();
                if (a.CurrentAction != null && a.CurrentAction.Overlay) DrawCarrier(a, p, sampling);   // ACT-07
                a.Searching = false;                                                                // DEC-03
                a.BredThisPeriod = false;
                a.LastObservation = d.Observation;                                                  // DEC-21
                a.LastSituation = d.Situation;
                a.LastProbabilities = d.Row;
                a.LastDecisionTick = t.Tick;
                var counts = species.Counters.ActionCounts;
                if (a.Action >= 0 && a.Action < counts.Length) counts[a.Action]++;
                if (a.Overlay >= 0 && a.Overlay != a.Action && a.Overlay < counts.Length) counts[a.Overlay]++;   // the overlay and its carrier
            }
        }

        /// <summary>
        /// The drawn action is an overlay (ACT-07): keeps it as the animal's overlay and draws the carrier from the same
        /// tempered row with every overlay set to 0, with the same stream (DEC-20). A row with nothing but overlays
        /// leaves no carrier: no second draw, and the action stays the overlay, so the animal stays.
        /// </summary>
        /// <param name="a">The deciding animal; its action is the overlay just drawn.</param>
        /// <param name="p">The tempered row the overlay was drawn from (never changed).</param>
        /// <param name="sampling">The species' sampling stream.</param>
        void DrawCarrier(Animal a, float[] p, RandomStream sampling)
        {
            var actions = a.Species.Actions;
            a.Overlay = a.Action;
            int ticks = a.CurrentAction.OverlayTicks;
            a.OverlayLeft = ticks > 0 ? ticks : 0;
            if (rest.Length != p.Length) rest = new float[p.Length];
            double total = 0;
            for (int i = 0; i < p.Length; i++)
            {
                rest[i] = i < actions.Count && actions[i].Overlay ? 0f : System.Math.Max(0f, p[i]);
                total += rest[i];
            }
            if (total > 0) a.Action = sampling.Choose(rest);   // Choose renormalises the weights
        }

        /// <summary>Reports this phase placed before Ask brains, or without it (V-08).</summary>
        /// <inheritdoc/>
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
