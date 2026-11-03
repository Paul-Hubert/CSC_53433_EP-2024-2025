namespace EvoSim.Neuro
{
    /// <summary>How fast a <see cref="TurnAction"/> moves while it turns (plan M16 §5.5).</summary>
    public enum NeuralSpeed
    {
        /// <summary>The walk speed (the default).</summary>
        Walk,
        /// <summary>The run speed; costs stamina like any run.</summary>
        Run,
        /// <summary>A fraction of the walk speed.</summary>
        Slow,
    }
}
