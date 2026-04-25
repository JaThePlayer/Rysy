using Hexa.NET.ImGui;

namespace Rysy.Gui;

/// <summary>
/// Calls <see cref="ImGui.PopID"/> when disposed.
/// </summary>
public readonly ref struct ScopedImGuiId : IDisposable {
    public ScopedImGuiId() {
    }

    public static ScopedImGuiId Push(int id) {
        ImGui.PushID(id);
        return new ScopedImGuiId();
    }
    
    public static ScopedImGuiId Push(ReadOnlySpan<byte> id) {
        ImGui.PushID(id);
        return new ScopedImGuiId();
    }
    
    public static ScopedImGuiId Push(ReadOnlySpan<char> id) {
        ImGui.PushID(ImGuiManager.Interpolator.Utf8(id));
        return new ScopedImGuiId();
    }

    public void Dispose() {
        ImGui.PopID();
    }
}