using Rysy.Graphics;

namespace Rysy;

public sealed class Map : IPackable {
    /// <summary>
    /// The package name of the map.
    /// </summary>
    public string? Package;
    /// <summary>
    /// The filename of the file this map comes from, if it came from a file.
    /// </summary>
    public string? Filepath;

    //public Dictionary<string, Room> Rooms { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();

    public MapMetadata Meta = new();

    public Autotiler BGAutotiler = new();
    public Autotiler FGAutotiler = new();

    /// <summary>
    /// Stylegrounds. Currently unparsed :(
    /// </summary>
    public BinaryPacker.Element Style;
    /// <summary>
    /// Filler rooms. Currently unparsed :(
    /// </summary>
    public BinaryPacker.Element Filler;

    private Map() {

    }

    public static Map NewMap(string packageName) {
        var map = new Map() {
            Package = packageName,
        };
        map.UseVanillaTilesetsIfNeeded();
        map.InitStyleAndFillerIfNeeded();

        return map;
    }

    public static Map FromBinaryPackage(BinaryPacker.Package from) {
        var map = new Map();
        map.Unpack(from.Data);
        map.Package = from.Name;
        map.Filepath = from.Filename;

        return map;
    }

    public BinaryPacker.Element Pack() {
        BinaryPacker.Element el = new("Map");

        el.Children = new BinaryPacker.Element[4];

        var levels = new BinaryPacker.Element("levels");
        levels.Children = Rooms.Select(r => r.Pack()).ToArray();


        el.Children[0] = levels;
        el.Children[1] = Meta.Pack();
        el.Children[2] = Style;
        el.Children[3] = Filler;

        return el;
    }

    public BinaryPacker.Package IntoBinary() {
        BinaryPacker.Package pack = new() {
            Name = Package!,
            Filename = Filepath,
            Data = Pack(),
        };


        return pack;
    }

    public void Unpack(BinaryPacker.Element from) {
        foreach (var child in from.Children) {
            if (child is null) {
                Console.WriteLine("Emtpty child in map!");
                continue;
            }
            switch (child.Name) {
                case "meta":
                    Meta.Unpack(child);

                    if (Meta.BackgroundTiles is { } moddedBgTiles) {
                        var cache = ModAssetHelper.GetModFileCache(moddedBgTiles.Unbackslash());
                        if (cache is { }) {
                            BGAutotiler.UseCache(cache);
                        } else {
                            Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find bg tileset xml {moddedBgTiles}");
                        }
                    }

                    if (Meta.ForegroundTiles is { } moddedFgTiles) {
                        var cache = ModAssetHelper.GetModFileCache(moddedFgTiles.Unbackslash());
                        if (cache is { }) {
                            FGAutotiler.UseCache(cache);
                        } else {
                            Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find fg tileset xml {moddedFgTiles}");
                        }
                    }
                    break;
                case "levels":
                    foreach (var room in child.Children) {
                        var r = new Room() {
                            Map = this,
                        };
                        r.Unpack(room);

                        Rooms.Add(r);
                    }

                    break;
                case "Filler":
                    Filler = child;
                    break;
                case "Style":
                    Style = child;
                    break;
            }
        }

        UseVanillaTilesetsIfNeeded();
        InitStyleAndFillerIfNeeded();
    }

    private void InitStyleAndFillerIfNeeded() {
        Style ??= new("Style");
        Filler ??= new("Filler");
    }

    private void UseVanillaTilesetsIfNeeded() {
        if (!BGAutotiler.IsLoaded()) {
            using var stream = File.OpenRead($"{Profile.Instance.CelesteDirectory}/Content/Graphics/BackgroundTiles.xml");
            BGAutotiler.ReadFromXml(stream);
        }
        if (!FGAutotiler.IsLoaded()) {
            using var stream = File.OpenRead($"{Profile.Instance.CelesteDirectory}/Content/Graphics/ForegroundTiles.xml");
            FGAutotiler.ReadFromXml(stream);
        }
    }
}

public sealed class MapMetadata {
    public string Parent { get; set; }

    public string Icon { get; set; }

    public bool? Interlude { get; set; }

    public int? CassetteCheckpointIndex { get; set; }

    public string TitleBaseColor { get; set; }

    public string TitleAccentColor { get; set; }

    public string TitleTextColor { get; set; }

    public string IntroType { get; set; }

    public bool? Dreaming { get; set; } = new bool?(false);

    public string ColorGrade { get; set; }

    public string Wipe { get; set; }

    public float? DarknessAlpha { get; set; }

    public float? BloomBase { get; set; }

    public float? BloomStrength { get; set; }

    public string Jumpthru { get; set; }

    public string CoreMode { get; set; }

    public string CassetteNoteColor { get; set; }

    public string CassetteSong { get; set; }

    public string PostcardSoundID { get; set; }

    public string ForegroundTiles { get; set; }

    public string BackgroundTiles { get; set; }

    public string AnimatedTiles { get; set; }

    public string Sprites { get; set; }

    public string Portraits { get; set; }

    public bool? OverrideASideMeta { get; set; }

    public MetaMode Mode { get; set; } = new();

    public MapMetaCassetteModifier CassetteModifier { get; set; } = new();

    //public MapMetaMountain Mountain { get; set; }

    //public MapMetaCompleteScreen CompleteScreen { get; set; }

    //public MapMetaCompleteScreen LoadingVignetteScreen { get; set; }

    //public MapMetaTextVignette LoadingVignetteText { get; set; }

    Dictionary<string, object> SerializeAttrs<T>(T obj) {
        return typeof(T).GetProperties()
            .Where(p => p.PropertyType.Namespace!.Contains("System"))
            .Select(p => (p, p.GetValue(obj)!))
            .Where(p => p.Item2 is { })
            .ToDictionary(p => p.p.Name, p => p.Item2);
    }

    void Deserialize<T>(BinaryPacker.Element? el, T into) {
        if (el is null)
            return;
        var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase);

        foreach (var (attrName, val) in el.Attributes) {
            var prop = props[attrName];

            if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(float?)) {
                prop.SetValue(into, Convert.ToSingle(val));
            } else {
                prop.SetValue(into, val);
            }


        }
    }

    public MapMetadata Unpack(BinaryPacker.Element el) {
        Deserialize(el, this);
        Deserialize(el.Children.FirstOrDefault(e => e.Name == "cassettemodifier"), CassetteModifier);
        var modes = el.Children.FirstOrDefault(e => e.Name == "mode");
        Deserialize(modes, Mode);
        Deserialize(modes?.Children.FirstOrDefault(e => e.Name == "audiostate"), Mode.AudioState);

        return this;
    }

    public BinaryPacker.Element Pack() {
        BinaryPacker.Element el = new("meta");
        el.Attributes = SerializeAttrs(this);

        el.Children = new BinaryPacker.Element[] {
            new("mode") {
                Attributes = SerializeAttrs(Mode),
                Children = new BinaryPacker.Element[] {
                    new("audiostate") {
                        Attributes = SerializeAttrs(Mode.AudioState)
                    }
                },
            },
            new("cassettemodifier") {
                Attributes = SerializeAttrs(CassetteModifier),
            }
        };

        return el;
    }
}

public sealed class MetaMode {
    public bool? IgnoreLevelAudioLayerData { get; set; }

    public string Inventory { get; set; }

    public string Path { get; set; }

    public string PoemID { get; set; }

    public string StartLevel { get; set; }

    public bool? HeartIsEnd { get; set; }

    public bool? SeekerSlowdown { get; set; }

    public bool? TheoInBubble { get; set; }

    public AudioState AudioState { get; set; } = new();
}

public class AudioState {
    public string Ambience { get; set; }
    public string Music { get; set; }
}

public class MapMetaCassetteModifier {
    public float TempoMult { get; set; } = 1f;

    public int LeadBeats { get; set; } = 16;

    public int BeatsPerTick { get; set; } = 4;

    public int TicksPerSwap { get; set; } = 2;

    public int Blocks { get; set; } = 2;

    public int BeatsMax { get; set; } = 256;

    public int BeatIndexOffset { get; set; }

    public bool OldBehavior { get; set; }
}