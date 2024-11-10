using UnityEngine;
using System.Collections.Generic;

public class ErosionBrush : TerrainBrush {
    public float erosionHeight = 0.1f;
    public float delta = 0.4f;
    public KernelType kernelType = KernelType.Square;

    private Vector2Int[] neighbors = new Vector2Int[] {
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0), 
        new Vector2Int(0, -1),
        new Vector2Int(0, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1)
    };

    public override void draw(int x, int z) {
        int diameter = 2 * radius + 1;
        float[,] buffer = new float[diameter, diameter];
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (!BrushKernel.kernels[kernelType].included(xi, zi, radius)) continue;
                
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int current = new Vector2Int(x + xi, z + zi);
                path.Add(current);

                while (true) {
                    float currentHeight = terrain.get(current.x, current.y);
                    List<Vector2Int> lowerNeighbors = new List<Vector2Int>();

                    foreach (Vector2Int offset in neighbors) {
                        Vector2Int neighbor = current + offset;
                        if (neighbor.x < 0 || neighbor.x >= terrain.gridSize().x || neighbor.y < 0 || neighbor.y >= terrain.gridSize().z) continue;
                        if (neighbor.x < x - radius || neighbor.x > x + radius || neighbor.y < z - radius || neighbor.y > z + radius) continue;
                        if (terrain.get(neighbor.x, neighbor.y) < currentHeight) {
                            lowerNeighbors.Add(neighbor);
                        }
                    }

                    if (lowerNeighbors.Count == 0 || path.Count == 2 * radius + 1) break;

                    current = lowerNeighbors[Random.Range(0, lowerNeighbors.Count)];
                    path.Add(current);
                }


                foreach (Vector2Int point in path) {
                    buffer[point.x - x + radius, point.y - z + radius]++;
                }
            }
        }

        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (buffer[xi + radius, zi + radius] > delta) {
                    terrain.set(x + xi, z + zi, terrain.get(x + xi, z + zi) - erosionHeight);
                }
            }
        }
    }
}
