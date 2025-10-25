using System.Numerics;

namespace Aup2Drawer.Properties;

public class Keyframe<T>
{
    public int Frame { get; }
    public T Value { get; }
    public InterpolationType Interpolation { get; }
    public List<SplineKnot> SplineKnots { get; }
    /// <summary>
    /// イージングの種類を指定する数値 (0:Default, 1:In, 2:Out, 3:InOut)
    /// </summary>
    public int EasingType { get; }

    public Keyframe(int frame, T value, InterpolationType interpolation, List<SplineKnot> splineKnots = null, int easingType = 0)
    {
        Frame = frame;
        Value = value;
        Interpolation = interpolation;
        SplineKnots = splineKnots;
        EasingType = easingType;
    }
}