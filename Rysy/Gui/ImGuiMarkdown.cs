using ImGuiNET;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Rysy.Extensions;
using Rysy.Graphics;
using System.Diagnostics;
using System.Security.Policy;

namespace Rysy.Gui;

internal static class ImGuiMarkdown {

    public static void RenderMarkdown(MarkdownObject doc, bool endLine = true, bool inTable = false) {
        Action? onBlockEnd = null;
        bool blockBegun = false;
        MarkdownObject? lastTablePara = null;
        bool inTableHeader = false;

        ImGui.PushFont(ImGuiThemer.DefaultFont);

        foreach (var item in doc.Descendants()) {
            if (item is Block) {
                if (blockBegun && lastTablePara is null) {
                    ImGui.NewLine();
                    onBlockEnd?.Invoke();
                    onBlockEnd = null;
                }
                blockBegun = true;
            }

            switch (item) {
                case HeadingBlock header:
                    ImGui.PushFont(header.Level switch { 
                        2 => ImGuiThemer.Header2Font,
                        _ => ImGuiThemer.HeaderFont,
                    });
                    onBlockEnd += () => {
                        ImGui.Separator();
                        ImGui.PopFont();
                    };
                    break;
                case LineBreakInline linebreak:
                    ImGui.NewLine();
                    break;
                case Table table:
                    var columns = table.ColumnDefinitions.Count - 1;
                    ImGui.BeginTable($"##", columns, ImGuiManager.TableFlags);

                    
                    foreach (var row in table.Descendants<TableRow>()) {
                        if (row.IsHeader) {
                            foreach (var inner in row) {
                                ImGui.TableSetupColumn(inner.Descendants<LiteralInline>()?.FirstOrDefault()?.ToString() ?? "?");
                            }

                            ImGui.TableHeadersRow();
                        }
                    }

                    lastTablePara = table.Descendants().Last();
                    inTableHeader = true;
                    break;
                case TableRow row:
                    if (row.IsHeader) {
                        inTableHeader = true;
                        continue;
                    }

                    ImGui.TableNextRow();
                    inTableHeader = false;
                    break;
                case TableCell cell:
                    if (inTableHeader)
                        continue;
                    ImGui.TableNextColumn();
                    break;
                case ParagraphBlock b:
                    break;
                case LiteralInline literalInline:
                    if (inTableHeader) {
                        continue;
                    }
                    var emphasis = new EmphasisTypes(literalInline);
                    if (emphasis.Bold) {
                        ImGui.PushFont(ImGuiThemer.BoldFont);
                    }

                    var start = ImGui.GetCursorScreenPos();
                    var colorPushed = false;
                    var renderText = true;

                    if (emphasis.Link is { }) {
                        if (emphasis.LinkIsImage) {
                            if (GFX.Atlas.TryGet(emphasis.Link, out var texture)) {
                                ImGuiManager.XnaWidget($"md.img.{emphasis.Link}", texture.Width, texture.Height, () => {
                                    ISprite.FromTexture(texture).Render();
                                });
                                if (ImGui.IsItemHovered()) {
                                    ImGui.BeginTooltip();
                                    ImGui.Text(literalInline.ToString());
                                    ImGui.EndTooltip();
                                }
                                renderText = false;
                            }
                        }

                        ImGui.PushStyleColor(ImGuiCol.Text, Color.LightSkyBlue.ToNumVec4());
                        colorPushed = true;
                    }

                    if (renderText)
                        ImGui.Text(literalInline.ToString());

                    if (emphasis.Link is { } link) {
                        if (ImGui.IsItemHovered()) {
                            ImGui.BeginTooltip();
                            ImGui.Text(link);
                            ImGui.EndTooltip();
                        }
                        if (ImGui.IsItemClicked())
                            OpenLink(emphasis.Link);
                    }

                    ImGui.SameLine(0f,0f);
                    var end = ImGui.GetCursorScreenPos();

                    if (renderText && emphasis.Strikethrough) {
                        var centerY = ImGui.GetTextLineHeight() / 2;
                        var strikeStart = start + new NumVector2(0f, centerY);
                        var strikeEnd = end + new NumVector2(0f, centerY);
                        ImGui.GetWindowDrawList().AddLine(strikeStart, strikeEnd, ImGui.GetColorU32(ImGuiCol.Text), 1);
                    }

                    if (renderText && emphasis.Underline) {
                        var centerY = ImGui.GetTextLineHeight();
                        var underlineStart = start + new NumVector2(0f, centerY);
                        var underlineEnd = end + new NumVector2(0f, centerY);
                        ImGui.GetWindowDrawList().AddLine(underlineStart, underlineEnd, ImGui.GetColorU32(ImGuiCol.Text), 1);
                    }

                    if (emphasis.Bold) {
                        ImGui.PopFont();
                    }

                    if (colorPushed) {
                        ImGui.PopStyleColor();
                    }

                    break;
                default:
                    break;
            }

            if (item == lastTablePara) {
                ImGui.EndTable();
                ImGui.SameLine(0f, 0f);
                lastTablePara = null;
            }
        }

        onBlockEnd?.Invoke();
        if (endLine)
            ImGui.NewLine();
        ImGui.PopFont();
    }



    static bool OpenLink(string potentialLink) {
        if (!Uri.TryCreate(potentialLink, UriKind.Absolute, out var uri)) {
            return false;
        }

        if (uri.IsFile)
            return false;

        if (uri.Scheme is not ("https" or "http"))
            return false;

        try {
            Process.Start(new ProcessStartInfo {
                FileName = potentialLink,
                UseShellExecute = true
            });

            return true;
        } catch {
            return false;
        }
    }

    struct EmphasisTypes {
        public bool Bold { get; private set; }

        public bool Italic { get; private set; }

        public bool Strikethrough { get; private set; }

        public bool Underline { get; private set; }

        public string? Link { get; private set; }

        public bool Autolink { get; private set; }

        public bool LinkIsImage { get; private set; }

        public EmphasisTypes(Inline obj) {
            var parent = obj.Parent;
            while (parent is { }) {
                switch (parent) {
                    case EmphasisInline emphasis:
                        switch (emphasis.DelimiterChar) {
                            case '*':
                                Bold = true;
                                break;
                            case '~':
                                Strikethrough = true;
                                break;
                            default:
                                break;
                        }
                        break;
                    case LinkInline link:
                        Link = link.Url;
                        Autolink = link.IsAutoLink;
                        Underline = true;
                        LinkIsImage = link.IsImage;
                        break;
                    case ContainerInline:
                        break;
                    default:
                        //Logger.Write("ImGuiMarkdown.Emphasis", LogLevel.Info, $"Unknown inline parent: {parent.GetType()}");
                        break;
                }
                parent = parent.Parent;
            }
        }
    }
}
