using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only species module that counts the births and deaths it hears and adds two statistics columns.</summary>
    public class HookProbe : SpeciesModule, IStatsColumns
    {
        public int Born, Died;
        public string LastCause;

        static readonly string[] columns = { "probe_born", "probe_died" };

        public override void OnBorn(Animal a) => Born++;

        public override void OnDied(Animal a, string cause)
        {
            Died++;
            LastCause = cause;
        }

        public IEnumerable<string> StatColumns => columns;

        public double StatValue(string column) => column == "probe_born" ? Born : Died;
    }
}
