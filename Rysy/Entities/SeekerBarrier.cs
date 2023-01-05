using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("seekerBarrier")]
public class SeekerBarrier : RectangleEntity {
    public override Color FillColor => OutlineColor * 0.45f;

    public override Color OutlineColor => Color.White * 0.9f;

    public override int Depth => 0;
}
