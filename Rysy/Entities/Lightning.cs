using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("lightning")]
public class Lightning : RectangleEntity
{
    private static readonly Color outlineColor = "fcf579".FromRGB();
    private static readonly Color color = outlineColor * .3f;

    public override Color FillColor => color;

    public override Color OutlineColor => outlineColor;

    public override int Depth => -1000100;
}
