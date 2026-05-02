using Hexa.NET.ImGui;

namespace Rysy.Gui;

/// <summary>
/// Calls <see cref="ImGui.BeginDisabled()"/> in the constructor, then <see cref="ImGui.EndDisabled()"/> when disposed.
/// </summary>
public readonly ref struct ScopedImGuiDisabled : IDisposable
{
    public ScopedImGuiDisabled()
    {
        ImGui.BeginDisabled();
    }

    public ScopedImGuiDisabled(bool disabled)
    {
        ImGui.BeginDisabled(disabled);
    }
    
    public void Dispose()
    {
        ImGui.EndDisabled();
    }
}
