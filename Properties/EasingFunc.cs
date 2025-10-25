using System.Numerics;

public static class EasingFunctions
{
    // --- Sine Functions ---
    public static float EaseInSine(double p) => (float)(1.0 - Math.Cos((p * Math.PI) / 2.0));
    public static float EaseOutSine(double p) => (float)Math.Sin((p * Math.PI) / 2.0);
    public static float EaseInOutSine(double p) => (float)(-(Math.Cos(Math.PI * p) - 1.0) / 2.0);

    // --- Quad Functions ---
    public static float EaseInQuad(double p) => (float)(p * p);
    public static float EaseOutQuad(double p) => (float)(1.0 - (1.0 - p) * (1.0 - p));
    public static float EaseInOutQuad(double p)
    {
        return (float)(p < 0.5 ? 2.0 * p * p : 1.0 - Math.Pow(-2.0 * p + 2.0, 2.0) / 2.0);
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