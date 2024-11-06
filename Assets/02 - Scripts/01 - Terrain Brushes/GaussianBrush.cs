using UnityEngine;

public class GaussianBrush : TerrainBrush {
    public float height = 5f;
    public float sigma = 10f;
    public KernelType kernelType = KernelType.Square;

    public override void draw(int x, int z) {
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (!BrushKernel.kernels[kernelType].included(xi, zi, radius)) continue;
                float distance = Mathf.Sqrt(xi * xi + zi * zi);
                float gaussian = height * Mathf.Exp(-(distance * distance) / (2 * sigma * sigma));
                
                terrain.set(x + xi, z + zi, terrain.get(x + xi, z + zi) + gaussian);
            }
        }
    }
}