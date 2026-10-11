namespace EvoSim.Testing
{
    /// <summary>A test-only entity of a kind of one's own: a fruit.</summary>
    public class Fruit : Entity
    {
        /// <summary>"fruit".</summary>
        public override string Kind => "fruit";
    }

    /// <summary>
    /// A test-only entity system for fruit: nothing moves or rots. Several systems of one kind can live in a world
    /// ("Apples", "Pears"), and actions find their entities with <c>NearestEntity&lt;Fruit&gt;</c>.
    /// </summary>
    public class FruitSystem : EntitySystem<Fruit>
    {
        /// <summary>"fruit".</summary>
        public override string Kind => "fruit";
    }
}
