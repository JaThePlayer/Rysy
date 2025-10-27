using Rysy.History;
using System.Xml.Linq;

namespace Rysy.Scripting.Builtin;

internal sealed class MapToXml : Script {
    public override string Name => "mapToXml";
    
    public override IHistoryAction? Prerun(ScriptArgs args) {
        using var writer = new StringWriter();
        var map = args.Rooms[0].Map;

        var doc = new XDocument();

        var xMap = new XElement("map", 
            new XAttribute("lastEditDate", FormatDate(DateTime.Now)));
        doc.Add(xMap);

        xMap.Add(new XElement("author", 
            new XAttribute("firstName", "Jan"),
            new XAttribute("lastName", "Malenta"),
            new XAttribute("index", "254807")
        ));

        List<string> difficulties = [ "beginner", "intermediate", "advanced", "expert", "grandmaster" ];

        var creationDateBase = DateTime.Today;

        const int roomCount = 20000;
        var rooms = map.Rooms.Take(roomCount).ToList();

        var allEntityTypes = rooms.SelectMany(r => r.Entities)
            .Select(x => x.Name)
            .Distinct()
            .Index()
            .ToDictionary();

        var allEntityTypesInv = allEntityTypes.SafeToDictionary(x => x.Value, x => x.Key);

        var xEntityTypes = new XElement("entityTypes");
        xMap.Add(xEntityTypes);
        foreach (var (id, name) in allEntityTypes) {
            xEntityTypes.Add(new XElement("entityType",
                new XAttribute("id", id),
                name
            ));
        }
        
        var xRooms = new XElement("rooms");
        xMap.Add(xRooms);
        foreach (var (roomId, room) in rooms.Index()) {
            xRooms.Add(new XElement("room", [
                new XAttribute("id", roomId),
                new XAttribute("name", room.Name),
                new XAttribute("creationDate", FormatDate(creationDateBase.AddDays(-Random.Shared.Next(0, 12)))),
                new XAttribute("difficulty", Random.Shared.ChooseFrom(difficulties)),
                .. room.Entities.Select(e => new XElement("entity",
                    new XAttribute("typeId", allEntityTypesInv[e.Name]),
                    new XAttribute("x", e.X),
                    new XAttribute("y", e.Y)
                ))
            ]));
        }
        
        var xPlayers = new XElement("players");
        List<string> playerNames = [ "Jan", "Zbigniew", "Ala", "Ryszard" ];
        xMap.Add(xPlayers);
        for (int i = 0; i < 4; i++) {
            xPlayers.Add(new XElement("player",
                new XAttribute("id", i),
                new XAttribute("currentRoomId", Random.Shared.Next(roomCount)),
                new XElement("name", playerNames[i]),
                new XElement("balance",
                    new XAttribute("currency", "pln"),
                    (Random.Shared.NextSingle() * 1200f).ToString("F2", CultureInfo.CurrentCulture)
                )
            ));
        }
        
        doc.Save(writer);
        Input.Clipboard.Set(writer.ToString());
        
        return null;
    }
    
    private string FormatDate(DateTime d) => d.ToString("d", CultureInfo.CurrentCulture);
}