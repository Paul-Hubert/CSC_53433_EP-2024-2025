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
        static IEnumerable<string> RuntimeFiles => SourceScan.Files("Runtime").Concat(SourceScan.Files("Http")).Concat(SourceScan.Files("Samples"));

        [Test, Description("RAND-05 (T-RAND-04, static): no foreach over a Dictionary or a HashSet in the runtime code and samples: their order isn't defined (sorted collections and lists are)")]
        public void NoIterationOverHashCollections()
        {
            var declared = new[]
            {
                new System.Text.RegularExpressions.Regex(@"(?<![<\w])(?:Dictionary|HashSet)<[^;=()]*>\s+(\w+)\s*[=;]"),   // not a List<Dictionary<…>>

                new System.Text.RegularExpressions.Regex(@"\bvar\s+(\w+)\s*=\s*new\s+(?:Dictionary|HashSet)<"),
            };
            var hits = new List<string>();
            foreach (var f in RuntimeFiles)
            {
                string code = SourceScan.Code(f);
                var names = declared.SelectMany(re => re.Matches(code).Cast<System.Text.RegularExpressions.Match>()).Select(m => m.Groups[1].Value).Distinct();
                foreach (var n in names) hits.AddRange(SourceScan.Find(new[] { f }, $@"foreach\s*\([^)]*\bin\s+(this\.)?{n}(\.(Keys|Values))?\s*\)"));
            }
            Assert.IsEmpty(hits, string.Join("\n", hits));
        }

        [Test, Description("T-CORE-05 static (CORE-05, ARCH-03): no species module of EvoSim keeps per-animal state: no field holds animals or maps animal ids")]
        public void ModulesKeepNoAnimalState()
        {
            bool MentionsAnimal(System.Type t) => t == typeof(Animal) || (t.IsArray && MentionsAnimal(t.GetElementType())) ||
                                                  (t.IsGenericType && t.GetGenericArguments().Any(MentionsAnimal));
            bool MapsIds(System.Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>) && t.GetGenericArguments()[0] == typeof(int);
            var assemblies = new[] { typeof(Units).Assembly, typeof(HttpBrain).Assembly, typeof(EvoSim.Samples.Thirst).Assembly };
            var hits = new List<string>();
            foreach (var t in assemblies.SelectMany(a => a.GetTypes()).Where(t => typeof(SpeciesModule).IsAssignableFrom(t)))
                foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                              System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                    if (MentionsAnimal(f.FieldType) || MapsIds(f.FieldType)) hits.Add($"{t.Name}.{f.Name} ({f.FieldType.Name})");
            Assert.IsEmpty(hits, "per-animal data belongs in the animal's stats and traits: " + string.Join(", ", hits));
        }

        [Test, Description("T-RAND-01 (RAND-01): no UnityEngine.Random, unseeded System.Random, Guid.NewGuid or time-based seeds in the runtime assemblies and samples")]
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
            string[] coreFolders = { "Core", "Phases", "Recording" };
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
