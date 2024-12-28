using System.Xml;

namespace Rysy.History;

internal sealed class AddAnimatedTileAction(string xmlContents) : IHistoryAction {
    List<string> createdTiles = [];
    
    public bool Apply(Map map) {
        createdTiles.Clear();
        
        var xml = map.AnimatedTiles.Xml;

        XmlNode? newEl;
        try {
            using var text = new StringReader(xmlContents);
            using var reader = XmlReader.Create(text, new XmlReaderSettings() {
                ConformanceLevel = ConformanceLevel.Fragment
            });
            newEl = xml.ReadNode(reader);
        } catch (Exception e) {
            Logger.Error(e, "Failed to parse animated tile xml");
            return false;
        }
        if (newEl is null)
            return false;

        if (newEl.Name == "Data") {
            foreach (var child in newEl.ChildNodes.OfType<XmlElement>().ToList()) {
                ImportEntry(map, child);
            }
        } else {
            if (!ImportEntry(map, newEl)) return false;
        }
        

        
        map.SaveAnimatedTilesXml();
        
        return true;
    }

    private bool ImportEntry(Map map, XmlNode newEl)
    {
        if (map.AnimatedTiles.ReadXmlNode(newEl) is not { } newTile)
            return false;
        map.AnimatedTiles.Xml.DocumentElement!.AppendChild(newEl);
        createdTiles.Add(newTile.Name);
        return true;
    }

    public void Undo(Map map) {
        foreach (var toRemoveName in createdTiles) {
            map.AnimatedTiles.Remove(toRemoveName);
        }
        
        map.SaveAnimatedTilesXml();
    }
}