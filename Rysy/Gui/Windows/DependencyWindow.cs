using Hexa.NET.ImGui;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows {
    public class EverestYAMLWindow : Window {
        
        private new static string Name => "rysy.everestyaml.window.name".Translate();
        private readonly EditorState _state;
        private readonly List<ModMeta>? _deps, _available;
        private List<string> _required;
        
        
        public EverestYAMLWindow(EditorState state) : base(Name, new(600, 500)) {
            _state = state;
            if (state.Map?.Mod != null) {
                var includeOptionalDeps = Settings.Instance.CountOptionalDependenciesAsDependencies;
                _deps = state.Map?.Mod.GetAllDependenciesRecursive(includeOptionalDeps).ToListIfNotList();
                _deps?.Sort(
                    (a, b) =>
                        string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                );
                
                RecalculateMissingDeps();
                
                if (_deps != null) {
                    _available = ModRegistry.Mods.Values.Where(m => !_deps.Contains(m)).ToList();
                    if (_required is not null)
                        _available.Sort(
                            (a, b) => 
                                _required.Contains(a.Name) && !_required.Contains(b.Name) ? -1 :
                                    !_required.Contains(a.Name) &&  _required.Contains(b.Name) ? 1 : 
                                string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                        );
                }
            }
        }
        

        protected override void Render() {

            var versionInput = new StringField();
            var yaml = _state.Map?.Mod?.EverestYaml.First();

            if (yaml is null) return;

            if (versionInput.RenderGui("Version", yaml.VersionString) is string newVersion && newVersion != "") {
                if (!Version.TryParse(newVersion, out var newVersionParsed)) return;
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

                    if (ImGui.Button($">>##{id++}")) {
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
                    if (ImGui.Button($"<<##{id++}")) {
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
            if (_available == null  || _state.Map?.Mod?.Filesystem is not IWriteableModFilesystem) return;
            
            ModMeta dep = _available[index];
            
            yaml.Dependencies.Add(new() {
                Name = dep.Name,
                Version = dep.Version,
            });

            _state.Map?.Mod?.TrySaveEverestYaml();
            
            _deps?.Add(dep);
            _deps?.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            
            _available.RemoveAt(index);
            RecalculateMissingDeps();
        }

        private void RemoveDependency(int index, EverestModuleMetadata yaml) {
            if (_deps == null  || _state.Map?.Mod?.Filesystem is not IWriteableModFilesystem) return;
            
            ModMeta dep = _deps[index];
            
            var yamlDeps = yaml.Dependencies;

            if (yamlDeps.Find(d => d.Name == dep.Name) is var i and not null) {
                yamlDeps.Remove(i);
            }
            
            _state.Map?.Mod?.TrySaveEverestYaml();
            
            _available?.Add(dep);
            
            _deps.RemoveAt(index);
            RecalculateMissingDeps();
            
            _available?.Sort(
                (a, b) => 
                    _required.Contains(a.Name) && !_required.Contains(b.Name) ? -1 :
                    !_required.Contains(a.Name) &&  _required.Contains(b.Name) ? 1 : 
                    string.Compare(a.Name, b.Name, StringComparison.Ordinal)
            );
        }

        private void RecalculateMissingDeps() {
            if (_state.Map?.Mod is null) return;
            _required = DependencyChecker
                .GetDependencies(_state.Map)
                .FindMissingDependencies(_state.Map.Mod)
                .ToListIfNotList();
        }
    }
}