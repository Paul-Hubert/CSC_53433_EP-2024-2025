using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test batched sense: one ray along the heading; "blocked" if it hits a collider within 5 m (T-SENSE-09).</summary>
    public class ObstacleSense : BatchedSense
    {
        readonly List<string> tokens = new List<string> { "clear", "blocked" };
        public override IReadOnlyList<string> Tokens => tokens;
        protected override string DefaultLabel => "Ahead";

        public override void RequestRays(Animal a, RayBatch rays) =>
            rays.Add(a.Position + Vector3.up * 0.5f, ActContext.HeadingDirection(a.Heading), 5f);

        public override int Read(Animal a, SenseContext s) => s.Rays.Hit(a, this, 0, out _) ? 1 : 0;
    }
}
