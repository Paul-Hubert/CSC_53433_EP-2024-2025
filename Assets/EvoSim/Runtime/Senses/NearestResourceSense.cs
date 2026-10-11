using UnityEngine;

namespace EvoSim
{
    /// <summary>The nearest item of a layer the species grazes (06 §4): here, the bands, none. Reference label "Food".</summary>
    /// <remarks>
    /// Tokens: here, adjacent, close, medium, far, none. "here" means within the act phase's graze reach
    /// (<see cref="SenseContext.GrazeReach"/>), where eating needs no walk. Texts: V1 "Food: here." / "Food: 1-4 meters
    /// away.", V2 "I am standing on food." / "The nearest food is 1-4 meters away." / "No food within 20 meters.".
    /// </remarks>
    public class NearestResourceSense : BandedSense
    {
        [SerializeField, Tooltip("The layer to sense; empty = every layer the diet grazes.")]
        string layerName = "";

        /// <summary>True: an item within graze reach is "here".</summary>
        /// <inheritdoc/>
        protected override bool HasHere => true;

        /// <summary>"Food".</summary>
        /// <inheritdoc/>
        protected override string DefaultLabel => "Food";

        /// <summary>
        /// "here" when the nearest item the diet grazes is within graze reach, else its band within the animal's vision, or
        /// "none".
        /// </summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            float vision = a.Trait(Vision);
            var item = s.NearestResource(a, layerName, vision);
            if (item == null) return NoneToken;
            return TokenFor(item.Value.Distance, vision, here: item.Value.Distance <= s.GrazeReach);
        }

        /// <summary>Sets the layer to sense from code (WorldBuilder, tests), as the Inspector field does.</summary>
        /// <param name="name">The layer's name ("Grass"…); empty or null for every layer the diet grazes.</param>
        public void SetLayer(string name) => layerName = name ?? "";

        /// <summary>Adds to the band checks V-20 for a layer name that matches no resource layer of the world.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            if (!string.IsNullOrEmpty(layerName) && World.Service<ResourceLayer>(layerName) == null)
                report.Error("V-20", this, $"Sense '{Label}' names the layer '{layerName}', which doesn't exist.");
        }
    }
}
