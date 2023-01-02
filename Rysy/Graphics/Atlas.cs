using Microsoft.Xna.Framework.Graphics;

namespace Rysy.Graphics;

public class Atlas : IAtlas
{
    public Dictionary<string, VirtTexture> Textures { get; private set; } = new(StringComparer.InvariantCultureIgnoreCase);

    public VirtTexture this[string key]
    {
        get
        {
            if (Textures.TryGetValue(key, out var texture))
            {
                return texture;
            }

            Logger.Write("Atlas", LogLevel.Warning, $"Tried to access texture {key} that doesn't exist!");
            return GFX.VirtPixel;
        }
    }

    public Atlas()
    {

    }

    /// <inheritdoc/>
    public void DisposeTextures()
    {
        foreach (var item in Textures.Values)
        {
            item.Dispose();
        }
    }

    public void AddTexture(string virtPath, VirtTexture texture)
    {
        lock (Textures)
        {
            Textures[virtPath] = texture;
        }
    }

    public IEnumerable<(string virtPath, VirtTexture texture)> GetTextures() => Textures.Select(t => (t.Key, t.Value));
}
