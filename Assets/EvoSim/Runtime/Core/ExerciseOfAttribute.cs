using System;

namespace EvoSim
{
    /// <summary>
    /// Marks a student class as the exercise version of a reference module (22 §0, the teaching path):
    /// EvoSim ▸ Exercises ▸ Replace with stub swaps the reference for it, keeping the settings; Restore reference undoes it.
    /// </summary>
    /// <remarks>The stub is named <c>My&lt;Reference&gt;</c>, derives from the reference's base class, and throws
    /// <see cref="Todo"/>'s message from each method until the student writes it.</remarks>
    /// <example>
    /// <code>
    /// [ExerciseOf(typeof(EatAction))]
    /// public class MyEatAction : AnimalAction
    /// {
    ///     public override void Act(Animal a, ActContext c) =&gt;
    ///         throw new NotImplementedException(ExerciseOfAttribute.Todo("MyEatAction", "Act"));
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ExerciseOfAttribute : Attribute
    {
        /// <summary>The reference module this class is the exercise version of.</summary>
        public Type Reference { get; }
        /// <summary>Marks the class as the exercise version of a reference module.</summary>
        /// <param name="reference">The reference module's type, e.g. <c>typeof(EatAction)</c>.</param>
        public ExerciseOfAttribute(Type reference) { Reference = reference; }

        /// <summary>The message a stub throws until the student writes it.</summary>
        /// <param name="module">The stub's class name.</param>
        /// <param name="method">The method still to write.</param>
        /// <returns>A message that names the method and points to 22 §0.</returns>
        public static string Todo(string module, string method) =>
            $"Exercise: write {module}.{method} your own way (Docs/Interface/22 §0; nothing has to match the reference).";
    }
}
