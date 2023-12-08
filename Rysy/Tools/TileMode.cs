using Rysy.Graphics;

namespace Rysy.Tools; 

/// <summary>
/// Represents a mode of the <see cref="TileTool"/>
/// </summary>
public abstract class TileMode : ToolMode {
    public readonly TileTool Tool;

    protected TileMode(TileTool tool) {
        Tool = tool;
    }
    
    public abstract void Render(Camera camera, Room room);
    
    public abstract void Update(Camera camera, Room room);
    
    public abstract void CancelInteraction();

    public abstract void Init();

    public void ClearTilegridSpriteCache(Room? room = null) {
        room ??= EditorState.CurrentRoom;
        if (room is null)
            return;
        
        Tool.GetGrid(room)?.ClearSpriteCache();
        Tool.GetSecondGrid(room)?.ClearSpriteCache();
    }
}