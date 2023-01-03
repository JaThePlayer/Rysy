using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;

public abstract class Tool
{
    public HistoryHandler History;

    public abstract string Layer { get; set; }

    public abstract void Update(Camera camera, Room currentRoom);

    /// <summary>
    /// Renders this tool. Before calling, the sprite batch should be set using currentRoom.StartBatch(camera)
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="currentRoom"></param>
    public abstract void Render(Camera camera, Room currentRoom);

    /// <summary>
    /// Renders the overlay for this tool. The spritebatch has no transformation matrix, used to draw directly to the screen.
    /// </summary>
    public abstract void RenderOverlay();
}
