using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Picks who decides (DEC-01): every living, not busy animal on the decision ticks, and animals without an action
    /// on the others; rebuilds the spatial index; casts the rays of batched senses in one batch; reads every sense of
    /// every deciding animal from the state at the start of the tick (TICK-04 #1, SENSE-04, TICK-05).
    /// </summary>
    public class SensePhase : TickPhase
    {
        SenseContext senses;
        readonly List<Animal> deciders = new List<Animal>();

        public override void Initialize() => senses = new SenseContext(World);

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
            foreach (var d in state.Due)
            {
                var species = d.Animal.Species;
                d.Observation = species.Observe(d.Animal, senses);
                d.Situation = species.Describe(d.Observation, style);
            }
        }

        /// <summary>The attachments of an animal's senses (SENSE-40), and a key for them or null (SENSE-41).</summary>
        internal static List<object> Attachments(Animal a, SenseContext s, out string key, out bool memoable)
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

        internal SenseContext Context => senses;
    }
}
