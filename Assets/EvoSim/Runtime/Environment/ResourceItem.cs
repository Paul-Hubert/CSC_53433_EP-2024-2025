using UnityEngine;

namespace EvoSim
{
    /// <summary>One item of a resource layer found by a query: its layer, cell, position and distance (ENV-01).</summary>
    /// <remarks>
    /// A value, not a reference to the item: pass it back to <see cref="ResourceLayer.Consume"/>, which checks that the
    /// item is still there (ENV-02).
    /// </remarks>
    public readonly struct ResourceItem
    {
        /// <summary>The layer that holds the item.</summary>
        public readonly ResourceLayer Layer;
        /// <summary>The item's place in its layer (a cell index for a <see cref="FoodGrid"/>); the layer decides what it means.</summary>
        public readonly int Cell;
        /// <summary>Where the item is, on the ground, in meters.</summary>
        public readonly Vector3 Position;
        /// <summary>The world distance from the query point to the item, in meters.</summary>
        public readonly float Distance;

        /// <summary>An item found by a layer's query.</summary>
        /// <param name="layer">The layer that holds it.</param>
        /// <param name="cell">Its place in the layer.</param>
        /// <param name="position">Where it is.</param>
        /// <param name="distance">Its distance from the query point, in meters.</param>
        public ResourceItem(ResourceLayer layer, int cell, Vector3 position, float distance)
        {
            Layer = layer; Cell = cell; Position = position; Distance = distance;
        }

        /// <summary>The layer, the cell and the distance, for logs: <c>Grass[1234] at 3.2 m</c>.</summary>
        /// <returns>A short text.</returns>
        public override string ToString() => $"{Layer?.LayerName}[{Cell}] at {Distance:0.##} m";
    }
}
