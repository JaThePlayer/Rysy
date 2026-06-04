using Hexa.NET.ImGui;

namespace Rysy.Gui;

/// <summary>
/// Provides methods to create "scopes" for imgui Push/Pop-styled methods,
/// which use <see cref="IDisposable"/> to perform Pop operations to avoid issues when exceptions happen deep in UI code.
/// </summary>
public static class ScopedImGui {
    public static IdScope Id(ReadOnlySpan<byte> id) {
        ImGui.PushID(id);
        return default;
    }
    
    public static IdScope Id(int id) {
        ImGui.PushID(id);
        return default;
    }

    public static DisabledScope Disabled() {
        ImGui.BeginDisabled();
        return default;
    }
    
    public static DisabledScope Disabled(bool disabled) {
        ImGui.BeginDisabled(disabled);
        return default;
    }
    
    public static TabBarScope TabBar(ReadOnlySpan<byte> id)
        => new TabBarScope(ImGui.BeginTabBar(id));

    public static TabItemScope TabItem(string id)
        => new TabItemScope(ImGui.BeginTabItem(id));
    
    public static TabItemScope TabItem(ReadOnlySpan<byte> id)
        => new TabItemScope(ImGui.BeginTabItem(id));

    public ref struct IdScope : IDisposable {
        public void Dispose() {
            ImGui.PopID();
        }
    }
    
    public record struct TabBarScope(bool Opened) : IDisposable {
        public void Dispose() {
            if (Opened)
                ImGui.EndTabBar();
        }
    }
    
    public record struct TabItemScope(bool Opened) : IDisposable {
        public void Dispose() {
            if (Opened)
                ImGui.EndTabItem();
        }
    }
    
    public record struct DisabledScope : IDisposable {
        public void Dispose() {
            ImGui.EndDisabled();
        }
    }
}