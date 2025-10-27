using Hexa.NET.ImGui;
using Rysy.Helpers;

namespace Rysy.Gui;

public record struct TextEmphasis {
    public int HeaderLevel { get; set; }
    
    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public bool Strikethrough { get; set; }

    public bool Underline { get; set; }

    public string? Link { get; set; }

    public bool Autolink { get; set; }

    public bool LinkIsImage { get; set; }

    public readonly ImFontPtr Font() {
        var headerFont = HeaderLevel switch {
            1 => ImGuiThemer.HeaderFont,
            >= 2 => ImGuiThemer.Header2Font,
            // if the cast is removed, null will be converted to ImFontPtr due to an implicit conversion from ImFont*...
            _ => (ImFontPtr?)null,
        };
        if (headerFont is { } font)
            return font;
        
        return (Bold, Italic) switch {
            (true, true) => ImGuiThemer.ItalicBoldFont,
            (true, false) => ImGuiThemer.BoldFont,
            (false, true) => ImGuiThemer.ItalicFont,
            (false, false) => ImGuiThemer.DefaultFont,
        };
    }
    
    public unsafe TextEmphasisPushCtx PushToImgui() {
        ImGui.PushFont(Font(), 0f);
        var start = ImGui.GetCursorScreenPos();
        var colorPushed = false;

        if (Link is { }) {
            ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextLink));
            colorPushed = true;
        }
        
        return new(start, colorPushed);
    }

    public void PopFromImgui(TextEmphasisPushCtx ctx) {
        ImGui.PopFont();
        
        if (HeaderLevel > 0)
            ImGui.Separator();
        
        if (Link is { } link) {
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.Text(link);
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked())
                LinkOpenHelper.OpenLinkIfValid(Link);
        }

        ImGui.SameLine(0f,0f);
        var end = ImGui.GetCursorScreenPos();

        if (Strikethrough) {
            var centerY = (ctx.CursorStart.Y + end.Y) / 2  + ImGui.GetTextLineHeight() / 2;
            var strikeStart = new NumVector2(ctx.CursorStart.X, centerY);
            var strikeEnd = new NumVector2(end.X, centerY);
            ImGui.GetWindowDrawList().AddLine(strikeStart, strikeEnd, ImGui.GetColorU32(ImGuiCol.Text), 1);
        }

        if (Underline) {
            var centerY = ImGui.GetTextLineHeight();
            var underlineStart = ctx.CursorStart + new NumVector2(0f, centerY);
            var underlineEnd = end + new NumVector2(0f, centerY);
            ImGui.GetWindowDrawList().AddLine(underlineStart, underlineEnd, ImGui.GetColorU32(ImGuiCol.Text), 1);
        }

        if (ctx.ColorPushed) {
            ImGui.PopStyleColor();
        }
    }
}

public record struct TextEmphasisPushCtx(NumVector2 CursorStart, bool ColorPushed);
