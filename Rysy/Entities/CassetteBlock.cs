using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using System;

namespace Rysy.Entities;

[CustomEntity("cassetteBlock")]
public class CassetteBlock : Entity, ISolid, IPlaceable {
    public override int Depth => Depths.Solids;

    public virtual string Sprite => "objects/cassetteblock/solid";

    public int Index => Int("index", 0);

    private Color Color => (Int("index", 0) switch {
        1 => "f049be",
        2 => "fcdc3a",
        3 => "38e04e",
        _ => "49aaf0",
    }).FromRGB();

    private Sprite GetSprite(NineSliceLocation loc) {
        var (sx, sy) = loc switch {
            NineSliceLocation.TopLeft => (0, 0),
            NineSliceLocation.TopMiddle => (8, 0),
            NineSliceLocation.TopRight => (16, 0),
            NineSliceLocation.Left => (0, 8),
            NineSliceLocation.Middle => (8, 8),
            NineSliceLocation.Right => (16, 8),
            NineSliceLocation.BottomLeft => (0, 16),
            NineSliceLocation.BottomMiddle => (8, 16),
            NineSliceLocation.BottomRight => (16, 16),

            NineSliceLocation.InnerCorner_UpRight => (24, 0),
            NineSliceLocation.InnerCorner_UpLeft => (24, 8),
            NineSliceLocation.InnerCorner_DownRight => (24, 16),
            NineSliceLocation.InnerCorner_DownLeft => (24, 24),
            _ => throw new NotImplementedException(),
        };

        return ISprite.FromTexture(Sprite).CreateSubtexture(sx, sy, 8, 8) with {
            Color = Color,
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        var index = Index;
        return ConnectedEntityHelper.GetSprites(this, Room.Entities[typeof(CassetteBlock)].Where(e => e is CassetteBlock b && b.Index == index), GetSprite, handleInnerCorners: true);
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public static FieldList GetFields() => new(new {
        index = Fields.Dropdown(0, Indexes),
        tempo = 1.0,
    });

    public static PlacementList GetPlacements() => Enumerable.Range(0, 4)
        .Select(i => new Placement($"cassette_block_{i}", new {
            index = i
        }))
        .ToPlacementList();

    private static TranslatedDictionary<int> Indexes = new("rysy.cassetteBlockIndex") {
        0, 1, 2, 3
    };
}
