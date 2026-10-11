using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The prompt's search rule (PROMPT-01): what an animal does when its action has nothing to act on (ACT-04).
    /// Put it on the species' own GameObject so it comes first among the rule lines.
    /// </summary>
    /// <remarks>
    /// It only writes the line that tells the brain about searching; the searching itself is
    /// <see cref="ActContext.Search"/>, which every reference action calls when nothing is in sight.
    /// </remarks>
    public class SearchRule : SpeciesModule
    {
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt (PROMPT-07).")]
        string promptRule = "If the chosen action has nothing to act on in sight (no food, predator, cover,\n" +
                            "animal or ready partner), the animal searches the surroundings instead.";

        /// <summary>Adds the search rule line to the prompt's rules.</summary>
        /// <inheritdoc/>
        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        /// <summary>Sets the rule line from code (WorldBuilder, tests).</summary>
        /// <param name="text">The new line; empty or blank adds no line.</param>
        public void SetPromptRule(string text) => promptRule = text;
    }
}
