using Aup2Drawer.Core;
using Aup2Drawer.Effects;
using Aup2Drawer.Properties;
using Raylib_cs;
using System.Diagnostics;
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
    /// このレンダラーインスタンスの生存時間を計測するタイマー
    /// </summary>
    private readonly Stopwatch _instanceStopwatch = new();

    /// <summary>
    /// 最初に描画が呼び出されたアプリケーション時刻
    /// </summary>
    private float? _firstDrawTime = null;

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
        if (_isLooping && IsFinished)
        {
            Reset(); // フレームをリセットしてから再生
        }
        IsPlaying = true;
        _instanceStopwatch.Start();
    }

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    public void Stop()
    {
        IsPlaying = false;
        _instanceStopwatch.Stop();
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
        _firstDrawTime = null;
        _instanceStopwatch.Reset();
    }

    /// <summary>
    /// レンダラーの状態を1フレーム分更新します。
    /// このメソッドは、描画ループの更新セクションで1フレームに1回だけ呼び出してください。
    /// </summary>
    public void Update()
    {
        if (IsFinished || !IsPlaying) return; // 終了しているか、再生中でなければ何もしない

        _internalTime += Raylib.GetFrameTime();
        CurrentFrame = (int)(_internalTime * _project.Rate);

        if (CurrentFrame > _project.Length)
        {
            if (_isLooping)
            {
                CurrentFrame %= (_project.Length + 1);
                _internalTime = (double)CurrentFrame / _project.Rate;
            }
            else
            {
                CurrentFrame = _project.Length;
                Stop();
                IsFinished = true;
            }
        }
    }

    /// <summary>
    /// 現在のフレームを指定されたオフセット位置に描画します。
    /// このメソッドは内部状態を変更しません。1フレームに複数回呼び出すことができます。
    /// </summary>
    /// <param name="offsetX">描画全体のXオフセット</param>
    /// <param name="offsetY">描画全体のYオフセット</param>
    /// <param name="fadeInDuration">オブジェクト表示時のフェードイン時間（秒）</param>
    public void Draw(float offsetX = 0, float offsetY = 0, float fadeInDuration = 0)
    {
        if (IsFinished) return;

        // 内部タイマーの現在時刻を取得
        float instanceTime = (float)_instanceStopwatch.Elapsed.TotalSeconds;
        if (_firstDrawTime == null)
        {
            _firstDrawTime = instanceTime;
        }

        // 描画処理本体を呼び出す
        DrawInternal(CurrentFrame, instanceTime, offsetX, offsetY, fadeInDuration);
    }

    /// <summary>
    /// 指定されたフレームを描画する
    /// </summary>
    private void DrawInternal(int frame, float instanceTime, float offsetX, float offsetY, float fadeInDuration)
    {
        // AviUtlの原点(0,0)は画面中央なので、プロジェクトサイズの半分を加算
        float originOffsetX = _project.Width / 2.0f;
        float originOffsetY = _project.Height / 2.0f;

        // 最終的なオフセットを計算
        float finalOffsetX = offsetX + originOffsetX;
        float finalOffsetY = offsetY + originOffsetY;

        float overallFadeAlpha = 1.0f;
        if (fadeInDuration > 0 && _firstDrawTime.HasValue)
        {
            float elapsedTime = instanceTime - _firstDrawTime.Value;
            overallFadeAlpha = Math.Clamp(elapsedTime / fadeInDuration, 0.0f, 1.0f);
        }

        // レイヤーが小さい順（奥から）に描画
        foreach (var obj in _project.Objects.OrderBy(o => o.Layer))
        {
            if (!obj.IsVisible(frame))
            {
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

            finalTransform.Opacity *= overallFadeAlpha;

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