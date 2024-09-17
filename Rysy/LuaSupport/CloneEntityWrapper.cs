using KeraLua;

namespace Rysy.LuaSupport;

// A wrapper over an entity that allows lua to mutate it, including the _name field, by storing all changes done to it into a temporary dictionary.
public class CloneEntityWrapper(Entity entity) : ILuaWrapper {
    public Dictionary<string, object> Changes { get; } = new(StringComparer.Ordinal);
    public string? NewSid { get; private set; }

    private NodesWrapper? _nodes;

    public Entity Entity => entity;

    public bool IsChanged { get; private set; }

    public int LuaIndex(Lua lua, long key) {
        return Entity.LuaIndex(lua, key);
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        if (NewSid is { } newSid && key is "_name") {
            lua.PushString(newSid);
            return 1;
        }

        if (key is "nodes") {
            lua.PushWrapper(_nodes ??= new NodesWrapper(this));
            return 1;
        }

        var changes = Changes;
        if (changes.Count > 0 && changes.TryGetValue(key.ToString(), out var changedVal)) {
            lua.Push(changedVal);
            return 1;
        }

        return Entity.LuaIndex(lua, key);
    }

    public void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) {
        IsChanged = true;
        switch (key) {
            case "_name":
                NewSid = value.ToString();
                break;
            default:
                Changes[key.ToString()] = value;
                break;
        }
    }

    public Entity? CreateMutatedCloneIfChanged() => IsChanged ? CreateMutatedClone() : null;
    
    public Entity CreateMutatedClone() => Entity.CloneWith(pl => {
        pl.SID = NewSid ?? Entity.Name;

        foreach (var (k, v) in Changes) {
            pl[k] = v;
        }

        if (_nodes is { Wrappers: {} nodeWrappers }) {
            pl.Nodes ??= new Vector2[nodeWrappers.Length];
            for (int i = 0; i < nodeWrappers.Length; i++) {
                if (nodeWrappers[i] is { X: {} x}) {
                    pl.Nodes[i].X = x;
                }
                if (nodeWrappers[i] is { Y: {} y}) {
                    pl.Nodes[i].Y = y;
                }
            }
        }
    });
    
    private sealed class NodesWrapper(CloneEntityWrapper entityWrapper) : ILuaWrapper {
        internal NodeWrapper?[]? Wrappers;
        
        public int LuaIndex(Lua lua, long key) {
            var i = (int)key - 1;
            var e = entityWrapper.Entity;

            if (i >= e.Nodes.Count) {
                lua.PushNil();
                return 1;
            }

            Wrappers ??= new NodeWrapper[e.Nodes.Count];

            var node = e.Nodes[i];
            lua.PushWrapper(Wrappers[i] ??= new NodeWrapper(entityWrapper, node));
            return 1;
        }

        public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
            lua.PushNil();
            return 1;
        }

        internal sealed class NodeWrapper(CloneEntityWrapper wrapper, Node node) : ILuaWrapper {
            public float? X, Y;
            
            public int LuaIndex(Lua lua, long key) {
                lua.PushNil();
                return 1;
            }

            public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
                switch (key) {
                    case "x":
                        lua.PushNumber(X ?? node.X);
                        return 1;
                    case "y":
                        lua.PushNumber(Y ?? node.Y);
                        return 1;
                }
                lua.PushNil();
                return 1;
            }

            public void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) {
                switch (key) {
                    case "x":
                        X = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                        wrapper.IsChanged = true;
                        return;
                    case "y":
                        Y = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                        wrapper.IsChanged = true;
                        return;
                }
            }
        }
    }
}
