namespace Aup2Drawer.Effects;

public class InvertFilterEffect : AupEffect
{
    public bool InvertY { get; set; } = false; // 上下反転
    public bool InvertX { get; set; } = false; // 左右反転

    public InvertFilterEffect()
    {
        Name = "反転";
    }
}