using Aup2Drawer;
using Aup2Drawer.Renderer;
using Raylib_cs;

// --- 1. パース ---
var parser = new AupParser();
var project = parser.Parse("path/to/your/animation.aup2.aup2"); // path/to/your/animation.aup2の部分は描画したいaup2ファイルのパスに置換

// --- 2. 初期化 ---
Raylib.InitWindow(project.Width, project.Height, "Aup2Drawer Example");

// アプリケーション自体のFPSを設定 (アニメーションのFPSとは独立)
Raylib.SetTargetFPS(120);

// --- レンダラーの初期化オプション ---
// 例1: ループ再生する。
var renderer = new AupRenderer(project, isLooping: true);

// 例2: ループせず、再生が終了したら停止
// var renderer = new AupRenderer(project, isLooping: false);

// 再生開始
renderer.Play();

// --- 3. メインループ ---
while (!Raylib.WindowShouldClose())
{
    // --- 描画 ---
    Raylib.BeginDrawing();
    Raylib.BeginBlendMode(BlendMode.Alpha); // 内部でrlgl.SrtBlendMode()を用いて合成モードを切り替えているので、これがないと合成モードが正しく切り替わらない
    Raylib.ClearBackground(Color.Black);

    // --- 描画位置の指定 ---
    // 例1: ウィンドウの中央に描画する
    float drawPosX = (Raylib.GetScreenWidth() - project.Width) / 2.0f;
    float drawPosY = (Raylib.GetScreenHeight() - project.Height) / 2.0f;
    renderer.UpdateAndDraw(drawPosX, drawPosY);

    // 例2: ウィンドウの左上に描画する (デフォルト)
    // renderer.UpdateAndDraw(0, 0);

    // 例3: マウスカーソルの位置に描画する
    // renderer.UpdateAndDraw(Raylib.GetMouseX(), Raylib.GetMouseY());

    Raylib.EndBlendMode();
    Raylib.EndDrawing();
}

// --- 4. クリーンアップ ---
renderer.Dispose();
Raylib.CloseWindow();