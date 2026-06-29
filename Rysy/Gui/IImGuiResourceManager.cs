namespace Rysy.Gui;

public interface IImGuiResourceManager {
    public nint BindTexture(Texture2D tex);
    
    public nint RebindTexture(Texture2D tex, nint id);

    public void UnbindTexture(IntPtr texPtr);
}