using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class IncrementalPerlinBrush : TerrainBrush
{

    public int max_fractal_level = 5;
    public int min_fractal_level = 0;
    public float height = 1f;
    public float detailScale = 100f;
    public int fractality = 5;
    public override void draw(int x, int z)
    {
        for (int zi = -radius; zi <= radius; zi++)
        {
            for (int xi = -radius; xi <= radius; xi++)
            {
                float hloc = terrain.get(x + xi, z + zi);
                float deltaH = 0;
                for (int k = min_fractal_level; k < max_fractal_level; k++)
                {
                    deltaH += height * (float)Math.Pow(1.5, -k) * Mathf.PerlinNoise((float)Math.Pow(2, k) * (float)(x + xi) / detailScale, (float)Math.Pow(2, k) * (float)(z + zi) / detailScale);
                }
                deltaH *= bell(xi, zi, radius);
                terrain.set(x + xi, z + zi, hloc + deltaH);
            }
        }
    }
    private static float bell(float x, float z, int radius)
    {
        return 1.0f / ((float)Math.Exp(((float)Math.Pow(x, 2) + (float)Math.Pow(z, 2)) / (float)Math.Pow(radius, 2)));
    }
}