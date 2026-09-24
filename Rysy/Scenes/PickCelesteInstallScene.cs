using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Gui.FieldTypes;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Platforms;

namespace Rysy.Scenes;

internal sealed class PickCelesteInstallScene : Scene {
    private Scene _nextScene;
    private readonly IReadOnlyList<string> _knownProfiles;
    private readonly bool _canCancel;

    public PickCelesteInstallScene(Scene nextScene, IReadOnlyList<string>? knownProfiles, bool canCancel) {
        _nextScene = nextScene;
        _knownProfiles = knownProfiles ?? RysyPlatform.Current.GetAllExistingProfileNames();
        _canCancel = canCancel;
    }

    public override void OnBegin() {
        base.OnBegin();
        AddWindow(new PickerWindow(this, _knownProfiles));
    }

    public async ValueTask AwaitInstallPickedAsync() {
        while (string.IsNullOrWhiteSpace(Settings.Instance.CurrentProfile)) {
            await Task.Delay(100);
        }
    }

    class PickerWindow : Window {
        private readonly PickCelesteInstallScene _scene;
        private readonly IReadOnlyList<string> _knownProfiles;

        private readonly List<FoundProfile> _foundProfiles;

        private readonly ComboCache<FoundProfile> _cache = new();

        private bool _anyProfileSelected;

        public PickerWindow(PickCelesteInstallScene scene, IReadOnlyList<string> knownProfiles) : base("rysy.windows.installPicker", 
            new NumVector2(800, 300) ) {
            _scene = scene;
            _knownProfiles = knownProfiles;
            _foundProfiles = [];

            NoSaveData = true;
            Closeable = false;
            
            Task.Run(async () => {
                try {
                    await foreach (var install in CelesteInstallLocator.FindAllInstalls()) {
                        lock (_foundProfiles) {
                            _foundProfiles.Add(install);
                            _cache.Clear();
                        }
                    }
                    
                    _cache.Clear();
                } catch (Exception ex) {
                    Logger.Error(ex, "HUH");
                }
                
            });
        }

        public override bool CloseableByHotkey => false;

        public override bool HasBottomBar => true;

        public override void RenderBottomBar() {
            base.RenderBottomBar();

            if (ImGuiManager.TranslatedButton("rysy.windows.installPicker.manual")) {
                if (FileDialogHelper.TryOpen("exe,dll", out var filePath)) {
                    if (Path.GetFileName(filePath) is "Celeste.exe" or "Celeste.dll") {
                        var dir = Path.GetDirectoryName(filePath) ?? "";
                        // If the user provided the Celeste.exe in the '/orig' directory, silently fix the path to use the main dir instead.
                        if (dir.EndsWith("/orig", StringComparison.Ordinal) || dir.EndsWith("\\orig", StringComparison.Ordinal)) {
                            var mainDir = dir[..^"/orig".Length];
                            if (File.Exists(Path.Combine(mainDir, "Celeste.exe"))) {
                                dir = mainDir;
                            }
                        }

                        OnClick(new FoundProfile(dir, "Default", null!));
                    }
                }
            }

            if (_scene._canCancel) {
                ImGui.SameLine();

                if (ImGuiManager.TranslatedButton("rysy.cancel")) {
                    RysyEngine.Scene = _scene._nextScene;
                }
            }
        }

        private string _newProfileCelesteDir;
        private string _newProfileName = "Default";

        private StringField? _newProfileNameField;
        private ValidationResult? _newProfileNameValidationResult;

        private void RenderPickNameWindow(Window obj) {
            if (_newProfileNameField is null) {
                _newProfileNameField = new StringField().WithValidator((ctx, name) => {
                    if (name.IsNullOrWhitespace() || !Profile.IsValidProfileName(name)) {
                        return ValidationResult.ProfileNameInvalid;
                    }
                    
                    if (_knownProfiles.Contains(name)) {
                        return ValidationResult.ProfileNameMustBeUnique;
                    }
                    
                    return ValidationResult.Ok;
                }).Translated("rysy.windows.installPicker.manual.pickName.field");
            }
            
            ImGuiManager.TranslatedTextWrapped("rysy.windows.installPicker.manual.pickName.desc");
            _newProfileName = _newProfileNameField.RenderGuiWithValidation(_newProfileName, out _newProfileNameValidationResult)?.ToString() ?? _newProfileName;
        }
        
        private void RenderPickNameWindowBottomBar(Window obj) {
            using var _ = ScopedImGui.Disabled(!(_newProfileNameValidationResult?.IsOk ?? false));
            var isPressed = ImGuiManager.TranslatedButton("rysy.ok");
            
            if (isPressed) {
                SaveProfileAndExit(new FoundProfile(_newProfileCelesteDir, _newProfileName, null!));
            }
        }

        protected override void Render() {
            base.Render();

            lock (_foundProfiles) {
                ImGuiManager.TranslatedTextWrapped("rysy.windows.installPicker.desc");
                
                if (ImGui.BeginChild("###profileList", new NumVector2(0, ImGui.GetContentRegionAvail().Y), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                    ImGuiManager.List(_foundProfiles, ItemNameGetter, _cache, OnClick);
                }
                if (_foundProfiles.Count == 0) {
                    ImGuiManager.TranslatedTextWrapped("rysy.windows.installPicker.desc.noProfile");
                }
                
                ImGui.EndChild();
            }
        }

        private void OnClick(FoundProfile obj) {
            _newProfileCelesteDir = obj.CelesteDirectory;
            _newProfileName = obj.Name;
            _scene.AddWindow(new ScriptedWindow("rysy.windows.installPicker.manual.pickName".Translate(), 
                RenderPickNameWindow, 
                new GuiSize(50, 5).CalculateWindowSize(true), 
                RenderPickNameWindowBottomBar) {
                NoSaveData = true
            });
        }

        private void SaveProfileAndExit(FoundProfile obj) {
            if (_anyProfileSelected)
                return;
            
            _anyProfileSelected = true;
            
            var profile = new Profile { StoredCelesteDirectory = obj.CelesteDirectory };
            Profile.Instance = profile;
            
            Settings.Instance.Profile = obj.Name;
            profile.Save();
            Settings.Instance.Save();
            RysyEngine.Scene = _scene._nextScene;
        }

        private Searchable ItemNameGetter(FoundProfile arg) {
            return new Searchable(arg.Name, [ arg.Source.Name, arg.CelesteDirectory.Censor() ], [ arg.Source.Name ]);
        }
    }
}