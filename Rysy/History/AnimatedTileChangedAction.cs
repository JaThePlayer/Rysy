using System.Xml;

namespace Rysy.History;

public class AnimatedTileChangedAction(string name, Dictionary<string, object> values) : IHistoryAction {
    private Dictionary<string, object> _old;
    private List<XmlAttribute> _added;
    
    public bool Apply(Map map) {
        var tiles = map.AnimatedTiles;
        if (!tiles.Tiles.TryGetValue(name, out var tile))
            return false;
        
        _old = new(tile.FakeData.Inner);
        foreach (var (k, v) in values) {
            tile.FakeData[k] = v;
        }
        _added = tile.UpdateData(values);
        
        map.SaveAnimatedTilesXml();

        return true;
    }

    public void Undo(Map map) {
        var tiles = map.AnimatedTiles;
        if (!tiles.Tiles.TryGetValue(name, out var tile))
            return;
        var xml = tile.Xml;
        if (xml is null)
            return;
        
        foreach (var (k, v) in _old) {
            tile.FakeData[k] = v;
            if (xml.Attributes![k] is { } existing) {
                existing.Value = v.ToString();
            } else {
                var attr = xml.OwnerDocument!.CreateAttribute(k);
                attr.Value = v.ToString();
                xml.Attributes.Append(attr);
            }
        }
        foreach (var attr in _added) {
            xml.RemoveChild(attr);
        }
        _added.Clear();
        
        tile.OnXmlChanged();
        
        map.SaveAnimatedTilesXml();
    }
}