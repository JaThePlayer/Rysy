using ImGuiNET;
using KeraLua;
using Rysy.Gui;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.LuaSupport;

public record LuaField : Field, ILuaWrapper, IFieldConvertible {
    private readonly LuaTableRef _table;
    private object _default;

    private List<LuaTableRef> _elements = [];

    private LuaFunctionRef? _getValue;
    private LuaFunctionRef? _setValue;
    
    private LuaFunctionRef? _fieldWarning;
    private LuaFunctionRef? _fieldValid;

    private readonly LuaDelayedString _fieldNameStr;
    private readonly LuaTooltip _luaTooltip;

    private readonly string _fieldType;
    private Dictionary<string, object> _fieldInfoEntry;
    private readonly string _fieldName;

    public LuaField(LuaTableRef table, object initial, LuaDelayedString fieldNameStr, LuaTooltip luaTooltip, string fieldType, Dictionary<string, object> fieldInfoEntry) {
        _table = table;
        _default = initial;
        _fieldNameStr = fieldNameStr;
        _luaTooltip = luaTooltip;
        _fieldType = fieldType;
        _fieldInfoEntry = fieldInfoEntry;
        _fieldName = fieldNameStr.Value;

        var lua = table.Lua;
        if (_table.TryGetValue("elements", out var elementsObj) && elementsObj is LuaTableRef elements) {
            elements.PushToStack();
            _elements = lua.ToList<LuaTableRef>(lua.GetTop(), makeLuaTableRefs: true) ?? [];
            if (_elements is [var label, var field, .. var rest]) {
                _elements = [field, label, .. rest];
            }
        }

        _getValue = _table["getValue"] as LuaFunctionRef;
        _setValue = _table["setValue"] as LuaFunctionRef;
        _fieldWarning = _table["fieldWarning"] as LuaFunctionRef;
        _fieldValid = _table["fieldValid"] as LuaFunctionRef;

        _table["__rysy_cs"] = this;
        _table["notifyFieldChanged"] = (LuaFunction)FormFieldNotifyFieldChanged;
    }

    private bool _fieldChangedThisFrame;

    private static int FormFieldNotifyFieldChanged(nint state) {
        var lua = Lua.FromIntPtr(state);
        var self = LuaTableRef.MakeFrom(lua, 1);
        
        if (self["__rysy_cs"] is LuaField luaField) {
            luaField._fieldChangedThisFrame = true;
        }

        return 0;
    }
    
    public override object GetDefault() => _default;

    public override void SetDefault(object newDefault) {
        _default = newDefault;
        _setValue?.InvokeVoid(_table, newDefault);
        _fieldChangedThisFrame = false;
    }

    public override ValidationResult IsValid(object? value) {
        var valid = (_fieldValid?.Invoke(_table)).CoerceToBool();
        if (!valid)
            return ValidationResult.GenericError;

        return base.IsValid(value);
    }
    
    public override object? RenderGui(string fieldName, object value) {
        _fieldNameStr.Value = fieldName;
        _luaTooltip.Tooltip = Tooltip;
        
        var i = 0;
        foreach (var elRef in _elements) {
            if (i > 0)
                ImGui.SameLine();
            i++;
            RenderElement(elRef);
        }

        if (_fieldChangedThisFrame) {
            var newValue = _getValue?.Invoke(_table);
            Console.WriteLine($"-> Changed field: {newValue}");
            _fieldChangedThisFrame = false;
            return newValue;
        }
        
        return null;
    }

    private static void RenderElement(LuaTableRef elRef) {
        var type = elRef.Attr("__type", "__unk__");
        switch (type) {
            case "label":
                RenderLabel(elRef);
                break;
            case "button":
                RenderButton(elRef);
                break;
            case "field":
                RenderField(elRef);
                break;
            case "row":
                RenderRow(elRef);
                break;
            case "icon":
                RenderIcon(elRef);
                break;
            default:
                ImGui.Text($"- {type}");
                break;
        }
    }

    private static void SetupElPos(LuaTableRef tbl) {
        var w = tbl.Float("width");
        ImGui.SetNextItemWidth(w);
    }

    private static Tooltip CreateTooltip(LuaTableRef tbl) {
        return tbl["tooltipText"] switch {
            string tooltip => new Tooltip(tooltip),
            ITooltip tooltip => new Tooltip(tooltip),
            _ => default
        };
    }

    private static void AddTooltip(LuaTableRef tbl) {
        if (!ImGui.IsItemHovered())
            return;
        
        switch (tbl["tooltipText"]) {
            case string tooltip:
                if (!string.IsNullOrWhiteSpace(tooltip) && ImGui.BeginTooltip()) {
                    ImGui.Text(tooltip);
                    ImGui.EndTooltip();
                }

                break;
            case ITooltip tooltip:
                if (ImGui.BeginTooltip()) {
                    tooltip.RenderImGui();
                    ImGui.EndTooltip();
                }
                break;
        }
    }
    
    private static void RenderLabel(LuaTableRef tbl) {
        var text = tbl.Attr("text", "ERROR");
        ImGui.Text(text);
        AddTooltip(tbl);
    }
    
    private static void RenderButton(LuaTableRef tbl) {
        var text = tbl.Attr("text", "ERROR");
        SetupElPos(tbl);
        var clicked = ImGui.Button(text);
        AddTooltip(tbl);
        if (clicked) {
            if (tbl.Obj<LuaFunctionRef>("cb") is { } cb) {
                cb.InvokeVoid(tbl);
            }
        }
    }

    private static void RenderIcon(LuaTableRef tbl) {
        var image = tbl["image"];
        var imagePath = image switch {
            LuaTableRef t => t.Attr("_filename"),
            _ => image?.ToString() ?? "null",
        };
        //var image = tbl.Attr("image", "ERROR");
        SetupElPos(tbl);
        var clicked = ImGui.Button(imagePath);
        AddTooltip(tbl);
        if (clicked) {
            if (tbl.Obj<LuaFunctionRef>("cb") is { } cb) {
                cb.InvokeVoid(tbl);
            }
        }
    }
    
    private static void RenderRow(LuaTableRef tbl, int exceptIndex = -1) {
        if (tbl.TryGetValue<LuaTableRef>("children", out var children)) {
            var i = 0;
            foreach (var (childId, child) in children.IPairs()) {
                if (childId == exceptIndex || child is not LuaTableRef childTable)
                    continue;
                
                if (i > 0)
                    ImGui.SameLine();
                i++;
                
                RenderElement(childTable);
            }
        }
    }
    
    private static void RenderField(LuaTableRef tbl) {
        var text = tbl.Attr("text", "ERROR");
        SetupElPos(tbl);
        
        var startCursorPosX = ImGui.GetCursorPosX();
        var ignoreIcon = false;
        if (tbl.TryGetValue<LuaTableRef>("children", out var children)) {
            var i = 0;
            foreach (var (childId, child) in children.IPairs()) {
                if (child is not LuaTableRef childTable)
                    continue;
                
                if (childId == 2 && ignoreIcon)
                    continue;
                
                if (i > 0)
                    ImGui.SameLine();
                i++;
                
                if (childId == 1) {
                    if (tbl.TryGetValue("_backingDropdown", out LuaTableRef? backingDropdown)
                        && backingDropdown.TryGetValue("_RYSY_listItems", out LuaTableRef? backingDropdownListOptions)) {
                        ignoreIcon = true;

                        var optionsRefs = backingDropdownListOptions
                            .IPairs()
                            .SelectWhereNotNull(x => x.Item2 as LuaTableRef)
                            .ToDictionary(x => x["data"]!, x => x.Attr("text"));
                        
                        object? value = text;
                        string search = "";
                        if (ImGuiManager.EditableCombo("", ref value, optionsRefs,
                                str => optionsRefs.GetValueOrDefault(str, str), ref search, tooltip: CreateTooltip(tbl))) {
                            text = value?.ToString() ?? "";
                            if (tbl.Obj<LuaFunctionRef>("setText") is { } setText) {
                                setText.InvokeVoid(tbl, text);
                            }
                        }
                    } else {
                        if (ImGui.InputText($"##{tbl.GetHashCode()}", ref text, 512, ImGuiInputTextFlags.None)) {
                            if (tbl.Obj<LuaFunctionRef>("setText") is { } setText) {
                                setText.InvokeVoid(tbl, text);
                            }
                        }
                        AddTooltip(tbl);
                    }
                    continue;
                }
                
                //ImGui.SetCursorPosX(startCursorPosX + tbl.Float("x"));
                RenderElement(childTable);
            }
        }
    }

    public override Field CreateClone()
        //=> new LuaField(_table, _default, _fieldNameStr);
        => Fields.CreateFromLonn(_default, _fieldType, _fieldInfoEntry, _fieldName)!;
    
    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        throw new NotImplementedException();
    }

    public T ConvertMapDataValue<T>(object value) {
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }
}