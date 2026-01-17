using Hexa.NET.ImGui;

namespace Rysy.Gui;

public readonly struct GuiSize(float widthInChars, float heightInLines) {
    public GuiSize AddLines(int amt) => new(widthInChars, heightInLines + amt);
    public GuiSize AddWidth(int amt) => new(widthInChars + amt, heightInLines);

    public GuiSize Append(string text) {
        var maxLen = widthInChars;
        var lines = heightInLines;
        foreach (var line in text.AsSpan().EnumerateLines()) {
            maxLen = Math.Max(maxLen, line.Length);
            lines++;
        }
        
        return new GuiSize(maxLen, lines);
    }
    
    public NumVector2 Calculate() {
        return new(
            ImGui.CalcTextSize("m").X * widthInChars,
            ImGui.GetTextLineHeightWithSpacing() * heightInLines
        );
    }
    
    public NumVector2 CalculateWindowSize(bool hasBottomBar) {
        var res = new NumVector2(
            ImGui.CalcTextSize("m").X * (widthInChars + 4),
            ImGui.GetTextLineHeightWithSpacing() * (heightInLines + 1)
        );

        res.Y += ImGui.GetFrameHeightWithSpacing() * 2;

        if (hasBottomBar) {
            res.Y += ImGuiManager.BottomBarHeight();
        }

        return res;
    }

    public static GuiSize From(string text) {
        return default(GuiSize).Append(text);
    }
}