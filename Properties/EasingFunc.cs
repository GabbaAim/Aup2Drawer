using System.Numerics;

public static class EasingFunctions
{
    public static float EaseInOutSine(double progress)
    {
        return (float)(-(Math.Cos(Math.PI * progress) - 1.0) / 2.0);
    }

    /// <summary>
    /// 3次ベジェ曲線のY座標を計算する
    /// </summary>
    /// <param name="t">セグメント内の進行度 (0.0 to 1.0)</param>
    /// <param name="p0">始点</param>
    /// <param name="p1">制御点1</param>
    /// <param name="p2">制御点2</param>
    /// <param name="p3">終点</param>
    /// <returns>値の進行度 (Y座標)</returns>
    public static float CubicBezier(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        // ここでも X(t) ≈ t の近似を用いる
        float u = 1.0f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        float y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;
        return y;
    }
}