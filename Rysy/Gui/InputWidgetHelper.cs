using Hexa.NET.ImGui;

namespace Rysy.Gui;

public readonly struct InputWidgetHelper {
    private readonly float _xPadding = ImGui.GetStyle().FramePadding.X;

    public InputWidgetHelper(int widgetAmt) {
        float buttonWidth = ImGui.GetFrameHeight();
        
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (buttonWidth + _xPadding) * (widgetAmt + 1));
    }

    /// <summary>
    /// Sets up state to allow rendering the next widget.
    /// </summary>
    public void Next() {
        ImGui.SameLine(0f, _xPadding);
    }
    
    /// <summary>
    /// Renders the final label of the field.
    /// </summary>
    public void Label(string txt) {
        Next();
        ImGui.TextUnformatted(txt);
    }
    
    /// <summary>
    /// Sets up state to allow rendering the next widget.
    /// </summary>
    public void Label(ReadOnlySpan<byte> txt) {
        Next();
        ImGui.TextUnformatted(txt);
    }
}