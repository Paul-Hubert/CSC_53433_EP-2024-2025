using System;

namespace EvoSim
{
    /// <summary>A fix button for a validation message (EDIT-02).</summary>
    public sealed class ValidationFix
    {
        public readonly string Label;
        public readonly Action Apply;
        public ValidationFix(string label, Action apply) { Label = label; Apply = apply; }
    }
}
