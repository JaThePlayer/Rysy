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
        PicoFont.Print(Text, new Rectangle(new(0, 0), RysyEngine.Instance.Window.ClientBounds.Size), Color.LightSkyBlue, 4f);
        GFX.EndBatch();
    }
}
