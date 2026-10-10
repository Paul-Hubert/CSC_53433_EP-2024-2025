using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>
    /// M11 done-when: every MUST rule of the contract is cited by a test (its Description names the rule id, singly,
    /// as a range "SENSE-01…05" or a list "ACT-10/11"); the test runs then show those tests pass.
    /// </summary>
    public class CoverageTests
    {
        static string Docs => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Docs", "Interface");

        /// <summary>The MUST rules of the contract documents (01–22), as "**SENSE-03 (MUST)**".</summary>
        public static SortedSet<string> MustRules()
        {
            var rules = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var f in Directory.GetFiles(Docs, "*.md"))
            {
                string name = Path.GetFileName(f);
                if (!char.IsDigit(name[0]) || string.CompareOrdinal(name, "30") >= 0) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(f), @"\*\*([A-Z]+-\d+) \(MUST\)\*\*")) rules.Add(m.Groups[1].Value);
            }
            return rules;
        }

        /// <summary>The rule ids cited by the descriptions of every test in the EvoSim test assemblies.</summary>
        public static SortedSet<string> CitedRules()
        {
            var cited = new SortedSet<string>(StringComparer.Ordinal);
            var tests = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("EvoSim.Tests"))
                .SelectMany(a => a.GetTypes()).SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public));
            foreach (var m in tests)
            {
                var d = m.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "TestAttribute" || a.GetType().Name == "UnityTestAttribute");
                if (d == null) continue;
                string text = (d.GetType().GetProperty("Description")?.GetValue(d) as string) ?? "";
                foreach (var attr in m.GetCustomAttributes<DescriptionAttribute>()) text += " " + attr.Properties.Get(NUnit.Framework.Internal.PropertyNames.Description);
                foreach (Match x in Regex.Matches(text, @"\b([A-Z]+)-(\d+)(?:…(\d+))?((?:/\d+)*)"))
                {
                    string prefix = x.Groups[1].Value;
                    int lo = int.Parse(x.Groups[2].Value);
                    int hi = x.Groups[3].Success ? int.Parse(x.Groups[3].Value) : lo;
                    for (int n = lo; n <= hi; n++) cited.Add($"{prefix}-{n:00}");
                    foreach (var more in x.Groups[4].Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                        cited.Add($"{prefix}-{int.Parse(more):00}");
                }
            }
            return cited;
        }

        [Test, Description("M11 (rule 4 of the prompt): every MUST rule of Docs/Interface is cited by at least one test")]
        public void EveryMustRuleIsCited()
        {
            var must = MustRules();
            Assert.Greater(must.Count, 150, "the contract's MUST rules were found");
            var missing = must.Where(r => !CitedRules().Contains(r)).ToList();
            Assert.IsEmpty(missing, "MUST rules no test cites: " + string.Join(", ", missing));
        }
    }
}
