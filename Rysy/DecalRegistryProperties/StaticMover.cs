﻿using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("staticMover")]
public sealed class StaticMoverDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        x = 0,
        y = 0,
        width = 16,
        height = 16,
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        yield return ISprite.FromTexture(texture).Centered();

        var center = new Vector2(Data.Float("x"), Data.Float("y"));

        yield return ISprite.OutlinedRect(center, Data.Int("width", 16), Data.Int("height", 16), Color.Transparent, Color.Red);
    }
}