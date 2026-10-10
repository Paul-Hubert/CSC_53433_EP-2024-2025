using System;

namespace EvoSim
{
    /// <summary>
    /// Marks a student class as the exercise version of a reference module (22 §0, the teaching path):
    /// EvoSim ▸ Exercises ▸ Replace with stub swaps the reference for it, keeping the settings; Restore reference undoes it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ExerciseOfAttribute : Attribute
    {
        public Type Reference { get; }
        public ExerciseOfAttribute(Type reference) { Reference = reference; }

        /// <summary>The message a stub throws until the student writes it.</summary>
        public static string Todo(string module, string method) =>
            $"Exercise: write {module}.{method} (Docs/Interface/22 §0; the reference module's tests grade it).";
    }
}
