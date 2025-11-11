using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Mods;
using System.Runtime.InteropServices;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

namespace Rysy.Helpers;

public static class DependencyCheker {
    public const string UnknownModName = "Unknown";

    public class Ctx {
        public Ctx() { }

        public ISet<string> Mods { get; internal set; } = new HashSet<string>();

        public IDictionary<string, IList<object>> ModRequirementSources { get; internal set; } = new Dictionary<string, IList<object>>();

        public IEnumerable<string> FindMissingDependencies(ModMeta targetMod) {
            var deps = targetMod.GetAllDependenciesRecursive().ToListIfNotList();

            foreach (var modName in Mods) {
                if (targetMod.Name == modName) {
                    continue;
                }

                if (deps.Any(dep => dep.Name == modName))
                    continue;

                yield return modName;
            }
        }
    }

    public record TilesetDependency(TileLayer Layer, string Texture) : IName {
        public string Name => $"{Layer}Tileset:{Texture}";
    }

    public record MetadataDependency(string FieldName, object Value);

    public static Ctx GetDependencies(Map map, Ctx? ctx = null) {
        ctx ??= new Ctx();

        if (map == null)
            return ctx;

        var modNames = ctx.Mods;
        var modValues = ctx.ModRequirementSources;

        var meta = map.Meta;
        HandleMetaItem(meta.AnimatedTiles, "AnimatedTiles");
        HandleMetaItem(meta.BackgroundTiles, "BackgroundTiles");
        HandleMetaItem(meta.ForegroundTiles, "ForegroundTiles");
        HandleMetaItem(meta.Sprites, "Sprites");
        HandleMetaItem(meta.Portraits, "Portraits");

        if (!string.IsNullOrWhiteSpace(meta.Icon))
            HandleMetaItem($"Graphics/Atlases/Gui/{meta.Icon}", "Icon");

        foreach (var (_, data) in map.FgAutotiler.Tilesets) {
            HandleTexture(new TilesetDependency(TileLayer.Fg, data.Filename), data.Texture);
        }
        foreach (var (_, data) in map.BgAutotiler.Tilesets) {
            HandleTexture(new TilesetDependency(TileLayer.Bg, data.Filename), data.Texture);
        }

        foreach (var room in map.Rooms) {
            foreach (var item in room.Entities) {
                HandleItem(item, CollectionsMarshal.AsSpan(EntityRegistry.GetAssociatedMods(item)));
            }
            foreach (var item in room.Triggers) {
                HandleItem(item, CollectionsMarshal.AsSpan(EntityRegistry.GetAssociatedMods(item)));
            }
            foreach (Decal decal in room.BgDecals) {
                HandleTexture(decal, decal.GetVirtTexture());
            }
            foreach (Decal decal in room.FgDecals) {
                HandleTexture(decal, decal.GetVirtTexture());
            }
        }

        foreach (var style in map.Style.AllStylesRecursive()) {
            var mods = EntityRegistry.GetAssociatedMods(style);

            HandleItem(style, CollectionsMarshal.AsSpan(mods));
        }

        ctx.Mods = modNames;
        ctx.ModRequirementSources = modValues;

        return ctx;

        void HandleTexture(object item, VirtTexture texture) {
            if (texture is ModTexture modTexture) {
                HandleItem(item, [ modTexture.Mod.Name ]);
            }

            if (texture == Gfx.UnknownTexture) {
                HandleItem(item, [ UnknownModName ]);
            }
        }

        void HandleItem(object item, ReadOnlySpan<string> mods) {
            foreach (var modName in mods) {
                var mod = ModRegistry.GetModByName(modName);
                if (mod?.IsVanilla ?? false)
                    continue;

                modNames.Add(modName);

                if (modValues.TryGetValue(modName, out var values)) {
                    values.Add(item);
                } else {
                    modValues[modName] = [ item ];
                }
            }
        }

        void HandleMetaItem(string? val, string valName) {
            if (string.IsNullOrEmpty(val)) {
                return;
            }

            var mod = ModRegistry.Filesystem.FindFirstModContaining(val);

            HandleItem(new MetadataDependency(valName, val), [ mod?.Name ?? UnknownModName ]);
        }
    }
}
