using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The neural species' columns in <c>stats.csv</c> (plan M16 §5.6, 13 §4): how fast the animals move, how much they
    /// eat per tick and per meter, the share of each action now, and the share caught in a steering loop (the same action
    /// for <see cref="LoopTicks"/> ticks in a row).
    /// </summary>
    /// <remarks>
    /// <para>Updated once a tick in the death phase, after the act phase moved everyone (the hook <see cref="HungerClock"/>
    /// uses); it never gives a cause of death and draws no random number, so a run's hash only changes by its stats. The
    /// per-animal state is in stats (CORE-05): <c>neuro.seen</c>, <c>neuro.streak</c>, <c>neuro.lastAction</c>,
    /// <c>neuro.x</c>, <c>neuro.z</c>, <c>neuro.meals</c>.</para>
    /// <para>Rates are over the last <see cref="Window"/> ticks: <c>nn_speed</c> (meters per animal-tick),
    /// <c>nn_meals_per_tick</c> (meals per animal-tick: a graze, strike or scavenge each count one) and
    /// <c>nn_meals_per_m</c>. <c>nn_loops</c> and <c>nn_share_&lt;action&gt;</c> are shares of the living animals now.</para>
    /// </remarks>
    public class NeuroStats : DeathRule, IStatsColumns
    {
        [SerializeField, Min(1), Tooltip("Ticks of the same action that count as a steering loop (plan §5.6: 50).")]
        int loopTicks = 50;
        [SerializeField, Min(1), Tooltip("Ticks the rates are averaged over (the reference stats row: every 100 ticks).")]
        int window = 100;

        StatId seen, streak, lastAction, x, z, meals;
        double[] ticksRing, metersRing, mealsRing;      // per tick: animal-ticks, meters and meals, over the window
        int lastTick = -1;
        readonly List<string> columns = new List<string>();

        /// <summary>Ticks of the same action that count as a loop.</summary>
        public int LoopTicks => loopTicks;
        /// <summary>Ticks the rates are averaged over.</summary>
        public int Window => window;

        /// <summary>Sets the loop length and the window from code (tests), as the Inspector does.</summary>
        /// <param name="loop">Ticks of the same action that count as a loop.</param>
        /// <param name="ticks">Ticks the rates are averaged over.</param>
        /// <returns>This module.</returns>
        public NeuroStats Configure(int loop, int ticks)
        {
            loopTicks = Mathf.Max(1, loop);
            window = Mathf.Max(1, ticks);
            return this;
        }

        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b)
        {
            seen = b.DeclareStat("neuro.seen", StatStart.Fixed(0f), 0f, 1f);
            streak = b.DeclareStat("neuro.streak", StatStart.Fixed(0f), 0f, HungerClock.MaxTicks);
            lastAction = b.DeclareStat("neuro.lastAction", StatStart.Fixed(-1f), -1f, 1e6f);
            x = b.DeclareStat("neuro.x", StatStart.Fixed(0f), -1e9f, 1e9f);
            z = b.DeclareStat("neuro.z", StatStart.Fixed(0f), -1e9f, 1e9f);
            meals = b.DeclareStat("neuro.meals", StatStart.Fixed(0f), 0f, HungerClock.MaxTicks);
        }

        /// <summary>Empties the window and lists the columns: the rates, the loop share, one share per action.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            ticksRing = new double[window];
            metersRing = new double[window];
            mealsRing = new double[window];
            lastTick = -1;
            columns.Clear();
            columns.AddRange(new[] { "nn_speed", "nn_meals_per_tick", "nn_meals_per_m", "nn_loops" });
            foreach (var a in Species.Actions) columns.Add("nn_share_" + a.Name);
        }

        /// <summary>Never a cause of death.</summary>
        /// <param name="a">A living animal.</param>
        /// <returns>Null.</returns>
        public override string CauseOfDeath(Animal a) => null;

        /// <summary>Updates every living animal's streak, distance and meals, and this tick's slot of the window.</summary>
        /// <param name="animals">The species' living animals.</param>
        /// <param name="causes">Left as they are.</param>
        public override void CheckAll(IReadOnlyList<Animal> animals, string[] causes)
        {
            int tick = World.Tick;
            int slot = ((tick % window) + window) % window;
            if (tick != lastTick) { ticksRing[slot] = 0; metersRing[slot] = 0; mealsRing[slot] = 0; lastTick = tick; }
            for (int i = 0; i < animals.Count; i++)
            {
                var a = animals[i];
                var p = a.Position;
                if (a[seen] < 0.5f)
                {
                    a[seen] = 1f;
                    a[streak] = 1f;
                    a[lastAction] = a.Action;
                    a[meals] = a.Meals;
                }
                else
                {
                    float dx = p.x - a[x], dz = p.z - a[z];
                    metersRing[slot] += Mathf.Sqrt(dx * dx + dz * dz);
                    mealsRing[slot] += Mathf.Max(0f, a.Meals - a[meals]);
                    a[meals] = a.Meals;
                    if (Mathf.RoundToInt(a[lastAction]) == a.Action) a[streak] = Mathf.Min(a[streak] + 1f, HungerClock.MaxTicks);
                    else { a[streak] = 1f; a[lastAction] = a.Action; }
                }
                a[x] = p.x;
                a[z] = p.z;
                ticksRing[slot] += 1;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> StatColumns => columns;

        /// <inheritdoc/>
        public double StatValue(string column)
        {
            switch (column)
            {
                case "nn_speed": return Ratio(metersRing, ticksRing);
                case "nn_meals_per_tick": return Ratio(mealsRing, ticksRing);
                case "nn_meals_per_m": return Ratio(mealsRing, metersRing);
                case "nn_loops": return Share(a => a[streak] >= loopTicks);
            }
            if (column.StartsWith("nn_share_"))
            {
                string name = column.Substring("nn_share_".Length);
                for (int k = 0; k < Species.Actions.Count; k++)
                    if (Species.Actions[k].Name == name) { int index = k; return Share(a => a.Action == index); }
            }
            return 0;
        }

        /// <summary>The window's sum of one ring over the sum of another; 0 when the second is 0.</summary>
        static double Ratio(double[] top, double[] bottom)
        {
            if (top == null) return 0;
            double t = 0, b = 0;
            for (int i = 0; i < top.Length; i++) { t += top[i]; b += bottom[i]; }
            return b > 0 ? t / b : 0;
        }

        /// <summary>The share of the living animals for which the test holds; 0 with none alive.</summary>
        double Share(System.Func<Animal, bool> test)
        {
            var animals = Species.Animals;
            if (animals.Count == 0) return 0;
            int n = 0;
            for (int i = 0; i < animals.Count; i++) if (test(animals[i])) n++;
            return (double)n / animals.Count;
        }
    }
}
