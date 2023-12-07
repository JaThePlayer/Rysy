﻿using ImGuiNET;
using Microsoft.Win32;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.History;
using System.Diagnostics;

namespace Rysy.Tools;

public class ToolHandler {
    public const float DefaultMaterialListWidth = 360f;

    public readonly HistoryHandler History;

    public readonly Input Input;

    public readonly ToolRegistry Registry;

    public List<Tool> Tools;

    private Tool _currentTool;
    public Tool CurrentTool {
        get => _currentTool ??= Tools.FirstOrDefault() ?? throw new UnreachableException("No tools registered?");
        set {
            if (_currentTool != value) {
                CancelInteraction();
                _currentTool = value;
            }
        }
    }

    private static Tool Create(Type type, HistoryHandler history, Input input) {
        var t = (Tool) Activator.CreateInstance(type)!;

        t.History = history;
        t.Input = input;
        t.Init();

        t.HotkeyHandler = new(input, updateInImgui: false);
        t.InitHotkeys(t.HotkeyHandler);

        return t;
    }

    private static List<string> HardcodedOrder = new() { "brush", "rectangle", "placement", "selection", "script" };

    public ToolHandler(HistoryHandler history, Input input, ToolRegistry? registry = null) {
        History = history;
        Input = input;
        Registry = registry ?? ToolRegistry.Global;

        CreateTools();
        Registry.Tools.OnChanged += CreateTools;
        EditorState.OnCurrentRoomChanged += CancelInteraction;
        History.OnUndo += CancelInteraction;
    }

    public void Unload() {
        UnloadAllTools();
        Registry.Tools.OnChanged -= CreateTools;
        EditorState.OnCurrentRoomChanged -= CancelInteraction;
        History.OnUndo -= CancelInteraction;
    }

    ~ToolHandler() {
        UnloadAllTools();
    }

    private void CreateTools() {
        UnloadAllTools();

        Tools = new();
        foreach (var toolType in Registry.Tools) {
            AddTool(Create(toolType, History, Input));
        }
        Tools = Tools.OrderBy(t => HardcodedOrder.IndexOf(t.Name)).ThenBy(t => t.Name).ToList();
    }

    private void UnloadAllTools() {
        if (Tools is { }) {
            foreach (var t in Tools) {
                t.Unload();
            }
            Tools.Clear();
        }

    }

    public ToolHandler UsePersistence(bool value) {
        foreach (var tool in Tools) {
            tool.UsePersistence = value;
        }

        return this;
    }

    public void CancelInteraction() {
        CurrentTool?.CancelInteraction();
    }

    private void AddTool(Tool tool) {
        Tools.Add(tool);
    }

    public T? GetTool<T>() where T : Tool {
        return (T?)Tools.FirstOrDefault(t => t is T);
    }

    public Tool? GetToolByName(string name) {
        return Tools.FirstOrDefault(t => t.Name == name);
    }

    public T? SetTool<T>() where T : Tool {
        var tool = GetTool<T>();

        if (tool is { })
            CurrentTool = tool;

        return tool;
    }

    public Tool? SetToolByName(string name) {
        var tool = GetToolByName(name);

        if (tool is { })
            CurrentTool = tool;

        return tool;
    }

    public void InitHotkeys(HotkeyHandler handler) {
        foreach (var tool in Tools) {
            tool.HotkeyHandler = new(Input, handler.UpdateInImgui);
            tool.InitHotkeys(tool.HotkeyHandler);
        }

        handler.AddHotkeyFromSettings("tools.nextTool", "tab", () => SwapToNextTool(1), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("tools.prevTool", "shift+tab", () => SwapToNextTool(-1), HotkeyModes.OnHoldSmoothInterval);
    }

    private void SwapToNextTool(int idOffset) {
        var i = Tools.IndexOf(CurrentTool);
        CurrentTool = Tools[(i + idOffset).MathMod(Tools.Count)];
    }

    public void Update(Camera camera, Room currentRoom) {
        CurrentTool.HotkeyHandler.Update();
        CurrentTool.Update(camera, currentRoom);
    }

    public void Render(Camera camera, Room currentRoom) {
        currentRoom.StartBatch(camera);

        CurrentTool.Render(camera, currentRoom);

        GFX.EndBatch();

        GFX.BeginBatch();
        CurrentTool.RenderOverlay();
        GFX.EndBatch();
    }

    private bool _firstGui = true;

    public void RenderGui() {
        RenderToolList(_firstGui, out float toolHeight);
        RenderLayerList(_firstGui, toolHeight);
        RenderModeList();

        if (CurrentTool.BeginMaterialListWindow(_firstGui) is { } size)
            CurrentTool.RenderGui(size);
        _firstGui = false;
    }

    private void RenderModeList() {
        var tool = CurrentTool;
        var currentMode = tool.Mode;
        
        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Mode", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();
        
        var windowSize = ImGui.GetWindowSize();
        if (ImGui.BeginListBox("##ToolModeBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * tool.ValidModes.Count + 2))) {
            foreach (var item in tool.ValidModes) {
                if (ImGui.Selectable(item.LocalizedName, currentMode == item)) {
                    tool.Mode = item;
                }
            }
            ImGui.EndListBox();
        }
        
        ImGui.End();
    }
    
    private void RenderLayerList(bool firstGui, float toolListHeight) {
        var tool = CurrentTool;
        var currentLayer = tool.Layer;

        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
            var size = new NumVector2(ImGuiManager.CalcListSize(Tools.SelectMany(t => t.ValidLayers).Select(t => t.Name)).X, ImGui.GetTextLineHeightWithSpacing() * Tools.Max(t => t.ValidLayers.Count) + ImGui.GetFrameHeightWithSpacing() * 2);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X - DefaultMaterialListWidth, menubarHeight + toolListHeight));
            ImGui.SetNextWindowSize(size);
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Layer", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginListBox("##ToolLayerBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * tool.ValidLayers.Count + 5))) {
            foreach (var item in tool.ValidLayers) {
                if (ImGui.Selectable(item.LocalizedName, currentLayer == item)) {
                    tool.Layer = item;
                }
            }
            ImGui.EndListBox();
        }

        ImGui.End();
    }

    private void RenderToolList(bool firstGui, out float height) {
        height = 0f;
        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
            var size = new NumVector2(120f, ImGui.GetTextLineHeightWithSpacing() * Tools.Count + ImGui.GetFrameHeightWithSpacing() * 2);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X - DefaultMaterialListWidth, menubarHeight));
            ImGui.SetNextWindowSize(size);
            height = size.Y;
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Tool", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginListBox("##ToolToolsBox", new(windowSize.X - 10, ImGui.GetTextLineHeightWithSpacing() * Tools.Count + 5))) {
            foreach (var item in Tools) {
                if (ImGui.Selectable(item.Name.TranslateOrHumanize("rysy.tools"), CurrentTool == item)) {
                    CurrentTool = item;
                }
            }
            ImGui.EndListBox();
        }

        ImGui.End();
    }
}
