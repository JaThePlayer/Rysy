using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Lua(nint handle) {
    public readonly nint Handle = handle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct LuaDebug
{
    [InlineArray(LuaImports.LUA_IDSIZE)]
    public struct ShortSrcArray {
        private sbyte _first;
    }
    
    public int _event;
    public unsafe byte* name;
    public unsafe byte* namewhat;
    public unsafe byte* what;
    public unsafe byte* source;
    public int currentline;
    public int nups;
    public int linedefined;
    public int lastlinedefined;
    public ShortSrcArray short_src;
    public int i_ci;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct LuaLReg
{
    private unsafe byte* name;
    private unsafe delegate* unmanaged[Stdcall]<int, Lua> func;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct LuaLBuffer
{
    [InlineArray(LuaImports.LUAL_BUFFERSIZE)]
    public struct InnerBuffer {
        private byte _first;
    }
    public unsafe byte* p;
    public int lvl;
    public Lua L;
    public InnerBuffer buffer;
}

public delegate IntPtr LuaAlloc(IntPtr ud, IntPtr ptr, ulong osize, ulong nsize);

public delegate int LuaFunction(Lua l);

public delegate void LuaHook(Lua l, LuaDebug ar);

public unsafe delegate void LuaJitProfileCallback(void* data, Lua L, int samples, int vmstate);

public delegate T LuaLFunction<T>(Lua l, int n);

public unsafe delegate nint LuaReader(Lua l, void* ud, ref long sz);

public unsafe delegate int LuaWriter(Lua l, void* p, long sz, void* ud);