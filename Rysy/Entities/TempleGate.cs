using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Entities;

[CustomEntity("templeGate")]
public class TempleGate : SpriteBankEntity, IPlaceable {
    public override int Depth => -9000;

    public override Vector2 Offset => new(4f, 0f);

    public override string SpriteBankEntry => $"templegate_{Attr("sprite", "default")}";

    public override string Animation => "idle";

    public override Point RecommendedMinimumSize => new(0, 48);

    public override bool ResizableY => false;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.Rect(Pos - new Vector2(2f, 8f), 14, 10, Color.Black);
        yield return GetSprite();
    }

    public static FieldList GetFields() => new(new {
        type = TempleGateModes.CloseBehindPlayer,
        sprite = Fields.SpriteBankPath("default", "^templegate_(.*)$")
    });

    public static PlacementList GetPlacements() => new[] { 
        new { Sprite = "theo", Type = "HoldingTheo" },
        new { Sprite = "default", Type = "CloseBehindPlayer" },
        new { Sprite = "mirror", Type = "CloseBehindPlayer" },
        new { Sprite = "default",Type =  "NearestSwitch" },
        new { Sprite = "mirror", Type = "NearestSwitch" },
        new { Sprite = "default", Type = "TouchSwitches" }
    }
    .Select(preset => new Placement($"{preset.Sprite}_{preset.Type.ToLowerInvariant()}", new {
        sprite = preset.Sprite,
        type = preset.Type,
    }))
    .ToPlacementList();
}
