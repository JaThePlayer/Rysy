using Hexa.NET.ImGui;

namespace Rysy.Gui;

public readonly struct GuiSize(float widthInChars, float heightInLines) {
    public NumVector2 Calculate() {
        return new(
            ImGui.CalcTextSize("m").X * widthInChars,
            ImGui.GetTextLineHeightWithSpacing() * heightInLines
        );
    }
}