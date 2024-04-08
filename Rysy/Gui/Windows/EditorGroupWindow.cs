using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;

namespace Rysy.Gui.Windows; 

public sealed class EditorGroupWindow : Window {
    public EditorGroupWindow() : base("rysy.editor_group_window".Translate(), null) {
        NoSaveData = false;
        Closeable = false;
    }

    internal void Select(Map map, EditorGroup group, Input? input) {
        if (input?.Keyboard.Shift() ?? false) {
            if (input.Mouse.LeftDoubleClicked()) {
                EnableAll(map, true);
            }
            
            group.Enabled = true;
        } else if (input?.Keyboard.Ctrl() ?? false) {
            if (input.Mouse.LeftDoubleClicked()) {
                EnableAll(map, false);
            }
            
            group.Enabled = false;
        } else {
            EnableAll(map, input?.Mouse.LeftDoubleClicked() ?? false);

            group.Enabled = true;
        }
        
        map.GroupsChanged();

        static void EnableAll(Map map, bool enabled) {
            foreach (var gr in map.EditorGroups) {
                gr.Enabled = enabled;
            }
        }
    }

    protected override void Render() {
        base.Render();

        if (!ImGui.BeginListBox("", new(ImGui.GetWindowWidth() - 10, ImGui.GetWindowHeight() - 10)))
            return;
        
        if (EditorState.Map is not { } map)
            return;
        
        var groups = map.EditorGroups;
        for (int i = 0; i < groups.Count; i++) {
            var g = groups[i];

            if (ImGui.Selectable(g.Name, g.Enabled)) {
                Select(map, g, Input.Global);
            }
            
            if (ImGuiManager.IndexDragDrop("group", ref i) is { } droppedIndex) {
                var droppedGroup = groups[droppedIndex];

                RysyEngine.OnEndOfThisFrame += () => {
                    groups.Swap(droppedGroup, g);
                };
            }

            if (g == EditorGroup.Default)
                continue;
            
            var id = Interpolator.Temp($"group_{g.Name}");
            
            ImGui.OpenPopupOnItemClick(id, ImGuiPopupFlags.MouseButtonRight);

            if (ImGui.BeginPopupContextWindow(id, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
                if (ImGuiManager.TranslatedButton("rysy.edit")) {
                    RysyEngine.Scene.AddWindow(new GroupEditWindow(this, map, g));
                }
                
                if (ImGuiManager.TranslatedButton("rysy.delete")) {
                    RysyEngine.OnEndOfThisFrame += () => EditorState.History?.ApplyNewAction(new RemoveEditorGroupAction(map, g));
                }

                ImGui.EndPopup();
            }

        }
        
        ImGuiManager.PushNullStyle();
        if (ImGuiManager.TranslatedButton("rysy.new")) {
            RysyEngine.Scene.AddWindow(new GroupEditWindow(this, map, group: null));
        }
        ImGuiManager.PopNullStyle();
        
        ImGui.EndListBox();
    }
}

internal sealed class GroupEditWindow : Window {
    private bool _valid;
    private string _newGroupName;
    private string _autoAssignString;
    private bool _autoAssignChanged;
    
    private readonly string _nameLabel = "rysy.name".Translate();
    private readonly Map _map;
    private readonly EditorGroupWindow _parent;
    private readonly Field _autoAssignToField;
    private readonly EditorGroup? _sourceGroup;

    private const string NewName = "rysy.editor_group_window.edit_group.new";
    private const string EditName = "rysy.editor_group_window.edit_group.edit";
    
    public GroupEditWindow(EditorGroupWindow parent, Map map, EditorGroup? group) 
        : base((group is {} ? EditName : NewName).Translate(), 
            new(FormWindow.ITEM_WIDTH * 1.75f, ImGui.GetTextLineHeightWithSpacing() * 8f)) {
        _map = map;
        _newGroupName = group?.Name ?? "";
        _parent = parent;
        _sourceGroup = group;
        _autoAssignString = group is { } ? string.Join(",", group.AutoAssignTo) : "";
        _autoAssignToField = Fields.List(_autoAssignString, Fields.Sid("", RegisteredEntityType.Entity | RegisteredEntityType.Trigger)).WithMinElements(0);
        
        UpdateValid();
    }

    private void UpdateValid() {
        _valid = !_newGroupName.IsNullOrWhitespace() && _map.EditorGroups.All(gr => gr == _sourceGroup || gr.Name != _newGroupName);
    }

    protected override void Render() {
        base.Render();
        
        UpdateValid();
        
        ImGuiManager.PushInvalidStyleIf(!_valid);
        if (ImGui.InputText(_nameLabel, ref _newGroupName, 64)) {
        }
        ImGuiManager.PopInvalidStyle();

        if (_autoAssignChanged)
            ImGuiManager.PushEditedStyle();
        if (_autoAssignToField.RenderGui("Auto Assign", _autoAssignString) is string newAutoAssign) {
            _autoAssignString = newAutoAssign;
            _autoAssignChanged = _sourceGroup is null || !EditorGroup.CreateAutoAssignFromString(_autoAssignString).SetEquals(_sourceGroup.AutoAssignTo);
        }
        ImGuiManager.PopEditedStyle();
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        base.RenderBottomBar();
        
        ImGui.BeginDisabled(!_valid);
        if (ImGuiManager.TranslatedButton("rysy.ok")) {
            EditorState.History?.ApplyNewAction(new ChangeEditorGroupAction(_map, _newGroupName, _autoAssignChanged ? _autoAssignString : null));
            if (_sourceGroup is null)
                _parent.Select(_map, _map.EditorGroups.GetOrCreate(_newGroupName), input: null);
            _valid = false;
            RemoveSelf();
        }
        ImGui.EndDisabled();
    }
}