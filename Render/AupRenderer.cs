using Aup2Drawer.Core;
using Aup2Drawer.Effects;
using Aup2Drawer.Properties;
using Raylib_cs;
using System.Numerics;

namespace Aup2Drawer.Renderer;

public class AupRenderer : IDisposable
{
    private readonly AupProject _project;
    // テクスチャをファイルパスでキャッシュするためのDictionary
    private readonly Dictionary<string, Texture> _textureCache = new();

    public AupRenderer(AupProject project)
    {
        _project = project;
        PreloadTextures();
    }

    /// <summary>
    /// プロジェクトで使われている画像ファイルを事前に読み込んでおく
    /// </summary>
    private void PreloadTextures()
    {
        var imageFileEffects = _project.Objects
            .SelectMany(o => o.Effects)
            .OfType<ImageFileEffect>();

        foreach (var effect in imageFileEffects)
        {
            if (!string.IsNullOrEmpty(effect.FilePath) && !_textureCache.ContainsKey(effect.FilePath))
            {
                var texture = new Texture(effect.FilePath);
                if (texture.IsEnable)
                {
                    // アンチエイリアスを有効にする
                    Raylib.SetTextureFilter(texture.RayTexture, TextureFilter.Bilinear);
                    _textureCache[effect.FilePath] = texture;
                }
            }
        }
    }

    /// <summary>
    /// 指定されたフレームを描画する
    /// </summary>
    public void DrawFrame(int frame)
    {
        float screenCenterX = _project.Width / 2.0f;
        float screenCenterY = _project.Height / 2.0f;

        // レイヤーが小さい順（奥から）に描画
        foreach (var obj in _project.Objects.OrderBy(o => o.Layer))
        {
            if (!obj.IsVisible(frame)) continue;

            var drawingEffect = obj.Effects.OfType<StandardDrawingEffect>().FirstOrDefault();
            var imageFileEffect = obj.Effects.OfType<ImageFileEffect>().FirstOrDefault();

            if (drawingEffect == null || imageFileEffect == null) continue;
            if (!_textureCache.TryGetValue(imageFileEffect.FilePath, out var texture)) continue;

            // --- ステップ1: オブジェクト自身のTransformを計算 ---
            var finalTransform = CalculateObjectTransform(frame, drawingEffect);

            // --- フィルターの適用 ---
            // 拡大率フィルター
            var scaleFilter = obj.Effects.OfType<ScaleFilterEffect>().FirstOrDefault();
            if (scaleFilter != null)
            {
                float baseScale = scaleFilter.BaseScale.GetValue(frame) / 100.0f;
                float scaleX = scaleFilter.X.GetValue(frame) / 100.0f;
                float scaleY = scaleFilter.Y.GetValue(frame) / 100.0f;

                var currentScale = finalTransform.Scale;
                currentScale.X *= baseScale * scaleX;
                currentScale.Y *= baseScale * scaleY;
                finalTransform.Scale = currentScale;
            }
            // 反転フィルター
            var invertFilter = obj.Effects.OfType<InvertFilterEffect>().FirstOrDefault();
            if (invertFilter != null)
            {
                // ▼▼▼ 修正点: Scaleの変更ではなく、boolプロパティをトグルする ▼▼▼
                if (invertFilter.InvertX)
                {
                    finalTransform.InvertX = !finalTransform.InvertX;
                }
                if (invertFilter.InvertY)
                {
                    finalTransform.InvertY = !finalTransform.InvertY;
                }
            }

            // --- ステップ2: グループ制御のTransformを適用 ---
            ApplyGroupControls(frame, obj, ref finalTransform);

            // --- ステップ3: 最終的なTransformを使って描画 ---
            DrawObject(texture, finalTransform, drawingEffect, screenCenterX, screenCenterY);
        }
    }

    /// <summary>
    /// オブジェクト単体のTransformを計算する
    /// </summary>
    private Transform CalculateObjectTransform(int frame, StandardDrawingEffect effect)
    {
        // 基本スケールを計算
        float baseScale = effect.Scale.GetValue(frame) / 100.0f;
        Vector2 scale = new Vector2(baseScale, baseScale);

        // 縦横比を取得
        float aspectRatio = effect.AspectRatio.GetValue(frame);

        // 縦横比を適用
        if (aspectRatio > 0)
        {
            // プラスの値: 横方向に縮む (Xスケールを小さくする)
            // aspectRatioが100のとき、スケールは0になる
            scale.X *= (1.0f - aspectRatio / 100.0f);
        }
        else if (aspectRatio < 0)
        {
            // マイナスの値: 縦方向に縮む (Yスケールを小さくする)
            // aspectRatioが-100のとき、スケールは0になる
            scale.Y *= (1.0f + aspectRatio / 100.0f);
        }

        return new Transform
        {
            Position = new Vector2(
                effect.X.GetValue(frame),
                effect.Y.GetValue(frame)
            ),
            Scale = scale, // 計算済みのスケールを設定
            RotationZ = effect.RotationZ.GetValue(frame),
            Opacity = 1.0f - (effect.Opacity.GetValue(frame) / 100.0f)
        };
    }

    /// <summary>
    /// 指定されたTransformにグループ制御の効果を適用する
    /// </summary>
    private void ApplyGroupControls(int frame, AupObject obj, ref Transform transform)
    {
        var originPosition = new Vector2(
                transform.Position.X,
                transform.Position.Y
                );

        // GroupControlsリストはオブジェクトに近い順（レイヤー降順）にソート済み
        foreach (var groupObj in obj.GroupControls)
        {
            if (!groupObj.IsVisible(frame)) continue;

            var groupEffect = groupObj.Effects.OfType<GroupControlEffect>().FirstOrDefault();
            if (groupEffect == null) continue;

            // グループ制御の現在のTransformを計算
            var groupScale = groupEffect.Scale.GetValue(frame) / 100.0f;
            var groupPosition = new Vector2(
                groupEffect.X.GetValue(frame),
                groupEffect.Y.GetValue(frame)
            );
            var groupRotationZ = groupEffect.RotationZ.GetValue(frame);
            var groupOpacity = 1.0f - (groupEffect.Opacity.GetValue(frame) / 100.0f);

            // --- 行列演算に基づいた変換 ---
            // 1. 回転
            // 2. 拡大・縮小
            // 3. 平行移動
            // RaylibのDrawTextureProは内部でこれに近い処理をしているので、各要素を正しく合成する

            // 拡大・縮小を適用 (オブジェクトの座標をグループの原点を中心に拡大)
            transform.Position *= groupScale;

            // 回転を適用
            Vector2 rotatedPos = Vector2.Transform(transform.Position, Matrix3x2.CreateRotation(MathHelper.ToRadians(groupRotationZ)));
            transform.Position = rotatedPos;

            // 平行移動を適用
            transform.Position = originPosition + groupPosition;

            // その他のプロパティを合成
            transform.Scale *= groupScale;
            transform.RotationZ += groupRotationZ;
            transform.Opacity *= groupOpacity;
        }
    }

    /// <summary>
    /// 最終的なTransform情報を使ってオブジェクトを描画する
    /// </summary>
    private void DrawObject(Texture texture, Transform transform, StandardDrawingEffect effect, float screenCenterX, float screenCenterY)
    {
        texture.ReferencePoint = ReferencePoint.Center;
        texture.Scale = transform.Scale;
        texture.Opacity = Math.Clamp(transform.Opacity, 0.0, 1.0);

        BlendMode raylibBlendMode = effect.BlendMode switch
        {
            BlendModeType.Normal => BlendMode.Alpha,
            BlendModeType.Add => BlendMode.Additive,
            BlendModeType.Subtract => BlendMode.SubtractColors,
            BlendModeType.Multiply => BlendMode.Multiplied,
            
            // 未対応のモードは通常描画
            _ => BlendMode.Alpha
        };

        Rlgl.SetBlendMode(raylibBlendMode);

        texture.Draw(
            screenCenterX + transform.Position.X,
            screenCenterY + transform.Position.Y,
            transform.RotationZ,
            sourceRect: null,
            drawOrigin: null,
            reverseX: transform.InvertX,
            reverseY: transform.InvertY
        );
    }

    /// <summary>
    /// 読み込んだテクスチャを解放する
    /// </summary>
    public void Dispose()
    {
        foreach (var texture in _textureCache.Values)
        {
            texture.Dispose();
        }
        _textureCache.Clear();
    }
}