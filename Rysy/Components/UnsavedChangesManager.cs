using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Signals;
using System.Diagnostics;

namespace Rysy.Components;

/// <summary>
/// Renders indicators for unsaved changes.
/// </summary>
public sealed class UnsavedChangesManager(IHistoryHandler historyHandler, Action saveMap)
    : ISignalListener<HistoryChanged>, ISignalListener<MapSaved>, ISignalListener<MapSwapped>,
      IMenubarIndicator, ISignalEmitter {
    private long? _unsavedSinceTimestamp;
    
    public bool Unsaved { 
        get;
        private set {
            if (field == value)
                return;
            
            field = value;

            RysyState.Instance.Game.Window.Title = value
                ? RysyState.Instance.Game.Window.Title.AddPrefixIfNeeded("* ")
                : RysyState.Instance.Game.Window.Title.TrimPrefix("* ");

            if (!value) {
                _unsavedSinceTimestamp = null;
            } else {
                _unsavedSinceTimestamp = Stopwatch.GetTimestamp();
            }
        }
    }

    private IHistoryAction? _savedOnAction;
    
    public void OnSignal(MapSaved signal) {
        _savedOnAction = historyHandler.MostRecentAction;
        Unsaved = false;
    }

    public void OnSignal(MapSwapped signal) {
        _savedOnAction = null;
        Unsaved = false;
    }
    
    public void OnSignal(HistoryChanged signal) {
        if (signal.Handler != historyHandler)
            return;
        
        Unsaved = _savedOnAction != historyHandler.MostRecentAction;
    }

    public void RenderMenubarIndicator(Menubar menubar) {
        var elapsed = _unsavedSinceTimestamp is not null ? Stopwatch.GetElapsedTime(_unsavedSinceTimestamp.Value) : TimeSpan.Zero;

        var (text, color) = elapsed switch {
            { TotalSeconds: < 1 } => ("", ThemeColors.FormNullColor),
            { TotalMinutes: < 1 } => ($" {elapsed.Seconds}s", ThemeColors.TextColor),
            { TotalHours: < 1 } => ($" {elapsed.Minutes}m",
                ThemeColors.FormWarningColor),
            _ => ($" {elapsed.TotalHours:N0}h {elapsed.Minutes}m",
                ThemeColors.FormInvalidColor)
        };
        var textWithIcon = ImGuiManager.PerFrameInterpolator.Utf8($"{(char) ImGuiIcons.Save}{text}");

        ImGuiManager.PushStyleColor(ImGuiCol.Text, color);
        var size = new NumVector2(ImGui.CalcTextSize(textWithIcon).X, 0);
        var langKey = Unsaved ? LangKey.Formatted("rysy.saveIndicator.tooltip", text) : "rysy.saveIndicator.noUnsaved.tooltip";
        if (ImGui.Selectable(textWithIcon, size).WithTranslatedTooltip(langKey)) {
            this.Emit(new RunAtEndOfThisFrame(saveMap));
        }
        ImGui.PopStyleColor();
    }

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}