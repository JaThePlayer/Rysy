using Rysy.Extensions;

namespace Rysy.History;

public record class EntityResizeAction(Entity Entity, Point Delta) : IHistoryAction {
    private Point? PrevSize;

    public bool Apply() {
        var changed = false;

        var minSize = Entity.MinimumSize;

        PrevSize ??= new(Entity.Width, Entity.Height);

        if (Entity.ResizableX && Delta.X != 0) {
            var prevW = Entity.Width;
            Entity.Width = (Entity.Width + Delta.X).AtLeast(minSize.X);
            if (prevW != Entity.Width)
                changed = true;
        }

        if (Entity.ResizableY && Delta.Y != 0) {
            var prevH = Entity.Height;
            Entity.Height = (Entity.Height + Delta.Y).AtLeast(minSize.Y);
            if (prevH != Entity.Height)
                changed = true;
        }

        return changed;
    }

    public void Undo() {
        Entity.Width = PrevSize!.Value.X;
        Entity.Height = PrevSize.Value.Y;
    }
}
