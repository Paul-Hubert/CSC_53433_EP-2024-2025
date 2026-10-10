using UnityEngine;

namespace EvoSim
{
    /// <summary>One item of a resource layer found by a query: its layer, cell, position and distance (ENV-01).</summary>
    public readonly struct ResourceItem
    {
        public readonly ResourceLayer Layer;
        public readonly int Cell;
        public readonly Vector3 Position;
        public readonly float Distance;

        public ResourceItem(ResourceLayer layer, int cell, Vector3 position, float distance)
        {
            Layer = layer; Cell = cell; Position = position; Distance = distance;
        }

        public override string ToString() => $"{Layer?.LayerName}[{Cell}] at {Distance:0.##} m";
    }
}
