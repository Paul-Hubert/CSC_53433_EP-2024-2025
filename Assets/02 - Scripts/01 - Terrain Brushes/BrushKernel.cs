using UnityEngine;
using System.Collections.Generic;

public abstract class BrushKernel {
    public abstract bool included(int dx, int dz, int radius);
    public static Dictionary<KernelType, BrushKernel> kernels = new Dictionary<KernelType, BrushKernel>() {
        { KernelType.Square, new SquareKernel() },
        { KernelType.Circle, new CircleKernel() },
        { KernelType.Diamond, new DiamondKernel() }
    };
}

[System.Serializable]
public enum KernelType {
    Square,
    Circle,
    Diamond
}

public class SquareKernel : BrushKernel {
    public override bool included(int dx, int dz, int radius) {
        return dx <= radius && dz <= radius;
    }
}

public class CircleKernel : BrushKernel {
    public override bool included(int dx, int dz, int radius) {
        int distance2 = dx * dx + dz * dz;
        return distance2 <= radius * radius;
    }
}

public class DiamondKernel : BrushKernel {
    public override bool included(int dx, int dz, int radius) {
        return Mathf.Abs(dx) + Mathf.Abs(dz) <= radius;
    }
} 