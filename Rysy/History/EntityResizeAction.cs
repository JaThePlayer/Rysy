using Rysy.Extensions;

namespace Rysy.History;

public record EntityResizeAction(EntityRef Entity, Point Delta) : IHistoryAction {
    private Point? PrevSize;

    public bool Apply(Map map) {
        var entity = Entity.Resolve(map);
        
        var changed = false;

        var minSize = entity.MinimumSize;

        PrevSize ??= new(entity.Width, entity.Height);

        if (entity.ResizableX && Delta.X != 0) {
            var prevW = entity.Width;
            entity.Width = (entity.Width + Delta.X).AtLeast(minSize.X);
            if (prevW != entity.Width)
                changed = true;
        }

        if (entity.ResizableY && Delta.Y != 0) {
            var prevH = entity.Height;
            entity.Height = (entity.Height + Delta.Y).AtLeast(minSize.Y);
            if (prevH != entity.Height)
                changed = true;
        }

        return changed;
    }

    public void Undo(Map map) {
        var entity = Entity.Resolve(map);
        
        entity.Width = PrevSize!.Value.X;
        entity.Height = PrevSize.Value.Y;
    }
}
