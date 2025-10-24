using Aup2Drawer;
using Aup2Drawer.Renderer;
using Raylib_cs;

// --- 1. パース ---
var parser = new AupParser(); // パーサーを初期化
var project = parser.Parse("path/to/your/animation.aup2"); // path/to/your/animation.aup2の部分は描画したいaup2ファイルのパスに置換

// --- 2. 初期化 ---
Raylib.InitWindow(project.Width, project.Height, "Aup2Drawer Example"); // aup2のサイズに合わせられる
Raylib.SetTargetFPS(project.Rate); // aup2のフレームレートに合わせられる
var renderer = new AupRenderer(project); // レンダラーを初期化

int currentFrame = 0;

// --- 3. メインループ ---
while (!Raylib.WindowShouldClose())
{
    currentFrame = (currentFrame + 1) % (project.Length + 1);

    Raylib.BeginDrawing();
    Raylib.BeginBlendMode(BlendMode.Alpha); // 内部でrlgl.SrtBlendMode()を用いて合成モードを切り替えているので、これがないと合成モードが正しく切り替わらない
    Raylib.ClearBackground(Color.Black);

    renderer.DrawFrame(currentFrame);

    Raylib.EndBlendMode();
    Raylib.EndDrawing();
}

// --- 4. クリーンアップ ---
renderer.Dispose();
Raylib.CloseWindow();