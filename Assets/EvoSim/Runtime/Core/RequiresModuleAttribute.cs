using System;

namespace EvoSim
{
    /// <summary>
    /// Names a module this one needs beside it (a drink needs thirst): a species module on the same species, or a world
    /// module (a service or a phase) under the World. Validation reports a missing one (V-26) with a fix that adds it, and
    /// the conformance suites add it before testing the module.
    /// </summary>
    /// <remarks>
    /// Put it on a species module's class, once per needed module; subclasses inherit it. A subclass of the named type
    /// counts. The check is skipped for a module that already reported an error of its own; the module should still
    /// cope with a missing companion (look it up in Initialize and test for null).
    /// </remarks>
    /// <example>
    /// <code>
    /// [RequiresModule(typeof(Thirst))]
    /// public class DiesOfThirst : DeathRule
    /// {
    ///     Thirst thirst;
    ///     public override void Initialize() => thirst = Species.Module&lt;Thirst&gt;();
    ///     // ...
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RequiresModuleAttribute : Attribute
    {
        /// <summary>The type of the module needed: a species module, a service or a phase.</summary>
        public Type Module { get; }

        /// <summary>Names a module this one needs.</summary>
        /// <param name="module">Its type, e.g. <c>typeof(Thirst)</c>. The fix can add it only when the type is not abstract.</param>
        public RequiresModuleAttribute(Type module) { Module = module; }
    }
}
