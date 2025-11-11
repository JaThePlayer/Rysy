using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Mods;

namespace Rysy.History;

public class RemoveTilesetAction(char tileId, bool bg, bool removeSourceTexture) : IHistoryAction {
    byte[]? _sourceTextureBackup;
    private TilesetData? _tilesetData;

    public bool Apply(Map map) {
        var autotiler = bg ? map.BgAutotiler : map.FgAutotiler;

        _tilesetData = autotiler.GetTilesetData(tileId);
        if (_tilesetData is null)
            return false;

        if (autotiler.Remove(_tilesetData)) {
            map.SaveTilesetXml(bg);
            
            if (removeSourceTexture && _tilesetData.Texture is ModTexture {
                    Mod.Filesystem: IWriteableModFilesystem fs
                } modTexture) {
                var path = modTexture.VirtPath;
                _sourceTextureBackup = fs.TryReadAllBytes(path);
                if (_sourceTextureBackup is { }) {
                    fs.TryDeleteFile(path);
                }
            }
            return true;
        }

        return false;
    }

    public void Undo(Map map) {
        var autotiler = bg ? map.BgAutotiler : map.FgAutotiler;
        
        if (_sourceTextureBackup is {} 
            && removeSourceTexture && _tilesetData!.Texture is ModTexture {
                Mod.Filesystem: IWriteableModFilesystem fs
            } modTexture) {
            var path = modTexture.VirtPath;
            fs.TryWriteToFile(path, _sourceTextureBackup);
        }
        
        autotiler.Add(_tilesetData!);
        map.SaveTilesetXml(bg);
    }
}