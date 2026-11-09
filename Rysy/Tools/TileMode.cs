using Rysy.Graphics;

namespace Rysy.Tools; 

/// <summary>
/// Represents a mode of the <see cref="TileTool"/>
/// </summary>
public abstract class TileMode : ToolMode {
    public readonly TileTool Tool;

    public int GridSize => Tool.GridSize;
    
    protected TileMode(TileTool tool) {
        Tool = tool;
    }
    
    public abstract void Render(Camera camera, Room room);
    
    public abstract void Update(Camera camera, Room room);
    
    public abstract void CancelInteraction();

    public abstract void Init();

    public void ClearTilegridSpriteCache(Room? room) {
        if (room is null)
            return;
        
        Tool.GetGrid(room)?.ClearSpriteCache();
    }
    
    // every TileMode will have a thickness, but modes like filled rectangles can override this to force it to be 1.
    protected virtual int Thickness => Tool.LineThickness;
}