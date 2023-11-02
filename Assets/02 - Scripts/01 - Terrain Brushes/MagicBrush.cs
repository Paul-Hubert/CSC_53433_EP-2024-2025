using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MagicBrush : TerrainBrush {

    public enum BrushType
    {
        SIMPLE,
        INCREMENTAL,
        NORMAL,
        NOISE,
        SMOOTH,
        ERASE
    };

    public enum BrushShape
    {
        SQUARE,
        CIRCLE,
    };

    public float height = 5;

    public BrushType type;
    public BrushShape shape;

    public override void draw(int x, int z) {
        float actualHeight;

        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (shape == BrushShape.CIRCLE && xi * xi + zi * zi > radius * radius) {
                    continue;
                }

                actualHeight = terrain.get(x + xi, z + zi);
                switch (type)
                {
                    case BrushType.SIMPLE:
                        terrain.set(x + xi, z + zi, height);
                        break;

                    case BrushType.INCREMENTAL:
                        float hloc = terrain.get(x + xi, z + zi);
                        terrain.set(x + xi, z + zi, height + actualHeight);
                        break;

                    case BrushType.NORMAL:
                        float standardDeviation = radius/2;
                        float distanceSquared = xi * xi + zi * zi;
                        float gaussianValue = Mathf.Exp(-distanceSquared / (2 * standardDeviation * standardDeviation));
                        float newheight = gaussianValue * height + actualHeight;
                        terrain.set(x + xi, z + zi, newheight);
                        break;

                    case BrushType.NOISE:
                        float randomFloat = UnityEngine.Random.Range(0.0f, height) + actualHeight;
                        terrain.set(x + xi, z + zi, randomFloat);
                        break;

                    case BrushType.SMOOTH:
                        float hlocprevx = terrain.get(x + xi - 1, z + zi);
                        float hlocnextx = terrain.get(x + xi + 1, z + zi);
                        float hlocprevz = terrain.get(x + xi, z + zi - 1);
                        float hlocnextz = terrain.get(x + xi, z + zi + 1);
                        float smoothheight = (hlocprevx + hlocnextx + hlocprevz + hlocnextz) / 4;
                        terrain.set(x + xi, z + zi, smoothheight);
                        break;

                    case BrushType.ERASE:
                        terrain.set(x + xi, z + zi, 0);
                        break;
                }
            }
        }
    }
}
