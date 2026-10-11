using UnityEngine;

namespace EvoSim
{
    /// <summary>Go to the nearest visible food and eat it (07 §4). Nothing in sight: search (ACT-04).</summary>
    /// <remarks>
    /// <para>The reference eat action of the prey, and teaching-path exercise 4. Each tick it:</para>
    /// <list type="bullet">
    /// <item><description>finds the nearest food item in sight that its diet grazes
    /// (<see cref="ActContext.NearestResource"/>), on every grazed layer, or only on the layer its <c>target</c>
    /// names;</description></item>
    /// <item><description>with none in sight, calls <see cref="ActContext.Search"/> and stops there;</description></item>
    /// <item><description>stays when the item is within <see cref="ActContext.GrazeReach"/>, else walks to it with a
    /// stop distance of 0 (MOVE-04);</description></item>
    /// <item><description>in both cases asks to graze it on arrival (<see cref="Interaction.Graze"/>), so the animal
    /// eats on the very tick it arrives (ACT-10).</description></item>
    /// </list>
    /// <para>With a <c>target</c> of <c>"egg:&lt;species&gt;"</c> it eats eggs instead (22 §15): it walks to the nearest
    /// egg of that species its diet eats (<see cref="ActContext.NearestEgg"/>) and eats it on arrival
    /// (<see cref="Interaction.EatEgg"/>).</para>
    /// </remarks>
    public class EatAction : AnimalAction
    {
        /// <inheritdoc/>
        protected override string DefaultDescription => "go to the nearest visible food and eat it";

        [SerializeField, Tooltip("A layer name; empty = every layer the diet grazes; \"egg:<species>\" = eggs (22 §15).")]
        string target = "";
        [SerializeField, Tooltip("The sense whose token says whether food is in sight (CTRL-10).")]
        string relevantSense = "Food";

        Species eggsOf;     // whose eggs this action eats, when its target is "egg:<species>"; null: any egg the diet eats
        bool eatsEggs;      // true when the target starts with "egg:"

        /// <summary>Reads the target: <c>"egg:&lt;species&gt;"</c> makes this action eat that species' eggs.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            eatsEggs = target.StartsWith("egg:");
            eggsOf = eatsEggs ? World.FindSpeciesByName(target.Substring(4).Trim()) : null;
        }

        /// <summary>
        /// Walks to the nearest visible food the diet allows and grazes it on arrival (or eats the nearest egg); searches
        /// when none is in sight (ACT-04).
        /// </summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            if (eatsEggs)
            {
                var egg = c.NearestEgg(a, eggsOf);
                if (egg == null) { c.Search(); return; }
                c.WalkTo(egg.Value.Position, stopAt: 0f);
                c.OnArrival(Interaction.EatEgg(egg.Value.Entity));
                return;
            }
            var food = c.NearestResource(a, target);                    // within vision, diet-checked
            if (food == null) { c.Search(); return; }                   // ACT-04
            if (food.Value.Distance <= c.GrazeReach) c.Stay();          // already on it
            else c.WalkTo(food.Value.Position, stopAt: 0f);             // MOVE-04
            c.OnArrival(Interaction.Graze(food.Value));                 // ACT-10, on the tick of arrival too
        }

        /// <summary>True when the food sense (<c>relevantSense</c>, reference <c>"Food"</c>) reports food in sight (CTRL-10).</summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o) => Seen(o.Token(relevantSense));

        /// <summary>True for a token that reports something: neither missing (no such sense) nor <c>"none"</c>.</summary>
        static bool Seen(string token) => token != null && token != "none";

        /// <summary>
        /// Sets the target from code (WorldBuilder, scenarios), before the world initializes: <see cref="Initialize"/>
        /// reads an egg target.
        /// </summary>
        /// <param name="layerOrEggs">
        /// A layer's name, empty for every layer the diet grazes, or <c>"egg:&lt;species&gt;"</c>; null counts as
        /// empty.
        /// </param>
        public void SetTarget(string layerOrEggs) => target = layerOrEggs ?? "";
    }
}
