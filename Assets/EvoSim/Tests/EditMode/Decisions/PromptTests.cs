using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EvoSim.Testing;
using NUnit.Framework;
using UnityEngine;

namespace EvoSim.Tests
{
    /// <summary>The prompt (08 §7): assembly, placeholders, ids, frozen prompts, names in texts.</summary>
    public class PromptTests : WorldFixture
    {
        const string Points = "Distribute 100 points across the actions according to how likely this animal is to choose each.";

        World Lab(string name = "Test World") => New(name: name).Ecology(48f).ReferencePhases()
            .DefaultBrain<ScriptedBrain>(b => b.Instruction = Points).Build();

        static DecisionQuery Query(World w, Species s, Dictionary<string, string> tokens, Genome genome = null)
        {
            var set = new ObservationSet();
            set.Add(tokens);
            var o = set.ToObservation(set.Rows[0], s);
            genome ??= Place.Animal(s, Place.At(1, 1)).Genome;
            var genes = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < s.Genes.Count; i++) genes.Add(new KeyValuePair<string, string>(s.Genes[i].Label, genome[i].Text));
            return new DecisionQuery(s, genes, o, s.Describe(o, w.TextStyle), w.TextStyle, genome.BrainKey);
        }

        static readonly Dictionary<string, string> ExampleSituation = new Dictionary<string, string>
        {
            { "energy", "low" }, { "stamina", "high" }, { "food", "close" }, { "predator", "medium" }, { "cover", "close" }, { "animal", "none" }, { "age", "adult" },
        };

        const string ExamplePrompt =
            "You decide what a wild animal does next in a simple world.\n\n" +
            "Actions:\n" +
            "- eat: go to the nearest visible food and eat it\n" +
            "- flee: run away from the nearest predator\n" +
            "- hide: go to the nearest cover and stay in it; predators can't see or catch an animal in cover\n" +
            "- follow: move toward the nearest other animal\n" +
            "- rest: stay still to catch your breath and save energy\n" +
            "- mate: walk to the nearest ready partner in sight and breed with it\n" +
            "If the chosen action has nothing to act on in sight (no food, predator, cover,\n" +
            "animal or ready partner), the animal searches the surroundings instead.\n" +
            "Animals move 1 meter per step. Predators run 2 meters per step when they hunt,\n" +
            "but they have 30 stamina against an animal's 60, so a long chase tires them first.\n" +
            "Every meter moved costs stamina. Standing still brings it back, which costs some\n" +
            "energy until stamina is full. Without stamina an animal cannot move.\n" +
            "Breeding needs only one of the two to choose mate: an adult that chooses mate\n" +
            "breeds as soon as it reaches a ready partner, whatever the partner is doing.\n\n" +
            "This animal's instincts (its genes). They define its personality: follow them\n" +
            "even when they seem unwise. Instincts that are meaningless have no effect.\n" +
            "- eat: \"Eat whenever food is close.\"\n" +
            "- flee: \"Run from any predator you see.\"\n" +
            "- hide: \"Hide when a predator is close.\"\n" +
            "- follow: \"Stay close to other animals.\"\n" +
            "- rest: \"Rest when you are tired.\"\n" +
            "- mate: \"Look for a partner when energy is high.\"\n\n" +
            "Situation: Energy: low. Stamina: high. Food: 1-4 meters away. Predator: 4-10 meters away. Cover: 1-4 meters away. Animal: none within 20 meters. Age: adult.\n\n" +
            Points;

        [Test, Description("T-PROMPT-01 (PROMPT-01, PROMPT-02) and the 08 §7 example: header, one line per action in order, the modules' rules, the genes block, the situation, the ask")]
        public void AssembledPrompt()
        {
            var w = Lab();
            var prey = w.FindSpecies("prey");
            var brain = w.GetComponentInChildren<ScriptedBrain>();
            string prompt = Query(w, prey, ExampleSituation).Prompt(brain);
            Assert.AreEqual(ExamplePrompt, prompt);
        }

        [Test, Description("T-PROMPT-02 (PROMPT-03): the predators' run speed changed from 2 to 3 → the rule line says 3")]
        public void NumbersComeFromSettings()
        {
            var b = New().Ecology(48f).ReferencePhases().DefaultBrain<ScriptedBrain>();
            b.Root.transform.Find("predator/Life/Locomotion").GetComponent<KinematicLocomotion>().SetSpeeds(1f, 3f);
            var w = b.Build();
            StringAssert.Contains("Predators run 3 meters per step", w.FindSpecies("prey").Prompt.Text);
            StringAssert.Contains("Predators run 3 meters per step", w.FindSpecies("predator").Prompt.Text);
        }

        [Test, Description("T-PROMPT-03 (PROMPT-04): the assembled prompts of both species for the reference genomes and situations → the pinned snapshot; a frozen prompt gets exactly {genes}, {situation}, {ask} filled")]
        public void PromptSnapshotAndFrozenPrompts()
        {
            var w = Lab();
            var brain = w.GetComponentInChildren<ScriptedBrain>();
            var sb = new StringBuilder();
            var preySet = ObservationSet.FromJsonLines(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "EvoSim/Data/Observations/prey_observations_v2.jsonl")));
            foreach (var s in w.AllSpecies)
            {
                var animal = Place.Animal(s, Place.At(2, 2));
                var rows = s.Id == "prey" ? preySet.Rows.Take(3).Select(r => r.Tokens).ToList()
                    : new List<Dictionary<string, string>> { new Dictionary<string, string> { { "energy", "low" }, { "stamina", "high" }, { "prey", "close" }, { "carcass", "none" }, { "other predator", "none" } } };
                foreach (var row in rows)
                    sb.Append("=== ").Append(s.Id).Append('\n').Append(Query(w, s, row, animal.Genome).Prompt(brain)).Append("\n\n");
            }
            sb.Append("=== example 08 §7\n").Append(Query(w, w.FindSpecies("prey"), ExampleSituation).Prompt(brain)).Append('\n');
            Golden.Check("prompts.txt", sb.ToString());

            var frozen = new TextAsset("Frozen rules.\nGenes:\n{genes}\nNow: {situation}\n{ask}");
            var b2 = New(name: "frozen").Ecology(48f).ReferencePhases().DefaultBrain<ScriptedBrain>(x => x.Instruction = "Pick one.");
            b2.Root.transform.Find("prey").GetComponent<Species>().SetFrozenPrompt(frozen);
            var w2 = b2.Build();
            var prey = w2.FindSpecies("prey");
            var q = Query(w2, prey, ExampleSituation);
            Assert.AreEqual("Frozen rules.\nGenes:\n" + q.GenesBlock + "\nNow: " + q.Situation + "\nPick one.", q.Prompt(w2.GetComponentInChildren<ScriptedBrain>()));
        }

        [Test, Description("T-PROMPT-04 (PROMPT-05, DEC-33): a one-character change in a template → a new prompt id and a cache miss")]
        public void PromptIds()
        {
            var w = Lab();
            var prey = w.FindSpecies("prey");
            var brain = w.GetComponentInChildren<ScriptedBrain>();
            var q = Query(w, prey, ExampleSituation);
            string id = prey.Prompt.Id, key = AnswerCache.KeyFor(brain, q);
            prey.SetPromptHeader(prey.PromptHeader + "!");
            Assert.IsTrue(w.Initialize());
            Assert.AreNotEqual(id, prey.Prompt.Id);
            Assert.AreNotEqual(key, AnswerCache.KeyFor(brain, Query(w, prey, ExampleSituation)));
        }

        [Test, Description("T-PROMPT-06 (PROMPT-07): the header, an action's line and a rule line edited → the prompt changes; the answer instruction has no inspector field and comes from the brain class")]
        public void InspectorFieldsAndTheAnswerInstruction()
        {
            var b = New().Ecology(48f).ReferencePhases().DefaultBrain<ScriptedBrain>(x => x.Instruction = Points);
            var prey = b.Root.transform.Find("prey");
            prey.GetComponent<Species>().SetPromptHeader("A new header.");
            prey.Find("Actions/eat").GetComponent<EatAction>().SetDescription("graze the nearest grass");
            prey.Find("Life/Stamina").GetComponent<Stamina>().SetPromptRule("Stamina is {max} meters.");
            var w = b.Build();
            string text = w.FindSpecies("prey").Prompt.Text;
            StringAssert.StartsWith("A new header.", text);
            StringAssert.Contains("- eat: graze the nearest grass", text);
            StringAssert.Contains("Stamina is 60 meters.", text);
            var fields = typeof(Brain).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.GetCustomAttribute<SerializeField>() != null || f.IsPublic).Select(f => f.Name.ToLowerInvariant());
            Assert.IsFalse(fields.Any(f => f.Contains("instruction") || f.Contains("ask")), "no inspector field for the answer instruction");
            var other = w.GetComponentInChildren<ScriptedBrain>();
            var q = Query(w, w.FindSpecies("prey"), ExampleSituation);
            StringAssert.EndsWith(Points, q.Prompt(other));
            other.Instruction = "Answer with one letter.";
            StringAssert.EndsWith("Answer with one letter.", q.Prompt(other), "another brain, another ending");
        }

        [Test, Description("V-53: an unknown placeholder in a prompt field, and a frozen prompt without its holes → errors")]
        public void PlaceholderErrors()
        {
            var b = New().Ecology(48f).ReferencePhases().DefaultBrain<ScriptedBrain>();
            b.Root.transform.Find("prey/Life/Stamina").GetComponent<Stamina>().SetPromptRule("Stamina is {nothing.here} meters.");
            var r = new ValidationReport();
            Assert.IsFalse(b.BuildUninitialized().Prepare(r));
            Assert.IsTrue(r.Has("V-53"), r.ToString());
            var b2 = New(name: "frozen").Ecology(48f).ReferencePhases().DefaultBrain<ScriptedBrain>();
            b2.Root.transform.Find("prey").GetComponent<Species>().SetFrozenPrompt(new TextAsset("No holes here."));
            var r2 = new ValidationReport();
            Assert.IsFalse(b2.BuildUninitialized().Prepare(r2));
            Assert.AreEqual(3, r2.Messages.Count(m => m.Id == "V-53"));
        }

        [Test, Description("T-SPEC-01 (SPEC-01, SPEC-20): situation texts name species; renaming predator to \"wolf\" changes the text and the cache key")]
        public void NamesInTexts()
        {
            var w = Lab();
            var prey = w.FindSpecies("prey");
            var brain = w.GetComponentInChildren<ScriptedBrain>();
            var q = Query(w, prey, ExampleSituation);
            StringAssert.Contains("Predator: 4-10 meters away.", q.Situation);
            string key = AnswerCache.KeyFor(brain, q);

            var b = New(name: "wolves").Ecology(48f).ReferencePhases().DefaultBrain<ScriptedBrain>(x => x.Instruction = Points);
            b.Root.transform.Find("predator").GetComponent<Species>().SetNames("predator", "wolf");
            var w2 = b.Build();
            var prey2 = w2.FindSpecies("prey");
            var row = new Dictionary<string, string>(ExampleSituation);
            row.Remove("predator");
            row["wolf"] = "medium";
            var q2 = Query(w2, prey2, row);
            StringAssert.Contains("Wolf: 4-10 meters away.", q2.Situation);
            Assert.AreNotEqual(key, AnswerCache.KeyFor(w2.GetComponentInChildren<ScriptedBrain>(), q2));
        }
    }
}
