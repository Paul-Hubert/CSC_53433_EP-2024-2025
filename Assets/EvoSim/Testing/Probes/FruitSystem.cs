namespace EvoSim.Testing
{
    /// <summary>A test-only entity of a kind of one's own.</summary>
    public class Fruit : Entity
    {
        public override string Kind => "fruit";
    }

    /// <summary>A test-only entity system for fruit: nothing moves or rots.</summary>
    public class FruitSystem : EntitySystem<Fruit>
    {
        public override string Kind => "fruit";
    }
}
