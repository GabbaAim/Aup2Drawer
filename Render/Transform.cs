using System.Numerics;

namespace Aup2Drawer.Renderer;

/// <summary>
/// 描画時の位置、拡大率、角度、透明度などを表すクラス
/// </summary>
public class Transform
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Scale { get; set; } = Vector2.One;
    public float RotationZ { get; set; } = 0.0f;
    public float Opacity { get; set; } = 1.0f;
    public bool InvertX { get; set; } = false;
    public bool InvertY { get; set; } = false;
}