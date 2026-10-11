using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Picks who decides (DEC-01): every living, not busy animal on the decision ticks, and animals without an action
    /// on the others; rebuilds the spatial index; casts the rays of batched senses in one batch; reads every sense of
    /// every deciding animal from the state at the start of the tick (TICK-04 #1, SENSE-04, TICK-05).
    /// </summary>
    /// <remarks>
    /// <para>First phase of the reference tick. A species' <see cref="DecisionSchedule"/> says which ticks are its decision
    /// ticks; without one, the World's decision period does (every <see cref="World.DecisionPeriod"/> ticks). An animal
    /// with no action yet (a newborn, an animal whose busy time just ended) decides at once (REPRO-13).</para>
    /// <para>For each deciding animal it fills a <see cref="Decision"/> in <see cref="World.Decisions"/>: the sense tokens
    /// (its observation) and their text (its situation). Senses are read species by species, one sense at a time over the
    /// whole group (<see cref="Sense.ReadAll"/>, ARCH-12). It only reads the world and draws no random number.</para>
    /// </remarks>
    public class SensePhase : TickPhase
    {
        SenseContext senses;                                        // reused every tick; exposed as Context
        readonly List<Animal> deciders = new List<Animal>();       // this tick's deciding animals, for the batched rays

        /// <summary>Creates the sense context that every tick reuses.</summary>
        /// <inheritdoc/>
        public override void Initialize() => senses = new SenseContext(World);

        /// <summary>Starts the tick's decisions and reads every sense of every deciding animal.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            var state = World.Decisions;
            state.Clear(t.Tick);
            World.Space.Rebuild();
            deciders.Clear();
            foreach (var s in World.AllSpecies)
            {
                var schedule = s.Module<DecisionSchedule>();
                foreach (var a in s.Animals)
                {
                    if (a.IsGone || a.Killed || a.IsBusy) continue;                       // DEC-02
                    bool periodic = schedule != null ? schedule.IsDue(a, t.Tick, World) : t.Tick % World.DecisionPeriod == 0;
                    if (!periodic && a.Action >= 0) continue;                               // DEC-01
                    deciders.Add(a);
                    state.Add(a);
                }
            }
            senses.CastRays(deciders);                                                      // SENSE-30
            var style = World.TextStyle;
            var due = state.Due;                                                            // species order, then id order
            // One group per species (the due list keeps a species' animals together); each sense reads the whole group.
            for (int start = 0, end; start < due.Count; start = end)
            {
                var species = due[start].Animal.Species;
                group.Clear();
                for (end = start; end < due.Count && due[end].Animal.Species == species; end++) group.Add(due[end].Animal);
                var list = species.Senses;
                var observed = new int[group.Count][];
                for (int i = 0; i < group.Count; i++) observed[i] = new int[list.Count];
                if (tokens.Length < group.Count) tokens = new int[Math.Max(group.Count, tokens.Length * 2)];
                for (int k = 0; k < list.Count; k++)
                {
                    list[k].ReadAll(group, senses, new Span<int>(tokens, 0, group.Count));   // the batch entry point (ARCH-12)
                    for (int i = 0; i < group.Count; i++) observed[i][k] = tokens[i];
                }
                for (int i = 0; i < group.Count; i++)
                {
                    var d = due[start + i];
                    d.Observation = new Observation(observed[i]);
                    d.Situation = species.Describe(d.Observation, style);
                }
            }
        }

        readonly List<Animal> group = new List<Animal>();      // the deciding animals of one species, reused
        int[] tokens = new int[64];                             // one sense's tokens for the group, reused and grown as needed

        /// <summary>The attachments of an animal's senses (SENSE-40), and a key for them or null (SENSE-41).</summary>
        /// <remarks>Called by the ask-brains phase for each deciding animal, after the senses were read.</remarks>
        /// <param name="a">The deciding animal.</param>
        /// <param name="s">This tick's sense context.</param>
        /// <param name="key">The attachments' keys joined by ";", or "" when there is none; part of the memo key.</param>
        /// <param name="memoable">False when an attachment has no key: the decision can then be neither memoised nor cached.</param>
        /// <returns>The attachments in sense order, or null when no sense gives one.</returns>
        public static List<object> Attachments(Animal a, SenseContext s, out string key, out bool memoable)
        {
            List<object> list = null;
            key = "";
            memoable = true;
            foreach (var sense in a.Species.Senses)
            {
                if (!sense.HasAttachments) continue;
                var att = sense.Attachment(a, s);
                if (att == null) continue;
                (list ??= new List<object>()).Add(att);
                string k = sense.AttachmentKey(att);
                if (k == null) memoable = false;
                else key += k + ";";
            }
            return list;
        }

        /// <summary>This tick's sense context (rays cast), for the phases after it.</summary>
        public SenseContext Context => senses;
    }
}
