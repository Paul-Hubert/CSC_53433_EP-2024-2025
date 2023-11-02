using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleBrush : TerrainBrush {

    public float height = 5;

    public override void draw(int x, int z) {
        // x^2 + z^2 = radius
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (xi * xi + zi * zi <= radius * radius)
                {
                    terrain.set(x + xi, z + zi, height);
                }
            }
        }
    }
}
