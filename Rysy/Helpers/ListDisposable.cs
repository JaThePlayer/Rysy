namespace Rysy.Helpers;

public sealed class ListDisposable(List<IDisposable> disposables) : IDisposable {
    public void Dispose() {
        foreach (var d in disposables) {
            d.Dispose();
        }
    }
}