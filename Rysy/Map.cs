using KeraLua;
using Rysy.Graphics;
using Rysy.Layers;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Stylegrounds;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Map : IPackable, ILuaWrapper {
    private static Map _dummyMap;
    
    /// <summary>
    /// An empty map that can be used for mocking
    /// </summary>
    public static Map DummyMap => _dummyMap ??= NewMap("DUMMY");

    /// <summary>
    /// The package name of the map.
    /// </summary>
    public string? Package;

    private string? _filepath;

    /// <summary>
    /// The filename of the file this map comes from, if it came from a file.
    /// </summary>
    public string? Filepath {
        get => _filepath;
        set {
            _filepath = value;
            _modChecked = false;
        }
    }

    public List<Room> Rooms { get; set; } = new();

    public void SortRooms() {
        Rooms.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }

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

    public AnimatedTileBank AnimatedTiles { get; set; } = new();
    
    public Autotiler BGAutotiler { get; set; } = new();
    public Autotiler FGAutotiler { get; set; } = new();

    public SpriteBank Sprites { get; set; } = new();

    public MapStylegrounds Style;
    /// <summary>
    /// Filler rooms. Currently unparsed :(
    /// </summary>
    public BinaryPacker.Element Filler;

    private ModMeta? _mod;
    private bool _modChecked;

    /// <summary>
    /// Mod from which this map is from.
    /// Null means that the map is either un-packaged, or in a directory outside of the mods folder.
    /// </summary>
    public ModMeta? Mod {
        get {
            if (_modChecked)
                return _mod;

            _mod = ModRegistry.GetModContainingRealPath(Filepath);
            _modChecked = true;
            return _mod;
        }
    }

    public EditorGroupRegistry EditorGroups { get; private set; } = new(EditorGroup.Default);

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

        var room = new Room(map, 40 * 8, 23 * 8) {
            Name = "a_00",
        };
        
        map.Rooms.Add(room);
        
        room.Entities.Add(EntityRegistry.Create(new("player") {
            Attributes = new() {
                ["x"] = 2 * 8,
                ["y"] = 21 * 8,
            },
        }, room, false));

        room.FG.SafeSetTile('1', 1, 21);
        room.FG.SafeSetTile('1', 2, 21);

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

    public static Map? FromFileOrNull(string virtPath, IModFilesystem fs) {
        if (fs.TryOpenFile(virtPath, stream => BinaryPacker.FromBinary(stream, virtPath), out var package)) {
            return FromBinaryPackage(package);
        }

        return null;
    }

    public BinaryPacker.Element Pack() {
        BinaryPacker.Element el = new("Map");

        var levels = new BinaryPacker.Element("levels");
        levels.Children = Rooms.Select(r => r.Pack()).ToArray();

        el.Children = new BinaryPacker.Element[5];
        el.Children[0] = levels;
        el.Children[1] = Meta.Pack();
        el.Children[2] = Style.Pack();
        el.Children[3] = Filler;
        el.Children[4] = EditorGroups.Pack();

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
        if (oldMeta?.AnimatedTiles != meta.AnimatedTiles || meta.AnimatedTiles.IsNullOrWhitespace() || AnimatedTiles.Empty()) {
            var readVanilla = true;
            
            if (meta.AnimatedTiles is { } moddedAnimatedTiles) {
                readVanilla = false;
                if (!ModRegistry.Filesystem.TryWatchAndOpen(moddedAnimatedTiles.Unbackslash(), stream => {
                        AnimatedTiles.ReadFromXml(stream);
                        Rooms.ForEach(r => r.ClearRenderCacheAggressively());
                    })) {
                    Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find animated tile xml {moddedAnimatedTiles}");
                    readVanilla = true;
                }
            }

            if (readVanilla) {
                ModRegistry.VanillaMod.Filesystem.OpenFile("Graphics/AnimatedTiles.xml", (stream) => {
                    AnimatedTiles.ReadFromXml(stream);
                    return true;
                });
            }
        }

        BGAutotiler.AnimatedTiles = AnimatedTiles;
        FGAutotiler.AnimatedTiles = AnimatedTiles;
        
        if (meta.BackgroundTiles is { } moddedBgTiles && oldMeta?.BackgroundTiles != meta.BackgroundTiles) {
            if (!ModRegistry.Filesystem.TryWatchAndOpen(moddedBgTiles.Unbackslash(), stream => {
                    BGAutotiler.ReadFromXml(stream);
                    Rooms.ForEach(r => r.ClearRenderCacheAggressively());
                })) {
                Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find bg tileset xml {moddedBgTiles}");
            }
        }

        if (meta.ForegroundTiles is { } moddedFgTiles && oldMeta?.ForegroundTiles != meta.ForegroundTiles) {
            if (!ModRegistry.Filesystem.TryWatchAndOpen(moddedFgTiles.Unbackslash(), stream => {
                    FGAutotiler.ReadFromXml(stream);
                    Rooms.ForEach(r => r.ClearRenderCacheAggressively());
                })) {
                Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find fg tileset xml {moddedFgTiles}");
            }
        }

        if (meta.Sprites is { } sprites && oldMeta?.Sprites != meta.Sprites) {
            Sprites.Clear();
            LoadVanillaSpritesXml();
            
            if (!ModRegistry.Filesystem.TryWatchAndOpenWithMod(sprites.Unbackslash(), (stream, mod) => {
                    Sprites.Load(stream, mod);
                    Rooms.ForEach(r => r.ClearRenderCacheAggressively());
                })) {
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
                case EditorGroupRegistry.BinaryPackerName:
                    EditorGroups = new();
                    EditorGroups.Unpack(child);
                    break;
            }
        }

        UseVanillaTilesetsIfNeeded();
        InitStyleAndFillerIfNeeded();
    }

    public string? GetDefaultAssetSubdirectory() {
        if (Filepath is null)
            return null;
        if (Mod is not { } mod)
            return null;
        
        return Path.GetDirectoryName(Path.GetRelativePath(Path.Combine(mod.Filesystem.Root, "Maps"), Filepath))!.Unbackslash();
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
        
        if (!Sprites.Loaded) {
            LoadVanillaSpritesXml();
        }
    }

    private void LoadVanillaSpritesXml() {
        ModRegistry.VanillaMod.Filesystem.TryOpenFile("Graphics/Sprites.xml", s => Sprites.Load(s, ModRegistry.VanillaMod));
        ModRegistry.Filesystem.TryWatchAndOpenAll("Graphics/Sprites.xml", Sprites.Load, Sprites.Clear, filter: m => !m.IsVanilla);
    }

    public Room? TryGetRoomByName(string name) => Rooms.FirstOrDefault(r => r.Name == name);

    public Rectangle GetBounds() => RectangleExt.Merge(Rooms.Select(r => r.Bounds));

    public void ClearRenderCache() {
        foreach (var r in Rooms) {
            r.ClearRenderCache();
        }
    }

    public void GroupsChanged() {
        // tell entities about the change
        //todo: make this more specific
        foreach (var e in GetAllEntities()) {
            e.OnChanged(new() {
                ChangedFieldName = Entity.EditorGroupEntityDataKey
            });
        }
        
        //todo: make this more specific
        ClearRenderCache();
    }

    internal IEnumerable<Entity> GetAllEntities() =>
        Rooms.SelectMany(r => r.Entities.Concat(r.Triggers).Concat(r.BgDecals).Concat(r.FgDecals));
    
    internal IEnumerable<Entity> GetEntitiesInGroup(EditorGroup group) => 
        GetAllEntities().Where(e => e.EditorGroups.Contains(group));

    public ModMeta? GetModContainingTilesetXml(bool bg) {
        var path = bg ? Meta.BackgroundTiles : Meta.ForegroundTiles;
        var xmlMod = ModRegistry.Filesystem.FindFirstModContaining(path);

        return xmlMod;
    }
    
    public void SaveTilesetXml(bool bg) {
        var autotiler = bg ? BGAutotiler : FGAutotiler;
        var path = bg ? Meta.BackgroundTiles : Meta.ForegroundTiles;
        
        if (GetModContainingTilesetXml(bg) is not { Filesystem: IWriteableModFilesystem fs })
            return;
        
        using var mem = new MemoryStream();
        autotiler.Xml.Save(mem);
        mem.Seek(0, SeekOrigin.Begin);
        fs.TryWriteToFile(path, mem);
    }

    public void SaveAnimatedTilesXml() {
        var path = Meta.AnimatedTiles;
        var tiles = AnimatedTiles;
        
        if (ModRegistry.Filesystem.FindFirstModContaining(path) is not { Filesystem: IWriteableModFilesystem fs })
            return;
        
        using var mem = new MemoryStream();
        tiles.Xml.Save(mem);
        mem.Seek(0, SeekOrigin.Begin);
        fs.TryWriteToFile(path, mem);
    }
    
    [GeneratedRegex(@"^([\w-]+?)([-_])(\d+)(\w*)$")]
    private static partial Regex StandardRoomNameRegex();

    [GeneratedRegex(@"^(\d+)(\w+)$")]
    private static partial Regex NumberPlusBranchRegex();
    
    public string? GuessNewRoomNameFromParent(Room? parentRoom) {
        if (parentRoom is null)
            return null;
        
        var prevName = parentRoom.Name;
        var map = parentRoom.Map;

        string? newName;
        
        // Room name is just a number
        if (int.TryParse(prevName, CultureInfo.InvariantCulture, out var res)) {
            // 1. Try incrementing the number
            if (Try($"{res + 1}", out newName))
                return newName;
            // 2. Try appending a letter from a-z
            for (char i = 'a'; i <= 'z'; i++) {
                if (Try($"{prevName}{i}", out newName))
                    return newName;
            }
        }

        // Room name is like `1a`
        if (NumberPlusBranchRegex().Match(prevName) is { Success: true } numberPlusBranchMatch) {
            var roomId = int.Parse(numberPlusBranchMatch.Groups[1].ValueSpan, CultureInfo.InvariantCulture);
            var roomBranch = numberPlusBranchMatch.Groups[2].ValueSpan;
            
            // We're already in a branch, we need to extend the branch
            // 1. Try appending a letter from a-z
            for (char i = 'a'; i <= 'z'; i++) {
                if (Try($"{roomId}{roomBranch}{i}", out newName))
                    return newName;
            }
        }
        
        if (StandardRoomNameRegex().Match(prevName) is { Success: true } m) {
            // Room name is in the standard checkpoint-numberBranch format, like 'a-00a'
            var chapterId = m.Groups[1].ValueSpan;
            var separator = m.Groups[2].ValueSpan;
            var roomId = int.Parse(m.Groups[3].ValueSpan, CultureInfo.InvariantCulture);
            var roomBranch = m.Groups[4].ValueSpan;

            if (roomBranch.IsEmpty) {
                // We're not in a branch, so the name is like 'a-00'
                
                // 1. Try incrementing the number
                if (Try($"{chapterId}{separator}{(roomId + 1).ToStringInvariant().PadLeft(m.Groups[3].ValueSpan.Length, '0')}{roomBranch}", out newName))
                    return newName;
                
                // 2. Try appending a letter from a-z
                for (char i = 'a'; i <= 'z'; i++) {
                    if (Try($"{chapterId}{separator}{roomId}{i}", out newName))
                        return newName;
                }
            } else {
                // We're already in a branch, we need to extend the branch
                // 1. Try appending a letter from a-z
                for (char i = 'a'; i <= 'z'; i++) {
                    if (Try($"{chapterId}{separator}{roomId}{roomBranch}{i}", out newName))
                        return newName;
                }
            }
        }

        return null;

        bool Try(string name, [NotNullWhen(true)] out string? nameRet) {
            if (map.TryGetRoomByName(name) is null) {
                nameRet = name;
                return true;
            }
            
            nameRet = null;
            return false;
        }
    }

    public string DeduplicateRoomName(string name) {
        var i = 1;
        var newName = name;
        while (Rooms.Any(r => r.Name == newName)) {
            i++;
            newName = $"{newName}-{i}";
        }

        return newName;
    }

    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "package":
                lua.PushString(Package);
                return 1;
            case "rooms":
                lua.PushWrapper(new WrapperListWrapper<Room>(Rooms));
                return 1;
            // todo: impl when rysy parses fillers.
            //case "fillers":
            //    lua.Push(new WrapperListWrapper<Filler>(Filler));
            //    return 1;
            case "stylesFg":
                lua.PushWrapper(new WrapperListWrapper<Style>(Style.Foregrounds));
                return 1;
            case "stylesBg":
                lua.PushWrapper(new WrapperListWrapper<Style>(Style.Backgrounds));
                return 1;
        }
        
        lua.PushNil();
        return 1;
    }
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

    public string? ColorGrade { get; set; }

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
            if (!props.TryGetValue(attrName, out var prop))
                continue;

            if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(float?)) {
                prop.SetValue(into, Convert.ToSingle(val, CultureInfo.InvariantCulture));
            }
            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?)) {
                prop.SetValue(into, Convert.ToInt32(val, CultureInfo.InvariantCulture));
            }
            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?)) {
                prop.SetValue(into, Convert.ToBoolean(val, CultureInfo.InvariantCulture));
            }
            else if (prop.PropertyType == typeof(string))
                prop.SetValue(into, val.ToStringInvariant());
            else {
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
        BinaryPacker.Element el = new("meta") { 
            Attributes = SerializeAttrs(this),
            Children = [
                new("mode") {
                    Attributes = SerializeAttrs(Mode),
                    Children = [
                        new("audiostate") {
                            Attributes = SerializeAttrs(Mode.AudioState)
                        }
                    ],
                },
                new("cassettemodifier") {
                    Attributes = SerializeAttrs(CassetteModifier),
                }
            ]
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