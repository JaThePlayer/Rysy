using Rysy.Graphics;

namespace Rysy;

public sealed class Map : IPackable
{
    public string? Package;
    public Dictionary<string, Room> Rooms = new();


    public Autotiler BGAutotiler = new();
    public Autotiler FGAutotiler = new();

    public Map()
    {

    }

    public static Map FromBinaryPackage(BinaryPacker.Package from)
    {
        var map = new Map();
        map.Unpack(from.Data);
        map!.Package = from.Name;


        return map;
    }

    public BinaryPacker.Element Pack()
    {
        throw new NotImplementedException();
    }

    public void Unpack(BinaryPacker.Element from)
    {
        //var bgAutotilerPath = $"{Settings.Instance.CelesteDirectory}/Content/Graphics/BackgroundTiles.xml";
        //var fgAutotilerPath = $"{Settings.Instance.CelesteDirectory}/Content/Graphics/ForegroundTiles.xml";

        foreach (var child in from.Children)
        {
            switch (child.Name)
            {
                case "meta":
                    if (child.Attributes.TryGetValue("BackgroundTiles", out var o) && o is string moddedBgTiles)
                    {
                        using var bgStream = ModAssetHelper.OpenModFile(moddedBgTiles.Unbackslash());
                        if (bgStream is { })
                        {
                            BGAutotiler.ReadFromXml(bgStream);
                        }
                        else
                        {
                            Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find bg tileset xml {moddedBgTiles}");
                        }
                    }
                    if (child.Attributes.TryGetValue("ForegroundTiles", out o) && o is string moddedFgTiles)
                    {
                        using var stream = ModAssetHelper.OpenModFile(moddedFgTiles.Unbackslash());
                        if (stream is { })
                        {
                            FGAutotiler.ReadFromXml(stream);
                        }
                        else
                        {
                            Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find fg tileset xml {moddedFgTiles}");
                        }
                    }
                    break;
                case "levels":
                    foreach (var room in child.Children)
                    {
                        var r = new Room()
                        {
                            Map = this,
                        };
                        r.Unpack(room);
                        Rooms[r!.Name] = r;
                    }

                    break;
                case "Filler":
                    Logger.Write("Map.Unpack", LogLevel.Error, "TODO: filler");

                    break;
                case "Style":
                    Logger.Write("Map.Unpack", LogLevel.Error, "TODO: style");

                    break;
            }
        }

        if (!BGAutotiler.IsLoaded())
        {
            using var stream = File.OpenRead($"{Settings.Instance.CelesteDirectory}/Content/Graphics/BackgroundTiles.xml");
            BGAutotiler.ReadFromXml(stream);
        }
        if (!FGAutotiler.IsLoaded())
        {
            using var stream = File.OpenRead($"{Settings.Instance.CelesteDirectory}/Content/Graphics/ForegroundTiles.xml");
            FGAutotiler.ReadFromXml(stream);
        }

        /*
#warning HARDCODED PATH
        using var bgStream = File.OpenRead(bgAutotilerPath);
        BGAutotiler.ReadFromXml(bgStream);
        using var fgStream = File.OpenRead(fgAutotilerPath);
        FGAutotiler.ReadFromXml(fgStream);*/
    }


}
