namespace Aup2Drawer.Core;

public class AupObjectSegment
{
    public int StartFrame { get; }
    public int EndFrame { get; }

    public AupObjectSegment(int startFrame, int endFrame)
    {
        StartFrame = startFrame;
        EndFrame = endFrame;
    }
}