using SDL3;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rysy.Helpers;

public static partial class Sdl3Ext {
    [LibraryImport("SDL3", EntryPoint = "SDL_GetClipboardText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial byte* SDL_GetClipboardText();
    
    [LibraryImport("SDL3", EntryPoint = "SDL_SetClipboardText")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial int SDL_SetClipboardText(byte* text);
        
    [LibraryImport("SDL3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial void SDL_free(nint memory);
    
    /// <summary>
    /// Gets the SDL clipboard without causing a stack overflow
    /// </summary>
    public static unsafe string GetClipboardFixed() {
        // Seems like this got fixed in sdl3.
        /*
        var utf8NullTerminated = SDL_GetClipboardText();
        var utf8 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(utf8NullTerminated);
        var str = Encoding.UTF8.GetString(utf8);
            
        SDL_free((nint)utf8NullTerminated);
        */
        return SDL.SDL_GetClipboardText();
    }
}