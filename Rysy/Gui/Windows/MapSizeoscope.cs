using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

public sealed class MapSizeoscopeWindow : Window {
    private readonly Map _map;
    private readonly HistoryHandler _history;
    
    private BinaryPacker.Package _package;
    private long _fullSize;
    private BinaryPacker _packer;
    private string _lookupSize;

    private bool _group;
    
    public MapSizeoscopeWindow(Map map, HistoryHandler history) : base("rysy.menubar.tab.map.sizeoscope_window".TranslateFormatted(map.Package ?? map.Filepath ?? ""), new(500, 500)) {
        _map = map;
        _history = history;
        
        PackMap();

        _history.OnApply += PackMap;
        _history.OnUndo += PackMap;
    }

    public override void RemoveSelf() {
        base.RemoveSelf();
        _history.OnApply -= PackMap;
        _history.OnUndo -= PackMap;
    }

    private void PackMap() {
        _package = _map.IntoBinary();

        using var memStream = new MemoryStream();
        BinaryPacker.SaveToStream(_package, memStream, saveDetailedInformation: true, out _packer);
        _fullSize = memStream.Length;

        _lookupSize = _packer.GetWritingLookupTable().Sum(p => Info(p.Key).Size).ToFilesize().ToString();
    }

    protected override void Render() {
        base.Render();

        ImGui.Checkbox("Group same entities", ref _group);
        
        ImGui.Text(Interpolator.TempU8($"{_package.Name} - {_fullSize.ToFilesize().ToString()}"));

        ImGui.BeginChild("scrollbar"); // Allow for a scrollbar
        
        if (ImGui.BeginTable("sizeoscope", 3, ImGuiManager.TableFlags)) {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Total Size");
            ImGui.TableSetupColumn("Self Size");
            
            ImGui.TableHeadersRow();
        
            Render(_package.Data);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var open = ImGui.TreeNodeEx("String Lookup");
            ImGui.TableNextColumn();
            ImGui.Text(_lookupSize);
            ImGui.TableNextColumn();
            ImGui.Text(_lookupSize);
            
            if (open) {
                var lookup = _packer.GetWritingLookupTable();
                var i = 0;
                foreach (var (str, _) in lookup.OrderByDescending(p => p.Key.Length)) {
                    ImGui.PushID(i++);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    const int maxSize = 128;
                    var trimmedName = str.Trim();
                    var nameSpan = trimmedName;
                    if (trimmedName.Length > maxSize) {
                        if (ImGui.TreeNodeEx(Interpolator.TempU8($"{nameSpan.AsSpan()[..maxSize]} (...)"), ImGuiTreeNodeFlags.Bullet)) 
                            ImGui.TreePop();
                    } else {
                        if (ImGui.TreeNodeEx(nameSpan, ImGuiTreeNodeFlags.Bullet)) 
                            ImGui.TreePop();
                    }

                    if (trimmedName.Length > maxSize && ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(trimmedName);
                    }
                    
                    var size = Info(str).Size.ToFilesize().ToSpanSharedU8();
                    ImGui.TableNextColumn();
                    ImGui.Text(size);
                    ImGui.TableNextColumn();
                    ImGui.Text(size);
                    ImGui.PopID();
                }
                
                ImGui.TreePop();
            }
            
            ImGui.EndTable();
        }
        
        ImGui.EndChild();
    }
    
    BinaryPacker.DetailedWriteInfo Info(BinaryPacker.Element el)
        => _packer.GetDetailedWriteInfo()![el];
    
    BinaryPacker.DetailedLookupWriteInfo Info(string lookupString)
        => _packer.GetDetailedWriteLookupInfo()![lookupString];

    static string ElName(BinaryPacker.Element el) {
        string? name = null;
        if (el.Attributes is { } attrs) {
            if (el.Name is EntityRegistry.BgDecalSid or EntityRegistry.FgDecalSid) {
                if (attrs.TryGetValue("texture", out var textureAttr))
                    name = textureAttr.ToString();
            } else {
                if (attrs.TryGetValue("name", out var nameAttr)) {
                    name = nameAttr.ToString();
                }
            }
        }
        
        name ??= el.Name ?? "";

        return name;
    }

    bool ShouldShowAttrs(BinaryPacker.Element el) => el.Name is not (
        "entities" or "triggers" or "bgdecals" or "fgdecals" or "level" or "levels"
        );
    
    void Render(BinaryPacker.Element el) {
        if (el is null)
            return;

        RenderElement(ElName(el), el.GetHashCode(), Info(el), ShouldShowAttrs(el) ? el.Attributes : null, el.Children, shouldGroup: _group && el.Name is "entities" or "triggers" or "bgdecals" or "fgdecals");
    }
    
    
    void RenderElement(string name, int hashCode, BinaryPacker.DetailedWriteInfo detailed, Dictionary<string, object>? attributes, BinaryPacker.Element[]? children, bool shouldGroup) {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var opened = ImGui.TreeNodeEx(
            Interpolator.TempU8($"{name}##{hashCode}"),
            children is { Length: > 0 } ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.Bullet);
        
        ImGui.TableNextColumn();
        ImGui.Text(detailed.TotalSize.ToFilesize().ToSpanSharedU8());
        ImGui.TableNextColumn();
        ImGui.Text(detailed.SelfSize.ToFilesize().ToSpanSharedU8());
        
        if (opened) {
            if (attributes is { }) {
                ImGuiManager.PushNullStyle();
                foreach (var (k, v) in attributes) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(Interpolator.TempU8($"{k} = {v?.ToString() ?? "null"}"));
                }
                
                
                ImGuiManager.PopNullStyle();
            }
            
            if (children is { }) {
                if (shouldGroup) {
                    foreach (var group in children
                                 .GroupBy(ElName)
                                 .Select(gr => new {
                                     Group = gr,
                                     Info = new BinaryPacker.DetailedWriteInfo() {
                                         TotalSize = gr.Sum(c => Info(c).TotalSize),
                                         SelfSize = gr.Sum(c => Info(c).SelfSize),
                                     }
                                 })
                                 .OrderByDescending(c => c.Info.TotalSize)) {
                        RenderElement(group.Group.Key, group.Group.Key.GetHashCode(StringComparison.Ordinal), group.Info, null, null, false);
                    }
                } else {
                    foreach (var child in children.OrderByDescending(c => Info(c).TotalSize)) {
                        Render(child);
                    }
                }
            }
            
            ImGui.TreePop();
        }
        
        
    }
}