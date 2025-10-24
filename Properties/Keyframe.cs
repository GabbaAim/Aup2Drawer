using System.Numerics;

namespace Aup2Drawer.Properties;

public class Keyframe<T>
{
    public int Frame { get; }
    public T Value { get; }
    public InterpolationType Interpolation { get; }
    public List<SplineKnot> SplineKnots { get; }

    public Keyframe(int frame, T value, InterpolationType interpolation, List<SplineKnot> splineKnots = null)
    {
        Frame = frame;
        Value = value;
        Interpolation = interpolation;
        SplineKnots = splineKnots;
    }
}