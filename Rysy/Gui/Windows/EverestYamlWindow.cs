using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Signals;

namespace Rysy.Gui.Windows;

public class EverestYamlWindow : Window, ISignalListener<MapSwapped>, ISignalListener<HistoryChanged> {
    private DependencyChecker.Ctx? _dependencyCheckerCtx;
    private readonly EditorState _state;
    private List<string>? _deps;
    private List<ModMeta>? _available;
    private List<string> _required;
    private readonly StringField _versionInput;
    private string? _versionString;
    
    public EverestYamlWindow(EditorState state) : base("rysy.everestyaml.window.name".Translate(), new(600, 500)) {
        _state = state;
            
        _versionInput = new StringField().WithValidator(
            s => Version.TryParse(s, out _) ? 
                ValidationResult.Ok :
                ValidationResult.InvalidVersion
        ).Translated("rysy.everestyaml.version");

        RecalculateDependencies();
    }
        

    protected override void Render() {
        if (_state.Map is not { Mod: { EverestYaml: [{ } yaml, ..] } mod } map) {
            return;
        }

        ImGui.SetNextItemWidth(FormWindow.ItemWidth);
        if (_versionInput.RenderGuiWithValidation(_versionString ??= yaml.VersionString, out var isValid) is string newVersion) {
            _versionString = newVersion;
            if (isValid.IsOk) {
                yaml.VersionString = newVersion;
                _state.Map?.Mod?.TrySaveEverestYaml(); 
            }
        }
            
            
        if (!ImGui.BeginTable("Dependencies", 4, ImGuiManager.TableFlags | ImGuiTableFlags.ScrollY)) {
            return;
        }
            
        var textBaseWidth = ImGui.CalcTextSize("A").X;
            
        ImGuiManager.TranslatedTableSetupColumn("rysy.everestyaml.dependencies");
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, textBaseWidth * 3f);
            
            
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, textBaseWidth * 3f);
        ImGuiManager.TranslatedTableSetupColumn("rysy.everestyaml.available");
        ImGui.TableHeadersRow();
            
        if (_deps is null || _available is null || _dependencyCheckerCtx is null)
            return;

        var id = 0;

        for (int i = 0; i < Math.Max(_deps.Count, _available.Count); i++) {
            if (i < _deps.Count) {
                var dependencyName = _deps[i];
                var dependency = ModRegistry.GetModByName(dependencyName);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                
                var open = ImGui.TreeNodeEx(dependency?.DisplayName ?? dependencyName, ImGuiTreeNodeFlags.SpanFullWidth);
                if (open) {
                    _dependencyCheckerCtx.GetDrawableDetailsFor(dependencyName).DrawImGui();

                    ImGui.TreePop();
                }
                if (dependency is not null) {
                    ImGuiManager.TextDisabled($"({dependency.Name} v{dependency.Version})");
                } else {
                    ImGuiManager.TextColored(ThemeColors.FormInvalidColor, ImGuiManager.Interpolator.Utf8($"{dependencyName} ({"rysy.everestyaml.missing".Translate()})"));
                }
                
                ImGui.TableNextColumn();

                if (ImGui.Button($">>##{id++}").WithTranslatedTooltip("rysy.everestyaml.remove.description")) {
                    RemoveDependency(i, yaml);
                }
            }
            else
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();    
                ImGui.TableNextColumn();
            }

            if (i < _available.Count) {
                var availableMod = _available[i];

                if (availableMod.Name == mod.Name)
                    _available.RemoveAt(i);
                    
                ImGui.TableNextColumn();
                if (ImGui.Button($"<<##{id++}").WithTranslatedTooltip("rysy.everestyaml.add.description")) {
                    AddDependency(i, yaml);
                }
                ImGui.TableNextColumn();
                if (_required.Contains(availableMod.Name)) {
                    ImGuiManager.PushStyleColor(ImGuiCol.Text, ThemeColors.FormInvalidColor);
                    var open = ImGui.TreeNodeEx(ImGuiManager.Interpolator.Utf8($"{availableMod.DisplayName} ({"rysy.everestyaml.required".Translate()})"), ImGuiTreeNodeFlags.SpanFullWidth);
                    ImGui.PopStyleColor();
                    if (open) {
                        _dependencyCheckerCtx.GetDrawableDetailsFor(availableMod.Name).DrawImGui();

                        ImGui.TreePop();
                    }
                }
                else
                    ImGui.TextUnformatted(availableMod.DisplayName);
                
                ImGuiManager.TextDisabled($"({availableMod.Name} v{availableMod.Version})");
            }
        }
            
        ImGui.EndTable();
    }

    private void AddDependency(int index, EverestModuleMetadata yaml) {
        if (_state.Map?.Mod is not { } mod) return;
        if (_available == null || _deps == null || mod.Filesystem is not IWriteableModFilesystem) return;
            
        ModMeta dep = _available[index];
            
        yaml.Dependencies.Add(new() {
            Name = dep.Name,
            Version = dep.Version,
        });

        mod.TrySaveEverestYaml();
        RecalculateDependencies();
    }

    private void RemoveDependency(int index, EverestModuleMetadata yaml) {
        if (_state.Map?.Mod is not { } mod) return;
        if (_deps == null || _available == null || mod.Filesystem is not IWriteableModFilesystem) return;
            
        var dep = _deps[index];
            
        var yamlDeps = yaml.Dependencies.RemoveAll(x => x.Name == dep);

        mod.TrySaveEverestYaml();
        RecalculateDependencies();
    }

    private void RecalculateDependencies() {
        if (_state.Map?.Mod is not { } mod)
            return;

        var includeOptionalDeps = Settings.Instance.CountOptionalDependenciesAsDependencies;
        _deps = mod.GetAllDependencyNames(includeOptionalDeps).ToListIfNotList();
        _deps.Sort((a, b) => string.Compare(a, b, StringComparison.Ordinal));

        RecalculateMissingDeps();

        _available = ModRegistry.Mods.Values.Where(m => !_deps.Contains(m.Name)).ToList();
        SortAvailableDependencies();
    }

    private void SortAvailableDependencies()
    {
        _available?.Sort(
            (a, b) => 
                _required.Contains(a.Name) && !_required.Contains(b.Name) ? -1 :
                !_required.Contains(a.Name) &&  _required.Contains(b.Name) ? 1 : 
                string.Compare(a.Name, b.Name, StringComparison.Ordinal)
        );
    }

    private void RecalculateMissingDeps() {
        if (_state.Map?.Mod is not {} mod)
            return;
        _dependencyCheckerCtx = DependencyChecker.GetDependencies(_state.Map);
        _required = _dependencyCheckerCtx
            .FindMissingDependencies(mod)
            .ToListIfNotList();
    }

    public void OnSignal(MapSwapped signal) {
        RecalculateDependencies();
    }

    public void OnSignal(HistoryChanged signal) {
        RecalculateDependencies();
    }
}