using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test-only world service that counts every species' births and deaths and adds one statistics column
    /// (<c>world_born</c>, after the species' columns in <c>stats.csv</c>).
    /// </summary>
    public class WorldHookProbe : WorldService, IStatsColumns
    {
        /// <summary>The births (founders, babies, immigrants) and the deaths heard so far, all species together.</summary>
        public int Born, Died;

        static readonly string[] columns = { "world_born" };   // shared, never changed

        /// <summary>Counts a birth.</summary>
        /// <inheritdoc/>
        public override void OnBorn(Animal a) => Born++;

        /// <summary>Counts a death.</summary>
        /// <inheritdoc/>
        public override void OnDied(Animal a, string cause) => Died++;

        /// <summary><c>world_born</c>.</summary>
        /// <inheritdoc/>
        public IEnumerable<string> StatColumns => columns;

        /// <summary>
        /// Half of <see cref="Born"/>, whatever the column: a value with a fraction, which only the column's place is tested on.
        /// </summary>
        /// <inheritdoc/>
        public double StatValue(string column) => Born / 2.0;
    }
}
