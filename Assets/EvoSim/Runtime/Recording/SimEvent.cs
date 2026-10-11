using System;
using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>One logged fact (OUT-02): kind, tick, animal id, species, and more fields. Keys are written sorted.</summary>
    /// <remarks>
    /// <para>Build it, add fields with <see cref="With"/>, then record it with <see cref="EventLog.Record"/> on
    /// <see cref="World.Events"/>. The reference kinds are <c>founder</c>, <c>immigrant</c>, <c>birth</c>, <c>death</c>,
    /// <c>egg_lost</c> and <c>species_created</c> (13 §3); a module may record kinds of its own.</para>
    /// <para>Use only values <see cref="CanonicalJson"/> writes the same way every time: numbers, strings, bools, lists and
    /// sorted dictionaries of those.</para>
    /// </remarks>
    public sealed class SimEvent
    {
        readonly SortedDictionary<string, object> fields = new SortedDictionary<string, object>(StringComparer.Ordinal);

        /// <summary>The kind of event, e.g. <c>"birth"</c>; also its <c>kind</c> field.</summary>
        public string Kind { get; }
        /// <summary>The tick it happened at; also its <c>t</c> field.</summary>
        public int Tick { get; }
        /// <summary>The animal (or egg) id, or -1 for events about no animal; also its <c>id</c> field when 0 or more.</summary>
        public int Id { get; }
        /// <summary>The species id, or null for world events; also its <c>species</c> field when set.</summary>
        public string Species { get; }
        /// <summary>Every field, keys in ordinal order, the four above included.</summary>
        public IReadOnlyDictionary<string, object> Fields => fields;

        /// <summary>An event with its four standard fields; add more with <see cref="With"/>.</summary>
        /// <param name="kind">The kind of event, e.g. <c>"death"</c>.</param>
        /// <param name="tick">The tick it happened at.</param>
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
        /// <param name="key">The field's name; an existing field of that name is replaced.</param>
        /// <param name="value">Its value.</param>
        /// <returns>This event, to chain calls.</returns>
        public SimEvent With(string key, object value)
        {
            fields[key] = value;
            return this;
        }

        /// <summary>A field's value by name.</summary>
        /// <param name="key">The field's name.</param>
        /// <returns>The value, or null when the event has no such field.</returns>
        public object this[string key] => fields.TryGetValue(key, out var v) ? v : null;
        /// <summary>Checks whether the event has a field.</summary>
        /// <param name="key">The field's name.</param>
        /// <returns>True when the field exists.</returns>
        public bool Has(string key) => fields.ContainsKey(key);

        /// <summary>The canonical JSON text: sorted keys, fixed number formatting (RAND-10).</summary>
        /// <returns>One line of JSON, as hashed and written to <c>events.jsonl</c>.</returns>
        public string ToJson() => CanonicalJson.Write(fields);

        /// <summary>The canonical JSON text, as <see cref="ToJson"/>.</summary>
        /// <returns>One line of JSON.</returns>
        public override string ToString() => ToJson();
    }
}
