using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// A module that adds columns to stats.csv (13 §4). A species module's columns get its species' prefix and hold that
    /// species' values; a world module's columns go at the end of the row, unprefixed. A new column during the run
    /// rewrites the header, with empty cells in the rows before it.
    /// </summary>
    public interface IStatsColumns
    {
        /// <summary>The column names, without the species prefix, in a fixed order.</summary>
        IEnumerable<string> StatColumns { get; }

        /// <summary>The value of one of the columns now (rounded to 4 decimals in the file).</summary>
        double StatValue(string column);
    }
}
