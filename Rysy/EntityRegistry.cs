using Rysy.Entities;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Loading;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Stylegrounds;
using System.Diagnostics;
using System.Reflection;

namespace Rysy;

public sealed class RegisteredEntity {
    public RegisteredEntity(string sid, RegisteredEntityType type) {
        Sid = sid;
        Type = type;
    }
    
    public string Sid { get; }

    public RegisteredEntityType Type { get; }
    
    public ModMeta? Mod { get; internal set; }

    public List<ModMeta> AssociatedMods { get; internal set; } = [];

    private List<string>? _associatedModNames;
    public List<string> AssociatedModNames => _associatedModNames ??= AssociatedMods.Select(m => m.Name).ToList();
    
    public Type? CSharpType { get; internal set; }
    
    public LonnEntityPlugin? LonnPlugin { get; internal set; }
    
    public LonnStylePlugin? LonnStylePlugin { get; internal set; }

    public Func<FieldList> Fields { get; internal set; } = () => new();

    private Placement? _mainPlacement;

    public Placement? MainPlacement {
        get => _mainPlacement;
        internal set {
            _mainPlacement = value;
            CachedMainPlacementValues = null;
        }
    }

    public List<Placement> Placements { get; } = [];
    
    internal Dictionary<string, object>? CachedMainPlacementValues { get; set; }

    public static RegisteredEntity UnknownEntity(string sid, RegisteredEntityType type) => new(sid, type) {
        CSharpType = type == RegisteredEntityType.Trigger ? typeof(Trigger) : typeof(UnknownEntity)
    };
}

[Flags]
public enum RegisteredEntityType {
    Entity = 1,
    Trigger = 2,
    Style = 4,
    DecalRegistryProperty = 8,
}

public static class EntityRegistry {
    static EntityRegistry() {
        RegisterHardcoded();
    }

    public static IEnumerable<Placement> EntityPlacements
        => Registered
            .Where(kv => kv.Value.Type == RegisteredEntityType.Entity)
            .SelectMany(kv => kv.Value.Placements);
    
    public static IEnumerable<Placement> TriggerPlacements
        => Registered
            .Where(kv => kv.Value.Type == RegisteredEntityType.Trigger)
            .SelectMany(kv => kv.Value.Placements);
    
    public static IEnumerable<Placement> StylegroundPlacements
        => RegisteredStyles
            .Where(kv => kv.Value.Type == RegisteredEntityType.Style)
            .SelectMany(kv => kv.Value.Placements);
    
    public static IEnumerable<Placement> DecalRegistryPropertyPlacements
        => RegisteredDecalRegistryProperties
            .Where(kv => kv.Value.Type == RegisteredEntityType.DecalRegistryProperty)
            .SelectMany(kv => kv.Value.Placements);

    public static ListenableDictionary<string, RegisteredEntity> Registered { get; } = new(StringComparer.Ordinal);
    public static ListenableDictionary<string, RegisteredEntity> RegisteredStyles { get; } = new(StringComparer.Ordinal);
    
    public static ListenableDictionary<string, RegisteredEntity> RegisteredDecalRegistryProperties { get; } = new(StringComparer.Ordinal);

    public static ListenableDictionary<string, RegisteredEntity> GetRegistryFor(RegisteredEntityType type) {
        return type switch {
            RegisteredEntityType.Style => RegisteredStyles,
            RegisteredEntityType.DecalRegistryProperty => RegisteredDecalRegistryProperties,
            _ => Registered,
        };
    }
    
    private static LuaCtx? _LuaCtx = null;
    private static LuaCtx LuaCtx => _LuaCtx ??= LuaCtx.CreateNew();

    public const string FGDecalSID = "fgDecal";
    public const string BGDecalSID = "bgDecal";

    public static RegisteredEntity? GetInfo(string sid, RegisteredEntityType type) => GetRegistryFor(type).TryGetValue(sid, out var ret) ? ret : null;

    public static RegisteredEntity? GetInfo(Entity e) => GetInfo(e.Name,
        e is Trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity);
    
    private static RegisteredEntity GetOrCreateInfo(string sid, RegisteredEntityType expectedType) {
        var reg = GetRegistryFor(expectedType);
        if (reg.TryGetValue(sid, out var existing))
            return existing;

        var newEntity = new RegisteredEntity(sid, expectedType);
        reg[sid] = newEntity;

        return newEntity;
    }
    
    public static Placement? GetMainPlacement(string sid, RegisteredEntityType type) {
        return GetInfo(sid, type)?.MainPlacement;
    }

    public static IReadOnlyDictionary<string, object> GetMainPlacementValues(string sid, RegisteredEntityType type) {
        var info = GetInfo(sid, type);
        if (info is null)
            return new Dictionary<string, object>();

        if (info.CachedMainPlacementValues is { } cached) {
            return cached;
        }

        var e = CreateFromMainPlacement(sid, default, Room.DummyRoom);
        var values = new Dictionary<string, object>(e.EntityData);
        info.CachedMainPlacementValues = values;

        return values;
    }

    public static FieldList GetFields(Entity entity)
        => GetFields(entity.Name, entity is Trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity);

    public static FieldList GetFields(string sid, RegisteredEntityType type) {
        var info = GetInfo(sid, type);
        if (info is null)
            return new();

        return info.Fields();
    }

    public static Type? GetTypeForSID(string sid, RegisteredEntityType type) {
        return GetInfo(sid, type)?.CSharpType;
    }

    public static ModMeta? GetDefiningMod(string sid, RegisteredEntityType type) {
        return GetInfo(sid, type)?.Mod;
    }

    public static List<string> GetAssociatedMods(Entity entity) {
        return entity.AssociatedMods
               ?? GetInfo(entity)?.AssociatedModNames
               ?? [DependencyCheker.UnknownModName];
    }

    public static List<string> GetAssociatedMods(Style style) {
        return style.AssociatedMods 
               ?? GetInfo(style.Name, RegisteredEntityType.Style)?.AssociatedModNames
               ?? [DependencyCheker.UnknownModName];
    }

    public static async ValueTask RegisterAsync(bool loadLuaPlugins = true, bool loadCSharpPlugins = true, SimpleLoadTask? task = null) {
        Registered.Clear();
        RegisteredStyles.Clear();
        RegisteredDecalRegistryProperties.Clear();
        
        RegisterHardcoded();

        const string baseText = "Registering entities:";
        task?.SetMessage(baseText);
        using var watch = new ScopedStopwatch("Registering entities");

        foreach (var (_, mod) in ModRegistry.Mods) {
            task?.SetMessage(1, mod.Name);

            LoadPluginsFromMod(mod, loadLuaPlugins, loadCSharpPlugins, task);
        }

        ModRegistry.RegisterModAssemblyScanner(ModScanner);
    }

    private static void ModScanner(ModMeta mod, Assembly? oldAsm) {
        if (oldAsm is { }) {
            foreach (var (t, rt) in GetEntityTypesFromAsm(oldAsm)) {
                foreach (var sid in GetSIDsForType(t)) {
                    var info = GetInfo(sid, rt);
                    if (info is null)
                        continue;
                    
                    if (info.CSharpType != t) {
                        continue;
                    }
                    
                    if (info.LonnPlugin is not {} && info.LonnStylePlugin is not {})
                        Registered.Remove(sid);
                    else {
                        // If the entity has a lonn plugin, we shouldn't unregister the entire entity
                        // just because the c# plugin is getting removed
                        info.CSharpType = info.Type switch {
                            RegisteredEntityType.Entity => typeof(LonnEntity),
                            RegisteredEntityType.Trigger => typeof(LonnTrigger),
                            RegisteredEntityType.Style => typeof(Style),
                            RegisteredEntityType.DecalRegistryProperty => typeof(DecalRegistryProperty),
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }
                }
            }
        }

        if (mod.PluginAssembly is { } asm) {
            RegisterFrom(asm, mod);
        }

        // perform a quick reload to make entities use their new C# type.
        if (RysyEngine.Scene is EditorScene editor) {
            editor.QuickReload();
        }
    }

    private static void LoadPluginsFromMod(ModMeta mod, bool loadLuaPlugins, bool loadCSharpPlugins, SimpleLoadTask? task) {
        if (!loadLuaPlugins)
            return;

        //var tasks = new List<Task>();
        
        foreach (var pluginPath in mod.Filesystem.FindFilesInDirectoryRecursive("Loenn", "lua").ToListIfNotList()) {
            if (pluginPath.StartsWith("Loenn/entities", StringComparison.Ordinal)) {
                task?.SetMessage(2, pluginPath);
                LoadLuaPluginFromModFile(mod, pluginPath, trigger: false);
                //tasks.Add(Task.Run(() => LoadLuaPluginFromModFile(mod, pluginPath, trigger: false)));
            } else if (pluginPath.StartsWith("Loenn/triggers", StringComparison.Ordinal)) {
                task?.SetMessage(2, pluginPath);
                LoadLuaPluginFromModFile(mod, pluginPath, trigger: true);
                //tasks.Add(Task.Run(() => LoadLuaPluginFromModFile(mod, pluginPath, trigger: true)));
            } else if (pluginPath.StartsWith("Loenn/effects", StringComparison.Ordinal)) {
                task?.SetMessage(2, pluginPath);
                LoadLuaEffectPlugin(mod, pluginPath);
            }
        }

        //Task.WhenAll(tasks).Wait();
    }

    private static void LoadLuaEffectPlugin(ModMeta mod, string pluginPath) {
        mod.Filesystem.TryWatchAndOpen(pluginPath, stream => {
            var plugin = stream.ReadAllText();

            RegisterStyleFromLua(plugin, pluginPath, mod);
        });
    }

    private static void RegisterStyleFromLua(string lua, string chunkName, ModMeta? mod = null) {
        try {
            LuaCtx.Lua.SetCurrentModName(mod);
            LuaCtx.Lua.PCallStringThrowIfError(lua, chunkName, results: 1);
            var plugins = LonnStylePlugin.FromCtx(LuaCtx);

            foreach (var plugin in plugins) {
                var info = GetOrCreateInfo(plugin.Name, RegisteredEntityType.Style);
                
                if (!HandleAssociatedMods(info, Array.Empty<string>(), mod)) {
                    continue;
                }
                
                info.CSharpType = typeof(LuaStyle);
                info.LonnStylePlugin = plugin;
                info.Mod = mod;
                if (plugin.FieldList is { })
                    info.Fields = () => plugin.FieldList(Style.FromName("parallax"));
                if (plugin.Placements.FirstOrDefault() is { } firstPlacement) {
                    info.MainPlacement = firstPlacement;
                }
                
                foreach (var pl in plugin.Placements) {
                    pl.SID ??= plugin.Name;
                    pl.RegisteredEntityType = RegisteredEntityType.Style;

                    info.Placements.Add(pl);
                }

                Registered[info.Sid] = info;
            }
        } catch (Exception ex) {
            Logger.Write("EntityRegistry.Lua", LogLevel.Error, $"Failed to register lua style {chunkName} [{mod?.Name}]: {ex}");
            return;
        }
    }

    private static void LoadLuaPluginFromModFile(ModMeta mod, string pluginPath, bool trigger) {
        mod.Filesystem.TryWatchAndOpen(pluginPath, stream => {
            var plugin = stream.ReadAllText();

            RegisterFromLua(plugin, pluginPath, trigger, mod);
        });
    }

    private static void Register(RegisteredEntity e) {
        lock (Registered)
            Registered[e.Sid] = e;
    }

    private static void RegisterHardcoded() {
        var decalFields = Decal.GetFields();
        Register(CreateDecalInfo(FGDecalSID));
        Register(CreateDecalInfo(BGDecalSID));

        RegisteredEntity CreateDecalInfo(string sid) => new(sid, RegisteredEntityType.Entity) {
            CSharpType = typeof(Decal),
            Fields = () => decalFields,
        };
    }

    private static void RegisterFromLua(string lua, string chunkName, bool trigger, ModMeta? mod = null) {
        try {
            LuaCtx.Lua.SetCurrentModName(mod);
            LuaCtx.Lua.PCallStringThrowIfError(lua, chunkName, results: 1);
            List<LonnEntityPlugin>? plugins = null;

            try {
                plugins = LonnEntityPlugin.FromCtx(LuaCtx);
            } finally {
                LuaCtx.Lua.Pop(1);
            }

            foreach (var pl in plugins) {
                pl.ParentMod = mod;
                var info = GetOrCreateInfo(pl.Name, trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity);

                if (!HandleAssociatedMods(info, Array.Empty<string>(), mod)) {
                    continue;
                }

                if (info.CSharpType is not {} prev || prev.IsAssignableTo(typeof(IHasLonnPlugin)))
                    info.CSharpType = trigger ? typeof(LonnTrigger) : typeof(LonnEntity);
                info.LonnPlugin = pl;
                if (mod is { IsVanilla: false }) { // if (!SIDToDefiningMod.ContainsKey(pl.Name))
                    info.Mod = mod;
                }

                RegisterLuaPlacements(info, trigger, pl.Placements);

                if (pl.FieldList is { } fields) {
                    info.Fields = fields;
                }

                Register(info);

                if (RysyEngine.Scene is EditorScene editor) {
                    editor.ClearMapRenderCache();
                }
            }
        } catch (Exception ex) {
            Logger.Write("EntityRegistry.Lua", LogLevel.Error, $"Failed to register lua entity {chunkName} [{mod?.Name}]: {ex}");
            return;
        }
    }

    internal static void RegisterLuaPlacements(RegisteredEntity into, bool trigger, List<LonnPlacement> placements) {
        var placementsRegistry = trigger ? TriggerPlacements : EntityPlacements;

        lock (placementsRegistry) {
            if (into.MainPlacement is { FromLonn: true })
                into.MainPlacement = null;
            foreach (var placement in into.Placements.Where(p => p.FromLonn).ToList()) {
                into.Placements.Remove(placement);
            }
            
            foreach (var lonnPlacement in placements) {
                var csPlacement = new Placement(lonnPlacement.Name) {
                    ValueOverrides = lonnPlacement.Data,
                    SID = into.Sid,
                    PlacementHandler = trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity,
                    RegisteredEntityType = trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity,
                    FromLonn = true,
                };

                into.MainPlacement ??= csPlacement;
                
                if (lonnPlacement.AssociatedMods is { } associatedMods)
                    csPlacement = csPlacement.WithAssociatedMods(associatedMods);
                
                into.Placements.Add(csPlacement);
            }
        }
    }

    private static IEnumerable<(Type, RegisteredEntityType)> GetEntityTypesFromAsm(Assembly asm)
        => asm.GetTypes()
            .Where(t => !t.IsAbstract 
                        && (t.IsSubclassOf(typeof(Entity)) || t.IsSubclassOf(typeof(Style)) || t.IsSubclassOf(typeof(DecalRegistryProperty))) && t != typeof(UnknownEntity) && t != typeof(Trigger))
            .Select(t => (t, CSharpToRegisteredType(t)));

    private static List<string> GetSIDsForType(Type type)
        => type.GetCustomAttributes<CustomEntityAttribute>().Select(attr => attr.Name).ToList();
    
    private static bool HandleAssociatedMods(RegisteredEntity into, IList<string> associated, ModMeta? mod) {
        if (associated.Count == 0 && mod is { }) {
            if (mod.IsVanilla) {
                into.AssociatedMods = [];
                return true;
            }

            associated = new[] { mod.Name };
        }

        var sid = into.Sid;
        
        var missingAssociatedMods = associated.Where(s => ModRegistry.GetModByName(s) is null).ToList();
        if (missingAssociatedMods.Count > 0) {
            Logger.Write("EntityRegistry", LogLevel.Info, $"Not loading entity {sid}, as the following associated mods are not loaded: {string.Join(',', missingAssociatedMods)}");
            return false;
        }

        if (mod is { IsVanilla: true } 
        && associated.FirstOrDefault(x => ModRegistry.GetModByName(x) is { IsVanilla: false, PluginAssembly: { } }) is { } associatedModWithRysyPlugins) {
            Logger.Write("EntityRegistry", LogLevel.Info, $"Not loading entity {sid} from Rysy, as {associatedModWithRysyPlugins} contains Rysy plugins already.");
            return false;
        }
        
        into.AssociatedMods = associated.Select(s => ModRegistry.GetModByName(s)!).ToList();

        return true;
    }

    private static void RegisterType(Type t, RegisteredEntityType rt, CustomEntityAttribute attr, ModMeta? mod = null) {
        var sid = attr.Name;

        var info = GetOrCreateInfo(sid, rt);
        info.CSharpType = t;

        if (!HandleAssociatedMods(info, attr.AssociatedMods, mod))
            return;

        if (mod is { IsVanilla: false }) {
            info.Mod = mod;
        }

        var getPlacementsForSIDMethod = t.GetMethod("GetPlacements", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });
        try {
            if (getPlacementsForSIDMethod is { }) {
                var placements = (IEnumerable<Placement>?) getPlacementsForSIDMethod.Invoke(null, new object[] { sid });

                if (placements is { })
                    AddPlacements(t, [sid], placements);
            }
        } catch (Exception e) {
            Logger.Error(e, $"Failed to get placements for entity {sid}");
        }

        var getFieldsMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
        if (getFieldsMethod is { }) {
            var dele = getFieldsMethod.CreateDelegate<Func<FieldList>>();
            info.Fields = dele;
        }

        var getFieldsForSIDMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });
        if (getFieldsForSIDMethod is { }) {
            var dele = getFieldsForSIDMethod.CreateDelegate<Func<string, FieldList>>();
            info.Fields = () => dele(sid);
        }
    }

    private static void RegisterFrom(Assembly asm, ModMeta? mod = null) {
        foreach (var (t, rt) in GetEntityTypesFromAsm(asm)) {
            var attrs = t.GetCustomAttributes<CustomEntityAttribute>()
                .Where(attr => HandleAssociatedMods(GetOrCreateInfo(attr.Name, rt), attr.AssociatedMods, mod))
                .ToList();
            
            if (attrs.Count == 0)
                continue;

            var getPlacementsMethod = t.GetMethod("GetPlacements", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            try {
                if (getPlacementsMethod is { }) {
                    var placements = (IEnumerable<Placement>?) getPlacementsMethod.Invoke(null, null);

                    if (placements is { })
                        AddPlacements(t, attrs.Select(a => a.Name).ToList(), placements);
                }
            } catch (Exception e) {
                Logger.Error(e, $"Failed to get placements for entity {t.FullName}");
            }

            foreach (var attr in attrs) {
                RegisterType(t, rt, attr, mod);
            }
        }
    }

    private static RegisteredEntityType CSharpToRegisteredType(Type t) =>
        t.IsSubclassOf(typeof(Style)) ? RegisteredEntityType.Style :
        t.IsSubclassOf(typeof(Trigger)) ? RegisteredEntityType.Trigger :
        t.IsSubclassOf(typeof(DecalRegistryProperty)) ? RegisteredEntityType.DecalRegistryProperty :
        RegisteredEntityType.Entity;

    private static void AddPlacements(Type? t, List<string> sids, IEnumerable<Placement> placements) {
        if (t is null)
            return;

        var rt = CSharpToRegisteredType(t);

        var plcementList = placements.ToListIfNotList();
        
        
        foreach (var placement in plcementList) {
            placement.SID ??= sids.Count == 1 ? sids[0] : throw new Exception($"Entity {t} has multiple {typeof(CustomEntityAttribute)} attributes, but its placement {placement.Name} doesn't have the SID field set");
            placement.PlacementHandler = rt == RegisteredEntityType.Trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity;
            placement.RegisteredEntityType = rt;
            
            var info = GetOrCreateInfo(placement.SID, rt);
            info.MainPlacement ??= placement;
            info.Placements.Add(placement);
        }
    }

    public static Dictionary<string, object> GetDataFromPlacement(Placement from) {
        var sid = from.SID ?? throw new NullReferenceException($"Placement.SID is null");
        Dictionary<string, object> data = new(from.ValueOverrides, StringComparer.Ordinal);

        if (GetFields(sid, from.RegisteredEntityType) is {} fields) {
            foreach (var (name, field) in fields) {
                if (!data.ContainsKey(name))
                    data.Add(name, field.GetDefault());
            }
        }

        data = data.Where(kv => kv.Value is { }).ToDictionary(StringComparer.Ordinal);

        return data;
    }

    public static Entity Create(Placement from, Vector2 pos, Room room, bool assignID, bool isTrigger) {
        ArgumentNullException.ThrowIfNull(from);

        var sid = from.SID ?? throw new ArgumentException($"Placement.SID is null");
        var data = GetDataFromPlacement(from);

        var entity = CreateCore(sid, pos, assignID ? null : -1, new(sid, data, from.Nodes), room, isTrigger,
            fromBinary: false);

        from.Finalizer?.Invoke(entity);

        var minimumNodes = entity.NodeLimits.Start.Value;
        if (minimumNodes > 0 && entity.Nodes is { Count: 0 }) {
            var nodes = entity.EntityData.Nodes;
            for (int i = 0; i < minimumNodes; i++) {
                nodes.Add(new(0, 0));
            }

            entity.InitializeNodePositions();
        }

        return entity;
    }

    public static Entity Create(BinaryPacker.Element from, Room room, bool trigger) {
        ArgumentNullException.ThrowIfNull(from);

        var sid = from.Name ?? throw new ArgumentException($"Entity SID is null in entity element???");

        return CreateCore(sid, new(from.Int("x"), from.Int("y")), from.Int("id"), new(sid, from), room, trigger,
            fromBinary: true);
    }

    public static Entity CreateFromMainPlacement(string sid, Vector2 pos, Room room, Dictionary<string, object>? overrides = null, bool isTrigger = false) {
        var main = GetMainPlacement(sid, isTrigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity);

        if (main is { }) {
            var entity = Create(main, pos, room, true, main.IsTrigger());
            if (overrides is { }) {
                foreach (var (key, value) in overrides) {
                    entity.EntityData[key] = value;
                }
            }

            return entity;
        }

        return CreateCore(sid, pos, null, new(sid, overrides ?? new()), room, isTrigger, 
            fromBinary: false);
    }

    private static Entity CreateCore(string sid, Vector2 pos, int? id, EntityData entityData, Room room, bool trigger,
        bool fromBinary) {
        Entity e;

        var info = GetInfo(sid, trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity);
        if (info?.CSharpType is null) {
            if (Settings.Instance?.LogMissingEntities ?? false)
                Logger.Write("EntityRegistry.Create", LogLevel.Warning, $"Unknown entity: {sid}");
            info = RegisteredEntity.UnknownEntity(sid, trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity);
            if (info.CSharpType is null)
                throw new UnreachableException($"{nameof(UnknownEntity)} returned null CSharpType");
        }
        
        e = Activator.CreateInstance(info.CSharpType) switch {
            Entity ent => ent,
            var other => throw new InvalidCastException($"Cannot convert {other} to {typeof(Entity)}")
        };

        e.EntityData = entityData;
        e.Id = id ?? room.NextEntityID();
        e.Room = room;

        if (e is IHasLonnPlugin hasLonnPlugin) {
            info.LonnPlugin ??= LonnEntityPlugin.Default(LuaCtx, sid);
            hasLonnPlugin.LonnPluginRef = Registered.GetReference(sid);
        }

        if (entityData.Attr(Entity.EditorGroupEntityDataKey, "") is not { } gr || gr.IsNullOrWhitespace()) {
            if (fromBinary) {
                e.EntityData[Entity.EditorGroupEntityDataKey] = EditorGroup.Default.Name;
            } else {
                if (e.Room.Map?.EditorGroups is { } currentGroups) {
                    e.EntityData[Entity.EditorGroupEntityDataKey] = currentGroups.CloneWithOnlyEnabled().ToString();
                }
            }
        }

        e.Pos = pos;

        var min = e.RecommendedMinimumSize;

        if (e.ResizableX && e.Width < min.X && !entityData.Has("width")) {
            e.Width = min.X;
        }

        if (e.ResizableY && e.Height < min.Y && !entityData.Has("height")) {
            e.Height = min.Y;
        }

        if (e is Decal d) {
            d.FG = sid == FGDecalSID;

            d.OnCreated();
        }
        
        e.OnChanged(new EntityDataChangeCtx { 
            AllChanged = true,
        });
        entityData.OnChanged += e.OnChanged;

        return e;
    }
}
