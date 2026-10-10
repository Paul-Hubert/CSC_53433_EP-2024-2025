using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>Collects the action lines and rule lines that modules contribute to a species' prompt (PROMPT-01).</summary>
    public sealed class PromptWriter
    {
        readonly List<string> actions = new List<string>();
        readonly List<string> rules = new List<string>();

        public Species Species { get; }
        public IReadOnlyList<string> ActionLines => actions;
        public IReadOnlyList<string> RuleLines => rules;

        public PromptWriter(Species species) { Species = species; }

        /// <summary>One line of the action list: "- name: description".</summary>
        public void Action(string name, string description) => actions.Add("- " + name + ": " + description);

        /// <summary>One rule line.</summary>
        public void Rule(string line)
        {
            if (!string.IsNullOrWhiteSpace(line)) rules.Add(line);
        }
    }
}
