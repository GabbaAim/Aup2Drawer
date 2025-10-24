using Aup2Drawer.Core;
using Aup2Drawer.Properties;

namespace Aup2Drawer.Effects;

public class ScaleFilterEffect : AupEffect
{
    public AnimatedProperty<float> BaseScale { get; } = new();
    public AnimatedProperty<float> X { get; } = new();
    public AnimatedProperty<float> Y { get; } = new();

    public ScaleFilterEffect()
    {
        Name = "拡大率";
    }

    public void SetPropertyFromValueString(string key, string value, List<AupObjectSegment> segments)
    {
        var parts = value.Split(',');
        var values = parts.Where(p => float.TryParse(p, out _)).Select(float.Parse).ToArray();

        var interpolationStr = parts.FirstOrDefault(p => !float.TryParse(p, out _));
        var interpolation = interpolationStr switch
        {
            "直線移動" => InterpolationType.Linear,
            "瞬間移動" => InterpolationType.Instant,
            "補間移動" => InterpolationType.Interpolated,
            _ => InterpolationType.Linear
        };

        if (values.Length == 0) return;

        AnimatedProperty<float> targetProperty = key switch
        {
            "拡大率" => this.BaseScale,
            "X" => this.X,
            "Y" => this.Y,
            _ => null
        };

        if (targetProperty == null) return;

        targetProperty.AddKeyframe(segments[0].StartFrame, values[0], interpolation);
        for (int i = 0; i < segments.Count; i++)
        {
            float val = (i + 1 < values.Length) ? values[i + 1] : values.Last();
            targetProperty.AddKeyframe(segments[i].EndFrame, val, interpolation);
        }
    }
}