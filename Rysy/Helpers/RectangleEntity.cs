﻿using Rysy.Graphics;

namespace Rysy.Helpers;

public abstract class RectangleEntity : Entity {
    public abstract Color FillColor { get; }
    public abstract Color OutlineColor { get; }

    public override IEnumerable<ISprite> GetSprites() {
        var w = Width switch {
            0 => 8,
            var other => other
        };
        var h = Height switch {
            0 => 8,
            var other => other
        };
        var rect = new Rectangle(X, Y, w, h);

        yield return ISprite.OutlinedRect(rect, FillColor, OutlineColor);
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;
}
