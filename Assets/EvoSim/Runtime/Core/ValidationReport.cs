using System;
using System.Collections.Generic;
using System.Text;

namespace EvoSim
{
    /// <summary>The messages of one validation pass. The same engine runs in the editor, at Initialize and in CI (EDIT-01).</summary>
    /// <remarks>
    /// Modules add messages in their Validate: one per problem, with a validator id (V-…), the object at fault, a
    /// sentence that names the rule, and a fix when there is an obvious one. Errors stop the run from starting; warnings
    /// and information don't.
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void Validate(ValidationReport report)
    /// {
    ///     if (Species.Module&lt;Thirst&gt;() == null) report.Error("V-20", this, "DiesOfThirst needs a Thirst module.");
    /// }
    /// </code>
    /// </example>
    public sealed class ValidationReport
    {
        readonly List<ValidationMessage> messages = new List<ValidationMessage>();

        /// <summary>Every message so far, in the order they were added.</summary>
        public IReadOnlyList<ValidationMessage> Messages => messages;

        /// <summary>Adds a message of any severity.</summary>
        /// <param name="id">The validator id, e.g. <c>"V-08"</c>.</param>
        /// <param name="severity">How serious it is.</param>
        /// <param name="obj">The object at fault (usually <c>this</c>), or null.</param>
        /// <param name="text">What is wrong, naming the rule.</param>
        /// <param name="fix">A fix the editor offers, or null.</param>
        public void Add(string id, Severity severity, UnityEngine.Object obj, string text, ValidationFix fix = null) =>
            messages.Add(new ValidationMessage(id, severity, obj, text, fix));

        /// <summary>Adds an error: the run won't start.</summary>
        /// <param name="id">The validator id.</param>
        /// <param name="obj">The object at fault, or null.</param>
        /// <param name="text">What is wrong, naming the rule.</param>
        /// <param name="fix">A fix the editor offers, or null.</param>
        public void Error(string id, UnityEngine.Object obj, string text, ValidationFix fix = null) => Add(id, Severity.Error, obj, text, fix);
        /// <summary>Adds a warning: the run starts, but something is probably not what was meant.</summary>
        /// <param name="id">The validator id.</param>
        /// <param name="obj">The object at fault, or null.</param>
        /// <param name="text">What is wrong, naming the rule.</param>
        /// <param name="fix">A fix the editor offers, or null.</param>
        public void Warning(string id, UnityEngine.Object obj, string text, ValidationFix fix = null) => Add(id, Severity.Warning, obj, text, fix);
        /// <summary>Adds an information message.</summary>
        /// <param name="id">The validator id.</param>
        /// <param name="obj">The object it is about, or null.</param>
        /// <param name="text">What is worth knowing.</param>
        /// <param name="fix">A fix the editor offers, or null.</param>
        public void Info(string id, UnityEngine.Object obj, string text, ValidationFix fix = null) => Add(id, Severity.Info, obj, text, fix);

        /// <summary>True when at least one message is an error.</summary>
        public bool HasErrors
        {
            get
            {
                foreach (var m in messages) if (m.Severity == Severity.Error) return true;
                return false;
            }
        }

        /// <summary>Counts the messages of one severity.</summary>
        /// <param name="s">The severity to count.</param>
        /// <returns>How many messages have it.</returns>
        public int Count(Severity s)
        {
            int n = 0;
            foreach (var m in messages) if (m.Severity == s) n++;
            return n;
        }

        /// <summary>True if a message with this validator id is present.</summary>
        /// <param name="id">The validator id, e.g. <c>"V-26"</c>.</param>
        /// <returns>True when some message has that id, whatever its severity.</returns>
        public bool Has(string id)
        {
            foreach (var m in messages) if (m.Id == id) return true;
            return false;
        }

        /// <summary>Every message, one per line, for logs and test failures.</summary>
        /// <returns>The messages' texts, each followed by a line break; empty when there is none.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var m in messages) sb.AppendLine(m.ToString());
            return sb.ToString();
        }
    }
}
