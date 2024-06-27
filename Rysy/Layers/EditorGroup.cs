using Rysy.Extensions;
using Rysy.Helpers;
using System.Collections;
using System.Diagnostics;

namespace Rysy.Layers; 

/// <summary>
/// Represents a editor group. The same instance should be used in all places that use the same group.
/// </summary>
public class EditorGroup {
    /// <summary>
    /// The name of this group, displayed in-editor and saved to entities.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Whether this group is enabled, and should be rendered at full opacity.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// A list of SID's that should be auto-assigned to this group.
    /// </summary>
    public HashSet<string> AutoAssignTo { get; set; } = new(0);

    /// <summary>
    /// Do not call manually, use a <see cref="EditorGroupRegistry"/> instead.
    /// </summary>
    internal EditorGroup(string name) {
        Name = name;
    }
    
    public override string ToString() => Name;
    
    /// <summary>
    /// The default group, which must always be present in all maps.
    /// </summary>
    public static EditorGroup Default { get; } = new("Default") {
        Enabled = true,
    };
    
    public static HashSet<string> CreateAutoAssignFromString(string list) =>
        list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();
}

public sealed class EditorGroupRegistry : IEnumerable<EditorGroup>, IPackable {
    private readonly List<EditorGroup> _groups = new();

    private readonly Dictionary<string, EditorGroup> _groupInstances = new();
    
    public IReadOnlyList<EditorGroup> Groups => _groups;

    public int Count => _groups.Count;
    
    public EditorGroupRegistry() {
        Add(EditorGroup.Default);
    }

    public EditorGroupRegistry(params EditorGroup[] groups) {
        foreach (var gr in groups) {
            Add(gr);
        }
    }

    public EditorGroup this[int index] => _groups[index];

    public EditorGroup GetOrCreate(string name, int index = -1) => GetOrCreate(name, out _, index);
    public EditorGroup GetOrCreate(string name, out bool created, int index = -1) {
        if (_groupInstances.TryGetValue(name, out var existing)) {
            created = false;
            return existing;
        }

        if (name.IsNullOrWhitespace()) {
            created = false;
            return EditorGroup.Default;
        }

        var newGroup = _groupInstances[name] = new(name);
        if (index == -1)
            _groups.Add(newGroup);
        else {
            _groups.Insert(index, newGroup);
        }

        created = true;
        return newGroup;
    }

    public int Remove(EditorGroup group) {
        _groupInstances.Remove(group.Name);

        var index = _groups.IndexOf(group);
        _groups.Remove(group);

        return index;
    }
    
    public void Add(EditorGroup group) {
        Insert(-1, group);
    }

    public void Insert(int index, EditorGroup group) {
        if (_groupInstances.TryGetValue(group.Name, out _))
            return;
        
        if (index == -1)
            _groups.Add(group);
        else {
            _groups.Insert(index, group);
        }

        _groupInstances[group.Name] = group;
    }
    
    public EditorGroupList CloneWithOnlyEnabled() {
        var newList = new EditorGroupList();

        foreach (var group in this) {
            if (group.Enabled)
                newList.Add(group);
        }
        
        return newList;
    }
    
    public EditorGroupList CloneWithOnlyEnabledAndAutoAssigned(string sid) {
        var auto = GetAutoAssigned(sid);
        foreach (var group in this) {
            if (group.Enabled)
                auto.Add(group);
        }

        var newList = new EditorGroupList();
        foreach (var group in auto) {
            newList.AddIfUnique(group);
        }
        
        return newList;
    }
    
    public HashSet<EditorGroup> GetAutoAssigned(string sid) {
        var list = new HashSet<EditorGroup>(0);

        foreach (var gr in this) {
            if (gr.AutoAssignTo.Contains(sid)) {
                list.Add(gr);
            }
        }
        
        return list;
    }

    public List<EditorGroup>.Enumerator GetEnumerator() => _groups.GetEnumerator();
    
    IEnumerator<EditorGroup> IEnumerable<EditorGroup>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Swap(EditorGroup a, EditorGroup b) {
        var aIndex = _groups.IndexOf(a);
        var bIndex = _groups.IndexOf(b);
        if (aIndex == -1 || bIndex == -1)
            return;

        _groups[aIndex] = b;
        _groups[bIndex] = a;
    }

    public const string BinaryPackerName = "_groups";

    public BinaryPacker.Element Pack() {
        var el = new BinaryPacker.Element(BinaryPackerName) {
            Children = new BinaryPacker.Element[_groups.Count],
            Attributes = new(),
        };

        for (int i = 0; i < _groups.Count; i++) {
            var gr = _groups[i];

            el.Children[i] = new BinaryPacker.Element(gr.Name) {
                Attributes = new() {
                    ["autoAssign"] = string.Join(",", gr.AutoAssignTo),
                    ["editorVisible"] = gr.Enabled,
                }
            };
        }

        return el;
    }

    public void Unpack(BinaryPacker.Element from) {
        foreach (var child in from.Children) {
            var gr = GetOrCreate(child.Name ?? throw new Exception($"EditorGroup is missing Name in the .bin: {child.ToJson()}"));
            Add(gr);
            gr.AutoAssignTo = EditorGroup.CreateAutoAssignFromString(child.Attr("autoAssign", ""));
            gr.Enabled = child.Bool("editorVisible");
        }
    }
}

public sealed class EditorGroupList : ListenableList<EditorGroup> {
    public bool Enabled {
        get {
            foreach (var group in this) {
                if (group.Enabled)
                    return true;
            }

            return Count == 0;
        }
    }
    
    public bool IsOnlyDefault {
        get {
            foreach (var group in this) {
                if (group.AutoAssignTo.Count != 0)
                    continue;
                if (group == EditorGroup.Default)
                    continue;
                
                return false;
            }

            return true;
        }
    }

    public EditorGroupList CloneWithOnlyEnabled() {
        var newList = new EditorGroupList();

        foreach (var group in this) {
            if (group.Enabled)
                newList.Add(group);
        }
        
        return newList;
    }

    public override string ToString() => string.Join(",", this);
    
    public void AddIfUnique(EditorGroup group) {
        if (!Contains(group))
            Add(group);
    }
    
    public void AddIfUnique(EditorGroupList groups) {
        foreach (var gr in groups) {
            AddIfUnique(gr);
        }
    }
    
    public static EditorGroupList FromString(EditorGroupRegistry registry, ReadOnlySpan<char> str) {
        var list = new EditorGroupList();

        str = str.Trim();
        if (str.Length == 0)
            return list;
        
        foreach (var groupName in str.EnumerateSplits(',')) {
            list.Add(registry.GetOrCreate(groupName.ToString()));
        }

        return list;
    }
}