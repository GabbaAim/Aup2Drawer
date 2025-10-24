﻿using System.Numerics;
using Raylib_cs;

namespace Aup2Drawer.Renderer;

public class Texture : IDisposable
{
    public Texture()
    {
        Rotation = 0.0f;
        Scale = new Vector2(1.0f, 1.0f);
        Opacity = 1.0f;
        ReferencePoint = ReferencePoint.TopLeft;
    }

    public Texture(string fileName) : this()
    {
        RayTexture = Raylib.LoadTexture(fileName);
        if (RayTexture.Id != 0)
        {
            IsEnable = true;
        }
        FileName = fileName;
    }

    public void Dispose()
    {
        if (IsEnable)
        {
            Raylib.UnloadTexture(RayTexture);
            IsEnable = false;
        }
    }

    /// <summary>
    /// テクスチャを描画する (左上が X0, Y0 の座標系)
    /// </summary>
    public void Draw(float x, float y, double Rotate, Rectangle? sourceRect = null, Vector2? drawOrigin = null, bool reverseX = false, bool reverseY = false)
    {
        if (!IsEnable) return;

        Rectangle source = sourceRect ?? new Rectangle(0, 0, RayTexture.Width, RayTexture.Height);

        // 1. originの計算には、常に正の幅/高さを使用する
        Rectangle originRect = new Rectangle(0, 0, Math.Abs(source.Width), Math.Abs(source.Height));
        Vector2 origin = drawOrigin ?? GetReferencePoint(originRect);

        // 2. 反転フラグに応じてsource矩形の符号を変更する
        if (reverseX) source.Width = -source.Width;
        if (reverseY) source.Height = -source.Height;

        // 3. スケールを考慮したoriginの最終調整
        origin.X *= Scale.X;
        origin.Y *= Scale.Y;

        Vector2 position = new Vector2(x, y);
        Vector2 scale = new Vector2(Scale.X, Scale.Y);
        Color color = new Color(255, 255, 255, (int)(Opacity * 255));

        // 描画先の矩形(destination)の幅/高さも絶対値を使用
        Rectangle destRect = new Rectangle(x, y, Math.Abs(source.Width) * Scale.X, Math.Abs(source.Height) * Scale.Y);

        Raylib.DrawTexturePro(RayTexture, source, destRect, origin, (float)Rotate, color);
    }

    /// <summary>
    /// 画面中央を基準に描画 (AviutlやYMM4の座標系)
    /// </summary>
    public void DrawCenteredCoords(float x, float y, Rectangle? sourceRect = null, Vector2? drawOrigin = null, bool reverseX = false, bool reverseY = false)
    {
        Rectangle source = sourceRect ?? new Rectangle(0, 0, RayTexture.Width, RayTexture.Height);
        Vector2 origin = drawOrigin ?? GetReferencePoint(source);

        //Draw((1920 / 2) + x, (1080 / 2) + y, sourceRect, origin, reverseX, reverseY);
    }

    private Vector2 GetReferencePoint(Rectangle rect)
    {
        return ReferencePoint switch
        {
            ReferencePoint.TopLeft => new Vector2(0, 0),
            ReferencePoint.TopCenter => new Vector2(rect.Width / 2, 0),
            ReferencePoint.TopRight => new Vector2(rect.Width, 0),
            ReferencePoint.CenterLeft => new Vector2(0, rect.Height / 2),
            ReferencePoint.Center => new Vector2(rect.Width / 2, rect.Height / 2),
            ReferencePoint.CenterRight => new Vector2(rect.Width, rect.Height / 2),
            ReferencePoint.BottomLeft => new Vector2(0, rect.Height),
            ReferencePoint.BottomCenter => new Vector2(rect.Width / 2, rect.Height),
            ReferencePoint.BottomRight => new Vector2(rect.Width, rect.Height),
            _ => new Vector2(0, 0),
        };
    }

    /// <summary>
    /// 有効かどうか
    /// </summary>
    public bool IsEnable { get; private set; }
    /// <summary>
    /// ファイル名
    /// </summary>
    public string FileName { get; private set; }
    /// <summary>
    /// 不透明度
    /// </summary>
    public double Opacity { get; set; }
    /// <summary>
    /// 回転角度
    /// </summary>
    public double Rotation { get; set; }
    /// <summary>
    /// 描画する基準点
    /// </summary>
    public ReferencePoint ReferencePoint { get; set; }
    /// <summary>
    /// 拡大率
    /// </summary>
    public Vector2 Scale { get; set; }
    /// <summary>
    /// Raylibのテクスチャ2D
    /// </summary>
    public Texture2D RayTexture { get; private set; }
}

/// <summary>
/// 描画する基準点
/// </summary>
public enum ReferencePoint
{
    TopLeft, TopCenter, TopRight,
    CenterLeft, Center, CenterRight,
    BottomLeft, BottomCenter, BottomRight
}
