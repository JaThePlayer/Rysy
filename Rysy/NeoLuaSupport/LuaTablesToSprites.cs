using Neo.IronLua;
using Rysy.Graphics;

namespace Rysy.NeoLuaSupport;

public static class LuaTablesToSprites {
    public static List<ISprite> Get(LuaResult res) {
        if (res is not [LuaTable t]) {
            return new();
        }

        if (t["_type"] is { }) {
            return new(1) { Get(t) };
        }

        return t.OfType<LuaTable>().Select(Get).ToList();
    }

    public static ISprite Get(LuaTable t) {
        var type = t.GetOptionalValue<string>("_type", null!);
        var pos = t.ToVector2();

        switch (type) {
            case "drawableSprite":
                //TODO
                return ISprite.FromTexture(pos, GFX.UnknownTexture);
            default:
                Logger.Write("NeoLua.LuaTablesToSprites", LogLevel.Warning, $"Unknown sprite type: {type}");
                return ISprite.FromTexture(pos, GFX.UnknownTexture);
        }
    }
}
