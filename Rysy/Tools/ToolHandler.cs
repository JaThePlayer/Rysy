using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;

public class ToolHandler {
    public readonly HistoryHandler History;

    public ToolHandler(HistoryHandler history) {
        History = history;
        Tools = new() {
        // TODO: autogen
            new BrushTool() { History = History },
            new TileRectTool() { History = History }
        };
    }

    public List<Tool> Tools;

    public Tool CurrentTool => Tools[ToolIndex];
    public int ToolIndex { get; set; } = 1;

    public void Update(Camera camera, Room currentRoom) {
        CurrentTool.Update(camera, currentRoom);
    }

    public void Render(Camera camera, Room currentRoom) {
        currentRoom.StartBatch(camera);

        CurrentTool.Render(camera, currentRoom);

        GFX.EndBatch();

        GFX.BeginBatch();
        CurrentTool.RenderOverlay();
        GFX.EndBatch();
    }
}
