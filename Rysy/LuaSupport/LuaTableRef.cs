using KeraLua;
using Rysy.Helpers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.LuaSupport;

public sealed class LuaTableRef : LuaRef, IUntypedData {
    internal LuaTableRef(Lua lua, long id) : base(lua, id)
    {
    }
    
    public new static LuaTableRef MakeFrom(Lua lua, int loc) {
        var r = LuaRef.MakeFrom(lua, loc);
        
        return r as LuaTableRef ?? throw new Exception("Tried to create LuaFunctionRef from a non-table value.");
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) {
        PushToStack();
        value = Lua.PeekTableCSharpValue(Lua.GetTop(), key);
        Lua.Pop(1);
        return value is { };
    }
    
    public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value) {
        PushToStack();
        (value, var ret) = Lua.PeekTableCSharpValue(Lua.GetTop(), key) is T t ? (t, true) : (default, false);
        Lua.Pop(1);
        return ret;
    }

    public LuaTableRef? GetMetatable() {
        PushToStack();
        if (Lua.GetMetaTable(Lua.GetTop())) {
            var r = MakeFrom(Lua, Lua.GetTop());
            Lua.Pop(2);
            return r;
        }

        Lua.Pop(1);
        return null;
    }

    public object? this[string key] {
        get {
            PushToStack();
            var value = Lua.PeekTableCSharpValue(Lua.GetTop(), key);
            Lua.Pop(1);
            return value;
        }
        set {
            PushToStack();
            var tableLoc = Lua.GetTop();
            Lua.PushString(key);
            Lua.Push(value);
            Lua.RawSet(tableLoc);
            Lua.Pop(1);
        }
    }
    
    public object? this[long key] {
        get {
            PushToStack();
            var value = Lua.PeekTableCSharpValue(Lua.GetTop(), key);
            Lua.Pop(1);
            return value;
        }
        set {
            PushToStack();
            var tableLoc = Lua.GetTop();
            Lua.Push(value);
            Lua.RawSetInteger(tableLoc, key);
            Lua.Pop(1);
        }
    }

    public IPairsEnumerable IPairs() {
        return new IPairsEnumerable(this);
    }

    public struct IPairsEnumerable(LuaTableRef tbl) : IEnumerable<(int, object?)>, IEnumerator<(int, object?)> {
        private const int NoEnumeratorCreated = -1;
        private const int NotYetStarted = 0;
        private const int Ended = -2;
        
        private int _i = NoEnumeratorCreated;
        private object? _current;

        public IEnumerator<(int, object?)> GetEnumerator() {
            if (_i != NoEnumeratorCreated) {
                return new IPairsEnumerable(tbl).GetEnumerator();
            }

            _i = NotYetStarted;
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public bool MoveNext() {
            var i = _i;
            if (i == Ended)
                return false;

            i++;
            var next = tbl[i];
            if (next is { }) {
                _current = next;
                _i = i;
                return true;
            }

            _i = Ended;
            _current = null;
            return false;
        }

        public void Reset() {
            throw new NotImplementedException();
        }

        (int, object?) IEnumerator<(int, object?)>.Current => (_i, _current);

        object? IEnumerator.Current => _current;

        public void Dispose() {
        }
    }
}