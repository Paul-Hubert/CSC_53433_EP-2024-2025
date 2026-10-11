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
    /// <remarks>
    /// <para>In the Project window: <c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Sense / Action / Gene kind / Stat / Phase / Brain /
    /// Mutation operator</c>. A save dialog asks for the class name (the file name); the script
    /// <c>Assets/Student/&lt;Name&gt;.cs</c> and its test <c>Assets/Student/Tests/&lt;Name&gt;Tests.cs</c> are written and the
    /// script is selected. Each script compiles as it is and does something trivial, with the methods to override; each
    /// test builds a small world in code (<c>WorldBuilder</c>) that uses the new class, and checks one fact.
    /// <c>Assets ▸ Create ▸ EvoSim ▸ Mutation Deck</c> saves a copy of the reference deck to edit.</para>
    /// <para>The first time, the <c>Student</c> assembly (it sees EvoSim.Runtime, EvoSim.Http and EvoSim.Samples) and the
    /// <c>Student.Tests</c> assembly (editor only, with EvoSim.Testing and NUnit) are created.</para>
    /// </remarks>
    public static class ScriptTemplates
    {
        /// <summary>The students' folder: <c>Assets/Student</c>, tests in <c>Assets/Student/Tests</c>.</summary>
        public const string StudentFolder = "Assets/Student";
        /// <summary>The kinds of script a template exists for, as <see cref="Render"/> and <see cref="Create"/> take them.</summary>
        public static readonly string[] Kinds = { "Sense", "Action", "Gene", "Stat", "Phase", "Brain", "MutationOperator" };

        /// <summary>The script and its test for a kind and a class name.</summary>
        /// <param name="kind">One of <see cref="Kinds"/>; an unknown kind gets the mutation operator template.</param>
        /// <param name="name">The class name; the test class is <c>&lt;name&gt;Tests</c>.</param>
        /// <returns>The C# source of the script and of its test.</returns>
        public static (string script, string test) Render(string kind, string name)
        {
            string lower = name.ToLowerInvariant();
            string script = Header + Body(kind).Replace("#NAME#", name).Replace("#LOWER#", lower);
            string test = TestHeader + TestBody(kind).Replace("#NAME#", name).Replace("#LOWER#", lower);
            return (script, test);
        }

        /// <summary>The using lines of every script.</summary>
        const string Header = "using System.Collections.Generic;\nusing EvoSim;\nusing UnityEngine;\n\n";
        /// <summary>The using lines of every test.</summary>
        const string TestHeader = "using EvoSim;\nusing EvoSim.Testing;\nusing NUnit.Framework;\n\n";

        /// <summary>The script template of a kind, with <c>#NAME#</c> and <c>#LOWER#</c> for the class name and its lower case.</summary>
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

        /// <summary>The test template of a kind: a world built in code that uses the new class, and one check.</summary>
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

        /// <summary>A test class <c>#NAME#Tests</c> with one test, <c>Works</c>, around the given body.</summary>
        static string Wrap(string body) =>
            "/// <summary>Tests of #NAME# (30 §2: worlds built in code with WorldBuilder).</summary>\n" +
            "public class #NAME#Tests : WorldFixture\n{\n    [Test]\n    public void Works()\n    {\n" + body + "    }\n}\n";

        /// <summary>Makes Assets/Student with its assembly and its test assembly, the first time.</summary>
        /// <remarks>Existing assembly files are left as they are, so a student may add references to them.</remarks>
        public static void EnsureStudentAssemblies()
        {
            Directory.CreateDirectory(StudentFolder + "/Tests");
            if (!File.Exists(StudentFolder + "/Student.asmdef"))
                File.WriteAllText(StudentFolder + "/Student.asmdef",
                    "{\n    \"name\": \"Student\",\n    \"references\": [\"EvoSim.Runtime\", \"EvoSim.Http\", \"EvoSim.Samples\"],\n    \"autoReferenced\": true\n}\n");   // HTTP brains and clients, and the samples, to build on
            if (!File.Exists(StudentFolder + "/Tests/Student.Tests.asmdef"))
                File.WriteAllText(StudentFolder + "/Tests/Student.Tests.asmdef",
                    "{\n    \"name\": \"Student.Tests\",\n    \"references\": [\"Student\", \"EvoSim.Runtime\", \"EvoSim.Http\", \"EvoSim.Samples\", \"EvoSim.Testing\", \"UnityEngine.TestRunner\", \"UnityEditor.TestRunner\"],\n" +
                    "    \"includePlatforms\": [\"Editor\"],\n    \"overrideReferences\": true,\n    \"precompiledReferences\": [\"nunit.framework.dll\"],\n" +
                    "    \"autoReferenced\": false,\n    \"defineConstraints\": [\"UNITY_INCLUDE_TESTS\"]\n}\n");
        }

        /// <summary>Writes a script and its test under Assets/Student, making the two assemblies the first time.</summary>
        /// <remarks>Files of the same name are overwritten. Unity imports and compiles the new files.</remarks>
        /// <param name="kind">One of <see cref="Kinds"/>.</param>
        /// <param name="name">The class name, which is also the file name.</param>
        /// <returns>The script's path, <c>Assets/Student/&lt;name&gt;.cs</c>.</returns>
        public static string Create(string kind, string name)
        {
            var (script, test) = Render(kind, name);
            EnsureStudentAssemblies();
            string path = $"{StudentFolder}/{name}.cs";
            File.WriteAllText(path, script);
            File.WriteAllText($"{StudentFolder}/Tests/{name}Tests.cs", test);
            AssetDatabase.Refresh();
            return path;
        }

        /// <summary>
        /// Asks for a class name in a save dialog, then creates the script and its test and selects the script. Only the
        /// chosen file name is used: the files always go to <see cref="StudentFolder"/>.
        /// </summary>
        static void Ask(string kind)
        {
            string path = EditorUtility.SaveFilePanelInProject("New " + kind, "My" + kind, "cs", "The class name is the file name.", StudentFolder);
            if (string.IsNullOrEmpty(path)) return;
            string name = Path.GetFileNameWithoutExtension(path);
            var created = Create(kind, name);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(created);
        }

        /// <summary><c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Sense</c>: a <see cref="Sense"/> with three tokens, and its test.</summary>
        [MenuItem("Assets/Create/EvoSim/Script/Sense", false, 80)] static void NewSense() => Ask("Sense");
        /// <summary><c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Action</c>: an <see cref="AnimalAction"/> that searches, and its test.</summary>
        [MenuItem("Assets/Create/EvoSim/Script/Action", false, 80)] static void NewAction() => Ask("Action");
        /// <summary>
        /// <c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Gene kind</c>: a number <see cref="Gene"/> that sets a trait, and its test.
        /// </summary>
        [MenuItem("Assets/Create/EvoSim/Script/Gene kind", false, 80)] static void NewGene() => Ask("Gene");
        /// <summary><c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Stat</c>: a species module that declares a stat, and its test.</summary>
        [MenuItem("Assets/Create/EvoSim/Script/Stat", false, 80)] static void NewStat() => Ask("Stat");
        /// <summary><c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Phase</c>: an empty <see cref="TickPhase"/>, and its test.</summary>
        [MenuItem("Assets/Create/EvoSim/Script/Phase", false, 80)] static void NewPhase() => Ask("Phase");
        /// <summary>
        /// <c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Brain</c>: a <see cref="Brain"/> that answers uniform probabilities, and its test.
        /// </summary>
        [MenuItem("Assets/Create/EvoSim/Script/Brain", false, 80)] static void NewBrain() => Ask("Brain");
        /// <summary><c>Assets ▸ Create ▸ EvoSim ▸ Script ▸ Mutation operator</c>: a <see cref="MutationOperator"/> that adds ±1 to a
        /// number, and its test.</summary>
        [MenuItem("Assets/Create/EvoSim/Script/Mutation operator", false, 80)] static void NewOperator() => Ask("MutationOperator");

        /// <summary>
        /// <c>Assets ▸ Create ▸ EvoSim ▸ Mutation Deck</c>: asks where to save a <c>.txt</c> deck and writes a copy of the
        /// reference deck <c>mutate_v4.txt</c> there, one mutation instruction per line (MUT-10).
        /// </summary>
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
