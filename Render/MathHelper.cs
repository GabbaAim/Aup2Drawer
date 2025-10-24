using System;

public static class MathHelper
{
    /// <summary>
    /// 度数法 (degree) から弧度法 (radian) に変換します。
    /// </summary>
    /// <param name="degrees">度数法で表された角度。</param>
    /// <returns>弧度法で表された角度。</returns>
    public static float ToRadians(float degrees)
    {
        return degrees * (MathF.PI / 180.0f);
    }

    /// <summary>
    /// 弧度法 (radian) から度数法 (degree) に変換します。
    /// </summary>
    /// <param name="radians">弧度法で表された角度。</param>
    /// <returns>度数法で表された角度。</returns>
    public static float ToDegrees(float radians)
    {
        return radians * (180.0f / MathF.PI);
    }
}