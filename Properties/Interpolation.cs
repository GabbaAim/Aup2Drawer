namespace Aup2Drawer.Properties;

public enum InterpolationType
{
    Linear,         // 直線移動
    Instant,        // 瞬間移動
    Interpolated,   // 補間移動 (イージング)
    TimeCtrl, // 時間制御
    Unknown
}

public enum BlendModeType
{
    Normal,      // 通常
    Add,         // 加算
    Subtract,    // 減算
    Multiply,    // 乗算
    Screen,      // スクリーン
    Overlay,     // オーバーレイ
    Lighten,     // 比較(明)
    Darken,      // 比較(暗)
    Unknown
}