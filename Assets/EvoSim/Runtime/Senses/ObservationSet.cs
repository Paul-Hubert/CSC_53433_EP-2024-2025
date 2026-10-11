using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EvoSim
{
    /// <summary>
    /// A fixed list of situations, written by sense label and token (15 §2, "the 48 situations"): the prototype's
    /// rows ({"energy": "low", "food": "close", "animal": "adjacent", "animal_ready": true, …}) are matched to a
    /// species' senses by label; a sense the row doesn't name takes a default token.
    /// </summary>
    /// <remarks>
    /// The brain integrity gates and tests use it to ask a brain about the same situations whatever the species' sense
    /// order: <see cref="FromJsonLines"/> or <see cref="PredatorReference"/> gives the rows, <see cref="For"/> turns them
    /// into observations of one species.
    /// </remarks>
    public sealed class ObservationSet
    {
        /// <summary>One situation: tokens by sense label, plus its source and tags.</summary>
        public sealed class Row
        {
            /// <summary>Token by sense label in lower case: "energy" → "low", "animal" → "adjacent, ready"….</summary>
            public readonly Dictionary<string, string> Tokens = new Dictionary<string, string>();

            /// <summary>Where the row comes from: "synthetic", "reference", "custom"… (the JSON's "source").</summary>
            public string Source = "";

            /// <summary>Labels the gates group situations by: "any", "prey_present", "animal_near"….</summary>
            public readonly List<string> Tags = new List<string>();
        }

        readonly List<Row> rows = new List<Row>();

        /// <summary>The situations, in the order they were read or added.</summary>
        public IReadOnlyList<Row> Rows => rows;

        /// <summary>
        /// Defaults by sense label for senses a row doesn't name: the prototype had no stamina and no cover sense. A sense
        /// with no default here takes its last token (usually "none").
        /// </summary>
        public readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
        {
            { "stamina", "high" }, { "cover", "none" }, { "age", "adult" }, { "carcass", "none" },
        };

        /// <summary>
        /// Reads JSON lines in the prototype's format (prototype/data/observations_v2.jsonl, copied to
        /// <c>Assets/EvoSim/Data/Observations/prey_observations_v2.jsonl</c>).
        /// </summary>
        /// <param name="text">One JSON object per line. "source" and "tags" fill the row's fields, "scale" is skipped, a
        /// boolean "animal_ready" adds ", ready" or ", not ready" to the "animal" token, and every other key is a sense
        /// label with its token. Keys are used as written, so write them in lower case.</param>
        /// <returns>The set, one row per non-empty line.</returns>
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
                // The kin sense writes readiness into its token ("adjacent, ready"); the prototype kept it apart.
                if (ready != null && row.Tokens.TryGetValue("animal", out var band) && band != "none")
                    row.Tokens["animal"] = band + ", " + ready;
                set.rows.Add(row);
            }
            return set;
        }

        /// <summary>
        /// Builds the predator's 48 reference situations (15 §2): energy × prey band × carcass × another predator, with the
        /// tags the brain checks use (any, prey_present, carcass_present, animal_present, animal_near).
        /// </summary>
        /// <returns>A new set of 48 rows (3 energies × 4 prey bands × 2 carcass × 2 other predator), stamina "medium".</returns>
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
        /// <param name="tokens">Token by sense label, such as "Energy" → "low"; labels are stored in lower case.</param>
        /// <param name="source">Where the row comes from, kept in <see cref="Row.Source"/>.</param>
        public void Add(Dictionary<string, string> tokens, string source = "custom")
        {
            var row = new Row { Source = source };
            foreach (var kv in tokens) row.Tokens[kv.Key.ToLowerInvariant()] = kv.Value;
            rows.Add(row);
        }

        /// <summary>
        /// Gives the observation of a row for a species: each sense's token by its label (case-insensitive; the kin sense
        /// "Other predator" also answers to "animal", a threat sense to "predator"), or the default.
        /// </summary>
        /// <param name="row">A row of this set.</param>
        /// <param name="species">The species whose senses read the row, in its sense order.</param>
        /// <returns>The observation, or null if a token is unknown to its sense (the species can't express the row).</returns>
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

        /// <summary>Gives every row as an observation of the species.</summary>
        /// <param name="species">The species whose senses read the rows.</param>
        /// <returns>The observations in row order; rows the species can't express are skipped.</returns>
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

        /// <summary>The index of a token in a sense's token list, or -1 when the sense has no such token.</summary>
        static int IndexOf(IReadOnlyList<string> list, string token)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == token) return i;
            return -1;
        }
    }
}
