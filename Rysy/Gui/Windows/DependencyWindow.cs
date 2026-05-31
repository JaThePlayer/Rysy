using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Signals;

namespace Rysy.Gui.Windows {
    public class EverestYAMLWindow : Window, ISignalListener<MapSwapped> {
        
        
        
        public new static string Name => "rysy.everestyaml.window.name".Translate();
        private readonly EditorState _state;
        private List<ModMeta>? _deps, _available;
        private List<string> _required;
        private readonly StringField _versionInput;
        
        
        public EverestYAMLWindow(EditorState state) : base(Name, new(600, 500)) {
            _state = state;
            
            if (state.Map is not { Mod: { } mod })
                return;
            
            _versionInput = new StringField().WithValidator(
                s => Version.TryParse(s, out _) ? 
                    ValidationResult.Ok :
                    ValidationResult.GenericError
                ).Translated("rysy.everestyaml.version.name");
            
            var includeOptionalDeps = Settings.Instance.CountOptionalDependenciesAsDependencies;
            _deps = mod.GetAllDependenciesRecursive(includeOptionalDeps).ToListIfNotList();
            if (_deps is null) return;
            _deps.Sort(
                (a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.Ordinal)
            );
            
            RecalculateMissingDeps();
            
            _available = ModRegistry.Mods.Values.Where(m => !_deps.Contains(m)).ToList();
            
            if (_required is null) return;
            
            _available.Sort(
                (a, b) => 
                    _required.Contains(a.Name) && !_required.Contains(b.Name) ? -1 :
                    !_required.Contains(a.Name) &&  _required.Contains(b.Name) ? 1 : 
                    string.Compare(a.Name, b.Name, StringComparison.Ordinal)
            );
        }
        

        protected override void Render() {


            if (_state.Map?.Mod?.EverestYaml.First() is not { } yaml) return;

            if (_versionInput.RenderGuiWithValidation(yaml.VersionString, out var isValid) is string newVersion && newVersion != "" && isValid.IsOk) {
                var newVersionParsed = Version.Parse(newVersion);
                yaml.Version = newVersionParsed;
                yaml.VersionString = newVersion;
                _state.Map?.Mod?.TrySaveEverestYaml();
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
                    ImGui.Text($"{dependency.DisplayName} ");
                
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
                        ImGui.TextColored(LogLevel.Error.ToColorNumVec(), $"{availableMod.DisplayName} (required)");
                    else
                        ImGui.Text($"{availableMod.DisplayName} ");
                
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
            
            _available.Sort(
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
            _available.Sort(
                (a, b) => 
                    _required.Contains(a.Name) && !_required.Contains(b.Name) ? -1 :
                    !_required.Contains(a.Name) &&  _required.Contains(b.Name) ? 1 : 
                    string.Compare(a.Name, b.Name, StringComparison.Ordinal)
            );
        }
    }
}