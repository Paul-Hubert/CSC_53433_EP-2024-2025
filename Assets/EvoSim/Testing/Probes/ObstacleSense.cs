using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>A test batched sense: one ray along the heading; "blocked" if it hits a collider within 5 m (T-SENSE-09).</summary>
    /// <remarks>
    /// The sense phase casts every animal's rays in one batch before any <see cref="Read"/>; a batched read must give the
    /// same tokens as reading one animal at a time (SENSE-31).
    /// </remarks>
    public class ObstacleSense : BatchedSense
    {
        readonly List<string> tokens = new List<string> { "clear", "blocked" };   // backs Tokens

        /// <summary>"clear" and "blocked".</summary>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>"Ahead".</summary>
        protected override string DefaultLabel => "Ahead";

        /// <summary>One ray of 5 m along the heading, from 0.5 m above the animal, on the default layers.</summary>
        /// <inheritdoc/>
        public override void RequestRays(Animal a, RayBatch rays) =>
            rays.Add(a.Position + Vector3.up * 0.5f, ActContext.HeadingDirection(a.Heading), 5f);

        /// <summary>"blocked" when the animal's ray hit something, else "clear".</summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s) => s.Rays.Hit(a, this, 0, out _) ? 1 : 0;
    }
}
