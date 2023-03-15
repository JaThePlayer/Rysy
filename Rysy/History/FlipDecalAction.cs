namespace Rysy.History;

public record class FlipDecalAction(Decal Decal, bool FlipX, bool FlipY) : IHistoryAction {
    public bool Apply() {
        Impl();
        return true;
    }

    public void Undo() {
        Impl();
    }

    private void Impl() {
        var newScale = Decal.Scale;
        if (FlipX) {
            newScale.X = -newScale.X;
        }
        if (FlipY) {
            newScale.Y = -newScale.Y;
        }

        Decal.Scale = newScale;
    }
}
