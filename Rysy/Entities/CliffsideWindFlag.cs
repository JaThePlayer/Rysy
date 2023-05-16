using Rysy.Graphics;
using Rysy.Helpers;
using System;

namespace Rysy.Entities;

[CustomEntity("cliffside_flag")]
public class CliffsideWindFlag : SpriteEntity, IPlaceable {
    public override string TexturePath => $"scenery/cliffside/flag{Int("index", 0):d2}";

    public override Vector2 Origin => new();

    public override int Depth => 8999;

    public static FieldList GetFields() => new(new {
        index = Fields.Dropdown(0, Enumerable.Range(0, 11).ToList())
    });

    public static PlacementList GetPlacements() => new("cliffside_flag");

    public override IEnumerable<ISprite> GetSprites() {
        var spr = GetSprite();

        var end = Pos + new Vector2(spr.ForceGetWidth(), 0f);
        return new SimpleCurve() {
            Start = Pos,
            End = end,
            Control = (Pos + end) / 2 + new Vector2(0f, 6f)
        }.GetSpritesForFloatySprite(spr);
    }

    public override ISelectionCollider GetMainSelection() 
        => ISelectionCollider.FromSprite(GetSprite());
}
