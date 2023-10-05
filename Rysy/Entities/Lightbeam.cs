using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("lightbeam")]
public sealed class Lightbeam : Entity, IPlaceable, IPreciseRotatable {
    public override int Depth => 0;

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public override IEnumerable<ISprite> GetSprites() {
        var texture = GFX.Atlas["util/lightbeam"];
        var color = new Color(0.8f, 1.0f, 1.0f, 0.4f);
        var angle = Float("rotation").ToRad();
        var (w, h) = (Width, Height);

        var offset = new Vector2(w / 2 * MathF.Cos(angle), w / 2 * MathF.Sin(angle));

        yield return ISprite.FromTexture(Pos + offset, texture) with {
            Rotation = angle + MathHelper.PiOver2,
            Color = color,
            Scale = new((h - 4) / (float)texture.Width, w),
            Origin = default,
        };
    }

    public override ISelectionCollider GetMainSelection() => ISelectionCollider.FromSprite((Sprite)GetSprites().First());

    public static FieldList GetFields() => new(new {
        flag = "",
        rotation = 0.0f
    });

    public static PlacementList GetPlacements() => new("lightbeam");

    public Entity? RotatePreciseBy(float angle) => CloneWith(pl => pl["rotation"] = Float("rotation") + angle.RadToDegrees());
}