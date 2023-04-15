using Rysy;
using Rysy.Extensions;
using Rysy.Mods;
using Rysy.Platforms;
using System.Reflection;

Environment.CurrentDirectory = Path.GetDirectoryName(typeof(RysyEngine).Assembly.Location) ?? Environment.CurrentDirectory;
new RysyEngine().Run();
return;

RysyPlatform.Current.Init();
Logger.Init();
Settings.Load();
Profile.Load();

await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory);


//var str = ModRegistry.GetModByName("FrostHelper")?.Filesystem.ReadAllText("Loenn/entities/bubbler.lua");
//var str = ModRegistry.Filesystem.ReadAllText("Loenn/entities/bubbler.lua");
//Console.WriteLine(str);

//ModRegistry.Filesystem.ReadAllText("Graphics/ForegroundTiles.xml").LogAsJson();

var fs = ModRegistry.GetModByName("FrostHelper")!.Filesystem;

fs.RegisterFilewatch("Loenn/entities/bubbler.lua", new() {
    OnChanged = (Stream stream) => {
        Console.WriteLine(stream.ReadAllText());
    },
});

ModRegistry.Filesystem.RegisterFilewatch("Loenn/scripts/test.lua", new() {
    OnChanged = (Stream stream) => {
        Console.WriteLine(stream.ReadAllText());
        Console.WriteLine("test zip changed");
    },
});

while (true) {
    await Task.Delay(10_000);
}

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
