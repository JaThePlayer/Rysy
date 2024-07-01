using Rysy.Extensions;
using Rysy.Layers;

namespace Rysy.Gui.FieldTypes; 

public record EditorGroupListField : ListField, IFieldConvertible<EditorGroupList> {
    private readonly EditorGroupRegistry _registry;
    
    public EditorGroupListField(EditorGroupRegistry registry, EditorGroupList? @default) 
        : base(new EditorGroupField(registry), @default?.ToString() ?? "") {
        _registry = registry;
        AllowEdits = false;
        MinElements = 0;
        ElementCanBeRemoved = CustomElementCanBeRemoved;
    }

    private bool CustomElementCanBeRemoved(string str) => !_registry.GetOrCreate(str).IsAutoAssigned;

    public EditorGroupList ConvertMapDataValue(object value) {
        return EditorGroupList.FromString(_registry, value.ToString());
    }
}

public record EditorGroupField : DropdownField<EditorGroup> {
    private readonly EditorGroupRegistry _registry;
    
    public EditorGroupField(EditorGroupRegistry reg) {
        StringToT = CustomStringToT;
        _registry = reg;
        Values = _ => EditorState.Map?.EditorGroups
            .Where(gr => gr.AutoAssignTo.Count == 0 && gr != EditorGroup.Default)
            .ToDictionary(gr => gr, gr => gr.Name) ?? new();
        
        //EditorState.Map?.EditorGroups.LogAsJson();
    }

    public override bool IsValid(object? value) {
        return true;
    }

    private EditorGroup CustomStringToT(string? str) => str is null ? EditorGroup.Default : _registry.GetOrCreate(str);
}