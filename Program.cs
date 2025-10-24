using Aup2Drawer;
using Aup2Drawer.Effects;
using Aup2Drawer.Properties;
using Aup2Drawer.Renderer;
using Raylib_cs;
using System.Diagnostics;
using System.Text;

class Program
{
    static void Main()
    {
        // --- 設定 ---
        const string aup2FilePath = @"test.aup2";
        const int windowWidth = 1920;
        const int windowHeight = 1080;

        // 1. .aup2ファイルをパースする
        Console.WriteLine($"Parsing '{aup2FilePath}'...");
        var parser = new AupParser();
        AupProject project;
        try
        {
            project = parser.Parse(aup2FilePath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to parse the file: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }

        // 2. パース結果の基本情報をコンソールに出力する
        PrintProjectDetails(project); // 詳細出力関数を呼び出す

        // --- ここから下はRaylibのウィンドウ表示処理 (変更なし) ---

        // 3. Raylibウィンドウを作成する
        Raylib.InitWindow(windowWidth, windowHeight, "Aup2Drawer");
        Raylib.SetTargetFPS(project.Rate);

        // 4. AupRendererをインスタンス化する
        var renderer = new AupRenderer(project);

        // 再生用のカウンター
        int currentFrame = 0;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // 5. メインループ
        while (!Raylib.WindowShouldClose())
        {
            // --- 更新処理 ---
            double elapsedTime = stopwatch.Elapsed.TotalSeconds;
            currentFrame = (int)(elapsedTime * project.Rate);

            if (currentFrame > project.Length)
            {
                currentFrame = 0;
                stopwatch.Restart();
            }

            // --- 描画処理 ---
            Raylib.BeginDrawing();
            Raylib.BeginBlendMode(BlendMode.Alpha);
            Raylib.ClearBackground(Color.DarkGray);

            // 6. AupRendererのDrawFrameメソッドを呼び出す
            renderer.DrawFrame(currentFrame);

            Raylib.DrawText($"Frame: {currentFrame}", 10, 10, 20, Color.White);
            Raylib.DrawFPS(10, 40);

            Raylib.EndDrawing();
        }

        renderer.Dispose();
        Raylib.CloseWindow();
    }

    /// <summary>
    /// パースされたAupProjectの詳細をコンソールに出力する
    /// </summary>
    static void PrintProjectDetails(AupProject project)
    {
        Console.WriteLine("--- Project Info ---");
        Console.WriteLine($"  Size: {project.Width}x{project.Height}");
        Console.WriteLine($"  Rate: {project.Rate}fps, Length: {project.Length} frames ({project.Length / (double)project.Rate:F2}s)");
        Console.WriteLine($"  Found {project.Objects.Count} objects.");
        Console.WriteLine("--------------------");
        Console.WriteLine();

        // 各オブジェクトの詳細を出力
        foreach (var obj in project.Objects.OrderBy(o => o.Layer).ThenBy(o => o.Segments.FirstOrDefault()?.StartFrame ?? 0))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"■ Object ID: {obj.Id} (Layer: {obj.Layer})");
            Console.ResetColor();

            // セグメント情報
            var segmentsStr = string.Join(", ", obj.Segments.Select(s => $"[{s.StartFrame}-{s.EndFrame}]"));
            Console.WriteLine($"  Segments: {segmentsStr}");

            // エフェクト情報
            foreach (var effect in obj.Effects)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ▶ Effect: {effect.Name}");
                Console.ResetColor();

                // 各エフェクトの詳細を出力
                switch (effect)
                {
                    case ImageFileEffect imageFile:
                        Console.WriteLine($"    - FilePath: {imageFile.FilePath}");
                        break;

                    case StandardDrawingEffect drawing:
                        PrintAnimatedProperty("X", drawing.X);
                        PrintAnimatedProperty("Y", drawing.Y);
                        PrintAnimatedProperty("Z", drawing.Z);
                        PrintAnimatedProperty("Scale", drawing.Scale, "拡大率");
                        PrintAnimatedProperty("Opacity", drawing.Opacity, "透明度");
                        PrintAnimatedProperty("RotationZ", drawing.RotationZ, "Z軸回転");
                        break;

                    case GroupControlEffect groupControl:
                        Console.WriteLine($"    - TargetLayerCount: {groupControl.TargetLayerCount}");
                        PrintAnimatedProperty("X", groupControl.X);
                        PrintAnimatedProperty("Y", groupControl.Y);
                        PrintAnimatedProperty("Scale", groupControl.Scale, "拡大率");
                        PrintAnimatedProperty("RotationZ", groupControl.RotationZ, "Z軸回転");
                        break;
                }
            }
            Console.WriteLine(); // オブジェクトごとに改行
        }
    }

    /// <summary>
    /// AnimatedPropertyの中身（キーフレーム）を整形して出力するヘルパー関数
    /// </summary>
    static void PrintAnimatedProperty(string propName, AnimatedProperty<float> prop, string displayName = null)
    {
        if (prop.Keyframes.Any())
        {
            var sb = new StringBuilder();
            sb.Append($"    - {displayName ?? propName}: ");
            foreach (var kf in prop.Keyframes)
            {
                sb.Append($"({kf.Frame}f: {kf.Value:F2}) ");
            }
            Console.WriteLine(sb.ToString());
        }
    }
}