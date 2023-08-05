using System.Diagnostics.CodeAnalysis;

namespace Rysy.Graphics;

public class Atlas : IAtlas {
    public Dictionary<string, VirtTexture> Textures { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public VirtTexture this[string key] {
        get {
            if (key is null)
                return GFX.UnknownTexture;

            if (TryGet(key, out var texture))
                return texture;

            Logger.Write("Atlas", LogLevel.Warning, $"Tried to access texture {key} that doesn't exist!");
            return GFX.UnknownTexture;
        }
    }

    public bool TryGet(string key, [NotNullWhen(true)] out VirtTexture? texture) {
        texture = null;

        if (key is null)
            return false;

        if (Textures.TryGetValue(key, out texture)) {
            return true;
        }

        for (int zeroCount = 1; zeroCount < 6; zeroCount++) {
            key += "0";
            if (Textures.TryGetValue(key, out texture)) {
                return true;
            }
        }

        return false;
    }

    public bool TryGet(string key, int frame, [NotNullWhen(true)] out VirtTexture? texture) {
        texture = null;

        if (key is null)
            return false;

        if (Textures.TryGetValue(key + frame, out texture)) {
            return true;
        }

        for (int zeroCount = 2; zeroCount < 6; zeroCount++) {
            key += "0";
            if (Textures.TryGetValue(key + frame, out texture)) {
                return true;
            }
        }

        return false;
    }

    public VirtTexture this[string key, int frame] {
        get {
            if (key is null)
                return GFX.UnknownTexture;

            if (TryGet(key, frame, out var texture))
                return texture;

            Logger.Write("Atlas", LogLevel.Warning, $"Tried to access texture {key}, frame {frame} that doesn't exist!");
            return GFX.UnknownTexture;
        }
    }

    public bool Exists(string key) => TryGet(key, out _);
    public bool Exists(string key, int frame) => TryGet(key, frame, out _);

    public IReadOnlyList<VirtTexture> GetSubtextures(string key) {
        var list = new List<VirtTexture>();

        string padding = "";
        for (int zeroCount = 0; zeroCount < 6; zeroCount++) {
            int i = 0;
            while (true) {
                var newKey = $"{key}{padding}{i}";
                if (!Textures.TryGetValue(newKey, out var texture)) {
                    break;
                }

                list.Add(texture);
                i++;
            }

            padding += "0";
        }

        return list;
    }

    public Atlas() {

    }

    public event Action<string> OnTextureLoad;
    public event Action OnUnload;
    public event Action OnChanged;

    /// <inheritdoc/>
    public void DisposeTextures() {
        foreach (var item in Textures.Values) {
            item.Dispose();
        }

        OnUnload?.Invoke();
        OnChanged?.Invoke();
    }

    public void AddTexture(string virtPath, VirtTexture texture) {
        lock (Textures) {
            Textures[virtPath] = texture;
        }

        OnTextureLoad?.Invoke(virtPath);
        OnChanged?.Invoke();
    }

    public void RemoveTextures(List<string> virtPaths) {
        lock (Textures) {
            foreach (var item in virtPaths) {
                Textures.Remove(item);
            }

            OnChanged?.Invoke();
        }
    }

    public IEnumerable<(string virtPath, VirtTexture texture)> GetTextures() => Textures.Select(t => (t.Key, t.Value));
}
