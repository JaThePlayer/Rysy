new RysyEngine().Run();

/*
using Rysy;
using Rysy.Extensions;
Settings.Load();
Profile.Load();

var mapPath = @$"{Profile.Instance.CelesteDirectory}\\Content\\Maps\\7-Summit.bin";

// print entity counts
var counts = Map.FromFile(mapPath).Rooms.Select(r => new {
    Room = r.Name,
    Entities = r.Entities.Count
});
counts.LogAsJson();*/
