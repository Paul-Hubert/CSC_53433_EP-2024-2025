using UnityEngine;

namespace EvoSim
{
    /// <summary>The nearest item of a layer the species grazes (06 §4): here, the bands, none. Reference label "Food".</summary>
    public class NearestResourceSense : BandedSense
    {
        [SerializeField, Tooltip("The layer to sense; empty = every layer the diet grazes.")]
        string layerName = "";

        protected override bool HasHere => true;

        protected override string DefaultLabel => "Food";

        public override int Read(Animal a, SenseContext s)
        {
            float vision = a.Trait(Vision);
            var item = s.NearestResource(a, layerName, vision);
            if (item == null) return NoneToken;
            return TokenFor(item.Value.Distance, vision, here: item.Value.Distance <= s.GrazeReach);
        }

        public void SetLayer(string name) => layerName = name ?? "";

        public override void Validate(ValidationReport report)
        {
            base.Validate(report);
            if (!string.IsNullOrEmpty(layerName) && World.Service<ResourceLayer>(layerName) == null)
                report.Error("V-20", this, $"Sense '{Label}' names the layer '{layerName}', which doesn't exist.");
        }
    }
}
