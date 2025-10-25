using System.Linq;
using System.Numerics;

namespace Aup2Drawer.Properties;

public class AnimatedProperty<T>
{
    public string DebugName { get; set; } = "Unnamed";
    public List<Keyframe<T>> Keyframes { get; } = new();

    public void AddKeyframe(int frame, T value, InterpolationType interpolation, List<SplineKnot> splineKnots = null, int easingType = 0)
    {
        // 同じフレームにキーが既にあれば上書き、なければ追加
        var existingKeyframe = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if (existingKeyframe != null)
        {
            Keyframes.Remove(existingKeyframe);
        }
        Keyframes.Add(new Keyframe<T>(frame, value, interpolation, splineKnots, easingType));
        Keyframes.Sort((a, b) => a.Frame.CompareTo(b.Frame));
    }

    public T GetValue(int frame)
    {
        // キーフレームがない場合はデフォルト値を返す
        if (Keyframes.Count == 0)
        {
            return default(T);
        }

        // キーフレームが1つしかない場合は、その値を返す
        if (Keyframes.Count == 1)
        {
            return Keyframes[0].Value;
        }

        // --- 指定フレームの前後のキーフレームを探す ---

        // 最初のキーフレームより前の場合
        if (frame <= Keyframes[0].Frame)
        {
            return Keyframes[0].Value;
        }

        // 最後のキーフレームより後の場合
        if (frame >= Keyframes.Last().Frame)
        {
            return Keyframes.Last().Value;
        }

        // 中間のフレームの場合、2つのキーフレームを探す
        Keyframe<T> startKey = null;
        Keyframe<T> endKey = null;
        for (int i = 0; i < Keyframes.Count - 1; i++)
        {
            if (frame >= Keyframes[i].Frame && frame < Keyframes[i + 1].Frame)
            {
                startKey = Keyframes[i];
                endKey = Keyframes[i + 1];
                break;
            }
        }

        // ちょうどキーフレーム上のフレームだった場合
        if (startKey == null)
        {
            return Keyframes.First(k => k.Frame == frame).Value;
        }

        // --- 補間計算 ---
        if (typeof(T) == typeof(float))
        {
            float startValue = Convert.ToSingle(startKey.Value);
            float endValue = Convert.ToSingle(endKey.Value);
            int startTime = startKey.Frame;
            int endTime = endKey.Frame;

            // 進行度 (0.0 ～ 1.0)
            double progress = (double)(frame - startTime) / (endTime - startTime);
            double easedProgress = progress;

            switch (startKey.Interpolation)
            {
                case InterpolationType.Instant:
                    // 瞬間移動: 終点にジャンプ
                    easedProgress = 1.0;
                    break;

                case InterpolationType.Interpolated:
                    // 補間移動: EaseInOutSineを適用
                    easedProgress = startKey.EasingType switch
                    {
                        0 => progress,
                        1 => EasingFunctions.EaseInSine(progress),
                        2 => EasingFunctions.EaseOutSine(progress),
                        _ => EasingFunctions.EaseInOutSine(progress) // 3, default
                    };
                    break;

                case InterpolationType.TimeCtrl:
                    if (startKey.SplineKnots != null && startKey.SplineKnots.Count >= 2)
                    {
                        var knots = startKey.SplineKnots;

                        // 1. 現在のprogressがどのセグメントに属するか探す
                        SplineKnot segStart = null;
                        SplineKnot segEnd = null;
                        for (int i = 0; i < knots.Count - 1; i++)
                        {
                            if (progress >= knots[i].Point.X && progress <= knots[i + 1].Point.X)
                            {
                                segStart = knots[i];
                                segEnd = knots[i + 1];
                                break;
                            }
                        }

                        if (segStart != null)
                        {
                            // 2. セグメントの4つの制御点を決定
                            Vector2 p0 = segStart.Point;
                            // P1は、始点アンカーの座標 + 右ハンドルの相対座標
                            Vector2 p1 = segStart.Point + segStart.RightHandle;
                            // P2は、終点アンカーの座標 - 左ハンドルの相対座標
                            // 左ハンドルは右ハンドルの点対称なので、-RightHandleとなる
                            Vector2 p2 = segEnd.Point - segEnd.RightHandle;
                            Vector2 p3 = segEnd.Point;

                            // 3. セグメント内でのローカルな進行度を計算
                            float localProgress = 0;
                            float segDuration = segEnd.Point.X - segStart.Point.X;
                            if (segDuration > 0)
                            {
                                localProgress = ((float)progress - segStart.Point.X) / segDuration;
                            }

                            // 4. ベジェ曲線で進行度を再計算
                            easedProgress = EasingFunctions.CubicBezier(localProgress, p0, p1, p2, p3);
                        }
                    }
                    break;

                case InterpolationType.Linear:
                default:
                    // 直線移動 (または不明な場合)
                    easedProgress = startKey.EasingType switch
                    {
                        1 => EasingFunctions.EaseInQuad(progress),
                        2 => EasingFunctions.EaseOutQuad(progress),
                        3 => EasingFunctions.EaseInOutQuad(progress),
                        _ => progress // 0, default
                    };
                    break;
            }

            float interpolatedValue = (float)(startValue + (endValue - startValue) * easedProgress);
            return (T)(object)interpolatedValue;
        }

        // float以外は未対応なので、開始キーの値をそのまま返す
        return startKey.Value;
    }
}