using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantVolumeBrush : TerrainBrush {
    public float height = 5;
    public KernelType kernelType = KernelType.Square;

    public override void draw(int x, int z) {
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (!BrushKernel.kernels[kernelType].included(xi, zi, radius)) continue;
                
                float addHeight = 0;
                float r = Mathf.Sqrt(xi * xi + zi * zi) / (radius / 2.0f);
                if (r <= 1.0f) {
                    addHeight = height * (1 - r * r);
                } else {
                    addHeight = -height * Mathf.Sqrt(r - 1.0f);
                }
                terrain.set(x + xi, z + zi, terrain.get(x + xi, z + zi) + addHeight);
            }
        }
    }
}
