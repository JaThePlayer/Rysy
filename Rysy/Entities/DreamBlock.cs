﻿using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("dreamBlock")]
public class DreamBlock : RectangleEntity, ISolid
{
    public override Color Color => Color.Black;

    public override Color OutlineColor => Color.White;

    public override int Depth => Depths.Solids;
}
