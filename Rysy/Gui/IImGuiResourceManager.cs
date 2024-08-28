namespace Rysy.Gui;

public interface IImGuiResourceManager {
    public IntPtr BindTexture(Texture2D tex);

    public void UnbindTexture(IntPtr texPtr);

    public void BuildFontAtlas();
}