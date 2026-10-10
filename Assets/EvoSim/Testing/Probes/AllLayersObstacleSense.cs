using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test batched sense that asks for a ray against every layer (RAND-11: the views' layer is still left out).</summary>
    public class AllLayersObstacleSense : ObstacleSense
    {
        public override void RequestRays(Animal a, RayBatch rays) =>
            rays.Add(a.Position + Vector3.up * 0.5f, ActContext.HeadingDirection(a.Heading), 5f, Physics.AllLayers);
    }
}
