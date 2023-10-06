using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Scenes;

public class LoadErrorScene : Scene {
    public string Text;

    public LoadErrorScene(string text) {
        Text = text;
    }

    public override void Render() {
        base.Render();

        GFX.BeginBatch();
        var bounds = RysyEngine.Instance.Window.ClientBounds.Size();
        PicoFont.Print(Text, new Rectangle(0, 0, bounds.X, bounds.Y), Color.LightSkyBlue, 4f);
        GFX.EndBatch();
    }
}
