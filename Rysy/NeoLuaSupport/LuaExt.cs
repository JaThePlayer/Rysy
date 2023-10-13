using Neo.IronLua;

namespace Rysy.NeoLuaSupport;
public static class LuaExt {
    public static Vector2 ToVector2(this LuaResult r, Vector2 def = default) {
        if (r.Values is [var ix, var iy, ..]) {
            return new(Convert.ToSingle(ix, CultureInfo.InvariantCulture), Convert.ToSingle(iy, CultureInfo.InvariantCulture));
        }

        if (r.Values is [LuaTable t]) {
            return t.ToVector2();
        }

        return def;
    }

    public static Vector2 ToVector2(this LuaTable t) {
        if (t.ArrayList is [var ix, var iy, ..]) {
            return new(Convert.ToSingle(ix, CultureInfo.InvariantCulture), Convert.ToSingle(iy, CultureInfo.InvariantCulture));
        }

        return new(t.GetOptionalValue("x", 0f), t.GetOptionalValue("y", 0f));
    }

    public static Color ToColor(this LuaResult res, Color def = default) {
        {
            if (res.Values is [var r, var g, var b]) {
                return new(
                    Convert.ToSingle(r, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(g, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(b, CultureInfo.InvariantCulture));
            }
        }
        {
            if (res.Values is [var r, var g, var b, var a]) {
                return new(
                    Convert.ToSingle(r, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(g, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(b, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(a, CultureInfo.InvariantCulture));
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
                return new(
                    Convert.ToSingle(r, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(g, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(b, CultureInfo.InvariantCulture));
            }
        }
        {
            if (t.Values is [var r, var g, var b, var a]) {
                return new(
                    Convert.ToSingle(r, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(g, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(b, CultureInfo.InvariantCulture), 
                    Convert.ToSingle(a, CultureInfo.InvariantCulture));
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
