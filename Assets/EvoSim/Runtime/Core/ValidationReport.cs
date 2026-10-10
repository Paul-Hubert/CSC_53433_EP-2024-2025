using System;
using System.Collections.Generic;
using System.Text;

namespace EvoSim
{
    /// <summary>The messages of one validation pass. The same engine runs in the editor, at Initialize and in CI (EDIT-01).</summary>
    public sealed class ValidationReport
    {
        readonly List<ValidationMessage> messages = new List<ValidationMessage>();

        public IReadOnlyList<ValidationMessage> Messages => messages;

        public void Add(string id, Severity severity, UnityEngine.Object obj, string text, ValidationFix fix = null) =>
            messages.Add(new ValidationMessage(id, severity, obj, text, fix));

        public void Error(string id, UnityEngine.Object obj, string text, ValidationFix fix = null) => Add(id, Severity.Error, obj, text, fix);
        public void Warning(string id, UnityEngine.Object obj, string text, ValidationFix fix = null) => Add(id, Severity.Warning, obj, text, fix);
        public void Info(string id, UnityEngine.Object obj, string text, ValidationFix fix = null) => Add(id, Severity.Info, obj, text, fix);

        public bool HasErrors
        {
            get
            {
                foreach (var m in messages) if (m.Severity == Severity.Error) return true;
                return false;
            }
        }

        public int Count(Severity s)
        {
            int n = 0;
            foreach (var m in messages) if (m.Severity == s) n++;
            return n;
        }

        /// <summary>True if a message with this validator id is present.</summary>
        public bool Has(string id)
        {
            foreach (var m in messages) if (m.Id == id) return true;
            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var m in messages) sb.AppendLine(m.ToString());
            return sb.ToString();
        }
    }
}
