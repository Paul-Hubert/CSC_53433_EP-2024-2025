using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>One logged fact (OUT-02): kind, tick, animal id, species, and more fields. Keys are written sorted.</summary>
    public sealed class SimEvent
    {
        readonly SortedDictionary<string, object> fields = new SortedDictionary<string, object>(StringComparer.Ordinal);

        public string Kind { get; }
        public int Tick { get; }
        public int Id { get; }
        public string Species { get; }
        public IReadOnlyDictionary<string, object> Fields => fields;

        /// <param name="id">The animal (or egg) id; -1 for events about no animal.</param>
        /// <param name="species">The species id; null for world events.</param>
        public SimEvent(string kind, int tick, int id, string species)
        {
            Kind = kind; Tick = tick; Id = id; Species = species;
            fields["kind"] = kind;
            fields["t"] = tick;
            if (id >= 0) fields["id"] = id;
            if (species != null) fields["species"] = species;
        }

        /// <summary>Adds a field: a number, a string, a bool, a list, or a dictionary of those.</summary>
        public SimEvent With(string key, object value)
        {
            fields[key] = value;
            return this;
        }

        public object this[string key] => fields.TryGetValue(key, out var v) ? v : null;
        public bool Has(string key) => fields.ContainsKey(key);

        /// <summary>The canonical JSON text: sorted keys, fixed number formatting (RAND-10).</summary>
        public string ToJson() => CanonicalJson.Write(fields);

        public override string ToString() => ToJson();
    }
}
