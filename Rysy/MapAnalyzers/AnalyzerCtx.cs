namespace Rysy.MapAnalyzers;

public record class AnalyzerCtx(Map Map) {
    private readonly List<IAnalyzerResult> _mutableResults = new();

    public IReadOnlyList<IAnalyzerResult> Results => _mutableResults;

    public void AddResult(IAnalyzerResult result) {
        _mutableResults.Add(result);
    }
}

public interface IAnalyzerResult {
    LogLevel Level { get; }

    string Message { get; }

    void RenderDetailImgui();

    bool AutoFixable { get; }

    void Fix();
}
