using Aup2Drawer.Effects;

namespace Aup2Drawer.Core;

public class AupObject
{
    public int Id { get; }
    public int Layer { get; set; }
    public List<AupObjectSegment> Segments { get; } = new();
    public List<AupEffect> Effects { get; } = new();
    /// <summary>
    /// このオブジェクトに影響を与えるグループ制御オブジェクトのリスト。
    /// レイヤー番号の降順（オブジェクトに近い順）でソート済み。
    /// </summary>
    public List<AupObject> GroupControls { get; } = new();

    public AupObject(int id)
    {
        Id = id;
    }

    /// <summary>
    /// "f1,f2,f3,..." 形式の文字列からセグメントを生成して追加する
    /// </summary>
    public void SetSegmentsFromFrameString(string frameString)
    {
        Segments.Clear();
        var frameValues = frameString.Split(',').Select(int.Parse).ToArray();

        if (frameValues.Length < 2) return;

        for (int i = 0; i < frameValues.Length - 1; i++)
        {
            int startFrame = frameValues[i];
            int endFrame = frameValues[i + 1];
            Segments.Add(new AupObjectSegment(startFrame, endFrame));
        }
    }

    public bool IsVisible(int frame)
    {
        if (Segments.Count == 0) return false;
        return frame >= Segments.First().StartFrame && frame <= Segments.Last().EndFrame;
    }
}