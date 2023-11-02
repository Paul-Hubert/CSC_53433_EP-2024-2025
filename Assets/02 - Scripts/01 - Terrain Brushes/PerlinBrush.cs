using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PerlinBrush : TerrainBrush
{

    public int fractal_level = 5;
    public float height = 30f;
    public float detailScale = 100f;
    public int fractality = 5;

    public override void draw(int x, int z)
    {
        for (int zi = -radius; zi <= radius; zi++)
        {
            for (int xi = -radius; xi <= radius; xi++)
            {
                //float hloc = terrain.get(x + xi, z + zi);
                float deltaH = 0;
                for (int k = 0; k < fractal_level; k++)
                {
                    deltaH += height * (float)Math.Pow(1.5, -k) * Mathf.PerlinNoise((float)Math.Pow(2, k) * (float)(x + xi) / detailScale, (float)Math.Pow(2, k) * (float)(z + zi) / detailScale);
                }
                terrain.set(x + xi, z + zi, deltaH);
            }
        }
    }
}