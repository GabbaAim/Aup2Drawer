using Aup2Drawer.Core;

namespace Aup2Drawer;

public class AupProject
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Rate { get; set; }
    public int Scale { get; set; }
    public int Length { get; set; }

    public List<AupObject> Objects { get; } = new();
}