using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Mods;
using Rysy.Stylegrounds;

namespace Rysy;

public sealed class Map : IPackable {
    /// <summary>
    /// An empty map that can be used for mocking
    /// </summary>
    public static Map DummyMap { get; } = Map.NewMap("DUMMY");

    /// <summary>
    /// The package name of the map.
    /// </summary>
    public string? Package;
    /// <summary>
    /// The filename of the file this map comes from, if it came from a file.
    /// </summary>
    public string? Filepath;

    public List<Room> Rooms { get; set; } = new();

    private MapMetadata _Meta = new();
    public MapMetadata Meta { 
        get => _Meta; 
        set {
            OnMetaChanged?.Invoke(_Meta, value);
            _Meta = value;
        }
    }

    /// <summary>
    /// (old, new)
    /// </summary>
    public Action<MapMetadata, MapMetadata>? OnMetaChanged { get; set; }

    public Autotiler BGAutotiler = new();
    public Autotiler FGAutotiler = new();

    public SpriteBank Sprites = new();

    public MapStylegrounds Style;
    /// <summary>
    /// Filler rooms. Currently unparsed :(
    /// </summary>
    public BinaryPacker.Element Filler;

    public ModMeta? Mod => ModRegistry.GetModContainingRealPath(Filepath);

    private Map() {
        OnMetaChanged += (old, @new) => {
            LoadAutotiler(old, @new);
        };
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

    public static Map FromFile(string filepath) => FromBinaryPackage(BinaryPacker.FromBinary(filepath));

    public BinaryPacker.Element Pack() {
        BinaryPacker.Element el = new("Map");

        var levels = new BinaryPacker.Element("levels");
        levels.Children = Rooms.Select(r => r.Pack()).ToArray();

        el.Children = new BinaryPacker.Element[4];
        el.Children[0] = levels;
        el.Children[1] = Meta.Pack();
        el.Children[2] = Style.Pack();
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

    private void LoadAutotiler(MapMetadata? oldMeta, MapMetadata meta) {
        if (meta.BackgroundTiles is { } moddedBgTiles && oldMeta?.BackgroundTiles != meta.BackgroundTiles) {
            if (!ModRegistry.Filesystem.TryWatchAndOpen(moddedBgTiles.Unbackslash(), BGAutotiler.ReadFromXml)) {
                Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find bg tileset xml {moddedBgTiles}");
            }
        }

        if (meta.ForegroundTiles is { } moddedFgTiles && oldMeta?.ForegroundTiles != meta.ForegroundTiles) {
            if (!ModRegistry.Filesystem.TryWatchAndOpen(moddedFgTiles.Unbackslash(), FGAutotiler.ReadFromXml)) {
                Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find fg tileset xml {moddedFgTiles}");
            }
        }

        if (meta.Sprites is { } sprites && oldMeta?.Sprites != meta.Sprites) {
            if (!ModRegistry.Filesystem.TryWatchAndOpenWithMod(sprites.Unbackslash(), Sprites.Load)) {
                Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find sprites xml {sprites}");
            }
        }
    }

    public void Unpack(BinaryPacker.Element from) {
        foreach (var child in from.Children) {
            if (child is null) {
                Console.WriteLine("Empty child in map!");
                continue;
            }
            switch (child.Name) {
                case "meta":
                    Meta = new MapMetadata().Unpack(child);
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
                    Style = new MapStylegrounds();
                    Style.Unpack(child);
                    break;
            }
        }

        UseVanillaTilesetsIfNeeded();
        InitStyleAndFillerIfNeeded();
    }

    private void InitStyleAndFillerIfNeeded() {
        Style ??= new();
        Filler ??= new("Filler");
    }

    private void UseVanillaTilesetsIfNeeded() {
        if (!BGAutotiler.Loaded) {
            ModRegistry.Filesystem.TryWatchAndOpen("Graphics/BackgroundTiles.xml", BGAutotiler.ReadFromXml);
        }

        if (!FGAutotiler.Loaded) {
            ModRegistry.Filesystem.TryWatchAndOpen("Graphics/ForegroundTiles.xml", FGAutotiler.ReadFromXml);
        }

        ModRegistry.Filesystem.TryWatchAndOpenAll("Graphics/Sprites.xml", Sprites.Load, Sprites.Clear);
        if (!Sprites.Loaded) {
        }
    }

    public Room? TryGetRoomByName(string name) => Rooms.FirstOrDefault(r => r.Name == name);

    public Rectangle GetBounds() => RectangleExt.Merge(Rooms.Select(r => r.Bounds));
}

public sealed record class MapMetadata {
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
            .Where(p => p.PropertyType.Namespace!.Contains("System", StringComparison.Ordinal))
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
                prop.SetValue(into, Convert.ToSingle(val, CultureInfo.InvariantCulture));
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