using System.IO;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// Assets ▸ Create ▸ EvoSim (21 §6): script templates for a new sense, action, gene kind, stat, phase, brain or
    /// mutation operator — each with the methods to override and a matching test — and a mutation deck. Scripts go to
    /// Assets/Student (with its assembly), tests to Assets/Student/Tests (with a test assembly that sees EvoSim.Testing).
    /// </summary>
    public static class ScriptTemplates
    {
        public const string StudentFolder = "Assets/Student";
        public static readonly string[] Kinds = { "Sense", "Action", "Gene", "Stat", "Phase", "Brain", "MutationOperator" };

        /// <summary>The script and its test for a kind and a class name.</summary>
        public static (string script, string test) Render(string kind, string name)
        {
            string lower = name.ToLowerInvariant();
            string script = Header + Body(kind).Replace("#NAME#", name).Replace("#LOWER#", lower);
            string test = TestHeader + TestBody(kind).Replace("#NAME#", name).Replace("#LOWER#", lower);
            return (script, test);
        }

        const string Header = "using System.Collections.Generic;\nusing EvoSim;\nusing UnityEngine;\n\n";
        const string TestHeader = "using EvoSim;\nusing EvoSim.Testing;\nusing NUnit.Framework;\n\n";

        static string Body(string kind)
        {
            switch (kind)
            {
                case "Sense": return
"/// <summary>#NAME#: what it senses, as a few tokens the brain reads (06; recipe 22 §3).</summary>\n" +
"public class #NAME# : Sense\n{\n" +
"    readonly List<string> tokens = new List<string> { \"none\", \"near\", \"far\" };\n\n" +
"    public override IReadOnlyList<string> Tokens => tokens;\n\n" +
"    protected override string DefaultLabel => \"#NAME#\";\n\n" +
"    /// <summary>The token index for this animal now; read only, never change the world here (SENSE-02).</summary>\n" +
"    public override int Read(Animal a, SenseContext s)\n    {\n        return 0;\n    }\n\n" +
"    /// <summary>The fragment of the situation text for a token (V1 short, V2 sentences).</summary>\n" +
"    public override string Write(int token, TextStyle style) => $\"{Label}: {Tokens[token]}.\";\n}\n";
                case "Action": return
"/// <summary>#NAME#: what the animal does when it chooses this action (07; recipe 22 §4).</summary>\n" +
"public class #NAME# : AnimalAction\n{\n" +
"    /// <summary>The action's line in the prompt.</summary>\n" +
"    protected override string DefaultDescription => \"describe what #LOWER# does\";\n\n" +
"    /// <summary>Set an intent through the context (move, interact); don't move the animal yourself (ACT-02).</summary>\n" +
"    public override void Act(Animal a, ActContext c)\n    {\n        c.Search();\n    }\n}\n";
                case "Gene": return
"/// <summary>#NAME#: a gene kind (09; recipe 22 §6) — its founders, and how a value is expressed.</summary>\n" +
"public class #NAME# : Gene\n{\n" +
"    [SerializeField, Tooltip(\"The trait this gene sets.\")] string trait = \"#LOWER#\";\n" +
"    [SerializeField, Tooltip(\"Founder values.\")] List<float> founders = new List<float> { 1f };\n\n" +
"    public override AlleleKind Kind => AlleleKind.Number;\n\n" +
"    public override IReadOnlyList<FounderAllele> FounderPool\n    {\n        get\n        {\n" +
"            var pool = new List<FounderAllele>();\n            foreach (var f in founders) pool.Add(new FounderAllele(AlleleValue.OfNumber(f), \"founder\"));\n" +
"            return pool;\n        }\n    }\n\n" +
"    public override void Express(AlleleValue value, Expression e) => e.SetTrait(trait, value.Number, false);\n}\n";
                case "Stat": return
"/// <summary>#NAME#: a stat every animal of the species carries (05; recipe 22 §8).</summary>\n" +
"public class #NAME# : SpeciesModule\n{\n" +
"    [SerializeField, Min(0f), Tooltip(\"Starting value.\")] float start = 50f;\n" +
"    [SerializeField, Min(0f), Tooltip(\"Maximum.\")] float max = 100f;\n\n" +
"    public StatId Value { get; private set; }\n\n" +
"    public override void Declare(SpeciesBuilder b) => Value = b.DeclareStat(\"#LOWER#\", StatStart.Fixed(start), 0f, max);\n}\n";
                case "Phase": return
"/// <summary>#NAME#: a step of every tick, in its place among the Phases children (12; recipe 22 §10).</summary>\n" +
"public class #NAME# : TickPhase\n{\n" +
"    /// <summary>Draw random numbers only from t.Stream(...) (RAND-03).</summary>\n" +
"    public override void Run(TickContext t)\n    {\n    }\n}\n";
                case "Brain": return
"/// <summary>#NAME#: a brain (08; recipe 22 §7) — one probability row per query, in the species' action order.</summary>\n" +
"public class #NAME# : Brain\n{\n" +
"    public override string Id => \"#LOWER#\";\n\n" +
"    public override BrainAnswer Ask(IReadOnlyList<DecisionQuery> batch)\n    {\n" +
"        var rows = new float[batch.Count][];\n" +
"        for (int i = 0; i < batch.Count; i++) rows[i] = Uniform(batch[i].Species.Actions.Count);\n" +
"        return BrainAnswer.Now(rows);\n    }\n}\n";
                default: return
"/// <summary>#NAME#: a mutation operator (10; recipe 22 §9); draw only from the given stream (MUT-30).</summary>\n" +
"public class #NAME# : MutationOperator\n{\n" +
"    public override bool Accepts(Gene g) => g.Kind == AlleleKind.Number;\n\n" +
"    public override MutationJob StartMutation(Gene gene, Allele parent, RandomStream rng) =>\n" +
"        MutationJob.Done(parent.Number + (rng.Chance(0.5f) ? 1f : -1f), \"#LOWER#\");\n}\n";
            }
        }

        static string TestBody(string kind)
        {
            string build = kind == "Phase" || kind == "Brain"
                ? "        var w = New().Flat(20, 20).Species(\"prey\", s => s.Population(4).Action<RestAction>(\"rest\"))" +
                  (kind == "Phase" ? ".Phase<#NAME#>()" : ".DefaultBrain<#NAME#>()") + ".Build();\n"
                : "";
            switch (kind)
            {
                case "Sense": return Wrap(
"        var w = New().Flat(20, 20).Species(\"prey\", s => s.Population(0).Action<RestAction>(\"rest\").Sense<#NAME#>()).Build();\n" +
"        var a = Place.Animal(w.FindSpecies(\"prey\"), Place.At(10, 10));\n" +
"        var sense = w.FindSpecies(\"prey\").Module<#NAME#>();\n" +
"        Assert.That(sense.Read(a, new SenseContext(w)), Is.InRange(0, sense.Tokens.Count - 1));\n");
                case "Action": return Wrap(
"        var w = New().Flat(20, 20).Phase<ActPhase>().Species(\"prey\", s => s.Population(0).Action<#NAME#>(\"#LOWER#\")).Build();\n" +
"        var a = Place.Animal(w.FindSpecies(\"prey\"), Place.At(10, 10));\n" +
"        a.Choose(\"#LOWER#\");\n        w.Advance(1);\n        Assert.IsFalse(a.IsGone);\n");
                case "Phase":
                case "Brain": return Wrap(build + "        w.Advance(10);\n        Assert.AreEqual(10, w.Tick);\n");
                default: return Wrap(
"        var w = New().Flat(20, 20).Species(\"prey\", s => s.Population(2).Action<RestAction>(\"rest\").Module<#NAME#>()).Build();\n" +
"        Assert.AreEqual(2, w.FindSpecies(\"prey\").Animals.Count);\n");
            }
        }

        static string Wrap(string body) =>
            "/// <summary>Tests of #NAME# (30 §2: worlds built in code with WorldBuilder).</summary>\n" +
            "public class #NAME#Tests : WorldFixture\n{\n    [Test]\n    public void Works()\n    {\n" + body + "    }\n}\n";

        /// <summary>Writes a script and its test under Assets/Student, making the two assemblies the first time.</summary>
        public static string Create(string kind, string name)
        {
            var (script, test) = Render(kind, name);
            Directory.CreateDirectory(StudentFolder + "/Tests");
            if (!File.Exists(StudentFolder + "/Student.asmdef"))
                File.WriteAllText(StudentFolder + "/Student.asmdef",
                    "{\n    \"name\": \"Student\",\n    \"references\": [\"EvoSim.Runtime\"],\n    \"autoReferenced\": true\n}\n");
            if (!File.Exists(StudentFolder + "/Tests/Student.Tests.asmdef"))
                File.WriteAllText(StudentFolder + "/Tests/Student.Tests.asmdef",
                    "{\n    \"name\": \"Student.Tests\",\n    \"references\": [\"Student\", \"EvoSim.Runtime\", \"EvoSim.Testing\", \"UnityEngine.TestRunner\", \"UnityEditor.TestRunner\"],\n" +
                    "    \"includePlatforms\": [\"Editor\"],\n    \"overrideReferences\": true,\n    \"precompiledReferences\": [\"nunit.framework.dll\"],\n" +
                    "    \"autoReferenced\": false,\n    \"defineConstraints\": [\"UNITY_INCLUDE_TESTS\"]\n}\n");
            string path = $"{StudentFolder}/{name}.cs";
            File.WriteAllText(path, script);
            File.WriteAllText($"{StudentFolder}/Tests/{name}Tests.cs", test);
            AssetDatabase.Refresh();
            return path;
        }

        static void Ask(string kind)
        {
            string path = EditorUtility.SaveFilePanelInProject("New " + kind, "My" + kind, "cs", "The class name is the file name.", StudentFolder);
            if (string.IsNullOrEmpty(path)) return;
            string name = Path.GetFileNameWithoutExtension(path);
            var created = Create(kind, name);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(created);
        }

        [MenuItem("Assets/Create/EvoSim/Script/Sense", false, 80)] static void NewSense() => Ask("Sense");
        [MenuItem("Assets/Create/EvoSim/Script/Action", false, 80)] static void NewAction() => Ask("Action");
        [MenuItem("Assets/Create/EvoSim/Script/Gene kind", false, 80)] static void NewGene() => Ask("Gene");
        [MenuItem("Assets/Create/EvoSim/Script/Stat", false, 80)] static void NewStat() => Ask("Stat");
        [MenuItem("Assets/Create/EvoSim/Script/Phase", false, 80)] static void NewPhase() => Ask("Phase");
        [MenuItem("Assets/Create/EvoSim/Script/Brain", false, 80)] static void NewBrain() => Ask("Brain");
        [MenuItem("Assets/Create/EvoSim/Script/Mutation operator", false, 80)] static void NewOperator() => Ask("MutationOperator");

        [MenuItem("Assets/Create/EvoSim/Mutation Deck", false, 81)]
        static void NewDeck()
        {
            string path = EditorUtility.SaveFilePanelInProject("New mutation deck", "MyDeck", "txt", "One instruction per line; # starts a comment (MUT-10).");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, "# One instruction per line (MUT-10). The reference deck v4:\n" + File.ReadAllText("Assets/EvoSim/Data/Decks/mutate_v4.txt"));
            AssetDatabase.Refresh();
        }
    }
}
