using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>
    /// A fixed list of situations, written by sense label and token (15 §2, "the 48 situations"): the prototype's
    /// rows ({"energy": "low", "food": "close", "animal": "adjacent", "animal_ready": true, …}) are matched to a
    /// species' senses by label; a sense the row doesn't name takes a default token.
    /// </summary>
    public sealed class ObservationSet
    {
        /// <summary>One situation: tokens by sense label, plus its source and tags.</summary>
        public sealed class Row
        {
            public readonly Dictionary<string, string> Tokens = new Dictionary<string, string>();
            public string Source = "";
            public readonly List<string> Tags = new List<string>();
        }

        readonly List<Row> rows = new List<Row>();
        public IReadOnlyList<Row> Rows => rows;

        /// <summary>Defaults for senses a row doesn't name: the prototype had no stamina and no cover sense.</summary>
        public readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
        {
            { "stamina", "high" }, { "cover", "none" }, { "age", "adult" }, { "carcass", "none" },
        };

        /// <summary>Reads JSON lines in the prototype's format (prototype/data/observations_v2.jsonl).</summary>
        public static ObservationSet FromJsonLines(string text)
        {
            var set = new ObservationSet();
            foreach (var line in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var o = JObject.Parse(line);
                var row = new Row();
                string ready = null;
                foreach (var p in o.Properties())
                {
                    if (p.Name == "source") row.Source = (string)p.Value;
                    else if (p.Name == "tags") foreach (var t in p.Value) row.Tags.Add((string)t);
                    else if (p.Name == "animal_ready") ready = p.Value.Type == JTokenType.Boolean ? ((bool)p.Value ? "ready" : "not ready") : null;
                    else if (p.Name == "scale") continue;
                    else row.Tokens[p.Name] = (string)p.Value;
                }
                if (ready != null && row.Tokens.TryGetValue("animal", out var band) && band != "none")
                    row.Tokens["animal"] = band + ", " + ready;
                set.rows.Add(row);
            }
            return set;
        }

        /// <summary>
        /// The predator's 48 reference situations (15 §2): energy × prey band × carcass × another predator, with the tags
        /// the brain checks use (prey_present, animal_present, animal_near, any).
        /// </summary>
        public static ObservationSet PredatorReference()
        {
            var set = new ObservationSet();
            foreach (var e in new[] { "low", "medium", "high" })
                foreach (var p in new[] { "none", "adjacent", "close", "far" })
                    foreach (var c in new[] { "none", "close" })
                        foreach (var k in new[] { "none", "close, ready" })
                        {
                            set.Add(new Dictionary<string, string> { { "energy", e }, { "prey", p }, { "carcass", c }, { "other predator", k }, { "stamina", "medium" } }, "reference");
                            var row = set.rows[set.rows.Count - 1];
                            row.Tags.Add("any");
                            if (p != "none") row.Tags.Add("prey_present");
                            if (c != "none") row.Tags.Add("carcass_present");
                            if (k != "none") { row.Tags.Add("animal_present"); row.Tags.Add("animal_near"); }
                        }
            return set;
        }

        /// <summary>Adds a row from label/token pairs.</summary>
        public void Add(Dictionary<string, string> tokens, string source = "custom")
        {
            var row = new Row { Source = source };
            foreach (var kv in tokens) row.Tokens[kv.Key.ToLowerInvariant()] = kv.Value;
            rows.Add(row);
        }

        /// <summary>
        /// The observation of a row for a species: each sense's token by its label (case-insensitive; the kin sense
        /// "Other predator" also answers to "animal"), or the default. Null if a token is unknown to the sense.
        /// </summary>
        public Observation ToObservation(Row row, Species species)
        {
            var tokens = new int[species.Senses.Count];
            for (int i = 0; i < tokens.Length; i++)
            {
                var sense = species.Senses[i];
                string key = sense.Label.ToLowerInvariant();
                if (!row.Tokens.TryGetValue(key, out var token))
                {
                    if (sense is NearestAnimalSense nas && nas.Targets == AnimalSet.Kin && row.Tokens.TryGetValue("animal", out var kin)) token = kin;
                    else if (sense is NearestAnimalSense t && t.Targets == AnimalSet.Threats && row.Tokens.TryGetValue("predator", out var threat)) token = threat;
                    else if (!Defaults.TryGetValue(key, out token)) token = sense.Tokens[sense.Tokens.Count - 1];
                }
                int index = IndexOf(sense.Tokens, token);
                if (index < 0) return null;
                tokens[i] = index;
            }
            return new Observation(tokens);
        }

        /// <summary>Every row as an observation of the species (rows it can't express are skipped).</summary>
        public List<Observation> For(Species species)
        {
            var list = new List<Observation>();
            foreach (var r in rows)
            {
                var o = ToObservation(r, species);
                if (o != null) list.Add(o);
            }
            return list;
        }

        static int IndexOf(IReadOnlyList<string> list, string token)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == token) return i;
            return -1;
        }
    }
}
