using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Stylegrounds;
using Rysy.Tools;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

namespace Rysy.Helpers;

public static class DependencyChecker {
    public const string UnknownModName = "Unknown";

    public class Ctx {
        public Ctx() { }

        public ISet<string> Mods { get; internal set; } = new HashSet<string>();

        public IDictionary<string, IList<object>> ModRequirementSources { get; internal set; } = new Dictionary<string, IList<object>>();

        public IEnumerable<string> FindMissingDependencies(ModMeta targetMod) {
            var includeOptionalDeps = Settings.Instance.CountOptionalDependenciesAsDependencies;
            var deps = targetMod.GetAllDependenciesRecursive(includeOptionalDeps).ToListIfNotList();

            foreach (var modName in Mods) {
                if (targetMod.Name == modName) {
                    continue;
                }

                if (deps.Any(dep => dep.Name == modName))
                    continue;

                yield return modName;
            }
        }

        public IImGuiDrawable GetDrawableDetailsFor(string modName) {
            if (!ModRequirementSources.TryGetValue(modName, out var source)) {
                return new DependencyCheckerDrawable([], modName);
            }

            var sources = source.GroupBy(o => (o.GetType(), GetSourceName(o))).ToList();
            
            return new DependencyCheckerDrawable([.. sources], modName);
        }

        private string GetSourceName(object source) {
            return source switch {
                Style s => $"style:{s.Name}",
                Decal d => $"{d.Name}:{d.Texture}",
                MetadataDependency d => $"metadata:{d.FieldName}",
                TilesetDependency t => $"{t.Layer}Tileset:{t.Texture}",
                IName name => name.Name,
                _ => source.ToString()!,
            };
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

internal class DependencyCheckerDrawable(
    List<IGrouping<(Type Type, string Name), object>> missingDeps,
    string depModName)
    : IImGuiDrawable {
    private bool IsUnknown => depModName == DependencyChecker.UnknownModName;

    private void RenderEntityList(IEnumerable<object> objs) {
        if (!ImGui.BeginTable("Entities", 2, ImGuiManager.TableFlags)) {
            return;
        }

        var textBaseWidth = ImGui.CalcTextSize("A").X;

        ImGui.TableSetupColumn("Room", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 10f);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var i = 0;
        foreach (Entity obj in objs) {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(obj.Room.Name);
            ImGui.TableNextColumn();

            ImGuiManager.PushNullStyle();
            if (RysyEngine.Scene is EditorScene editor && ImGui.Selectable($"Select...##{i++}")) {
                editor.CurrentRoom = obj.Room;
                editor.Camera.CenterOnRealPos(obj.Center + obj.Room.Pos);

                var selectionTool = editor.ToolHandler.SetTool<SelectionTool>();
                if (selectionTool is { }) {
                    selectionTool.Deselect();
                    selectionTool.AddSelection(obj.CreateSelection());
                }

            }
            ImGuiManager.PopNullStyle();
        }

        ImGui.EndTable();
    }

    public void DrawImGui() {
        if (missingDeps.Count == 0) {
            ImGuiManager.TranslatedTextWrapped("rysy.dependencyChecker.noSources");
            return;
        }

        if (!IsUnknown) {
            ImGuiManager.TranslatedText("rysy.dependencyChecker.usedBy");
        }

        foreach (var group in missingDeps) {
            var open = ImGui.TreeNodeEx(group.Key.Name, ImGuiTreeNodeFlags.SpanFullWidth);

            if (open) {
                if (group.Key.Type.IsSubclassOf(typeof(Entity))) {
                    RenderEntityList(group);
                }
                if (group.Key.Type == typeof(DependencyChecker.MetadataDependency)) {
                    ImGui.Text((group.First() as DependencyChecker.MetadataDependency)?.Value.ToString());
                }

                ImGui.TreePop();
            }
        }
    }
}