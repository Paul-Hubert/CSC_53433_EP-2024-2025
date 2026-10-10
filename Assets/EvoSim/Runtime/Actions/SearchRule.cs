using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The prompt's search rule (PROMPT-01): what an animal does when its action has nothing to act on (ACT-04).
    /// Put it on the species' own GameObject so it comes first among the rule lines.
    /// </summary>
    public class SearchRule : SpeciesModule
    {
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt (PROMPT-07).")]
        string promptRule = "If the chosen action has nothing to act on in sight (no food, predator, cover,\n" +
                            "animal or ready partner), the animal searches the surroundings instead.";

        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        public void SetPromptRule(string text) => promptRule = text;
    }
}
