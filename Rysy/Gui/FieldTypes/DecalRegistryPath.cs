using Hexa.NET.ImGui;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.FieldTypes;

public record DecalRegistryPathField(bool SupportDirectoryType, string TooltipPrefix = "rysy.decalRegistryEntryType") : ComplexTypeField<DecalRegistryPath> {
    public DecalRegistryWindow? DecalRegistryWindow;
    
    internal ModMeta? Mod => DecalRegistryWindow?.Mod;

    private bool EntryExistsFor(string path) => DecalRegistryWindow?.EntryExistsFor(path) ?? false;
    private bool IsValidPath(FoundPath path) => DecalRegistryWindow?.IsValidPath(path) ?? true;

    private bool SingleTextureIsValid(string path) {
        if (!Gfx.Atlas.TryGet(Decal.MapTextureToPath(path), out var texture))
            return false;
        
        if (DecalRegistryWindow is null)
            return true;

        return texture is ModTexture modTexture && modTexture.Mod == Mod;
    }
    
    public override DecalRegistryPath Parse(string data) {
        return new(data);
    }

    public override string ConvertToString(DecalRegistryPath data) {
        return data.SavedName;
    }

    public override bool RenderDetailedWindow(ref DecalRegistryPath data) {
        if (!_firstRender) {
            _firstRender = true;
            _newEntryType = data.Type;
        }
        
        var changed = ImGuiManager.EnumComboTranslated(SupportDirectoryType 
            ? [ DecalRegistryEntry.Types.SingleTexture, DecalRegistryEntry.Types.Directory, DecalRegistryEntry.Types.StartsWith ] 
            : [ DecalRegistryEntry.Types.SingleTexture, DecalRegistryEntry.Types.StartsWith], 
            TooltipPrefix, ref data.Type);

        var newEntryName = data.SavedName;
        
        var isInvalid = EntryExistsFor(newEntryName);

        PathField? field = null;
        switch (data.Type) {
            case DecalRegistryEntry.Types.StartsWith:
                _newEntryDecalPathFieldStartsWith ??= new("", Gfx.Atlas, "^decals/(.*)$") {
                    Filter = IsValidPath,
                    Editable = true,
                };

                field = _newEntryDecalPathFieldStartsWith;
                break;
            case DecalRegistryEntry.Types.Directory:
                _newEntryDecalPathFieldDirectory ??= new("", Gfx.Atlas, "^decals/(.*)/.*$") {
                    Filter = IsValidPath,
                    Editable = true,
                };

                field = _newEntryDecalPathFieldDirectory;
                break;
            default:
                if (!isInvalid) {
                    isInvalid = !SingleTextureIsValid(newEntryName);
                }

                _newEntryDecalPathFieldSingleTexture ??= new("", Gfx.Atlas, "^decals/(.*)$") {
                    Filter = IsValidPath,
                    Editable = true,
                };
                field = _newEntryDecalPathFieldSingleTexture;
                break;
        }
            
        ImGuiManager.PushInvalidStyleIf(isInvalid);
        ImGui.SetNextItemWidth(400f);
        var ret = (string?)field.RenderGui("rysy.decalRegistryWindow.newEntryPath".Translate(), data.Value);
        ImGuiManager.PopInvalidStyle();

        if (ret is { }) {
            data.Value = ret;
            return true;
        }

        if (changed) {
            return true;
        }
        
        return false;
    }
    
    private PathField? _newEntryDecalPathFieldSingleTexture;
    private PathField? _newEntryDecalPathFieldStartsWith;
    private PathField? _newEntryDecalPathFieldDirectory;

    private DecalRegistryEntry.Types _newEntryType;

    private bool _firstRender;
}