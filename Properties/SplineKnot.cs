using System.Numerics;

namespace Aup2Drawer.Properties;

/// <summary>
/// 時間制御スプラインの1つの制御点（アンカーポイント）を表す
/// </summary>
public class SplineKnot
{
    /// <summary>
    /// 制御点の座標 (X:時間進行度, Y:値の進行度)
    /// </summary>
    public Vector2 Point { get; }

    /// <summary>
    /// この点から出る右側ハンドルの相対座標
    /// </summary>
    public Vector2 RightHandle { get; }

    public SplineKnot(Vector2 point, Vector2 rightHandle)
    {
        Point = point;
        RightHandle = rightHandle;
    }
}