using Aup2Drawer.Core;
using Aup2Drawer.Properties;

namespace Aup2Drawer.Effects;

public class OpacityFilterEffect : AupEffect
{
    public AnimatedProperty<float> Opacity { get; } = new();

    public OpacityFilterEffect()
    {
        Name = "透明度";
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

        int.TryParse(parts.LastOrDefault(), out int easingType);

        if (values.Length == 0) return;

        AnimatedProperty<float> targetProperty = key switch
        {
            "透明度" => this.Opacity,
            _ => null
        };

        if (targetProperty == null) return;

        targetProperty.AddKeyframe(segments[0].StartFrame, values[0], interpolation, null, easingType);
        for (int i = 0; i < segments.Count; i++)
        {
            float val = (i + 1 < values.Length) ? values[i + 1] : values.Last();
            targetProperty.AddKeyframe(segments[i].EndFrame, val, interpolation, null, easingType);
        }
    }
}