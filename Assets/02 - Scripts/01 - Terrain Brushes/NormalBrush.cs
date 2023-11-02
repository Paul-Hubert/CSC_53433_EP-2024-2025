using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class NormalBrush : TerrainBrush {

    public float maxHeight = 10;

    public override void draw(int x, int z) {
        float height = 0;
        float standardDeviation = radius / 2;
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                float actualHeight = terrain.get(x + xi, z + zi);
                float distanceSquared = xi * xi + zi * zi;
                float gaussianValue = Mathf.Exp(-distanceSquared / (2 * standardDeviation * standardDeviation));
                height = gaussianValue * maxHeight + actualHeight;
                terrain.set(x + xi, z + zi, (int) height );
            }
        }
    }
}
