using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncrementalBrush : TerrainBrush
{

    public float height = 5;
    public override void draw(int x, int z)
    {
        for (int zi = -radius; zi <= radius; zi++)
        {
            for (int xi = -radius; xi <= radius; xi++)
            {
                float hloc = terrain.get(x + xi, z + zi);
                terrain.set(x + xi, z + zi, hloc + height);
            }
        }
    }
}