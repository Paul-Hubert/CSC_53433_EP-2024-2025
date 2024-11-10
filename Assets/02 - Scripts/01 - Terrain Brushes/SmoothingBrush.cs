using UnityEngine;

public class SmoothingBrush : TerrainBrush {
    public float step = 0.1f;
    public KernelType outerKernelType = KernelType.Square;
    public KernelType innerKernelType = KernelType.Circle;
    public int innerRadius = 1;

    public override void draw(int x, int z) {
        int diameter = 2 * radius + 1;
        float[,] buffer = new float[diameter, diameter];

        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (!BrushKernel.kernels[outerKernelType].included(xi, zi, radius)) continue;

                float sum = 0f;
                int count = 0;

                for (int innerZi = -innerRadius; innerZi <= innerRadius; innerZi++) {
                    for (int innerXi = -innerRadius; innerXi <= innerRadius; innerXi++) {
                        if (!BrushKernel.kernels[innerKernelType].included(innerXi, innerZi, innerRadius)) continue;

                        sum += terrain.get(x + xi + innerXi, z + zi + innerZi);
                        count++;
                    }
                }

                if (count > 0) {
                    float mean = sum / count;
                    float currentValue = terrain.get(x + xi, z + zi);
                    float newValue = Mathf.MoveTowards(currentValue, mean, step);
                    buffer[xi + radius, zi + radius] = newValue;
                }
            }
        }
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (BrushKernel.kernels[outerKernelType].included(xi, zi, radius)) {
                    terrain.set(x + xi, z + zi, buffer[xi + radius, zi + radius]);
                }
            }
        }
    }
} 