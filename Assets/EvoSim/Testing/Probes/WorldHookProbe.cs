using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only world service that counts every species' births and deaths and adds one statistics column.</summary>
    public class WorldHookProbe : WorldService, IStatsColumns
    {
        public int Born, Died;

        static readonly string[] columns = { "world_born" };

        public override void OnBorn(Animal a) => Born++;

        public override void OnDied(Animal a, string cause) => Died++;

        public IEnumerable<string> StatColumns => columns;

        public double StatValue(string column) => Born / 2.0;
    }
}
