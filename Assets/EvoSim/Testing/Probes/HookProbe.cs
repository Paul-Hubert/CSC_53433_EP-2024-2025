using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test-only species module that counts the births and deaths it hears and adds two statistics columns
    /// (<c>probe_born</c>, <c>probe_died</c>, written with the species' prefix in <c>stats.csv</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// var b = New(5, name: "hooks").Lab1(prey: 20, predators: 3);
    /// var probe = b.Root.transform.Find("prey").gameObject.AddComponent&lt;HookProbe&gt;();
    /// var w = b.Build();
    /// w.Advance(800);
    /// var c = w.FindSpecies("prey").Counters;
    /// Assert.AreEqual(c.Founders + c.Births + c.Immigrants, probe.Born);
    /// Assert.AreEqual(c.DeathsTotal, probe.Died);
    /// </code>
    /// </example>
    public class HookProbe : SpeciesModule, IStatsColumns
    {
        /// <summary>The births (founders, babies, immigrants) and the deaths heard so far.</summary>
        public int Born, Died;
        /// <summary>The cause of the last death heard, or null.</summary>
        public string LastCause;

        static readonly string[] columns = { "probe_born", "probe_died" };   // shared, never changed

        /// <summary>Counts a birth.</summary>
        /// <inheritdoc/>
        public override void OnBorn(Animal a) => Born++;

        /// <summary>Counts a death and keeps its cause.</summary>
        /// <inheritdoc/>
        public override void OnDied(Animal a, string cause)
        {
            Died++;
            LastCause = cause;
        }

        /// <summary><c>probe_born</c> and <c>probe_died</c>.</summary>
        /// <inheritdoc/>
        public IEnumerable<string> StatColumns => columns;

        /// <summary><see cref="Born"/> for <c>probe_born</c>, <see cref="Died"/> for any other column.</summary>
        /// <inheritdoc/>
        public double StatValue(string column) => column == "probe_born" ? Born : Died;
    }
}
