using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// A module that adds columns to stats.csv (13 §4). A species module's columns get its species' prefix and hold that
    /// species' values; a world module's columns go at the end of the row, unprefixed. A new column during the run
    /// rewrites the header, with empty cells in the rows before it.
    /// </summary>
    /// <remarks>
    /// <para>Implement it on a <see cref="SpeciesModule"/> (its columns follow the species' <c>act_…</c> columns) or on a
    /// <see cref="WorldModule"/>, a phase or a service (its columns end the row, phases first). The
    /// <see cref="RunRecorder"/> asks for the values when it writes a row, every <c>statsEvery</c> ticks, at the end of the
    /// record phase. A column whose name is already in the row is skipped.</para>
    /// <para><see cref="StatValue"/> only reads: it must not change the world or draw random numbers.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // A world service that counts every species' births and adds one column (from EvoSim's test probes).
    /// public class WorldHookProbe : WorldService, IStatsColumns
    /// {
    ///     public int Born;
    ///     static readonly string[] columns = { "world_born" };
    ///
    ///     public override void OnBorn(Animal a) =&gt; Born++;
    ///
    ///     public IEnumerable&lt;string&gt; StatColumns =&gt; columns;
    ///     public double StatValue(string column) =&gt; Born;
    /// }
    /// </code>
    /// </example>
    public interface IStatsColumns
    {
        /// <summary>The column names, without the species prefix, in a fixed order.</summary>
        IEnumerable<string> StatColumns { get; }

        /// <summary>The value of one of the columns now (rounded to 4 decimals in the file).</summary>
        /// <param name="column">One of the names of <see cref="StatColumns"/>, without the prefix.</param>
        /// <returns>The value; a count, a mean or a rate.</returns>
        double StatValue(string column);
    }
}
