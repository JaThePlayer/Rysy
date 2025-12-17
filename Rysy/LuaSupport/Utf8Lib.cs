using System.Text;
using System.Text.Unicode;

using static LuaNET.LuaJIT.Lua;

namespace Rysy.LuaSupport;

// https://github.com/love2d/love/blob/5670df13b6980afd025cd7e7d442a24499bf86a7/src/libraries/lua53/lutf8lib.c
internal static class Utf8Lib {
    /*
static struct luaL_Reg funcs[] = {
  {"offset", byteoffset},
  {"codepoint", codepoint},
  {"char", utfchar},
  {"len", utflen},
  {"codes", iter_codes},
  /* placeholders * /
  {"charpattern", NULL},
  {NULL, NULL}
};
     */

    public static void Register(Lua lua) {
        lua.Register("__RYSY_utf8_load", CreateLib);
    }

    private static int Len(Lua lua) {
        var str = lua.ToStringIntoAscii(1);
        
        lua.PushInteger(Encoding.UTF8.GetCharCount(str));
        return 1;
    }
    
    static int u_posrelat (int pos, int len) {
        if (pos >= 0) return pos;
        if (0u - pos > len) return 0;
        return len + pos + 1;
    }

    private static bool iscont(byte p) {
        return (p & 0xC0) == 0x80;
    }
        
    private static int Offset(Lua lua) {
        var s = lua.ToStringIntoAscii(1);
        var n = lua.ToInteger(2);

        var len = s.Length;
        var posi = (n >= 0) ? 1 : len + 1;
        posi = u_posrelat((int)LuaNET.LuaJIT.Lua.luaL_optinteger(lua, 3, posi), len);
        LuaNET.LuaJIT.Lua.luaL_argcheck(lua, 1 <= posi && --posi <= len, 3,
            "position out of range");
        if (n == 0) {
            /* find beginning of current byte sequence */
            while (posi > 0 && iscont(s[posi])) posi--;
        }
        else {
            if (iscont(s[posi]))
                LuaNET.LuaJIT.Lua.luaL_error(lua, "initial position is a continuation byte");
            if (n < 0) {
                while (n < 0 && posi > 0) {  /* move back */
                    do {  /* find beginning of previous character */
                        posi--;
                    } while (posi > 0 && iscont(s[posi]));
                    n++;
                }
            }
            else {
                n--;  /* do not move for 1st character */
                while (n > 0 && posi < len) {
                    do {  /* find beginning of next character */
                        posi++;
                    } while (iscont(s[posi]));  /* (cannot pass final '\0') */
                    n--;
                }
            }
        }
        
        if (n == 0)  /* did it find given character? */
            lua.PushInteger(posi + 1);
        else  /* no such character */
            lua.PushNil();
        return 1;
    }
    
    static unsafe char* utf8_decode (char* o, int *val) {
        ReadOnlySpan<int> limits = [0xFF, 0x7F, 0x7FF, 0xFFFF];
        char* s = o;
        int c = s[0];
        int res = 0;  /* final result */
        if (c < 0x80)  /* ascii? */
            res = c;
        else {
            int count = 0;  /* to count number of continuation bytes */
            while ((c & 0x40) != 0) {  /* still have continuation bytes? */
                int cc = s[++count];  /* read next byte */
                if ((cc & 0xC0) != 0x80)  /* not a continuation byte? */
                    return null;  /* invalid byte sequence */
                res = (res << 6) | (cc & 0x3F);  /* add lower 6 bits from cont. byte */
                c <<= 1;  /* to test next bit */
            }
            res |= ((c & 0x7F) << (count * 5));  /* add first byte */
            if (count > 3 || res > 0x10FFFF || res <= limits[count])
                return null;  /* invalid byte sequence */
            s += count;  /* skip continuation bytes read */
        }
        if (val != null) *val = res;
        return s + 1;  /* +1 to include first byte */
    }

    private static int Codepoint(Lua L) {
        ulong len = 0;
        var s = luaL_checklstring(L, 1, ref len) ?? "";
        var posi = u_posrelat((int)luaL_optinteger(L, 2, 1), (int)len);
        var pose = u_posrelat((int)luaL_optinteger(L, 3, posi), (int)len);
        int n;
        luaL_argcheck(L, posi >= 1, 2, "out of range");
        luaL_argcheck(L, pose <= (int)len, 3, "out of range");
        if (posi > pose) return 0;  /* empty interval; return no values */
        n = pose -  posi + 1;
        if (posi + n <= pose)  /* (lua_Integer -> int) overflow? */
            return luaL_error(L, "string slice too long");
        luaL_checkstack(L, n, "string slice too long");
        n = 0;
        unsafe {
            fixed (char* sPtrX = &s.AsSpan()[0]) {
                var sPtr = sPtrX;
                var se = sPtr + pose;
                for (sPtr += posi - 1; sPtr < se;) {
                    int code;
                    sPtr = utf8_decode(sPtr, &code);
                    if (sPtr == null)
                        return luaL_error(L, "invalid UTF-8 code");
                    lua_pushinteger(L, code);
                    n++;
                }
            }
        }
        

        return n;
    }

    private const int UTF8BUFFSZ = 8;
    private const int MAXUNICODE = 0x10FFFF;
    
    /* taken from lobject.c */
    static unsafe int utf8esc (char* buff, ulong x) {
        int n = 1;  /* number of bytes put in buffer (backwards) */
        //lua_assert(x <= 0x10FFFF);
        if (x < 0x80)  /* ascii? */
            buff[UTF8BUFFSZ - 1] = (char) x;
        else {  /* need continuation bytes */
            uint mfb = 0x3f;  /* maximum that fits in first byte */
            do {  /* add continuation bytes */
                buff[UTF8BUFFSZ - (n++)] = (char) (0x80 | (x & 0x3f));
                x >>= 6;  /* remove added bits */
                mfb >>= 1;  /* now there is one less bit available in first byte */
            } while (x > mfb);  /* still needs continuation byte? */
            buff[UTF8BUFFSZ - n] = (char) ((~mfb << 1) | x);  /* add first byte */
        }
        return n;
    }
    
    static unsafe void pushutfchar(Lua L, int arg) {
        var code = luaL_checkinteger(L, arg);
        luaL_argcheck(L, 0 <= code && code <= MAXUNICODE, arg, "value out of range");

        /* the %U string format does not exist in lua 5.1 or 5.2, so we emulate it */
        /* (code from luaO_pushvfstring in lobject.c) */
        char* buff = stackalloc char[UTF8BUFFSZ];
        int l = utf8esc(buff, (ulong)code);
        L.PushBuffer(new ReadOnlySpan<byte>(buff + UTF8BUFFSZ - l, l));
        //lua_pushlstring(L, buff + UTF8BUFFSZ - l, l);
    }
    
    static int utfchar (Lua L) {
        int n = lua_gettop(L);  /* number of arguments */
        if (n == 1)  /* optimize common case of single char */
            pushutfchar(L, 1);
        else {
            int i;
            luaL_Buffer b = default;
            luaL_buffinit(L, b);
            for (i = 1; i <= n; i++) {
                pushutfchar(L, i);
                luaL_addvalue(b);
            }
            luaL_pushresult(b);
        }
        return 1;
    }

    private static int CreateLib(IntPtr s) {
        Lua lua = Lua.FromIntPtr(s);
        lua.CreateTable(0, 5);
        var t = lua.GetTop();
        
        lua.PushCFunction(Len);
        lua.SetField(t, "len");
        
        lua.PushCFunction(Offset);
        lua.SetField(t, "offset");
        
        lua.PushCFunction(Codepoint);
        lua.SetField(t, "codepoint");
        
        lua.PushCFunction(utfchar);
        lua.SetField(t, "char");
        
        return 1;
    }
}