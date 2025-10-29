# Aup2Drawer
Aup2Drawerは、AviUtlのプロジェクトファイル(.aup)からエクスポートされたオブジェクトファイル(.aup2形式のテキスト)をパースし、Raylib-csを使用してリアルタイムで描画するためのライブラリです。
描画まわりを変更すれば他ライブラリでも使えるはず。
## 主な機能
* .aup2形式のテキストファイルのパースとオブジェクト構造の構築
* アニメーションのリアルタイム描画
* 中間点を含むオブジェクトの再生
* グループ制御
* 一部フィルター効果の適用
## 使い方（Program.csより）
```
using Aup2Drawer;
using Aup2Drawer.Renderer;
using Raylib_cs;

// --- 1. パース ---
var parser = new AupParser();
var project = parser.Parse("path/to/your/animation.aup2"); // path/to/your/animation.aup2の部分は描画したいaup2ファイルのパスに置換

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

    renderer.UpdateAndDraw(drawPosX, drawPosY); // 25/10/29 drawPosYのあとに秒数を指定するとその秒数だけフェードインして描画されるように

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
```
## 対応仕様
### オブジェクトとプロパティ
機能 | 対応状況 | 備考
--- | --- | ---
基本オブジェクト | ✅ | 画像ファイル、グループ制御
中間点 | ✅ | 1つのオブジェクトに複数のキーフレームを持つ構造に対応
座標 (X, Y, Z) | ✅ | Z座標はパースされますが、2D描画では使用されません
拡大率 | ✅ | 
透明度 | ✅ | 
回転 | ⚠️ | Z軸回転のみ対応
縦横比 | ✅ | 
### 移動方法
移動方法 | 対応状況 | 備考
--- | --- | ---
直線移動 | ✅ | デフォルトでは線形補間、加速の場合EaseInQuad、減速の場合EaseOutQuad、加減速の場合EaseInOutQuadでそれぞれ近似
瞬間移動 | ✅ | 区間に入ったら終点の値になる
補間移動 | ✅ | デフォルト（加減速ON）ではEaseInOutSine、加速の場合EaseInSine、減速の場合EaseOutSine、加減速なしの場合線形補間でそれぞれ近似
時間制御 | ✅ | 3次ベジェ曲線（スプライン）として実装
その他	 | ❌ | 未対応の移動方法はすべて「直線移動」として処理されます
### 合成モード（ブレンドモード）
合成モード | 対応状況 | 対応したRaylib-csのBlendMode
--- | --- | ---
通常 | ✅ | BlendMode.Alpha
加算 | ✅ | BlendMode.Additive
減算 | ✅ | BlendMode.SubtractColors
乗算 | ✅ | BlendMode.Multiplied
その他 | ❌ | 未対応の合成モードは通常モードとして扱われます
### フィルター効果
フィルター | 対応状況 | 備考
--- | --- | ---
拡大率 | ✅ | 基準拡大率、X、Yに対応
反転 | ⚠️ | 上下反転、左右反転にのみ対応
その他 | ❌ | 未対応のフィルターは無視されます（が、透明度とかは使えるはず）
## 未対応・制限事項
* 音声/図形オブジェクト
  * 未対応のデータはすべて無視されます。
* カメラ制御
  * カメラ制御オブジェクトには対応していません。
* テキストオブジェクト
  * テキストオブジェクトの描画には対応していません。
* スクリプト制御
  * スクリプトによる制御には対応していません。
* 3D関連のプロパティ
  * X軸回転, Y軸回転 など、3Dに関連するプロパティは無視されます。
## 謝辞
Texture.csはazarea09様の[Potesara](https://github.com/azarea09/Potesara/blob/master/Texture.cs)からお借りしています。