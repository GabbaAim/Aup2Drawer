using Aup2Drawer.Properties;
using System.Numerics;

namespace Aup2Drawer.Effects;

public class GroupControlEffect : AupEffect
{
    // 標準描画と同様に、時間変化するプロパティを持つ
    public AnimatedProperty<float> X { get; } = new();
    public AnimatedProperty<float> Y { get; } = new();
    public AnimatedProperty<float> Z { get; } = new();
    public AnimatedProperty<float> Scale { get; } = new();
    public AnimatedProperty<float> Opacity { get; } = new();
    public AnimatedProperty<float> RotationZ { get; } = new();

    // グループ制御固有のプロパティ
    public int TargetLayerCount { get; set; }

    public GroupControlEffect()
    {
        Name = "グループ制御";
    }

    // StandardDrawingEffectと同様のヘルパーメソッドを追加
    public void SetPropertyFromValueString(string key, string value, List<Core.AupObjectSegment> segments)
    {
        var mainParts = value.Split('|');
        var valuePart = mainParts[0];
        var timeCtrlPart = mainParts.Length > 1 ? mainParts[1] : null;

        var parts = value.Split(',');
        var values = parts.Where(p => float.TryParse(p, out _)).Select(float.Parse).ToArray();

        var interpolationStr = parts.FirstOrDefault(p => !float.TryParse(p, out _));
        int.TryParse(parts.LastOrDefault(), out int easingType);
        var interpolation = interpolationStr switch
        {
            "直線移動" => InterpolationType.Linear,
            "瞬間移動" => InterpolationType.Instant,
            "補間移動" => InterpolationType.Interpolated,
            "直線移動(時間制御)" => InterpolationType.TimeCtrl,
            "補間移動(時間制御)" => InterpolationType.TimeCtrl,
            _ => InterpolationType.Linear // 不明な場合は直線移動
        };

        if (values.Length == 0) return;

        AnimatedProperty<float> targetProperty = key switch
        {
            "X" => this.X,
            "Y" => this.Y,
            "Z" => this.Z,
            "拡大率" => this.Scale,
            "透明度" => this.Opacity,
            "Z軸回転" => this.RotationZ,
            _ => null
        };

        if (targetProperty == null) return;

        List<SplineKnot> knots = null;
        if (interpolation == InterpolationType.TimeCtrl && timeCtrlPart != null)
        {
            knots = new List<SplineKnot>();
            var cp_values = timeCtrlPart.Split(',').Select(float.Parse).ToArray();

            if (cp_values.Length == 4) // 従来の2制御点モード
            {
                // これを2つのノットを持つスプラインとして解釈する
                knots.Add(new SplineKnot(new Vector2(0, 0), new Vector2(cp_values[0], cp_values[1])));
                knots.Add(new SplineKnot(new Vector2(1, 1), new Vector2(cp_values[2], cp_values[3])));
            }
            else if (cp_values.Length >= 8 && cp_values.Length % 4 == 0) // 3制御点以上
            {
                for (int i = 0; i < cp_values.Length; i += 4)
                {
                    var point = new Vector2(cp_values[i], cp_values[i + 1]);
                    var handle = new Vector2(cp_values[i + 2], cp_values[i + 3]);
                    knots.Add(new SplineKnot(point, handle));
                }
            }
        }

        // 最初のキーフレームにスプライン情報を格納
        targetProperty.AddKeyframe(segments[0].StartFrame, values[0], interpolation, knots, easingType);

        for (int i = 0; i < segments.Count; i++)
        {
            float val = (i + 1 < values.Length) ? values[i + 1] : values.Last();
            targetProperty.AddKeyframe(segments[i].EndFrame, val, interpolation, null, easingType);
        }
    }
}