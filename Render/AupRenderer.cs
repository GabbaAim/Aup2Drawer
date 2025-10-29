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

    private readonly bool _isLooping;
    private double _internalTime = 0;

    /// <summary>
    /// 各オブジェクトが最初に表示された時刻を記録する
    /// </summary>
    private readonly Dictionary<AupObject, double> _objectAppearTimes = new();
    private double _rendererTime = 0; // レンダラー全体の経過時間

    /// <summary>
    /// 現在の再生フレーム
    /// </summary>
    public int CurrentFrame { get; private set; } = 0;

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying { get; private set; } = false;

    /// <summary>
    /// 再生が（ループせずに）終了したかどうか
    /// </summary>
    public bool IsFinished { get; private set; } = false;

    /// <summary>
    /// レンダラーを初期化します。
    /// </summary>
    /// <param name="project">描画するAupProject</param>
    /// <param name="isLooping">アニメーションをループ再生するかどうか</param>
    public AupRenderer(AupProject project, bool isLooping = true)
    {
        _project = project;
        _isLooping = isLooping;
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

    // 再生制御メソッドを追加
    /// <summary>
    /// 再生を開始します。
    /// </summary>
    public void Play()
    {
        if (IsFinished)
        {
            Reset(); // フレームをリセットしてから再生
        }
        _rendererTime = (double)CurrentFrame / _project.Rate;
        IsPlaying = true;
    }

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    public void Stop()
    {
        IsPlaying = false;
    }

    /// <summary>
    /// 再生をリセットし、フレームを0に戻します。
    /// </summary>
    public void Reset()
    {
        IsPlaying = false;
        IsFinished = false;
        CurrentFrame = 0;
        _internalTime = 0;
        _objectAppearTimes.Clear();
        _rendererTime = 0;
    }

    /// <summary>
    /// フレームを更新し、指定されたオフセット位置に描画します。
    /// </summary>
    /// <param name="offsetX">描画全体のXオフセット</param>
    /// <param name="offsetY">描画全体のYオフセット</param>
    public void UpdateAndDraw(float offsetX = 0, float offsetY = 0, float fadeInDuration = 0)
    {
        if (IsFinished)
        {
            return;
        }

        // --- フレーム更新処理 ---
        if (IsPlaying)
        {
            float frameTime = Raylib.GetFrameTime();
            _internalTime += frameTime;
            _rendererTime += frameTime; // レンダラー全体の時間も更新
            CurrentFrame = (int)(_internalTime * _project.Rate);

            if (CurrentFrame > _project.Length)
            {
                if (_isLooping)
                {
                    // ループ
                    CurrentFrame %= (_project.Length + 1);
                    _internalTime = (double)CurrentFrame / _project.Rate;
                }
                else
                {
                    // 再生終了
                    CurrentFrame = _project.Length;
                    Stop();
                    IsFinished = true;
                }
            }
        }

        // --- 描画処理 ---
        DrawInternal(CurrentFrame, offsetX, offsetY, fadeInDuration);
    }

    /// <summary>
    /// 指定されたフレームを描画する
    /// </summary>
    private void DrawInternal(int frame, float offsetX, float offsetY, float fadeInDuration)
    {
        // AviUtlの原点(0,0)は画面中央なので、プロジェクトサイズの半分を加算
        float originOffsetX = _project.Width / 2.0f;
        float originOffsetY = _project.Height / 2.0f;

        // 最終的なオフセットを計算
        float finalOffsetX = offsetX + originOffsetX;
        float finalOffsetY = offsetY + originOffsetY;

        // レイヤーが小さい順（奥から）に描画
        foreach (var obj in _project.Objects.OrderBy(o => o.Layer))
        {
            if (!obj.IsVisible(frame))
            {
                // オブジェクトが非表示になったら出現時刻をリセット
                _objectAppearTimes.Remove(obj);
                continue;
            }

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

            if (fadeInDuration > 0)
            {
                // オブジェクトが初めて現れたかチェック
                if (!_objectAppearTimes.ContainsKey(obj))
                {
                    // 初めてなら現在の時刻を記録
                    _objectAppearTimes[obj] = _rendererTime;
                }

                // 描画開始からの経過時間を計算
                double appearTime = _objectAppearTimes[obj];
                double elapsedTime = _rendererTime - appearTime;

                // フェードインの進行度 (0.0 to 1.0) を計算
                float fadeInProgress = Math.Clamp((float)(elapsedTime / fadeInDuration), 0.0f, 1.0f);

                // 最終的な透明度にフェードイン効果を乗算
                finalTransform.Opacity *= fadeInProgress;
            }

            // --- ステップ3: 最終的なTransformを使って描画 ---
            DrawObject(texture, finalTransform, drawingEffect, finalOffsetX, finalOffsetY);
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

            // --- 正しい行列演算に基づいた変換 ---

            // 1. 拡大・縮小を適用
            //    現在のオブジェクトの位置を、グループの原点(0,0)からの相対ベクトルとみなし、拡大する。
            transform.Position *= groupScale;

            // 2. 回転を適用
            //    拡大後の位置ベクトルを、グループの原点(0,0)を中心に回転させる。
            if (groupRotationZ != 0)
            {
                var rotMatrix = Matrix3x2.CreateRotation(MathHelper.ToRadians(groupRotationZ));
                transform.Position = Vector2.Transform(transform.Position, rotMatrix);
            }

            // 3. 平行移動を適用
            //    グループ自体の位置を加算する。
            transform.Position += groupPosition;

            // その他のプロパティを合成
            transform.Scale *= groupScale;
            transform.RotationZ += groupRotationZ;
            transform.Opacity *= groupOpacity;
        }
    }

    /// <summary>
    /// 最終的なTransform情報を使ってオブジェクトを描画する
    /// </summary>
    private void DrawObject(Texture texture, Transform transform, StandardDrawingEffect effect, float totalOffsetX, float totalOffsetY)
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
            totalOffsetX + transform.Position.X,
            totalOffsetY + transform.Position.Y,
            transform.RotationZ,
            sourceRect: null,
            drawOrigin: null,
            reverseX: transform.InvertX,
            reverseY: transform.InvertY
        );

        Rlgl.SetBlendMode(BlendMode.Alpha);
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