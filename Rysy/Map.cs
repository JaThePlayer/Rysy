using KeraLua;
using Rysy.Graphics;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Stylegrounds;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Map : IPackable, ILuaWrapper, IDisposable {
    private static Map _dummyMap;
    
    /// <summary>
    /// An empty map that can be used for mocking
    /// </summary>
    public static Map DummyMap => _dummyMap ??= NewMap("DUMMY");

    /// <summary>
    /// The package name of the map.
    /// </summary>
    public string? Package;

    /// <summary>
    /// The filename of the file this map comes from, if it came from a file.
    /// </summary>
    public string? Filepath {
        get;
        set {
            field = value;
            _modChecked = false;
        }
    }

    public List<Room> Rooms { get; set; } = new();

    public void SortRooms() {
        Rooms.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }

    public MapMetadata Meta {
        get;
        set {
            OnMetaChanged?.Invoke(field, value);
            field = value;
        }
    } = new();

    /// <summary>
    /// (old, new)
    /// </summary>
    public Action<MapMetadata, MapMetadata>? OnMetaChanged { get; set; }

    public AnimatedTileBank AnimatedTiles { get; set; } = new();
    
    public Autotiler BgAutotiler { get; set; } = new();
    public Autotiler FgAutotiler { get; set; } = new();

    public SpriteBank Sprites { get; set; } = new();

    // TODO: make Map IDisposable?
    private IDisposable? _animatedTilesWatcher, _bgTilesWatcher, _fgTilesWatcher, _spritesWatcher;

    public MapStylegrounds Style;
    /// <summary>
    /// Filler rooms. Currently unparsed :(
    /// </summary>
    public BinaryPacker.Element Filler;

    public Dictionary<string, BinaryPacker.Element> UnknownTopLevelElements = [];

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

        room.Fg.SafeSetTile('1', 1, 21);
        room.Fg.SafeSetTile('1', 2, 21);

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
        
        el.Children = [ levels, Meta.Pack(), Style.Pack(), Filler, EditorGroups.Pack(), .. UnknownTopLevelElements.Select(x => x.Value) ];

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
        void Load(Func<MapMetadata?, string?> xmlPathGetter, bool forceRead,
            Action<Stream, ModMeta> readFromXml,
            string errorPopupTitle, string missingFilePopupTitle,
            string vanillaPath, ref IDisposable? watcher) {
            
            watcher?.Dispose();
            watcher = null;
            
            var oldPath = xmlPathGetter(oldMeta);
            var newPath = xmlPathGetter(meta);
            if (oldPath != newPath || newPath.IsNullOrWhitespace() || forceRead) {
                var shouldReadVanilla = true;
            
                if (!newPath.IsNullOrWhitespace()) {
                    shouldReadVanilla = false;
                    if (!ModRegistry.Filesystem.TryWatchAndOpenWithMod(newPath.Unbackslash(), (stream, mod) => {
                            shouldReadVanilla = PopupNotificationWindow.ShowOnException(
                                errorPopupTitle.ToLangKey(newPath),
                                () => readFromXml(stream, mod));
                            Rooms.ForEach(r => r.ClearRenderCacheAggressively());
                        }, out watcher)) {
                        PopupNotificationWindow.Show(missingFilePopupTitle.ToLangKey(newPath));
                        Logger.Write("Autotiler", LogLevel.Error, $"Couldn't find xml '{newPath}'");
                        shouldReadVanilla = true;
                    }
                }

                if (shouldReadVanilla) {
                    ModRegistry.VanillaMod.Filesystem.OpenFile(vanillaPath, (stream) => {
                        readFromXml(stream, ModRegistry.VanillaMod);
                        return true;
                    });
                }
            }
        }
        
        Load(x => x?.AnimatedTiles, forceRead: AnimatedTiles.Empty(),
            readFromXml: (stream, _) => AnimatedTiles.ReadFromXml(stream),
            errorPopupTitle: "rysy.popups.map.animatedTilesError",
            missingFilePopupTitle: "rysy.popups.map.animatedTilesMissing",
            vanillaPath: "Graphics/AnimatedTiles.xml",
            ref _animatedTilesWatcher);

        BgAutotiler.AnimatedTiles = AnimatedTiles;
        FgAutotiler.AnimatedTiles = AnimatedTiles;
        
        Load(x => x?.BackgroundTiles, forceRead: false,
            readFromXml: (stream, _) => BgAutotiler.ReadFromXml(stream),
            errorPopupTitle: "rysy.popups.map.backgroundTilesError",
            missingFilePopupTitle: "rysy.popups.map.backgroundTilesMissing",
            vanillaPath: "Graphics/BackgroundTiles.xml",
            ref _bgTilesWatcher
        );
        
        Load(x => x?.ForegroundTiles, forceRead: false,
            readFromXml: (stream, _) => FgAutotiler.ReadFromXml(stream),
            errorPopupTitle: "rysy.popups.map.foregroundTilesError",
            missingFilePopupTitle: "rysy.popups.map.foregroundTilesMissing",
            vanillaPath: "Graphics/ForegroundTiles.xml",
            ref _fgTilesWatcher
        );
        
        Load(x => x?.Sprites, forceRead: false,
            readFromXml: (stream, mod) => {
                Sprites.Clear();
                Sprites.Load(stream, mod);
            },
            errorPopupTitle: "rysy.popups.map.spritesXmlError",
            missingFilePopupTitle: "rysy.popups.map.spritesXmlMissing",
            vanillaPath: "Graphics/Sprites.xml",
            ref _spritesWatcher
        );
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
                default:
                    UnknownTopLevelElements.TryAdd(child.Name ?? "", child);
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
        if (!BgAutotiler.Loaded) {
            _bgTilesWatcher?.Dispose();
            ModRegistry.Filesystem.TryWatchAndOpen("Graphics/BackgroundTiles.xml", BgAutotiler.ReadFromXml, out _bgTilesWatcher);
        }

        if (!FgAutotiler.Loaded) {
            _fgTilesWatcher?.Dispose();
            ModRegistry.Filesystem.TryWatchAndOpen("Graphics/ForegroundTiles.xml", FgAutotiler.ReadFromXml, out _fgTilesWatcher);
        }
        
        if (!Sprites.Loaded) {
            LoadVanillaSpritesXml();
        }
    }

    private void LoadVanillaSpritesXml() {
        ModRegistry.VanillaMod.Filesystem.TryOpenFile("Graphics/Sprites.xml", s => Sprites.Load(s, ModRegistry.VanillaMod));
        _spritesWatcher?.Dispose();
        _spritesWatcher = ModRegistry.Filesystem.TryWatchAndOpenAll("Graphics/Sprites.xml", Sprites.Load, Sprites.Clear, filter: m => !m.IsVanilla);
    }

    public Room? TryGetRoomByName(string name) => Rooms.FirstOrDefault(r => r.Name == name);

    public Rectangle GetBounds() => RectangleExt.Merge(Rooms.Select(r => r.Bounds));

    public void ClearRenderCache() {
        foreach (var r in Rooms) {
            r.ClearRenderCache();
        }
    }
    
    public void ClearRenderCacheAggressively() {
        foreach (var r in Rooms) {
            r.ClearRenderCacheAggressively();
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
        var autotiler = bg ? BgAutotiler : FgAutotiler;
        var path = bg ? Meta.BackgroundTiles : Meta.ForegroundTiles;
        
        if (path is null || GetModContainingTilesetXml(bg) is not { Filesystem: IWriteableModFilesystem fs })
            return;
        
        using var mem = new MemoryStream();
        autotiler.Xml.Save(mem);
        mem.Seek(0, SeekOrigin.Begin);
        fs.TryWriteToFile(path, mem);
    }

    public void SaveAnimatedTilesXml() {
        var path = Meta.AnimatedTiles;
        var tiles = AnimatedTiles;
        
        if (path is null || ModRegistry.Filesystem.FindFirstModContaining(path) is not { Filesystem: IWriteableModFilesystem fs })
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

    public void Dispose() {
        GC.SuppressFinalize(this);
        
        _animatedTilesWatcher?.Dispose();
        _bgTilesWatcher?.Dispose();
        _fgTilesWatcher?.Dispose();
        _spritesWatcher?.Dispose();
        _animatedTilesWatcher = null;
        _bgTilesWatcher = null;
        _fgTilesWatcher = null;
        _spritesWatcher = null;
    }

    ~Map() {
        Dispose();
    }
}

public sealed record MapMetadata {
    private BinaryPacker.Element _data = new("meta");

    public string Parent {
        get => _data.Attr("parent");
        set => _data.Attributes["parent"] = value;
    }

    public string Icon {
        get => _data.Attr("icon");
        set => _data.Attributes["icon"] = value;
    }

    public bool? Interlude {
        get => _data.Has("interlude") ? _data.Bool("interlude") : null;
        set {
            if (value is null) {
                _data.Attributes.Remove("interlude");
            } else {
                _data.Attributes["interlude"] = value.Value;
            }
        }
    }

    public int? CassetteCheckpointIndex {        
        get => _data.Has("cassetteCheckpointIndex") ? _data.Int("cassetteCheckpointIndex") : null;
        set {
            if (value is null) {
                _data.Attributes.Remove("cassetteCheckpointIndex");
            } else {
                _data.Attributes["cassetteCheckpointIndex"] = value.Value;
            }
        }
    }

    public string TitleBaseColor {
        get => _data.Attr("titleBaseColor", "6c7c81"); 
        set => _data.Attributes["titleBaseColor"] = value;
    }

    public string TitleAccentColor {
        get => _data.Attr("titleAccentColor", "2f344b"); 
        set => _data.Attributes["titleAccentColor"] = value;
    }

    public string TitleTextColor {
        get => _data.Attr("titleTextColor", "ffffff"); 
        set => _data.Attributes["titleTextColor"] = value;
    }

    public string? IntroType {
        get => _data.Attr("introType", null!);
        set => _data.SetNullableObj("introType", value);
    }

    public bool Dreaming {
        get => _data.Bool("dreaming");
        set => _data.Attributes["dreaming"] = value;
    }

    public string? ColorGrade {
        get => _data.Attr("colorGrade", null!);
        set => _data.SetNullableObj("colorGrade", value);
    }

    public string? Wipe {
        get => _data.Attr("wipe", null!);
        set => _data.SetNullableObj("wipe", value);
    }

    public float DarknessAlpha {
        get => _data.Float("darknessAlpha", 0.05f);
        set => _data.Attributes["darknessAlpha"] = value;
    }

    public float BloomBase {
        get => _data.Float("bloomBase", 0f);
        set => _data.Attributes["bloomBase"] = value;
    }

    public float BloomStrength {
        get => _data.Float("bloomStrength", 1f);
        set => _data.Attributes["bloomStrength"] = value;
    }

    public string? Jumpthru {
        get => _data.Attr("jumpthru", null!);
        set => _data.SetNullableObj("jumpthru", value);
    }

    public string? CoreMode {
        get => _data.Attr("coreMode", null!);
        set => _data.SetNullableObj("coreMode", value);
    }

    public string? CassetteNoteColor {
        get => _data.Attr("cassetteNoteColor", null!);
        set => _data.SetNullableObj("cassetteNoteColor", value);
    }

    public string? CassetteSong {
        get => _data.Attr("cassetteSong", null!);
        set => _data.SetNullableObj("cassetteSong", value);
    }

    public string? PostcardSoundId {
        get => _data.Attr("postcardSoundID", null!);
        set => _data.SetNullableObj("postcardSoundID", value);
    }

    public string? ForegroundTiles {
        get => _data.Attr("foregroundTiles", null!);
        set => _data.SetNullableObj("foregroundTiles", value);
    }

    public string? BackgroundTiles {
        get => _data.Attr("backgroundTiles", null!);
        set => _data.SetNullableObj("backgroundTiles", value);
    }

    public string? AnimatedTiles {
        get => _data.Attr("animatedTiles", null!);
        set => _data.SetNullableObj("animatedTiles", value);
    }

    public string Sprites {
        get => _data.Attr("sprites");
        set => _data.SetNullableObj("sprites", value);
    }

    public string Portraits {
        get => _data.Attr("portraits");
        set => _data.SetNullableObj("portraits", value);
    }

    public bool OverrideASideMeta {
        get => _data.Bool("overrideASideMeta");
        set => _data.Attributes["overrideASideMeta"] = value;
    }

    public MetaMode Mode { get; set; } = new();

    public MapMetaCassetteModifier CassetteModifier { get; set; } = new();

    //public MapMetaMountain Mountain { get; set; }

    //public MapMetaCompleteScreen CompleteScreen { get; set; }

    //public MapMetaCompleteScreen LoadingVignetteScreen { get; set; }

    //public MapMetaTextVignette LoadingVignetteText { get; set; }

    public MapMetadata Unpack(BinaryPacker.Element el) {
        _data = el.CreateWithComparer(StringComparer.OrdinalIgnoreCase);
        if (_data.Children.FirstOrDefault(e => e.Name == "mode") is not { } mode) {
            mode = _data.AddChild(new("mode"));
        }
        Mode.Data = mode;
        if (_data.Children.FirstOrDefault(e => e.Name == "cassettemodifier") is not { } modifier) {
            modifier = _data.AddChild(new("cassettemodifier"));
        }
        CassetteModifier.Data = modifier;

        return this;
    }

    public BinaryPacker.Element Pack() {
        return _data.Clone();
    }
}

public sealed class MetaMode {
    public BinaryPacker.Element Data {
        get;
        set {
            field = value;
            if (field.Children.FirstOrDefault(e => e.Name == "audiostate") is not { } audioState) {
                audioState = field.AddChild(new("audiostate"));
            }

            AudioState.Data = audioState;
        }
    } = new("mode");

    public bool? IgnoreLevelAudioLayerData {
        get => Data.NullableBool("ignoreLevelAudioLayerData");
        set => Data.SetNullableStruct("ignoreLevelAudioLayerData", value);
    }

    public string Inventory {
        get => Data.Attr("inventory");
        set => Data.SetNullableObj("inventory", value);
    }

    public string Path {
        get => Data.Attr("path");
        set => Data.SetNullableObj("path", value);
    }

    public string PoemId {
        get => Data.Attr("poemId");
        set => Data.SetNullableObj("poemId", value);
    }

    public string StartLevel {
        get => Data.Attr("startLevel");
        set => Data.SetNullableObj("startLevel", value);
    }

    public bool? HeartIsEnd {
        get => Data.NullableBool("heartIsEnd");
        set => Data.SetNullableStruct("heartIsEnd", value);
    }

    public bool? SeekerSlowdown {
        get => Data.NullableBool("seekerSlowdown");
        set => Data.SetNullableStruct("seekerSlowdown", value);
    }

    public bool? TheoInBubble {
        get => Data.NullableBool("theoInBubble");
        set => Data.SetNullableStruct("theoInBubble", value);
    }

    public AudioState AudioState { get; set; } = new();
}

public class AudioState {
    public BinaryPacker.Element Data { get; set; } = new("audiostate");

    public string Ambience {
        get => Data.Attr("ambience");
        set => Data.SetNullableObj("ambience", value);
    }
    
    public string Music {
        get => Data.Attr("music");
        set => Data.SetNullableObj("music", value);
    }
}

public class MapMetaCassetteModifier {
    public BinaryPacker.Element Data { get; set; } = new("cassettemodifier");
    
    public float TempoMult {
        get => Data.Float("tempoMult", 1f); 
        set => Data.Attributes["tempoMult"] = value;
    }

    public int LeadBeats {
        get => Data.Int("leadBeats", 16); 
        set => Data.Attributes["leadBeats"] = value;
    }

    public int BeatsPerTick {
        get => Data.Int("beatsPerTick", 4); 
        set => Data.Attributes["beatsPerTick"] = value;
    }

    public int TicksPerSwap {
        get => Data.Int("ticksPerSwap", 2); 
        set => Data.Attributes["ticksPerSwap"] = value;
    }

    public int Blocks {
        get => Data.Int("blocks", 2); 
        set => Data.Attributes["blocks"] = value;
    }

    public int BeatsMax {
        get => Data.Int("beatsMax", 256); 
        set => Data.Attributes["beatsMax"] = value;
    }

    public int BeatIndexOffset {
        get => Data.Int("beatIndexOffset", 0); 
        set => Data.Attributes["beatIndexOffset"] = value;
    }

    public bool OldBehavior {
        get => Data.Bool("oldBehavior");
        set => Data.Attributes["oldBehavior"] = value;
    }
}