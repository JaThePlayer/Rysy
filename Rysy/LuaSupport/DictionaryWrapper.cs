using KeraLua;
using Rysy.Helpers;

namespace Rysy.LuaSupport;

public interface ILuaDictionaryWrapper {
    public Dictionary<string, object> Dictionary { get; }
}

public class DictionaryWrapper(Dictionary<string, object> dict) : ILuaWrapper, ILuaDictionaryWrapper {
    private readonly Dictionary<string, object>.AlternateLookup<ReadOnlySpan<char>> _alternateLookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

    public Dictionary<string, object> Dictionary => dict;
    
    public bool MutatedByLua { get; private set; }
    
    public int LuaIndex(Lua lua, long key) {
        return 0;
    }

    public virtual int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        if (_alternateLookup.TryGetValue(key, out var result)) {
            lua.Push(result);
            return 1;
        }

        return 0;
    }

    public virtual void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) {
        MutatedByLua = true;
        _prevNextEnumerator = null;
        _alternateLookup[key] = value;
    }
    
    private (Dictionary<string, object>.Enumerator enumerator, string key)? _prevNextEnumerator;
    
    public int LuaNext(Lua lua, object? key = null) {
        if (key is null && Dictionary.Count > 0) {
            var (k, v) = Dictionary.First();
            lua.Push(k);
            lua.Push(v);
            return 2;
        }

        if (key is not string s) {
            goto keyDoesNotExist;
        }
        
        // If we're resuming the enumeration 
        if (_prevNextEnumerator is { } prev && prev.key == s) {
            var innerEnum = prev.enumerator;

            try {
                if (innerEnum.MoveNext()) {
                    var (nextKey, nextValue) = innerEnum.Current;
                    _prevNextEnumerator = (innerEnum, nextKey);
                
                    lua.PushString(nextKey);
                    lua.Push(nextValue);
                    return 2;
                } else {
                    goto keyDoesNotExist;
                }
            } catch {
                // failed to resume the enumerator due to mutating the dictionary in between calls, we'll fall back to the slow path
            }
        }
        
        var enumerator = Dictionary.GetEnumerator();
        while (enumerator.MoveNext()) {
            var (k, _) = enumerator.Current;
            if (k != s) continue;
            
            if (enumerator.MoveNext()) {
                var (nextKey, nextValue) = enumerator.Current;
                _prevNextEnumerator = (enumerator, nextKey);
                lua.PushString(nextKey);
                lua.Push(nextValue);
                return 2;
            }

            break;
        }

        keyDoesNotExist:
        _prevNextEnumerator = null;
        lua.PushNil();
        lua.PushNil();
        return 2;
    }
}

/// <summary>
/// Special wrapper used by fakeTilesHelper.getTilesOptions, receives special handling, allowing for creating of the proper tileset dropdown.
/// </summary>
/// <param name="layer"></param>
public class LuaTilesetsDictionaryWrapper(TileLayer layer) : DictionaryWrapper(GenerateDict(layer)) {
    public TileLayer TileLayer => layer;
    
    static Dictionary<string, object> GenerateDict(TileLayer layer) {
        if (EditorState.Map is not {} map) {
            return [];
        }

        var autotiler = layer == TileLayer.Bg ? map.BgAutotiler : map.FgAutotiler;
        var tiles = autotiler.Tilesets.Select(t => (t.Key, autotiler.GetTilesetDisplayName(t.Key)));
        
        return tiles.ToDictionary(x => x.Item2, x => (object) x.Key.ToString());
    }

    public Field CreateField(char def) {
        return Fields.TileDropdown(def, TileLayer == TileLayer.Bg);
    }
}
