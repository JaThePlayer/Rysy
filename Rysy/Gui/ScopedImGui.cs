using Hexa.NET.ImGui;
using JetBrains.Annotations;

namespace Rysy.Gui;

/// <summary>
/// Provides methods to create "scopes" for imgui Push/Pop-styled methods,
/// which use <see cref="IDisposable"/> to perform Pop operations to avoid issues when exceptions happen deep in UI code.
/// </summary>
public static class ScopedImGui {
    [MustDisposeResource]
    public static IdScope Id(string id) {
        ImGui.PushID(id);
        return default;
    }
    
    [MustDisposeResource]
    public static IdScope Id(ReadOnlySpan<byte> id) {
        ImGui.PushID(id);
        return default;
    }
    
    [MustDisposeResource]
    public static IdScope Id(int id) {
        ImGui.PushID(id);
        return default;
    }

    [MustDisposeResource]
    public static DisabledScope Disabled() {
        ImGui.BeginDisabled();
        return default;
    }
    
    [MustDisposeResource]
    public static DisabledScope Disabled(bool disabled) {
        ImGui.BeginDisabled(disabled);
        return default;
    }
    
    [MustDisposeResource]
    public static TabBarScope TabBar(ReadOnlySpan<byte> id)
        => new TabBarScope(ImGui.BeginTabBar(id));

    [MustDisposeResource]
    public static TabItemScope TabItem(string id)
        => new TabItemScope(ImGui.BeginTabItem(id));
    
    [MustDisposeResource]
    public static TabItemScope TabItem(ReadOnlySpan<byte> id)
        => new TabItemScope(ImGui.BeginTabItem(id));

    [MustDisposeResource]
    public ref struct IdScope : IDisposable {
        public void Dispose() {
            ImGui.PopID();
        }
    }
    
    [MustDisposeResource]
    public record struct TabBarScope(bool Opened) : IDisposable {
        public void Dispose() {
            if (Opened)
                ImGui.EndTabBar();
        }
    }
    
    [MustDisposeResource]
    public record struct TabItemScope(bool Opened) : IDisposable {
        public void Dispose() {
            if (Opened)
                ImGui.EndTabItem();
        }
    }
    
    [MustDisposeResource]
    public record struct DisabledScope : IDisposable {
        public void Dispose() {
            ImGui.EndDisabled();
        }
    }
}