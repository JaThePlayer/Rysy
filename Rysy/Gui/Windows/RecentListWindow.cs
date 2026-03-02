using Hexa.NET.ImGui;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Tools;

namespace Rysy.Gui.Windows;

public sealed class RecentListWindow : Window {
    private readonly ToolHandler _toolHandler;
    private readonly QuickActionRegistry _quickActionRegistry;
    private readonly Input _input;

    public RecentListWindow(ToolHandler toolHandler, QuickActionRegistry quickActionRegistry, Input input) : base("rysy.recent_list".Translate()) {
        _toolHandler = toolHandler;
        _quickActionRegistry = quickActionRegistry;
        _input = input;
        NoSaveData = false;
    }

    protected override void Render() {
        var windowSize = ImGui.GetContentRegionAvail();
        var actionWidth = Tool.PreviewSize + ImGui.GetStyle().ItemSpacing.X;
        var actionHeight = Tool.PreviewSize + ImGui.GetStyle().ItemSpacing.Y;
        var visibleActionsPerRow = (int)(ImGui.GetWindowWidth() / actionWidth);
        var visibleRows = (int)(windowSize.Y / actionHeight);
        _quickActionRegistry.MaxQuickActions = visibleActionsPerRow * visibleRows;
        
        var actions = _quickActionRegistry.QuickActions;
        var i = 0;
        foreach (var action in actions.Take(_quickActionRegistry.MaxQuickActions)) {
            var tool = _toolHandler.GetToolByName(action.ToolName);
            if (tool is null)
                continue;
            var layer = EditorLayers.EditorLayerFromName(action.Layer, tool.ValidLayers);
            if (layer is null)
                continue;

            if (i % visibleActionsPerRow == 0 && i > 0) {
                ImGui.NewLine();
            }
            ImGui.PushID($"quick-action-{i++}-{action.MaterialString}");
            
            var size = new NumVector2(0, 0);
            
            if (action.GetMaterial(_toolHandler) is { } material && tool.GetMaterialPreview(layer, material) is {} preview) {
                var cursorStart = ImGui.GetCursorPos();
                size.X = preview.W;
                size.Y = preview.H;
                
                if (ImGui.Selectable("##selectable"u8, _toolHandler.CurrentTool == tool && ISimilar.Check(material, tool.Material), ImGuiSelectableFlags.AllowOverlap, size)) {
                    action.Apply(_toolHandler);
                }

                if (ImGui.IsItemHovered()) {
                    if (ImGui.IsItemActive() && _input.Mouse.LeftDoubleClicked()) {
                        _input.Mouse.ConsumeLeft();
                        action.IsFavourite = !action.IsFavourite;
                        _quickActionRegistry.CleanupQuickActions();
                    }
                    
                    tool.RenderMaterialTooltip(layer, material, tool.GetMaterialSearchable(layer, material));
                }

                ImGui.SetCursorPos(cursorStart);
                ImGuiManager.XnaWidget(preview);
                ImGui.SameLine();
                
                if (action.IsFavourite) {
                    ImGui.SetCursorPos(cursorStart);
                    ImGuiManager.FavoriteIcon();
                    ImGui.SameLine();
                    // Avoid imgui assertions for using SetCursorPos to move forward
                    ImGui.SetCursorPos(cursorStart);
                    ImGui.Dummy(size);
                    ImGui.SameLine();
                }
            }
            
            ImGui.PopID();
        }
    }
}