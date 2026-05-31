using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Signals;

namespace Rysy.Gui.Windows {
    public class EverestYAMLWindow : Window, ISignalListener<MapSwapped> {
        private readonly EditorState _state;
        private List<ModMeta>? _deps, _available;
        private List<string> _required;
        private readonly StringField _versionInput;
        private string? _versionString;
        
        
        public EverestYAMLWindow(EditorState state) : base("rysy.everestyaml.window.name".Translate(), new(600, 500)) {
            _state = state;
            
            _versionInput = new StringField().WithValidator(
                s => Version.TryParse(s, out _) ? 
                    ValidationResult.Ok :
                    ValidationResult.GenericError
                ).Translated("rysy.everestyaml.version.name");

            OnSignal(new MapSwapped(state, null, state.Map));
        }
        

        protected override void Render() {
            if (_state.Map?.Mod?.EverestYaml.First() is not { } yaml) return;

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
            
            ImGui.TableSetupColumn("Dependencies");
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, textBaseWidth * 3f);
            
            
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, textBaseWidth * 3f);
            ImGui.TableSetupColumn("Available");
            ImGui.TableHeadersRow();
            
            if (_deps is null || _available is null) return;
            var id = 0;

            for (int i = 0; i < Math.Max(_deps.Count, _available.Count); i++) {
                if (i < _deps.Count) {
                    var dependency = _deps[i];
                    
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{dependency.DisplayName} ");
                
                    ImGui.TextDisabled($"({dependency.Name} v{dependency.Version})");
                
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

                    if (availableMod.Name == _state.Map?.Mod?.Name) _available.RemoveAt(i);
                    
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"<<##{id++}").WithTranslatedTooltip("rysy.everestyaml.add.description")) {
                        AddDependency(i, yaml);
                    }
                    ImGui.TableNextColumn();
                    if (_required.Contains(availableMod.Name))
                        ImGuiManager.TextColored(ThemeColors.FormInvalidColor, $"{availableMod.DisplayName} (required)");
                    else
                        ImGui.TextUnformatted($"{availableMod.DisplayName} ");
                
                    ImGui.TextDisabled($"({availableMod.Name} v{availableMod.Version})");
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
            
            _deps.Add(dep);
            _deps.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            
            _available.RemoveAt(index);
            RecalculateMissingDeps();
        }

        private void RemoveDependency(int index, EverestModuleMetadata yaml) {
            if (_state.Map?.Mod is not { } mod) return;
            if (_deps == null || _available == null || mod.Filesystem is not IWriteableModFilesystem) return;
            
            ModMeta dep = _deps[index];
            
            var yamlDeps = yaml.Dependencies;

            if (yamlDeps.Find(d => d.Name == dep.Name) is var i and not null) {
                yamlDeps.Remove(i);
            }
            
            mod.TrySaveEverestYaml();
            
            _available.Add(dep);
            
            _deps.RemoveAt(index);
            RecalculateMissingDeps();
            
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
            if (_state.Map?.Mod is not {} mod) return;
            _required = DependencyChecker
                .GetDependencies(_state.Map)
                .FindMissingDependencies(mod)
                .ToListIfNotList();
        }

        public void OnSignal(MapSwapped signal) {
            if (signal.NewMap is not { } map) return;
            if (map.Mod is not { } mod) return;
                
            var includeOptionalDeps = Settings.Instance.CountOptionalDependenciesAsDependencies;
            _deps = mod.GetAllDependenciesRecursive(includeOptionalDeps).ToListIfNotList();
            _deps?.Sort(
                (a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.Ordinal)
            );
            
            RecalculateMissingDeps();
            
            if (_deps is null) return;
            
            _available = ModRegistry.Mods.Values.Where(m => !_deps.Contains(m)).ToList();
            SortAvailableDependencies();
        }
    }
}