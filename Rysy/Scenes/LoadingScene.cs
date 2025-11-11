using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Loading;

namespace Rysy.Scenes;

public class LoadingScene(LoadTaskManager taskManager, Action onCompleted) : Scene {
    private Task<Exception?>? _loadTask;
    private bool _completed;

    /// <summary>
    /// Text used in the loading scene.
    /// </summary>
    //public static string Text { get; set; }
    public override void Update() {
        base.Update();
        
        _loadTask ??= Task.Run(taskManager.LoadAll);

        if (!_completed && _loadTask.IsCompleted) {
            _completed = true;

            if (_loadTask.Result is { } ex) {
                Logger.Error(ex, "Failed to load");
            }
            
            onCompleted();
        }
    }

    public override void Render() {
        base.Render();

        var windowSize = RysyState.Window.ClientBounds.Size();
        Gfx.BeginBatch();

        const int scale = 4;
        
        PicoFont.Print("Rysy (dev)", new Rectangle(0, windowSize.Y / 4, windowSize.X, windowSize.Y / 2), Color.White, scale: scale);
        PicoFont.Print($"{TimeActive:0.00s}", new Rectangle(0, windowSize.Y / 3, windowSize.X, windowSize.Y / 2), Color.LightSkyBlue, scale: scale);

        var messages = taskManager.GetCurrentMessages();
        int yOffset = 0;
        foreach (var text in messages) {
            PicoFont.Print(text, new Rectangle(0, windowSize.Y / 2 + yOffset, windowSize.X, windowSize.Y / 2), Color.LightSkyBlue, scale: scale);
            yOffset += PicoFont.H * scale;
        }
        Gfx.EndBatch();
    }
}
