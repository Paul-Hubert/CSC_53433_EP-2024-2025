using System;

namespace EvoSim
{
    /// <summary>A fix button for a validation message (EDIT-02).</summary>
    /// <remarks>The editor's validation panel shows <see cref="Label"/> on a button and runs <see cref="Apply"/> when it is
    /// clicked; tests call Apply directly. Capture what the fix needs in local variables, not in loop variables.</remarks>
    /// <example>
    /// <code>
    /// var action = a;
    /// report.Error("V-03", a, $"Two actions are named '{a.Name}' (ACT-01).",
    ///              new ValidationFix("Rename", () =&gt; action.gameObject.name = action.Name + " 2"));
    /// </code>
    /// </example>
    public sealed class ValidationFix
    {
        /// <summary>The button's text, a short verb phrase: "Rename", "Add Rest", "Move it up".</summary>
        public readonly string Label;
        /// <summary>The change the button makes to the scene.</summary>
        public readonly Action Apply;
        /// <summary>Makes a fix.</summary>
        /// <param name="label">The button's text.</param>
        /// <param name="apply">The change to make when it is clicked.</param>
        public ValidationFix(string label, Action apply) { Label = label; Apply = apply; }
    }
}
