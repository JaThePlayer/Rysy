using Rysy.Graphics;
using System.Xml;

namespace Rysy.History;

internal sealed class RemoveAnimatedTileAction(string name) : IHistoryAction {
    private XmlNode? _removedTile;
    
    public bool Apply(Map map) {
        if (!map.AnimatedTiles.Tiles.TryGetValue(name, out var removed))
            return false;
        _removedTile = removed.Xml;
        if (!map.AnimatedTiles.Remove(name))
            return false;

        
        map.SaveAnimatedTilesXml();
        return true;
    }

    public void Undo(Map map) {
        if (map.AnimatedTiles.ReadXmlNode(_removedTile!) is {} tile)
            map.AnimatedTiles.Xml.DocumentElement!.AppendChild(tile.Xml!);
        map.SaveAnimatedTilesXml();
    }
}