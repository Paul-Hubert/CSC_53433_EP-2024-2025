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
    /// The coverage gate (30, first paragraph): every MUST rule of the contract documents is cited by at least one test,
    /// whose Description names the rule id singly, as a range <c>SENSE-01…05</c> or as a list <c>ACT-10/11</c>. The test
    /// runs then show whether those tests pass.
    /// </summary>
    /// <remarks>
    /// It reads the Markdown files of <c>Docs/Interface</c> from the repository and the Descriptions of every test in the
    /// EvoSim test assemblies by reflection; it builds no world. A test that keeps a new MUST rule only has to name the rule
    /// in its Description.
    /// </remarks>
    public class CoverageTests
    {
        /// <summary>The folder of the contract documents, <c>Docs/Interface</c> at the repository root.</summary>
        static string Docs => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Docs", "Interface");

        /// <summary>
        /// Reads the MUST rules of the contract documents 01–22, written in them as <c>**SENSE-03 (MUST)**</c> or
        /// <c>**TICK-04 (MUST) Title.**</c>.
        /// </summary>
        /// <returns>The rule ids, sorted.</returns>
        public static SortedSet<string> MustRules()
        {
            var rules = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var f in Directory.GetFiles(Docs, "*.md"))
            {
                string name = Path.GetFileName(f);
                if (!char.IsDigit(name[0]) || string.CompareOrdinal(name, "30") >= 0) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(f), @"\*\*([A-Z]+-\d+) \(MUST\)")) rules.Add(m.Groups[1].Value);   // "(MUST)**" or "(MUST) Title.**"
            }
            return rules;
        }

        /// <summary>
        /// Collects the rule ids cited by the Descriptions of every <c>[Test]</c> and <c>[UnityTest]</c> method in the
        /// EvoSim test assemblies (EditMode and PlayMode).
        /// </summary>
        /// <returns>The rule ids, sorted.</returns>
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
                Cite(text, cited);
            }
            return cited;
        }

        /// <summary>
        /// Adds the rule ids a Description cites: <c>SENSE-03</c>, every id of a range <c>SENSE-01…05</c>, every id of a list
        /// <c>ACT-10/11</c>; never a test id such as <c>T-DEC-02</c>.
        /// </summary>
        /// <param name="text">The Description of one test.</param>
        /// <param name="cited">The set the ids are added to, written with two digits (<c>ACT-01</c>).</param>
        public static void Cite(string text, ISet<string> cited)
        {
            foreach (Match x in Regex.Matches(text, @"(?<![A-Z]-)\b([A-Z]+)-(\d+)(?:…(\d+))?((?:/\d+)*)"))
            {
                string prefix = x.Groups[1].Value;
                int lo = int.Parse(x.Groups[2].Value);
                int hi = x.Groups[3].Success ? int.Parse(x.Groups[3].Value) : lo;
                for (int n = lo; n <= hi; n++) cited.Add($"{prefix}-{n:00}");
                foreach (var more in x.Groups[4].Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                    cited.Add($"{prefix}-{int.Parse(more):00}");
            }
        }

        [Test, Description("The coverage gate's own rules: a test id is no citation, ranges and lists are, and a rule written \"(MUST) Title.\" counts")]
        public void CitationsAreReadCorrectly()
        {
            var cited = new SortedSet<string>(StringComparer.Ordinal);
            Cite("T-DEC-02 (DEC-10): …; T-ANIM-02 (ANIM-10, SENSE-01…03, ACT-10/12)", cited);
            CollectionAssert.AreEqual(new[] { "ACT-10", "ACT-12", "ANIM-10", "DEC-10", "SENSE-01", "SENSE-02", "SENSE-03" }, cited);
            Assert.IsTrue(MustRules().Contains("TICK-04"), "\"**TICK-04 (MUST) The reference phase list.**\" is a MUST rule");
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
