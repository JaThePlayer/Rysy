using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Scenes;

public class PickCelesteInstallScene : Scene {
    private Scene _nextScene;
    public PickCelesteInstallScene(Scene nextScene) {
        _nextScene = nextScene;
    }

    protected internal override void OnFileDrop(string filePath) {
        base.OnFileDrop(filePath);
        StoreCelestePath(filePath); // if the dropped file is not a directory, this is a no-op
    }

    private void StoreCelestePath(string dirPath) {
        if (Directory.Exists(dirPath)) {
            string fullPath;
            try {
                fullPath = Path.GetFullPath(dirPath);
            } catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
                return;
            }
            Profile.Instance.CelesteDirectory = fullPath;
            Profile.Instance.Save();

            RysyEngine.Scene = _nextScene;
        }
    }

    public async ValueTask AwaitInstallPickedAsync() {
        while (string.IsNullOrWhiteSpace(Profile.Instance.CelesteDirectory)) {
            await Task.Delay(100);
        }
    }

    public override void Render() {
        base.Render();

        Gfx.BeginBatch();
        var windowSize = RysyState.Window.ClientBounds.Size();
        var height = 4 * 6;
        var center = windowSize.Y / 2;
        PicoFont.Print("Please drop the", new Rectangle(0, center - 48, windowSize.X, height), Color.White, scale: 4f);
        PicoFont.Print("Celeste game directory", new Rectangle(0, center - 16, windowSize.X, height), Color.LightSkyBlue, scale: 4f);
        PicoFont.Print("game directory", new Rectangle(PicoFont.W * 2 * "Celeste ".Length, center - 16, windowSize.X, height), Color.White, scale: 4f);
        PicoFont.Print("onto this window", new Rectangle(0, center + 16, windowSize.X, height), Color.White, scale: 4f);
        PicoFont.Print("(or click to browse)", new Rectangle(0, center + 48, windowSize.X, height), Color.White, scale: 4f);
        Gfx.EndBatch();
    }

    public override void Update() {
        if (Input.Global?.Mouse.Clicked(0) ?? false) {
            if (FileDialogHelper.TryOpenDir(out string? dirPath)) {
                StoreCelestePath(dirPath);
            }
        }
    }
}
