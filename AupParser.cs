using Aup2Drawer.Core;
using Aup2Drawer.Effects;
using Aup2Drawer.Properties;
using System.IO;
using System.Text.RegularExpressions;

namespace Aup2Drawer;

public class AupParser
{
    // オブジェクトID ([0], [1]など) を抽出するための正規表現
    private static readonly Regex ObjectRegex = new(@"^\[(\d+)\]$");
    // エフェクトID ([0.0], [0.1]など) を抽出するための正規表現
    private static readonly Regex EffectRegex = new(@"^\[(\d+)\.(\d+)\]$");

    public AupProject Parse(string filePath)
    {
        var project = new AupProject();
        // .aup2ファイルの親ディレクトリを取得
        string baseDirectory = Path.GetDirectoryName(filePath);
        var lines = File.ReadLines(filePath);

        AupObject currentObject = null;
        AupEffect currentEffect = null;
        string currentSectionName = null;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // --- セクションヘッダーの処理 ---
            if (line.StartsWith("["))
            {
                currentSectionName = line.Trim();
                var objectMatch = ObjectRegex.Match(currentSectionName);
                var effectMatch = EffectRegex.Match(currentSectionName);

                if (objectMatch.Success)
                {
                    // オブジェクトセクション ([0], [1]など)
                    int id = int.Parse(objectMatch.Groups[1].Value);
                    currentObject = new AupObject(id);
                    project.Objects.Add(currentObject);
                    currentEffect = null;
                }
                else if (effectMatch.Success)
                {
                    // エフェクトセクション ([0.0], [0.1]など)
                    currentEffect = null;
                }
                else
                {
                    // その他のセクション ([project], [scene.0]など)
                    currentObject = null;
                    currentEffect = null;
                }
                continue;
            }

            // --- キーと値の処理 ---
            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length < 2) continue;
            string key = parts[0];
            string value = parts[1];

            // --- 各セクションに応じたパース処理 ---
            switch (currentSectionName)
            {
                case "[project]":
                    // TODO: versionなど必要なら
                    break;

                case "[scene.0]":
                    if (key == "video.width") project.Width = int.Parse(value);
                    else if (key == "video.height") project.Height = int.Parse(value);
                    else if (key == "video.rate") project.Rate = int.Parse(value);
                    else if (key == "video.scale") project.Scale = int.Parse(value);
                    // aup2のlengthは最終フレーム+1なので、-1する
                    else if (key == "video.length") project.Length = int.Parse(value) - 1;
                    break;
            }

            if (currentObject != null)
            {
                // --- オブジェクトのプロパティ ([0]など) ---
                if (EffectRegex.IsMatch(currentSectionName) == false)
                {
                    if (key == "layer")
                    {
                        // aup2のレイヤーは0始まりだが、AviUtlのUI上は1始まり。
                        // データとしては0始まりのまま扱うのが素直。
                        currentObject.Layer = int.Parse(value);
                    }
                    else if (key == "frame")
                    {
                        // "0,1920" や "0,58,119" のような形式
                        currentObject.SetSegmentsFromFrameString(value);
                    }
                }
                // --- エフェクトのプロパティ ([0.0]など) ---
                else
                {
                    if (key == "effect.name")
                    {
                        currentEffect = value switch
                        {
                            "標準描画" => new StandardDrawingEffect(),
                            "画像ファイル" => new ImageFileEffect(),
                            "グループ制御" => new GroupControlEffect(),
                            "拡大率" => new ScaleFilterEffect(),
                            "反転" => new InvertFilterEffect(),
                            _ => new UnknownEffect(value)
                        };
                        currentObject.Effects.Add(currentEffect);
                    }
                    else
                    {
                        // StandardDrawingEffect または GroupControlEffect のプロパティをパース
                        if (currentEffect is StandardDrawingEffect drawingEffect)
                        {
                            if (key == "合成モード")
                            {
                                drawingEffect.BlendMode = value switch
                                {
                                    "通常" => BlendModeType.Normal,
                                    "加算" => BlendModeType.Add,
                                    "減算" => BlendModeType.Subtract,
                                    "乗算" => BlendModeType.Multiply,
                                    /*
                                    "スクリーン" => BlendModeType.Screen,
                                    "オーバーレイ" => BlendModeType.Overlay,
                                    "比較(明)" => BlendModeType.Lighten,
                                    "比較(暗)" => BlendModeType.Darken,
                                    */
                                    _ => BlendModeType.Unknown
                                };
                            }
                            else
                            {
                                drawingEffect.SetPropertyFromValueString(key, value, currentObject.Segments);
                            }
                        }
                        else if (currentEffect is ImageFileEffect imageFileEffect)
                        {
                            if (key == "ファイル")
                            {
                                if (Path.IsPathRooted(value))
                                {
                                    imageFileEffect.FilePath = value;
                                }
                                else
                                {
                                    imageFileEffect.FilePath = Path.Combine(baseDirectory, value);
                                }
                            }
                        }
                        else if (currentEffect is GroupControlEffect groupControlEffect)
                        {
                            if (key == "対象レイヤー数")
                            {
                                groupControlEffect.TargetLayerCount = int.Parse(value);
                            }
                            else
                            {
                                // X, Y, 拡大率などのプロパティをパース
                                groupControlEffect.SetPropertyFromValueString(key, value, currentObject.Segments);
                            }
                        }
                        else if (currentEffect is ScaleFilterEffect scaleFilterEffect)
                        {
                            scaleFilterEffect.SetPropertyFromValueString(key, value, currentObject.Segments);
                        }
                        else if (currentEffect is InvertFilterEffect invertFilterEffect)
                        {
                            if (key == "上下反転") invertFilterEffect.InvertY = (value == "1");
                            else if (key == "左右反転") invertFilterEffect.InvertX = (value == "1");
                        }
                    }
                }
            }
        }

        // 全てのパースが終わった後に、一度だけソートする
        foreach (var obj in project.Objects)
        {
            foreach (var effect in obj.Effects)
            {
                if (effect is StandardDrawingEffect d)
                {
                    d.X.SortKeyframes();
                    d.Y.SortKeyframes();
                    d.Z.SortKeyframes();
                    d.Scale.SortKeyframes();
                    d.Opacity.SortKeyframes();
                    d.RotationZ.SortKeyframes();
                    d.AspectRatio.SortKeyframes();
                }
                else if (effect is GroupControlEffect g)
                {
                    g.X.SortKeyframes();
                    g.Y.SortKeyframes();
                    g.Z.SortKeyframes();
                    g.Scale.SortKeyframes();
                    g.Opacity.SortKeyframes();
                    g.RotationZ.SortKeyframes();
                }
                else if (effect is ScaleFilterEffect s)
                {
                    s.BaseScale.SortKeyframes();
                    s.X.SortKeyframes();
                    s.Y.SortKeyframes();
                }
            }
        }

        // --- グループ制御の関連付け ---
        var groupObjects = project.Objects
            .Where(o => o.Effects.Any(e => e is GroupControlEffect))
            .ToList();

        foreach (var obj in project.Objects)
        {
            if (!obj.Segments.Any()) continue;

            foreach (var groupObj in groupObjects)
            {
                if (!groupObj.Segments.Any()) continue;
                if (obj.Layer <= groupObj.Layer) continue;

                var groupEffect = groupObj.Effects.OfType<GroupControlEffect>().FirstOrDefault();
                if (groupEffect == null) continue;

                // 対象レイヤー範囲内かチェック
                // TargetLayerCountが0の場合は範囲無限として扱う
                bool isInRange = (groupEffect.TargetLayerCount == 0) ||
                                 (obj.Layer <= groupObj.Layer + groupEffect.TargetLayerCount);

                if (isInRange)
                {
                    // 時間的な重複があるかチェック
                    int objStart = obj.Segments.First().StartFrame;
                    int objEnd = obj.Segments.Last().EndFrame;
                    int groupStart = groupObj.Segments.First().StartFrame;
                    int groupEnd = groupObj.Segments.Last().EndFrame;
                    if (objStart <= groupEnd && objEnd >= groupStart)
                    {
                        obj.GroupControls.Add(groupObj);
                    }
                }
            }

            // オブジェクトに近い順（レイヤー番号が大きい順）にソート
            obj.GroupControls.Sort((a, b) => b.Layer.CompareTo(a.Layer));
        }


        if (project.Objects.Any(o => o.Segments.Any()))
        {
            project.Length = project.Objects
                .SelectMany(o => o.Segments)
                .Max(s => s.EndFrame);
        }
        else
        {
            project.Length = 0;
        }

        return project;
    }
}