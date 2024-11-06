using UnityEngine;

public class NoiseBrush : TerrainBrush {
    [System.Serializable]
    public struct NoiseLayer {
        public float scale;
        public float amplitude;
    }

    public NoiseLayer[] noiseLayers = new NoiseLayer[] {
        new NoiseLayer { scale = 0.1f, amplitude = 5f },
        new NoiseLayer { scale = 0.2f, amplitude = 2.5f },
        new NoiseLayer { scale = 0.4f, amplitude = 1.25f }
    };
    public KernelType kernelType = KernelType.Square;

    public override void draw(int x, int z) {
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (!BrushKernel.kernels[kernelType].included(xi, zi, radius)) continue;
                float totalNoise = 0f;

                foreach (var layer in noiseLayers) {
                    totalNoise += Mathf.PerlinNoise((x + xi) * layer.scale, (z + zi) * layer.scale) * layer.amplitude;
                }

                terrain.set(x + xi, z + zi, totalNoise);
            }
        }
    }
}