﻿using Rysy.Graphics.TextureTypes;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Graphics;

public class Atlas : IAtlas {
    public Dictionary<string, VirtTexture> Textures { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

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

    public bool TryGetWithoutTryingFrames(string key, [NotNullWhen(true)] out VirtTexture? texture) {
        if (key is null) {
            texture = null;
            return false;
        }

        return Textures.TryGetValue(key, out texture);
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

        if (list.Count == 0) {
            if (Textures.TryGetValue(key, out var unanimated))
                list.Add(unanimated);
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
            // Don't allow replacing non-modded assets - vanilla and rysy built-ins
            // as it breaks the dependency checker...
            if (Textures.TryGetValue(virtPath, out var prevTexture) && prevTexture is not ModTexture { Mod.IsVanilla: false }) {
                return;
            }
            Textures[virtPath] = texture;
        }

        OnTextureLoad?.Invoke(virtPath);
        OnChanged?.Invoke();
    }

    public void RemoveTextures(params List<string> paths) {
        lock (Textures) {
            foreach (var item in paths) {
                Textures.Remove(item);
            }

            OnChanged?.Invoke();
        }
    }
    
    public void RemoveTextures(params List<VirtTexture> paths) {
        lock (Textures) {
            foreach (var item in paths) {
                var key = Textures.FirstOrDefault(x => x.Value == item).Key;
                if (key is { }) {
                    Textures.Remove(key);
                }
            }

            OnChanged?.Invoke();
        }
    }

    public IEnumerable<(string virtPath, VirtTexture texture)> GetTextures() => Textures.Select(t => (t.Key, t.Value));
}
