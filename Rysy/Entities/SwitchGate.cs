﻿using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("switchGate")]
public class SwitchGate : NineSliceEntity, ISolid, IPlaceable {
    public override int Depth => Depths.Solids;
    public override string TexturePath => $"objects/switchgate/{Attr("sprite", "block")}";

    public override string? CenterSpritePath => "objects/switchgate/icon00";
    public override Color CenterSpriteColor => "5fcde4".FromRGB();
    public override Color CenterSpriteOutlineColor => Color.Black;

    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new(new {
        sprite = Fields.AtlasPath("block", "^objects/switchgate/(.*)") with {
            Filter = (p) => GFX.Atlas[p.Path] is { Width: 24, Height: 24 }
        },
        persistent = false
    });

    public static PlacementList GetPlacements() => new string[] { "block", "mirror", "temple", "stars" }
        .Select(t => new Placement(t, new {
            sprite = t
        }))
        .ToPlacementList();
}
