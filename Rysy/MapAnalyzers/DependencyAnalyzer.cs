using ImGuiNET;
using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Tools;

namespace Rysy.MapAnalyzers;

public class DependencyAnalyzer : MapAnalyzer {
    public override void Analyze(AnalyzerCtx ctx) {
        if (ctx.Map.Mod is not { } mod) {
            ctx.AddResult(new NotInModResult());
            return;
        }

        var depCtx = DependencyCheker.GetDependencies(ctx.Map);

        var missing = depCtx.FindMissingDependencies(mod).ToListIfNotList();

        foreach (var dep in missing) {
            var sources = depCtx.ModRequirementSources[dep].GroupBy(o => (o.GetType(), GetSourceName(o))).ToList();

            ctx.AddResult(new MissingDepResult(mod, dep, ModRegistry.GetModByName(dep), sources));
        }
    }

    private string GetSourceName(object source) {
        return source switch {
            Style s => $"style:{s.Name}",
            Decal d => $"{d.Name}:{d.Texture}",
            DependencyCheker.MetadataDependency d => $"metadata:{d.FieldName}",
            DependencyCheker.TilesetDependency t => $"{t.Layer}Tileset:{t.Texture}",
            IName name => name.Name,
            _ => source.ToString()!,
        };
    }

    record class NotInModResult() : IAnalyzerResult {
        public LogLevel Level => LogLevel.Info;

        public string Message => "rysy.analyzers.dependency.not_in_mod".Translate();

        public bool AutoFixable => false;

        public void Fix() {
            throw new NotImplementedException();
        }

        public void RenderDetailImgui() {
            
        }
    }

    record class MissingDepResult(ModMeta BaseMod, string DepModName, ModMeta? DepModMeta, List<IGrouping<(Type Type, string Name), object>> Sources) : IAnalyzerResult {
        private bool IsUnknown => DepModName == DependencyCheker.UnknownModName;

        public LogLevel Level => IsUnknown ? LogLevel.Warning : LogLevel.Error;

        public string Message => IsUnknown 
            ? "rysy.analyzers.dependency.missing.unknown".Translate()
            : "rysy.analyzers.dependency.missing".TranslateFormatted(DepModName);

        public bool AutoFixable => !IsUnknown && DepModMeta is { } && BaseMod.Filesystem is FolderModFilesystem;

        private void RenderEntityList(IEnumerable<object> objs) {
            if (!ImGui.BeginTable("Entities", 2, ImGuiManager.TableFlags)) {
                return;
            }

            var textBaseWidth = ImGui.CalcTextSize("A").X;

            ImGui.TableSetupColumn("Room", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 10f);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (Entity obj in objs) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(obj.Room.Name);
                ImGui.TableNextColumn();

                ImGuiManager.PushNullStyle();
                if (RysyEngine.Scene is EditorScene editor && ImGui.Selectable($"Select...##{obj.ID}")) {
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

        public void RenderDetailImgui() {
            if (!IsUnknown)
                ImGui.Text("Used by:");

            foreach (var group in Sources) {
                var open = ImGui.TreeNodeEx(group.Key.Name, ImGuiTreeNodeFlags.SpanFullWidth);

                if (open) {
                    if (group.Key.Type.IsSubclassOf(typeof(Entity))) {
                        RenderEntityList(group);
                    }
                    if (group.Key.Type == typeof(DependencyCheker.MetadataDependency)) {
                        ImGui.Text((group.First() as DependencyCheker.MetadataDependency)?.Value.ToString());
                    }

                    ImGui.TreePop();
                }
            }
        }

        public void Fix() {
            if (DepModMeta is null || BaseMod.Filesystem is not FolderModFilesystem fs)
                return;

            BaseMod.EverestYaml.First().Dependencies.Add(new() {
                Name = DepModMeta.Name,
                Version = DepModMeta.Version,
            });

            var yaml = YamlHelper.Serializer.Serialize(BaseMod.EverestYaml);
            var yamlPath = fs.FileExists("everest.yml") ? $"{fs.Root}/everest.yml" : $"{fs.Root}/everest.yaml";

            if (File.Exists(yamlPath)) {
                File.Move(yamlPath, yamlPath + ".backup", overwrite: true);
            }

            File.WriteAllText(yamlPath, yaml);
        }
    }
}
