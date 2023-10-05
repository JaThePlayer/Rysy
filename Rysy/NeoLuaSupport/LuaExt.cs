using Neo.IronLua;

namespace Rysy.NeoLuaSupport;
public static class LuaExt {
    public static Vector2 ToVector2(this LuaResult r, Vector2 def = default) {
        if (r.Values is [var ix, var iy, ..]) {
            return new(Convert.ToSingle(ix), Convert.ToSingle(iy));
        }

        if (r.Values is [LuaTable t]) {
            return t.ToVector2();
        }

        return def;
    }

    public static Vector2 ToVector2(this LuaTable t) {
        if (t.ArrayList is [var ix, var iy, ..]) {
            return new(Convert.ToSingle(ix), Convert.ToSingle(iy));
        }

        return new(t.GetOptionalValue("x", 0f), t.GetOptionalValue("y", 0f));
    }

    public static Color ToColor(this LuaResult res, Color def = default) {
        {
            if (res.Values is [var r, var g, var b]) {
                return new(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b));
            }
        }
        {
            if (res.Values is [var r, var g, var b, var a]) {
                return new(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b), Convert.ToSingle(a));
            }
        }

        if (res.Values is [LuaTable t]) {
            return t.ToColor(def);
        }

        return def;
    }

    public static Color ToColor(this LuaTable t, Color def = default) {
        {
            if (t.Values is [var r, var g, var b]) {
                return new(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b));
            }
        }
        {
            if (t.Values is [var r, var g, var b, var a]) {
                return new(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b), Convert.ToSingle(a));
            }
        }

        return def;
    }

    public static LuaTable ToLuaTable<T>(this IEnumerable<T> values) {
        var t = new LuaTable();

        var i = 1;
        foreach (var v in values) {
            t[i++] = v;
        }

        return t;
    }
}
