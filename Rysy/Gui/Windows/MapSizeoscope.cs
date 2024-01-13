using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.Windows;

public sealed class MapSizeoscopeWindow : Window {
    private readonly Map _map;
    private readonly BinaryPacker.Package _package;
    private readonly long _fullSize;
    private readonly BinaryPacker _packer;
    private readonly string _lookupSize;

    private bool _group;
    
    public MapSizeoscopeWindow(Map map) : base("rysy.menubar.tab.map.sizeoscope_window".TranslateFormatted(map.Package ?? map.Filepath ?? ""), new(500, 500)) {
        _map = map;
        _package = map.IntoBinary();

        using var memStream = new MemoryStream();
        BinaryPacker.SaveToStream(_package, memStream, saveDetailedInformation: true, out _packer);
        _fullSize = memStream.Length;

        _lookupSize = _packer.GetWritingLookupTable().Sum(p => Info(p.Key).Size).ToFilesize();
    }

    protected override void Render() {
        base.Render();

        ImGui.Checkbox("Group same entities", ref _group);
        ImGui.Text(Interpolator.Temp($"{_package.Name} - {_fullSize.ToFilesize()}"));

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
                foreach (var (str, _) in lookup.OrderByDescending(p => p.Key.Length)) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    const int maxSize = 128;
                    var trimmedName = str.AsSpan().Trim();
                    var nameSpan = trimmedName;
                    if (trimmedName.Length > maxSize) {
                        nameSpan = Interpolator.Temp($"{nameSpan[..maxSize]} (...)");
                    }
                    
                    if (ImGui.TreeNodeEx(nameSpan, ImGuiTreeNodeFlags.Bullet)) 
                        ImGui.TreePop();
                    if (trimmedName.Length > maxSize && ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(trimmedName);
                    }
                    
                    var size = Info(str).Size.ToFilesize();
                    ImGui.TableNextColumn();
                    ImGui.Text(size);
                    ImGui.TableNextColumn();
                    ImGui.Text(size);
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
            if (el.Name is EntityRegistry.BGDecalSID or EntityRegistry.FGDecalSID) {
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
            Interpolator.Temp($"{name}##{hashCode}"),
            children is { Length: > 0 } ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.Bullet);
        
        ImGui.TableNextColumn();
        ImGui.Text(detailed.TotalSize.ToFilesize());
        ImGui.TableNextColumn();
        ImGui.Text(detailed.SelfSize.ToFilesize());
        
        if (opened) {
            if (attributes is { }) {
                ImGuiManager.PushNullStyle();
                foreach (var (k, v) in attributes) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(Interpolator.Temp($"{k} = {v?.ToString() ?? "null"}"));
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
                        RenderElement(group.Group.Key, group.Group.Key.GetHashCode(), group.Info, null, null, false);
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