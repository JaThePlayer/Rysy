using Rysy.Extensions;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Entities.Modded;

[CustomEntity("FancyTileEntities/FancySolidTiles", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyTileEntity : TilegridEntity, IPlaceable {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        randomSeed = 0,
        blendEdges = true,
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyFloatySpaceBlock", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyFloatySpaceBlock : TilegridEntity, IPlaceable {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        connectsTo = Fields.TileDropdown('3', bg: false),
        randomSeed = 0,
        disableSpawnOffset = false
    });

    public static PlacementList GetPlacements() => new("Fancy Floaty Space Block");
}

[CustomEntity("FancyTileEntities/FancyCoverupWall", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyCoverupWall : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        blendIn = true,
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyCrumbleWallOnRumble", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyCrumbleWallOnRumble : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        blendin = true,
        persistent = false,
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyDashBlock", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyDashBlock : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        blendin = true,
        canDash = true,
        permanent = true
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyExitBlock", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyExitBlock : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        playTransitionReveal = false
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyFakeWall", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyFakeWall : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        playTransitionReveal = false
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyFallingBlock", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyFallingBlock : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        climbFall = true,
        behind = false,
        tileDataHighlight = "",
        manualTrigger = false
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyIntroCrusher", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyIntroCrusher : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public override Range NodeLimits => 0..1;

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        manualTrigger = false,
        delay = 1.2,
        speed = 2.0,
        flags = "1,0b"
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyFinalBossMovingBlock", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyFinalBossMovingBlock : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        tileDataHighlight = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        nodeIndex = 0,
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyRidgeGate", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyRidgeGate : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        flag = ""
    });

    public static PlacementList GetPlacements() => new();
}

[CustomEntity("FancyTileEntities/FancyConditionBlock", associatedMods: new string[] { "FancyTileEntities" })]
internal sealed class FancyConditionBlock : TilegridEntity {
    public override char[,] ParseTilegrid(string gridString, int widthTiles, int heightTiles)
        => TileMapHelper.GenerateTileMap(gridString, widthTiles, heightTiles);

    public static FieldList GetFields() => new(new {
        tileData = Fields.Tilegrid(TileLayer.FG).WithTilegridParser(TileMapHelper.GenerateTileMap, TilegridField.DefaultGridToSavedString),
        condition = CelesteEnums.ConditionBlockModes.Key,
        conditionID = "1:1"
    });

    public static PlacementList GetPlacements() => new();
}

file static class TileMapHelper {
    //https://github.com/coloursofnoise/FancyTileEntities/blob/dev/FancyTileEntities/Extensions.cs#L158
    public static char[,] GenerateTileMap(string tileMap, int widthTiles, int heightTiles) {
        if (string.IsNullOrWhiteSpace(tileMap)) {
            var tiles = new char[widthTiles, heightTiles];
            tiles.Fill('0');
            return tiles;
        }

        // Backwards compatibility, tileMap strings previously used `,` as the row separator
        char delim = tileMap.Contains(',') ? ',' : '\n';

        string[] tileStrings = tileMap.Split(delim);
        tileStrings = Array.ConvertAll(tileStrings, s => s.Trim());

        int columns = tileStrings.Max(s => s.Length);
        int rows = tileStrings.Length;

        tileStrings = Array.ConvertAll(tileStrings, s => {
            while (s.Length < columns) {
                s += '0';
            }
            return s;
        });

        char[,] tileData = new char[columns, rows];
        for (int x = 0; x < columns; x++) {
            for (int y = 0; y < rows; y++) {
                tileData[x, y] = tileStrings[y][x];
            }
        }

        return tileData.CreateResized(widthTiles, heightTiles, '0');
    }
}