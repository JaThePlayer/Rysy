using Hexa.NET.ImGui;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Diagnostics;

namespace Rysy.Gui;

public record struct TextEmphasis {
    public int HeaderLevel { get; set; }
    
    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public bool Strikethrough { get; set; }

    public bool Underline { get; set; }

    public string? Link { get; set; }

    public bool Autolink { get; private set; }

    public bool LinkIsImage { get; set; }

    public TextEmphasis(Inline obj, int headerLevel = 0) {
        HeaderLevel = headerLevel;
        
        var parent = obj.Parent;
        while (parent is { }) {
            switch (parent) {
                case EmphasisInline emphasis:
                    switch (emphasis.DelimiterChar) {
                        case '*':
                            Bold |= emphasis.DelimiterCount is 2 or 3;
                            Italic |= emphasis.DelimiterCount is 1 or 3;
                            break;
                        case '~':
                            Strikethrough = true;
                            break;
                    }
                    break;
                case LinkInline link:
                    Link = link.Url;
                    Autolink = link.IsAutoLink;
                    LinkIsImage = link.IsImage;
                    Underline = !LinkIsImage;
                    break;
                default:
                    if (parent.GetType() != typeof(ContainerInline))
                        Logger.Write("ImGuiMarkdown.Emphasis", LogLevel.Info, $"Unknown inline parent: {parent.GetType()}");
                    break;
            }
            parent = parent.Parent;
        }
    }

    public readonly ImFontPtr Font() {
        var headerFont = HeaderLevel switch {
            1 => ImGuiThemer.HeaderFont,
            >= 2 => ImGuiThemer.Header2Font,
            // if the cast is removed, null will be converted to ImFontPtr due to a implicit conversion from ImFont*...
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
    
    public TextEmphasisPushCtx PushToImgui() {
        ImGui.PushFont(Font(), 0f);
        var start = ImGui.GetCursorScreenPos();
        var colorPushed = false;

        if (Link is { }) {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.LightSkyBlue.ToNumVec4());
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
