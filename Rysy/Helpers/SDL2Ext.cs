using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Rysy.Helpers;

public static partial class SDL2Ext {
    [LibraryImport("SDL2", EntryPoint = "SDL_GetClipboardText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial byte* SDL_GetClipboardText();
    
    [LibraryImport("SDL2", EntryPoint = "SDL_SetClipboardText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial int SDL_SetClipboardText(byte* text);
        
    [LibraryImport("SDL2")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SDL_free(nint memory);
    
    /// <summary>
    /// Gets the SDL clipboard without causing a stack overflow
    /// </summary>
    public static unsafe string GetClipboardFixed() {
        var utf8NullTerminated = SDL_GetClipboardText();
        var utf8 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(utf8NullTerminated);
        var str = Encoding.UTF8.GetString(utf8);
            
        SDL_free((nint)utf8NullTerminated);

        return str;
    }
}