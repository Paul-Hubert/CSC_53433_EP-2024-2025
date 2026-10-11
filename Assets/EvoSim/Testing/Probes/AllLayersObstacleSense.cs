using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// A test batched sense that asks for its ray against every physics layer: even so, the views' layer is left out, so
    /// what is rendered never changes what animals sense (RAND-11).
    /// </summary>
    public class AllLayersObstacleSense : ObstacleSense
    {
        /// <summary>One ray of 5 m along the heading, from 0.5 m above the animal, with every layer in its mask.</summary>
        /// <inheritdoc/>
        public override void RequestRays(Animal a, RayBatch rays) =>
            rays.Add(a.Position + Vector3.up * 0.5f, ActContext.HeadingDirection(a.Heading), 5f, Physics.AllLayers);
    }
}
