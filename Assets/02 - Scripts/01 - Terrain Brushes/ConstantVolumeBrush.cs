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
                
                float r = Mathf.Sqrt(xi * xi + zi * zi) / (radius);
                float addHeight = -height * Mathf.Pow(1 - r, 2) * Mathf.Pow(1 + r, 2) * (r + 1.0f / Mathf.Sqrt(7.0f)) * (r - 1.0f / Mathf.Sqrt(7.0f));
                terrain.set(x + xi, z + zi, terrain.get(x + xi, z + zi) + addHeight);
            }
        }
    }
}
