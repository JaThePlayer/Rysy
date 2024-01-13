#define KERA_LUA
#if !KERA_LUA
using Rysy.NeoLuaSupport;
#endif

using Microsoft.CodeAnalysis;
using Rysy.Entities;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Loading;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Stylegrounds;
using System.Reflection;

namespace Rysy;

public static class EntityRegistry {
    static EntityRegistry() {
        RegisterHardcoded();
    }

    private static ListenableDictionary<string, Type> _SIDToType = new(StringComparer.Ordinal);

    public static ReadOnlyListenableDictionary<string, Type> SIDToType => _SIDToType;

#if KERA_LUA
    private static ListenableDictionary<string, LonnEntityPlugin> _SIDToLonnPlugin { get; set; } = new(StringComparer.Ordinal);

    public static ReadOnlyListenableDictionary<string, LonnEntityPlugin> SIDToLonnPlugin => _SIDToLonnPlugin;
    
    private static ListenableDictionary<string, LonnStylePlugin> _SIDToLonnStylePlugin { get; set; } = new(StringComparer.Ordinal);

    public static ReadOnlyListenableDictionary<string, LonnStylePlugin> SIDToLonnStylePlugin => _SIDToLonnStylePlugin;
#else
    private static Dictionary<string, NeoLonnEntityHandler> SIDToLonnPlugin { get; set; } = new(StringComparer.Ordinal);
    private static Dictionary<string, LonnEntityPlugin> LegacySIDToLonnPlugin = new(StringComparer.Ordinal);
#endif

    private static Dictionary<string, Func<FieldList>> SIDToFields { get; set; } = new(StringComparer.Ordinal);
    private static Dictionary<string, ModMeta> SIDToDefiningMod { get; set; } = new(StringComparer.Ordinal);
    private static Dictionary<string, List<ModMeta>> SIDToAssociatedMods { get; set; } = new(StringComparer.Ordinal);

    public static List<Placement> EntityPlacements { get; set; } = new();
    public static List<Placement> TriggerPlacements { get; set; } = new();
    public static List<Placement> StylegroundPlacements { get; set; } = new();

    private static Dictionary<string, Placement> MainPlacementsForSid { get; set; } = new();
    private static Dictionary<string, Dictionary<string, object>> MainPlacementValuesForSid { get; set; } = new();

    private static LuaCtx? _LuaCtx = null;
    private static LuaCtx LuaCtx => _LuaCtx ??= LuaCtx.CreateNew();

    public const string FGDecalSID = "fgDecal";
    public const string BGDecalSID = "bgDecal";

    public static Placement? GetMainPlacement(string sid) {
        return MainPlacementsForSid.GetValueOrDefault(sid);
    }

    public static IReadOnlyDictionary<string, object> GetMainPlacementValues(string sid) {
        if (MainPlacementValuesForSid.TryGetValue(sid, out var cached))
            return cached;

        var e = CreateFromMainPlacement(sid, default, Room.DummyRoom);
        var values = new Dictionary<string, object>(e.EntityData);
        MainPlacementValuesForSid[sid] = values;

        return values;
    }

    public static FieldList GetFields(Entity entity)
        => GetFields(entity.Name);

    public static FieldList GetFields(string sid) {
        if (SIDToFields.TryGetValue(sid, out var getter)) {
            return getter();
        }

        return new();
    }

    public static Type? GetTypeForSID(string sid) {
        if (SIDToType.TryGetValue(sid, out var type))
            return type;

        return null;
    }

    public static ModMeta? GetDefiningMod(string sid) {
        return SIDToDefiningMod.GetValueOrDefault(sid);
    }

    public static List<string> GetAssociatedMods(Entity entity) {
        return entity.AssociatedMods 
               ?? SIDToAssociatedMods.GetValueOrDefault(entity.Name)?.Select(m => m.Name).ToList() 
               ?? [DependencyCheker.UnknownModName];
    }

    public static List<string> GetAssociatedMods(Style style) {
        return style.AssociatedMods 
               ?? SIDToAssociatedMods.GetValueOrDefault(style.Name)?.Select(m => m.Name).ToList() 
               ?? [DependencyCheker.UnknownModName];
    }

    public static async ValueTask RegisterAsync(bool loadLuaPlugins = true, bool loadCSharpPlugins = true, SimpleLoadTask? task = null) {
        _LuaCtx = null;
        _SIDToType.Clear();
        _SIDToLonnPlugin.Clear();
        SIDToFields.Clear();
        EntityPlacements.Clear();
        TriggerPlacements.Clear();
        StylegroundPlacements.Clear();
        SIDToDefiningMod.Clear();
        SIDToAssociatedMods.Clear();
        MainPlacementValuesForSid.Clear();
        MainPlacementsForSid.Clear();

        RegisterHardcoded();

        const string baseText = "Registering entities:";
        task?.SetMessage(baseText);
        using var watch = new ScopedStopwatch("Registering entities");

        /*
        if (loadLuaPlugins && Settings.Instance.LonnPluginPath is { } path) {
            using var w = new ScopedStopwatch("Registering Lua entities");
            LoadingScene.Text = $"{baseText} Loenn";

            foreach (var item in Directory.EnumerateFiles(Path.Combine(path, "entities"), "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item), trigger: false);
            }

            foreach (var item in Directory.EnumerateFiles(Path.Combine(path, "triggers"), "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item), trigger: true);
            }
        }*/

        foreach (var (_, mod) in ModRegistry.Mods) {
            //task?.SetMessage($"{baseText} {mod.Name}");
            task?.SetMessage(1, mod.Name);

            LoadPluginsFromMod(mod, loadLuaPlugins, loadCSharpPlugins, task);
        }

        ModRegistry.RegisterModAssemblyScanner(ModScanner);
    }

    private static void ModScanner(ModMeta mod, Assembly? oldAsm) {
        if (oldAsm is { }) {
            foreach (var t in GetEntityTypesFromAsm(oldAsm)) {
                foreach (var sid in GetSIDsForType(t)) {
                    if (_SIDToType.TryGetValue(sid, out var typeForThisSid) && typeForThisSid != t) {
                        continue;
                    }
                    _SIDToType.Remove(sid);
                    SIDToDefiningMod.Remove(sid);
                    SIDToAssociatedMods.Remove(sid);
                    EntityPlacements.RemoveAll(pl => !pl.FromLonn && pl.SID == sid);
                    TriggerPlacements.RemoveAll(pl => !pl.FromLonn && pl.SID == sid);
                    StylegroundPlacements.RemoveAll(pl => !pl.FromLonn && pl.SID == sid);
                    MainPlacementsForSid.Remove(sid);
                    MainPlacementValuesForSid.Remove(sid);
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
        if (loadLuaPlugins)
            foreach (var pluginPath in mod.Filesystem.FindFilesInDirectoryRecursive("Loenn", "lua").ToListIfNotList()) {
                if (pluginPath.StartsWith("Loenn/entities", StringComparison.Ordinal)) {
                    task?.SetMessage(2, pluginPath);
                    LoadLuaPluginFromModFile(mod, pluginPath, trigger: false);
                } else if (pluginPath.StartsWith("Loenn/triggers", StringComparison.Ordinal)) {
                    task?.SetMessage(2, pluginPath);
                    LoadLuaPluginFromModFile(mod, pluginPath, trigger: true);
                } else if (pluginPath.StartsWith("Loenn/effects", StringComparison.Ordinal)) {
                    task?.SetMessage(2, pluginPath);
                    LoadLuaEffectPlugin(mod, pluginPath);
                }
            }
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
                if (!HandleAssociatedMods(plugin.Name, Array.Empty<string>(), mod)) {
                    continue;
                }

                _SIDToType[plugin.Name] = typeof(LuaStyle);
                _SIDToLonnStylePlugin[plugin.Name] = plugin;
                
                if (mod is { })
                    SIDToDefiningMod[plugin.Name] = mod;
                if (plugin.FieldList is { })
                    SIDToFields[plugin.Name] = () => plugin.FieldList(Style.FromName("parallax"));

                if (plugin.Placements.FirstOrDefault() is {} firstPlacement)
                    MainPlacementsForSid[plugin.Name] = firstPlacement;
                
                foreach (var pl in plugin.Placements) {
                    pl.SID ??= plugin.Name;

                    StylegroundPlacements.Add(pl);
                }
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

    private static void RegisterHardcoded() {
        _SIDToType[FGDecalSID] = typeof(Decal);
        _SIDToType[BGDecalSID] = typeof(Decal);

        var decalFields = Decal.GetFields();

        SIDToFields[FGDecalSID] = () => decalFields;
        SIDToFields[BGDecalSID] = () => decalFields;
    }

    private static void RegisterFromLua(string lua, string chunkName, bool trigger, ModMeta? mod = null) {
        try {
#if !KERA_LUA
            LuaLoader.CurrentMod = mod?.Name;
            var handlers = NeoLonnEntityHandler.FromLua(lua, chunkName);

            foreach (var pl in handlers) {
                pl.Mod = mod;

                lock (_SIDToType) {
                    _SIDToType[pl.Name] = trigger ? typeof(NeoLonnTrigger) : typeof(NeoLonnEntity);
                }
                lock (SIDToLonnPlugin) {
                    SIDToLonnPlugin[pl.Name] = pl;
                }
            }
#endif

#if KERA_LUA
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

                if (!HandleAssociatedMods(pl.Name, Array.Empty<string>(), mod)) {
                    continue;
                }

                lock (_SIDToType) {
                    _SIDToType[pl.Name] = trigger ? typeof(LonnTrigger) : typeof(LonnEntity);
                }
                lock (_SIDToLonnPlugin) {
                    _SIDToLonnPlugin[pl.Name] = pl;
                }
                if (mod is { } && !mod.IsVanilla)
                    lock (SIDToDefiningMod) {
                        if (!SIDToDefiningMod.ContainsKey(pl.Name))
                            SIDToDefiningMod[pl.Name] = mod;
                    }

                RegisterLuaPlacements(pl.Name, trigger, pl.Placements);

                if (pl.FieldList is { } fields) {
                    lock (SIDToFields) {
                        SIDToFields[pl.Name] = fields;
                    }
                }

                if (RysyEngine.Scene is EditorScene editor) {
                    editor.ClearMapRenderCache();
                }
            }
#endif
        } catch (Exception ex) {
            Logger.Write("EntityRegistry.Lua", LogLevel.Error, $"Failed to register lua entity {chunkName} [{mod?.Name}]: {ex}");
            return;
        }
    }

    internal static void RegisterLuaPlacements(string sid, bool trigger, List<LonnPlacement> placements) {
        var placementsRegistry = trigger ? TriggerPlacements : EntityPlacements;

        lock (placementsRegistry) {
            foreach (var lonnPlacement in placements) {
                var csPlacement = new Placement(lonnPlacement.Name) {
                    ValueOverrides = lonnPlacement.Data,
                    SID = sid,
                    PlacementHandler = trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity,
                    FromLonn = true,
                };

                MainPlacementsForSid.TryAdd(sid, csPlacement);
                
                if (lonnPlacement.AssociatedMods is { } associatedMods)
                    csPlacement = csPlacement.WithAssociatedMods(associatedMods);
                
                placementsRegistry.Add(csPlacement);
            }
        }
    }

    private static IEnumerable<Type> GetEntityTypesFromAsm(Assembly asm)
        => asm.GetTypes().Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(Entity)) || t.IsSubclassOf(typeof(Style))) && t != typeof(UnknownEntity) && t != typeof(Trigger));

    private static List<string> GetSIDsForType(Type type)
        => type.GetCustomAttributes<CustomEntityAttribute>().Select(attr => attr.Name).ToList();

    private static bool HandleAssociatedMods(string sid, string[] associated, ModMeta? mod) {
        if (associated.Length == 0 && mod is { }) {
            if (mod.IsVanilla) {
                lock (SIDToAssociatedMods)
                    SIDToAssociatedMods[sid] = new(0);
                return true;
            }

            associated = new[] { mod.Name };
        }

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

        lock (SIDToAssociatedMods)
            SIDToAssociatedMods[sid] = associated.Select(s => ModRegistry.GetModByName(s)!).ToList();

        return true;
    }

    private static void RegisterType(Type t, CustomEntityAttribute attr, ModMeta? mod = null) {
        var sid = attr.Name;

        if (!HandleAssociatedMods(sid, attr.AssociatedMods, mod))
            return;

        lock (_SIDToType)
            _SIDToType[sid] = t;

        if (mod is { IsVanilla: false })
            lock (SIDToDefiningMod) {
                SIDToDefiningMod.TryAdd(sid, mod);
            }

        var getPlacementsForSIDMethod = t.GetMethod("GetPlacements", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });
        try {
            if (getPlacementsForSIDMethod is { }) {
                var placements = (IEnumerable<Placement>?) getPlacementsForSIDMethod.Invoke(null, new object[] { sid });

                if (placements is { })
                    AddPlacements(t, new() { sid }, placements);
            }
        } catch (Exception e) {
            Logger.Error(e, $"Failed to get placements for entity {sid}");
        }

        var getFieldsMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
        if (getFieldsMethod is { }) {
            var dele = getFieldsMethod.CreateDelegate<Func<FieldList>>();
            SIDToFields[sid] = dele;
        }

        var getFieldsForSIDMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });
        if (getFieldsForSIDMethod is { }) {
            var dele = getFieldsForSIDMethod.CreateDelegate<Func<string, FieldList>>();
            SIDToFields[sid] = () => dele(sid);
        }
    }

    private static void RegisterFrom(Assembly asm, ModMeta? mod = null) {
        foreach (var t in GetEntityTypesFromAsm(asm)) {
            var attrs = t.GetCustomAttributes<CustomEntityAttribute>()
                .Where(attr => HandleAssociatedMods(attr.Name, attr.AssociatedMods, mod))
                .ToList();

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
                RegisterType(t, attr, mod);
            }
        }
    }

    private static void AddPlacements(Type? t, List<string> sids, IEnumerable<Placement> placements) {
        if (t is null)
            return;

        var isTrigger = t.IsSubclassOf(typeof(Trigger));
        var isStyle = t.IsSubclassOf(typeof(Style));

        var plcementList = placements.ToListIfNotList();

        var placementsRegistry = isStyle ? StylegroundPlacements : isTrigger ? TriggerPlacements : EntityPlacements;

        lock (placementsRegistry) {
            foreach (var placement in plcementList) {
                placement.SID ??= sids.Count == 1 ? sids[0] : throw new Exception($"Entity {t} has multiple {typeof(CustomEntityAttribute)} attributes, but its placement {placement.Name} doesn't have the SID field set");
                placement.PlacementHandler = isTrigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity;
                
                MainPlacementsForSid.TryAdd(placement.SID, placement);
            }
            placementsRegistry.AddRange(plcementList);
        }
    }

    public static Dictionary<string, object> GetDataFromPlacement(Placement from) {
        var sid = from.SID ?? throw new NullReferenceException($"Placement.SID is null");
        Dictionary<string, object> data = new(from.ValueOverrides, StringComparer.Ordinal);

        if (SIDToFields.TryGetValue(sid, out var fields)) {
            foreach (var (name, field) in fields()) {
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
        var main = GetMainPlacement(sid);

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
        if (SIDToType.TryGetValue(sid, out var type)) {
            e = Activator.CreateInstance(type) switch {
                Entity ent => ent,
                var other => throw new InvalidCastException($"Cannot convert {other} to {typeof(Entity)}")
            };
        } else {
            if (Settings.Instance?.LogMissingEntities ?? false)
                Logger.Write("EntityRegistry.Create", LogLevel.Warning, $"Unknown entity: {sid}");
            e = trigger ? new Trigger() : new UnknownEntity();
        }

        e.EntityData = entityData;
        e.ID = id ?? room.NextEntityID();
        e.Room = room;
        
#if KERA_LUA
        if (e is LonnEntity lonnEntity) {
            if (!_SIDToLonnPlugin.TryGetValue(sid, out var plugin)) {
                plugin = LonnEntityPlugin.Default(LuaCtx, sid);
                _SIDToLonnPlugin[sid] = plugin;
            }

            lonnEntity.PluginRef = _SIDToLonnPlugin.GetReference(sid);
        }
        if (e is LonnTrigger lonnTrigger) {
            if (!_SIDToLonnPlugin.TryGetValue(sid, out var plugin)) {
                plugin = LonnEntityPlugin.Default(LuaCtx, sid);
                _SIDToLonnPlugin[sid] = plugin;
            }

            lonnTrigger.PluginRef = _SIDToLonnPlugin.GetReference(sid);
        }
#else
        if (e is INeoLonnObject lonnObject) {
            if (!SIDToLonnPlugin.TryGetValue(sid, out var handler)) {
                handler = NeoLonnEntityHandler.Default(sid);
                SIDToLonnPlugin[sid] = handler;
            }
            lonnObject.Handler = handler;
        }

        if (e is LonnEntity lonnEntity) {
            if (!LegacySIDToLonnPlugin.TryGetValue(sid, out var plugin)) {
                plugin = LonnEntityPlugin.Default(LuaCtx, sid);
                LegacySIDToLonnPlugin[sid] = plugin;
            }

            lonnEntity.Plugin = plugin;
        }
        if (e is LonnTrigger lonnTrigger) {
            if (!LegacySIDToLonnPlugin.TryGetValue(sid, out var plugin)) {
                plugin = LonnEntityPlugin.Default(LuaCtx, sid);
                LegacySIDToLonnPlugin[sid] = plugin;
            }

            lonnTrigger.Plugin = plugin;
        }
#endif

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

        var min = e.MinimumSize;

        if (e.ResizableX && e.Width < min.X) {
            e.Width = min.X;
        }

        if (e.ResizableY && e.Height < min.Y) {
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
