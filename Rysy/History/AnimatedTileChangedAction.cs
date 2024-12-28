using System.Xml;

namespace Rysy.History;

public class AnimatedTileChangedAction(string name, Dictionary<string, object> values) : IHistoryAction {
    private Dictionary<string, object> Old;
    private List<XmlAttribute> Added;
    
    public bool Apply(Map map) {
        var tiles = map.AnimatedTiles;
        if (!tiles.Tiles.TryGetValue(name, out var tile))
            return false;
        
        Old = new(tile.FakeData.Inner);
        foreach (var (k, v) in values) {
            tile.FakeData[k] = v;
        }
        Added = tile.UpdateData(values);
        
        map.SaveAnimatedTilesXml();

        return true;
    }

    public void Undo(Map map) {
        var tiles = map.AnimatedTiles;
        if (!tiles.Tiles.TryGetValue(name, out var tile))
            return;
        var xml = tile.Xml;
        
        foreach (var (k, v) in Old) {
            tile.FakeData[k] = v;
            if (xml.Attributes[k] is { } existing) {
                existing.Value = v.ToString();
            } else {
                var attr = xml.OwnerDocument!.CreateAttribute(k);
                attr.Value = v.ToString();
                xml.Attributes.Append(attr);
            }
        }
        foreach (var attr in Added) {
            xml.RemoveChild(attr);
        }
        Added.Clear();
        
        tile.OnXmlChanged();
        
        map.SaveAnimatedTilesXml();
    }
}