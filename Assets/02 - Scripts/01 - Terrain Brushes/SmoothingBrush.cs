using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothingBrush : TerrainBrush
{


    public override void draw(int x, int z)
    {
        for (int zi = -radius; zi <= radius; zi++)
        {
            for (int xi = -radius; xi <= radius; xi++)
            {
                float hlocprevx = terrain.get(x + xi - 1, z + zi);
                float hlocnextx = terrain.get(x + xi + 1, z + zi);
                float hlocprevz = terrain.get(x + xi, z + zi - 1);
                float hlocnextz = terrain.get(x + xi, z + zi + 1);
                float height = (hlocprevx + hlocnextx + hlocprevz + hlocnextz) / 4;
                terrain.set(x + xi, z + zi, height);
            }
        }
    }
}