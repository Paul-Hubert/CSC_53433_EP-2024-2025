using System;

namespace EvoSim
{
    /// <summary>
    /// Names a module this one needs beside it (a drink needs thirst): a species module on the same species, or a world
    /// module (a service or a phase) under the World. Validation reports a missing one (V-26) with a fix that adds it, and
    /// the conformance suites add it before testing the module.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class RequiresModuleAttribute : Attribute
    {
        public Type Module { get; }

        public RequiresModuleAttribute(Type module) { Module = module; }
    }
}
