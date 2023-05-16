using Rysy.Extensions;

namespace Rysy.History;

public record class EntityResizeAction(Entity Entity, Point Delta) : IHistoryAction {
    private Point RealDelta;

    public bool Apply() {
        var changed = false;

        var minSize = Entity.MinimumSize;

        if (Entity.ResizableX && Delta.X != 0) {
            var prevW = Entity.Width;
            Entity.Width = (Entity.Width + Delta.X).AtLeast(minSize.X);
            RealDelta.X = Entity.Width - prevW;
            changed = true;
        }

        if (Entity.ResizableY && Delta.Y != 0) {
            var prevH = Entity.Height;
            Entity.Height = (Entity.Height + Delta.Y).AtLeast(minSize.Y);
            RealDelta.Y = Entity.Height - prevH;
            changed = true;
        }

        return changed;
    }

    public void Undo() {
        Entity.Width -= RealDelta.X;
        Entity.Height -= RealDelta.Y;
    }
}
