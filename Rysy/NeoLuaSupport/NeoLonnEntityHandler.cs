using Neo.IronLua;
using Rysy.Graphics;
using Rysy.Mods;
using System.Dynamic;

namespace Rysy.NeoLuaSupport;

public sealed class NeoRoomWrapper : DynamicObject {
    public Room Room { get; private set; }

    public NeoRoomWrapper(Room room) {
        Room = room;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result) {
        var name = binder.Name;

        result = name switch {
            "x" => Room.X,
            "y" => Room.Y,
            "width" => Room.Width,
            "height" => Room.Height,
            "name" => Room.Name,
            _ => null,
        };

        return true;
    }
}

public sealed class NeoLonnEntityHandler {
    public ModMeta? Mod;

    public readonly string Name;

    public Func<NeoRoomWrapper, NeoEntityWrapper, string?>? GetTexture;
    public Func<NeoRoomWrapper, NeoEntityWrapper, float> GetRotation;
    public Func<NeoRoomWrapper, NeoEntityWrapper, Vector2> GetOrigin;
    public Func<NeoRoomWrapper, NeoEntityWrapper, Vector2> GetScale;
    public Func<NeoRoomWrapper, NeoEntityWrapper, Color> GetColor;

    public Func<NeoRoomWrapper, NeoEntityWrapper, List<ISprite>>? GetSprites;

    public LuaTable LuaTable;

    private T Passthru<T>(string key, object? obj, T def) {
        if (obj is not null)
            (key, obj.GetType().Name).LogAsJson();

        return def;
    }

    public NeoLonnEntityHandler(LuaTable table) {
        LuaTable = table;

        Name = table["name"].ToString() ?? throw new Exception("Entity handler has no name!");

        GetTexture = LoadFromPlugin("texture", null, (r) => r.ToString(CultureInfo.InvariantCulture), returnNullIfMissing: true);
        GetRotation = LoadFromPlugin("rotation", 0f, (r) => r.ToSingle(CultureInfo.InvariantCulture));
        GetOrigin = LoadFromPlugin("justification", Vector2.One / 2f, (r) => r.ToVector2(Vector2.One / 2f));
        GetScale = LoadFromPlugin("scale", Vector2.One, (r) => r.ToVector2(Vector2.One));
        GetColor = LoadFromPlugin("color", Color.White, (r) => r.ToColor(Color.White));

        GetSprites = LoadFromPlugin("sprite", new List<ISprite>(), (r) => LuaTablesToSprites.Get(r), returnNullIfMissing: true);
    }

    private Func<NeoRoomWrapper, NeoEntityWrapper, T> LoadFromPlugin<T>(string key, T? def, Func<LuaResult, T> converter, bool returnNullIfMissing = false) {
        return LuaTable[key] switch {
            T t => (r, e) => t,
            Func<object, object, LuaResult> f => (r, e) => converter(f(r, e)),
            LuaTable t => (r, e) => converter(new(t)),
            var other => Passthru<Func<NeoRoomWrapper, NeoEntityWrapper, T>>(key, other, returnNullIfMissing ? null! : (r, e) => def!),
        };
    }

    public static NeoLonnEntityHandler Default(string sid) {
        return FromLua($$"""
            return {
                name = {{sid}},
            }

            """, $"defaultHandler.{sid}")[0];
    }

    public static List<NeoLonnEntityHandler> FromLua(string lua, string chunkName) {
        var handlers = new List<NeoLonnEntityHandler>();
        LuaResult? res;
        try {
            res = LuaLoader.DoString(lua, chunkName);
        } catch (Exception e) {
            //Console.WriteLine($"failed {chunkName}");
            //return handlers;
            throw;
        }

        // (chunkName, res).LogAsJson();
        if (res is not [LuaTable ret]) {
            Logger.Write("NeoLua.Entity", LogLevel.Warning, $"Plugin returned multiple or no values: {chunkName}");
            return handlers;
        }

        if (ret["name"] is { }) {
            handlers.Add(new(ret));
        } else {
            foreach (var innerHandler in ret.OfType<LuaTable>()) {
                handlers.Add(new(innerHandler));
            }
        }

        return handlers;
    }
}
