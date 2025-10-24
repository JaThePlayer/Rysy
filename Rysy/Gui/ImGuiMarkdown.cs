using Hexa.NET.ImGui;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Runtime.InteropServices;
using System.Text;

namespace Rysy.Gui;

internal static partial class ImGuiMarkdown {
    public static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .UseBootstrap()
        .UseAutoLinks()
        .UseGridTables()
        .UsePipeTables()
        .EnableTrackTrivia()
        .Build();

    public static void RenderMarkdown(MarkdownObject doc) {
        ImGui.PushFont(ImGuiThemer.DefaultFont, 0f);
        var startX = ImGui.GetCursorPosX();
        
        if (doc is ContainerBlock container) {
            foreach (var contained in container) {
                ImGui.SetCursorPosX(startX);

                switch (contained) {
                    case HeadingBlock headingBlock: {
                        if (headingBlock.Inline is null)
                            continue;

                        var first = true;
                        foreach (var inline in headingBlock.Inline) {
                            RenderInline(inline, headingBlock.Level, first, startX);
                            first = false;
                        }
                    }
                        break;
                    case ParagraphBlock paragraphBlock: {
                        if (paragraphBlock.Inline is null)
                            continue;
                        var first = true;
                        foreach (var inline in paragraphBlock.Inline) {
                            RenderInline(inline, 0, first, startX);
                            first = false;
                        }
                    }
                        break;
                    case ListBlock list:
                        var bullet = list.BulletType;
                        foreach (var innerBlock in list) {
                            ImGui.Bullet();
                            ImGui.SameLine();
                            RenderMarkdown(innerBlock);
                            ImGui.SetCursorPosX(startX);
                        }
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
                            } else { 
                                ImGui.TableNextRow();
                                foreach (var innerBlock in row) {
                                    if (ImGui.TableNextColumn())
                                        RenderMarkdown(innerBlock);
                                }
                                
                            }
                        }
                        ImGui.EndTable();
                        break;
                    case FencedCodeBlock fencedCodeBlock: {
                        var language = fencedCodeBlock.Info;

                        switch (language) {
                            case "search":
                                RenderSearchCodeBlock(fencedCodeBlock);
                                break;
                            default:
                                RenderUnknownCodeBlock(fencedCodeBlock);
                                break;
                        }
                        break;
                    }
                    case LeafBlock codeBlock: {
                        var first = true;

                        if (codeBlock.Inline is null) {
                            if (codeBlock.Lines is { Lines: {} })
                                foreach (var line in codeBlock.Lines.Lines) {
                                    ImGui.Text(line.Slice.AsSpan().ToString());
                                    first = false;
                                }
                            
                            continue;
                        }

                        foreach (var inline in codeBlock.Inline) {
                            RenderInline(inline, 0, first, startX);
                            first = false;
                        }
                        
                        break;
                    }
                    default:
                        Logger.Write("Markdown", LogLevel.Warning, $"Unknown block: {contained}");
                        break;
                }
                
                ImGui.NewLine();
            }
        }
        
        ImGui.PopFont();
    }

    private static void RenderUnknownCodeBlock(FencedCodeBlock block) {
        ImGui.BeginDisabled();
        if (block.Lines is {})
            foreach (var line in block.Lines.Lines.AsSpan()
                         .SkipWhileFromEnd(l => l.ToString().IsNullOrWhitespace())) {
                ImGui.Text(line.Slice.AsSpan().ToString());
            }
        ImGui.EndDisabled();
    }

    private static void RenderSearchLine(ReadOnlySpan<char> text) {
        if (!text.IsWhiteSpace()) {
            var parsed = SearchHelper.ParseSearch(text);
            parsed.RenderImGui();
        }
    }

    private static void RenderSearchCodeBlock(FencedCodeBlock block) {
        ImGui.BeginDisabled();
        foreach (var line in block.Lines.Lines) {
            var text = line.Slice.AsSpan();
            RenderSearchLine(text);
        }
        ImGui.EndDisabled();
    }

    private static void RenderInline(Inline inline, int headerLevel, bool firstInThisLine, float startX) {
        switch (inline) {
            case EmphasisInline emphasisInline:

                break;
            case CodeInline codeInline: {
                var content = codeInline.Content.AsSpan();
                
                if (content is ['^', 's', 'e', 'a', 'r', 'c', 'h', ' ', .. var searchText]) {
                    RenderSearchLine(searchText);
                } else {
                    ImGui.BeginDisabled();
                    RenderTextWrapped(content, startX);
                    ImGui.EndDisabled();
                }

                ImGui.SameLine(0f, 0f);
                break;
            }
            case LiteralInline literalInline: {
                var emphasis = CreateEmphasis(literalInline, headerLevel);
                var renderText = true;

                var content = literalInline.Content.AsSpan();
                var contentTrimmed = firstInThisLine ? content.TrimStart() : content;
                    
                ImGuiManager.PushEmphasis(emphasis);
                    
                if (emphasis is { Link: {  } link, LinkIsImage: true }) {
                    if (GFX.Atlas.TryGet(link, out var texture)) {
                        DrawTexture(texture, contentTrimmed);
                    } else if (LinkOpenHelper.IsValidLink(link, out var uri)) {
                        if (GFX.GetTextureFromWebIfReady(uri) is { } webTexture) {
                            DrawTexture(webTexture, contentTrimmed);
                        }
                    }
                }

                if (renderText) {
                    RenderTextWrapped(contentTrimmed, startX);
                }
                    
                ImGuiManager.PopEmphasis();
                break;
                
                void DrawTexture(VirtTexture texture, ReadOnlySpan<char> linkTooltip) {
                    ImGuiManager.XnaWidget($"md.img.{link}", texture.Width, texture.Height, () => {
                        ISprite.FromTexture(texture).Render(SpriteRenderCtx.Default(true));
                    });
                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.Text(linkTooltip.ToString());
                        ImGui.EndTooltip();
                    }
                    renderText = false;
                }
            }
            case LineBreakInline:
                ImGui.NewLine();
                break;
            case LinkInline:
                break;
            default:
                Logger.Write("Markdown", LogLevel.Warning, $"Unknown inline: {inline}");
                break;
        }

        if (inline is ContainerInline container) {
            foreach (var inner in container) {
                RenderInline(inner, headerLevel, false, startX);
            }
        }
    }
    
    private static TextEmphasis CreateEmphasis(Inline obj, int headerLevel = 0) {
        var emph = new TextEmphasis();
        emph.HeaderLevel = headerLevel;
        
        var parent = obj.Parent;
        while (parent is { }) {
            switch (parent) {
                case EmphasisInline emphasis:
                    switch (emphasis.DelimiterChar) {
                        case '*':
                            emph.Bold |= emphasis.DelimiterCount is 2 or 3;
                            emph.Italic |= emphasis.DelimiterCount is 1 or 3;
                            break;
                        case '~':
                            emph.Strikethrough = true;
                            break;
                    }
                    break;
                case LinkInline link:
                    emph.Link = link.Url;
                    emph.Autolink = link.IsAutoLink;
                    emph.LinkIsImage = link.IsImage;
                    emph.Underline = !emph.LinkIsImage;
                    break;
                default:
                    if (parent.GetType() != typeof(ContainerInline))
                        Logger.Write("ImGuiMarkdown.CreateEmphasis", LogLevel.Info, $"Unknown inline parent: {parent.GetType()}");
                    break;
            }
            parent = parent.Parent;
        }

        return emph;
    }


    // ImGui::TextWrapped will wrap at the starting position
    // so to work around this we render using our own wrapping for the first line
    // furthermore, we need to pop/push the Emphasis to render underline/strikethrough text correctly
    // based on https://gist.github.com/dougbinks/65d125e0c11fba81c5e78c546dcfe7af
    private static unsafe void RenderTextWrapped(ReadOnlySpan<char> textUtf16, float wrapStart) {
        if (textUtf16 is "") {
            ImGui.NewLine();
            return;
        }
        
        var pFont = ImGui.GetFont();
        float scale = ImGui.GetStyle().FontSizeBase;
        float widthLeft = ImGui.GetColumnWidth();
        
        var utf8ByteCount = Encoding.UTF8.GetByteCount(textUtf16);
        Span<byte> utf8 = utf8ByteCount <= 2048 ? stackalloc byte[utf8ByteCount + 1] : new byte[utf8ByteCount + 1];
        var utf8Written = Encoding.UTF8.GetBytes(textUtf16, utf8);
        utf8[utf8Written] = 0;
        utf8 = utf8[..utf8Written];
        
        fixed (byte* utf8Ptr = &utf8[0]) {
            var textEnd = &utf8Ptr[utf8.Length];
            var endPrevLine = pFont.CalcWordWrapPosition(scale, utf8Ptr, textEnd, widthLeft);

            if (endPrevLine == textEnd) {
                //ImGuiNative.igText(utf8ptr);
                ImGui.Text(textUtf16.ToString());
                return;
            }
            ImGui.TextUnformatted(utf8Ptr, endPrevLine);
            
            while (endPrevLine < textEnd)
            {
                widthLeft = ImGui.GetContentRegionAvail().X;
                
                var text = endPrevLine;
                if( *text == ' ' ) { ++text; } // skip a space at start of line
                endPrevLine = pFont.CalcWordWrapPosition(scale, text, textEnd, widthLeft);
                
                var popped = ImGuiManager.PopEmphasis();
                if (popped is { }) {
                    ImGui.NewLine();
                    ImGui.SetCursorPosX(wrapStart);
                    ImGuiManager.PushEmphasis(popped.Value);
                }
                
                ImGui.SetCursorPosX(wrapStart);
                ImGui.TextUnformatted(text, endPrevLine);
            }
            ImGuiManager.PopEmphasis();
        }
    }
}
