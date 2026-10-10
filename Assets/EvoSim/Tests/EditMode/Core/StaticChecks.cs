using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>Static checks of the runtime code (30 §3).</summary>
    public class StaticChecks
    {
        static IEnumerable<string> RuntimeFiles => SourceScan.Files("Runtime").Concat(SourceScan.Files("Http"));

        [Test, Description("T-RAND-01 (RAND-01): no UnityEngine.Random, unseeded System.Random, Guid.NewGuid or time-based seeds in the runtime assemblies")]
        public void NoGlobalRandomness()
        {
            var hits = new List<string>();
            hits.AddRange(SourceScan.Find(RuntimeFiles, @"UnityEngine\.Random\b"));
            hits.AddRange(SourceScan.Find(RuntimeFiles, @"(?<![\w.])Random\.(Range|value|InitState|insideUnit|onUnit|rotation|ColorHSV|state)\b"));
            hits.AddRange(SourceScan.Find(RuntimeFiles, @"new\s+(System\.)?Random\s*\("));
            hits.AddRange(SourceScan.Find(RuntimeFiles, @"Guid\.NewGuid"));
            hits.AddRange(SourceScan.Find(RuntimeFiles, @"Environment\.TickCount|DateTime\.(Now|UtcNow)\.(Ticks|Millisecond)"));
            Assert.IsEmpty(hits, string.Join("\n", hits));
        }

        [Test, Description("T-CORE-08 (CORE-03): the core code names no species, action or sense; only reference modules carry such defaults")]
        public void CoreNamesNoSpecies()
        {
            string[] coreFolders = { "Core", "Phases", "Recording", "Population", "Reproduction", "Mutation" };
            var speciesWords = new[] { "prey", "predator", "wolf", "rabbit", "fox" };
            var actionNames = new[] { "eat", "flee", "hide", "follow", "rest", "mate", "hunt", "graze", "scavenge" };
            var hits = new List<string>();
            foreach (var f in SourceScan.Files("Runtime"))
            {
                string folder = Path.GetFileName(Path.GetDirectoryName(f));
                if (!coreFolders.Contains(folder)) continue;
                foreach (var (line, text) in SourceScan.Literals(f))
                {
                    string lower = text.ToLowerInvariant();
                    if (speciesWords.Any(w => System.Text.RegularExpressions.Regex.IsMatch(lower, $@"\b{w}\b")) || actionNames.Contains(lower))
                        hits.Add($"{Path.GetFileName(f)}:{line}: \"{text}\"");
                }
            }
            Assert.IsEmpty(hits, string.Join("\n", hits));
        }
    }
}
