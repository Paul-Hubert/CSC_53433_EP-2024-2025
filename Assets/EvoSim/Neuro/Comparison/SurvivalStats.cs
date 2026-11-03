using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// Live survival columns for the comparison worlds (plan M16 §6.2), and <c>survival.json</c> at the run's end: for each
    /// species, the living, births, newcomers, deaths by cause, and kills by the killer's species.
    /// </summary>
    /// <remarks>
    /// <para>Columns, at the end of each <c>stats.csv</c> row (counts are cumulative): <c>sv_&lt;species&gt;_alive</c>,
    /// <c>_births</c>, <c>_immigrants</c>, <c>_deaths_&lt;cause&gt;</c> for starvation, killed, old_age and migrated, and
    /// <c>sv_&lt;killer&gt;_kills_&lt;victim&gt;</c> for each species whose diet strikes another.</para>
    /// <para><c>survival.json</c> (in the recorder's run folder, when it writes files) adds what the analysis needs and
    /// the events don't hold: each species' maturity, cap, floor, initial population and whether it is neural, the
    /// matched pairs, the stop reason and the <see cref="PlateauStop"/> tick. <see cref="SurvivalAnalysis"/> reads it
    /// with the events. It only reads the world: the run's hash doesn't change with it.</para>
    /// </remarks>
    public class SurvivalStats : WorldService, IStatsColumns
    {
        /// <summary>The causes that always get a column.</summary>
        public static readonly string[] Causes = { "starvation", "killed", "old_age", SurvivalAnalysis.Migrated };

        readonly List<string> columns = new List<string>();
        readonly Dictionary<string, double> values = new Dictionary<string, double>();
        readonly Dictionary<int, string> speciesOf = new Dictionary<int, string>();      // living animals' species, for killers

        /// <summary>Lists the columns for the species of the world.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            columns.Clear(); values.Clear(); speciesOf.Clear();
            foreach (var s in World.AllSpecies)
            {
                string p = "sv_" + s.Id + "_";
                Add(p + "alive"); Add(p + "births"); Add(p + "immigrants");
                foreach (var c in Causes) Add(p + "deaths_" + c);
            }
            foreach (var killer in World.AllSpecies)
                foreach (var victim in World.AllSpecies)
                    if (World.IsThreat(killer, victim)) Add("sv_" + killer.Id + "_kills_" + victim.Id);
        }

        void Add(string column) { columns.Add(column); values[column] = 0; }

        /// <inheritdoc/>
        public override void OnSpeciesAdded(Species species) => Initialize();

        /// <summary>Counts an arrival: a birth, a newcomer, or a founder.</summary>
        /// <inheritdoc/>
        public override void OnBorn(Animal a)
        {
            speciesOf[a.Id] = a.Species.Id;
            if (a.Origin == "birth") Bump("sv_" + a.Species.Id + "_births");
            else if (a.Origin == "immigrant") Bump("sv_" + a.Species.Id + "_immigrants");
        }

        /// <summary>Counts a death by cause, and the kill by the killer's species.</summary>
        /// <inheritdoc/>
        public override void OnDied(Animal a, string cause)
        {
            Bump("sv_" + a.Species.Id + "_deaths_" + cause);
            if (a.Killed && a.KilledBy >= 0 && speciesOf.TryGetValue(a.KilledBy, out var killer))
                Bump("sv_" + killer + "_kills_" + a.Species.Id);
            speciesOf.Remove(a.Id);
        }

        void Bump(string column)
        {
            if (!values.ContainsKey(column)) columns.Add(column);
            values[column] = (values.TryGetValue(column, out var v) ? v : 0) + 1;
        }

        /// <inheritdoc/>
        public IEnumerable<string> StatColumns => columns;

        /// <inheritdoc/>
        public double StatValue(string column)
        {
            if (column.StartsWith("sv_") && column.EndsWith("_alive"))
            {
                var s = World.FindSpecies(column.Substring(3, column.Length - 3 - "_alive".Length));
                return s != null ? s.Animals.Count : 0;
            }
            return values.TryGetValue(column, out var v) ? v : 0;
        }

        /// <summary>The count so far of one column (tests, the report).</summary>
        /// <param name="column">A column name.</param>
        /// <returns>Its value now.</returns>
        public double Value(string column) => StatValue(column);

        /// <summary>Writes <c>survival.json</c> in the recorder's run folder.</summary>
        /// <inheritdoc/>
        public override void OnRunStopped(string reason)
        {
            var recorder = World.Service<RunRecorder>();
            if (recorder == null || !recorder.WriteFiles || recorder.RunFolder == null) return;
            File.WriteAllText(Path.Combine(recorder.RunFolder, "survival.json"), Json(reason) + "\n", new UTF8Encoding(false));
        }

        /// <summary>The content of <c>survival.json</c>, canonical JSON.</summary>
        /// <param name="reason">Why the run stopped.</param>
        /// <returns>The JSON text.</returns>
        public string Json(string reason)
        {
            var root = new SortedDictionary<string, object>(System.StringComparer.Ordinal)
            {
                ["end_tick"] = World.Tick,
                ["stop_reason"] = reason ?? "",
            };
            var plateau = World.GetComponentInChildren<PlateauStop>(true);
            root["plateau_tick"] = plateau != null ? plateau.PlateauTick : -1;
            var pairs = new List<object>();
            var mp = World.GetComponentInChildren<MatchedPairs>(true);
            if (mp != null) foreach (var p in mp.Pairs) pairs.Add(new List<object> { p.jev, p.neural });
            root["pairs"] = pairs;
            var sp = new SortedDictionary<string, object>(System.StringComparer.Ordinal);
            foreach (var s in World.AllSpecies)
            {
                var maturity = s.Declarations.FindTrait("maturity");
                var cap = s.Module<CapRule>();
                var floor = s.Module<FloorRule>();
                var deaths = new SortedDictionary<string, object>(System.StringComparer.Ordinal);
                foreach (var kv in s.Counters.Deaths) deaths[kv.Key] = kv.Value;
                var kills = new SortedDictionary<string, object>(System.StringComparer.Ordinal);
                foreach (var v in World.AllSpecies)
                {
                    string col = "sv_" + s.Id + "_kills_" + v.Id;
                    if (values.TryGetValue(col, out var k) && k > 0) kills[v.Id] = (int)k;
                }
                sp[s.Id] = new SortedDictionary<string, object>(System.StringComparer.Ordinal)
                {
                    ["neural"] = s.Module<NeuralGenomeGene>() != null,
                    ["maturity"] = maturity.IsValid ? Mathf.RoundToInt(maturity.Default) : 0,
                    ["cap"] = cap != null ? cap.Cap : 0,
                    ["floor"] = floor != null ? floor.Floor : 0,
                    ["initial"] = s.InitialPopulation,
                    ["alive"] = s.Animals.Count,
                    ["births"] = s.Counters.Births,
                    ["immigrants"] = s.Counters.Immigrants,
                    ["deaths"] = deaths,
                    ["kills"] = kills,
                };
            }
            root["species"] = sp;
            return CanonicalJson.Write(root);
        }
    }
}
