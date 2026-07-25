using SDL3;

namespace Rysy.Extensions {
    public static class FnaMonogameCompat {
        #if FNA
        private static Dictionary<MouseCursor, IntPtr> SdlMouseCursors = new() {
            [MouseCursor.Arrow] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT),
            [MouseCursor.IBeam] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT),
            [MouseCursor.SizeAll] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE),
            [MouseCursor.SizeNs] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE),
            [MouseCursor.SizeWe] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE),
            [MouseCursor.SizeNesw] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE),
            [MouseCursor.SizeNwse] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE),
            [MouseCursor.Hand] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER),
            [MouseCursor.No] = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED),
        };
        #endif
        
        public static void SetMouseCursor(MouseCursor cursor) {
            #if FNA
            if (SdlMouseCursors.TryGetValue(cursor, out var sdlCursor))
                SDL.SDL_SetCursor(sdlCursor);
            #else
            Mouse.SetCursor(cursor);
            #endif
        }
        
        public static bool IsBorderlessShared(this GameWindow window) {
#if FNA
            return window.IsBorderlessEXT;
#else
            return window.IsBorderless;
#endif
        }

        public static Point GetPosition(this GameWindow window) {
#if FNA
            SDL.SDL_GetWindowPosition(window.Handle, out var x, out var y);
            return new(x, y);
#else
            return window.Position;
#endif
        }

        public static void SetPosition(this GameWindow window, Point p) {
#if FNA
            SDL.SDL_SetWindowPosition(window.Handle, p.X, p.Y);
#else
            window.Position = p;
#endif
        }

        public static Texture2D Texture2DFromFile(GraphicsDevice d, string file) {
#if FNA
            using var str = File.OpenRead(file);
            return Texture2D.FromStream(d, str);
#else
            return Texture2D.FromFile(d, file);
#endif
        }
    }
}


#if FNA
namespace Microsoft.Xna.Framework {
    public enum MouseCursor {
        Arrow, IBeam, SizeAll, SizeNs, SizeWe, SizeNesw, SizeNwse, Hand, No
    }

    public static class FnaMonogame {
        public static Vector2 ToVector2(this Point point) => new(point.X, point.Y);
        public static NumVector2 ToNumerics(this Vector2 point) => new(point.X, point.Y);
        public static NumVector3 ToNumerics(this Vector3 point) => new(point.X, point.Y, point.Z);
        public static NumVector4 ToNumerics(this Vector4 point) => new(point.X, point.Y, point.Z, point.W);
        public static Point ToPoint(this Vector2 point) => new((int) point.X, (int) point.Y);

        public static void Deconstruct(this Point point, out int x, out int y) {
            x = point.X;
            y = point.Y;
        }

        public static void Deconstruct(this Vector2 point, out float x, out float y) {
            x = point.X;
            y = point.Y;
        }

        public static bool Contains(this Rectangle r, Vector2 v) => r.Contains(v.ToPoint());
    }
}
#endif
