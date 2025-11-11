using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Selections;

namespace Rysy.Loading;

public static class DefaultLoadTasks {
    public static async Task<LoadTaskResult> LoadMods(SimpleLoadTask task, bool cSharpPlugins = true) {
        await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory, task, cSharpPlugins);
        
        return LoadTaskResult.Success();
    }
    
    public static async Task<LoadTaskResult> LoadGfx(SimpleLoadTask task) {
        await Gfx.LoadAsync(task);
        
        return LoadTaskResult.Success();
    }
    
    public static Task<LoadTaskResult> LoadDecalRegistry(SimpleLoadTask task) {
        Gfx.LoadDecalRegistry(task);
        
        return Task.FromResult(LoadTaskResult.Success());
    }
    
    public static async Task<LoadTaskResult> LoadEntities(SimpleLoadTask task, bool luaPlugins = true, bool cSharpPlugins = true) {
        await EntityRegistry.RegisterAsync(luaPlugins, cSharpPlugins, task);
        
        return LoadTaskResult.Success();
    }
    
    public static async Task<LoadTaskResult> LoadLangFiles(SimpleLoadTask task) {
        await LangRegistry.LoadAllAsync(task);
        
        return LoadTaskResult.Success();
    }
    
    public static Task<LoadTaskResult> LoadTheme(SimpleLoadTask task) {
        RysyState.OnEndOfThisFrame += () => {
            Themes.LoadThemeFromFile(Settings.Instance.Theme);
            Themes.SetFontSize(Settings.Instance.FontSize);
        };

        
        return Task.FromResult(LoadTaskResult.Success());
    }
    
    public static Task<LoadTaskResult> CallOnNextReload(SimpleLoadTask task) {
        task.SetMessage("Calling OnReload");
        RysyState.DispatchOnNextReload();

        return Task.FromResult(LoadTaskResult.Success());
    }
    
    public static Task<LoadTaskResult> InitializeSelectionContextWindowRegistry(SimpleLoadTask task) {
        SelectionContextWindowRegistry.Init();

        return Task.FromResult(LoadTaskResult.Success());
    }
    
    public static async Task<LoadTaskResult> LoadMapFromPersistence(SimpleLoadTask task) {
        var editor = new EditorScene();
        await editor.LoadFromPersistence();
                
        RysyState.Scene = editor;
                
        return LoadTaskResult.Success();
    }
}