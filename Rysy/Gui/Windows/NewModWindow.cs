using ImGuiNET;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using System.Text.RegularExpressions;

namespace Rysy.Gui.Windows;

internal sealed partial class NewModWindow : Window {
    [GeneratedRegex("^[a-zA-Z0-9_]+$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex NameRegex();
    
    private string _modName = "";
    private string _binFilename => _modName;
    private string _modAuthor = "";

    private string _mapEnglishName = "";
    private string _levelsetEnglishName = "";

    private bool _folderExists;

    private bool _anyInvalidFields = true;
    
    private Task<Dictionary<string, DatabaseModInfo>> _databaseMods;
    
    public NewModWindow() : base("rysy.newMod.windowName".Translate(), new(500, ImGui.GetMainViewport().Size.Y * 0.8f)) {
        _databaseMods = IModDatabase.DefaultDatabase.GetKnownModsAsync();
    }


    private bool IsCodeNameValid(string name) {
        return NameRegex().IsMatch(name);
    }
    
    private bool IsEnglishNameValid(string name) {
        return name.Length > 0 && name.AsSpan().IndexOfAny("\r\n=") == -1;
    }

    protected override void Render() {
        base.Render();

        var anyInvalid = false;
        
        var knownMods = _databaseMods.IsCompleted ? _databaseMods.Result : null;

        var isModIdUsedAlready = ModRegistry.GetModByName(_modName) is {} || (knownMods?.TryGetValue(_modName, out _) ?? false);
        
        anyInvalid |= ImGuiManager.PushInvalidStyleIf(_folderExists || !IsCodeNameValid(_modName) || isModIdUsedAlready);
        if (ImGuiManager.TranslatedInputText("rysy.newMod.id", ref _modName, 32)) {
            _folderExists = Path.Exists($"{Profile.Instance.ModsDirectory}/{_modName}");
        }

        if (isModIdUsedAlready) {
            ImGuiManager.TranslatedText("rysy.newMod.id.used");
            var link = knownMods is {} && knownMods.TryGetValue(_modName, out var mod) ? $"https://gamebanana.com/mods/{mod.GameBananaId}" : null;
            if (link is { }) {
                ImGuiManager.Link(link);
            }
        }
        ImGuiManager.PopInvalidStyle();
        
        anyInvalid |= ImGuiManager.PushInvalidStyleIf(!IsCodeNameValid(_modAuthor));
        ImGuiManager.TranslatedInputText("rysy.newMod.author", ref _modAuthor, 32);
        ImGuiManager.PopInvalidStyle();
        
        ImGui.SeparatorText("rysy.newMod.dialog".Translate());
        anyInvalid |= ImGuiManager.PushInvalidStyleIf(!IsEnglishNameValid(_mapEnglishName));
        ImGuiManager.TranslatedInputText("rysy.newMod.map.name", ref _mapEnglishName, 256);
        ImGuiManager.PopInvalidStyle();
        
        anyInvalid |= ImGuiManager.PushInvalidStyleIf(!IsEnglishNameValid(_levelsetEnglishName));
        ImGuiManager.TranslatedInputText("rysy.newMod.map.levelsetName", ref _levelsetEnglishName, 256);
        ImGuiManager.PopInvalidStyle();
        
        ImGui.SeparatorText("rysy.newMod.structure".Translate());

        var file = CreateFileStructure();

        ImGuiManager.RenderFileStructure(file);

        _anyInvalidFields = anyInvalid;
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        base.RenderBottomBar();

        var editor = RysyState.Scene as EditorScene;

        ImGui.BeginDisabled(_anyInvalidFields || editor is null);
        if (ImGuiManager.TranslatedButton("rysy.newMod.create") && editor is not null) {
            var mod = ModRegistry.CreateNewMod(_modName);
            if (mod.Filesystem is not IWriteableModFilesystem fs)
                throw new Exception("Cannot write to the mod folder for some reason? This is a Rysy bug.");

            foreach (var f in CreateFileStructure().ChildFiles ?? []) {
                WriteFile(fs, "", f);
            }

            var map = Map.NewMap(_binFilename);
            map.Filepath = Path.Combine(fs.Root, "Maps", _modAuthor, _modName, $"{_binFilename}.bin");
            map.Meta.Sprites = $"Graphics/{_modAuthor}/{_modName}XMLs/Sprites.xml";
            map.Meta.ForegroundTiles = $"Graphics/{_modAuthor}/{_modName}XMLs/ForegroundTiles.xml";
            map.Meta.BackgroundTiles = $"Graphics/{_modAuthor}/{_modName}XMLs/BackgroundTiles.xml";
            map.Meta.AnimatedTiles = $"Graphics/{_modAuthor}/{_modName}XMLs/AnimatedTiles.xml";
            
            editor.Map = map;            

            editor.Save();
            RemoveSelf();
        }
        ImGui.EndDisabled();
    }

    private void WriteFile(IWriteableModFilesystem fs, string dir, FileStructureInfo file) {
        var path = $"{dir}/{file.Name}";
        
        if (file.ChildFiles is { } childFiles) {
            fs.TryCreateDirectory(path);
            foreach (var f in childFiles) {
                WriteFile(fs, path, f);
            }
        } else {
            fs.TryWriteToFile(path, file.Contents ?? "");
        }
    }

    FileStructureInfo CreateFileStructure() => new(_modName, [
        new ("Dialog", [
            new("English.txt", Contents: GetEnglishDialogContents()),
        ]),
        new ("Graphics", [
            new("Atlases", [
                new ("Gameplay", [
                    new ("decals", [
                        new(_modAuthor, [
                            new(_modName, [
                            ])
                        ])
                    ])
                ]),
                new ("Checkpoints", [
                    new(_modAuthor, [
                        new(_modName, [
                            new(_binFilename, [
                                new ("A", [])
                            ]),
                        ])
                    ])
                ]),
            ]),
            new(_modAuthor, [
                new($"{_modName}XMLs", [
                    new("Sprites.xml", Contents: SpritesXmlContents.Value),
                    new("ForegroundTiles.xml", Contents: ForegroundTilesXmlContents.Value),
                    new("BackgroundTiles.xml", Contents: BackgroundTilesXmlContents.Value),
                    new("AnimatedTiles.xml", Contents: AnimatedTilesXmlContents.Value),
                ])
            ])
        ]),
        new("Maps", [
            new(_modAuthor, [
                new(_modName, [
                    new($"{_binFilename}.bin"),
                ])
            ])
        ]),
        
        new("everest.yaml", Contents: GetEverestYamlContents()),
    ]);
    
    private string GetEnglishDialogContents() => $"""
    {_modAuthor}_{_modName}={_levelsetEnglishName}
    {_modAuthor}_{_modName}_{_binFilename}={_mapEnglishName}
    """;
    
    private string GetEverestYamlContents() => $"""
    - Name: {_modName}
      Version: 1.0.0
    """;

    private static readonly Lazy<string> SpritesXmlContents = new(() => ModRegistry.VanillaMod.Filesystem.TryReadAllText("Graphics/Sprites.xml") ?? "");
    public static readonly Lazy<string> ForegroundTilesXmlContents = new(() => ModRegistry.VanillaMod.Filesystem.TryReadAllText("Graphics/ForegroundTiles.xml") ?? "");
    public static readonly Lazy<string> BackgroundTilesXmlContents = new(() => ModRegistry.VanillaMod.Filesystem.TryReadAllText("Graphics/BackgroundTiles.xml") ?? "");
    private static readonly Lazy<string> AnimatedTilesXmlContents = new(() => ModRegistry.VanillaMod.Filesystem.TryReadAllText("Graphics/AnimatedTiles.xml") ?? "");
}