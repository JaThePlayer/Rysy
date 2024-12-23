using Rysy.Graphics;
using System.Diagnostics;
using System.Xml;

namespace Rysy.History;

public class TilesetChangedAction(char tilesetId, bool bg, Dictionary<string, object> values) : IHistoryAction {
    private Dictionary<string, object> Old;
    private List<XmlAttribute> Added;
    
    public bool Apply(Map map) {
        var autotiler = bg ? map.BGAutotiler : map.FGAutotiler;
        var tileset = autotiler.GetTilesetData(tilesetId);
        if (tileset is not { Xml: { Attributes: {} attributes } xml })
            return false;

        Old = new(tileset.FakeData.Inner);
        foreach (var (k, v) in values) {
            tileset.FakeData[k] = v;
        }
        Added = tileset.UpdateData(values);

        return true;
    }

    public void Undo(Map map) {
        var autotiler = bg ? map.BGAutotiler : map.FGAutotiler;
        var tileset = autotiler.GetTilesetData(tilesetId);
        if (tileset is not { Xml: { Attributes: {} attributes } xml })
            throw new UnreachableException("Upon undoing a TilesetChangedAction, the tileset does not have an XML?!");
        
        foreach (var (k, v) in Old) {
            tileset.FakeData[k] = v;
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
        
        autotiler.ReadTilesetNode(tileset.Xml, into: tileset);
    }
}