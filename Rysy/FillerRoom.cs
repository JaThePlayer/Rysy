using Rysy.Helpers;
using Rysy.LuaSupport;

namespace Rysy;

public sealed class FillerRoom : IPackable, ILuaWrapper {
    private BinaryPacker.Element _element;

    public int X => _element.Int("x") * 8;
    public int Y => _element.Int("y") * 8;
    public int Width => _element.Int("w") * 8;
    public int Height => _element.Int("h") * 8;

    public Rectangle Bounds => new Rectangle(X, Y, Width, Height);
    
    public BinaryPacker.Element Pack() {
        return _element;
    }

    public void Unpack(BinaryPacker.Element from) {
        _element = from;
    }

    public int LuaIndex(Lua lua, long key) {
        lua.PushNil();
        return 1;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "x":
                lua.PushInteger(X);
                return 1;
            case "y":
                lua.PushInteger(Y);
                return 1;
            case "width":
                lua.PushInteger(Width);
                return 1;
            case "height":
                lua.PushInteger(Height);
                return 1;
        }

        lua.PushNil();
        return 1;
    }
}
