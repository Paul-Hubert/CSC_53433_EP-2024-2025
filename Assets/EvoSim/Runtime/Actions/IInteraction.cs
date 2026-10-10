namespace EvoSim
{
    /// <summary>
    /// A custom contact effect (22 §3, drink): applied after moving when the animal is within <see cref="Reach"/> of the
    /// point its action walked or ran to (ACT-10).
    /// </summary>
    public interface IInteraction
    {
        float Reach { get; }
        void Apply(Animal actor, InteractionContext c);
    }
}
