namespace Rysy.MapAnalyzers;

public record class AnalyzerCtx(Map Map) {
    private readonly List<IAnalyzerResult> MutableResults = new();

    public IReadOnlyList<IAnalyzerResult> Results => MutableResults;

    public void AddResult(IAnalyzerResult result) {
        MutableResults.Add(result);
    }
}

public interface IAnalyzerResult {
    LogLevel Level { get; }

    string Message { get; }

    void RenderDetailImgui();

    bool AutoFixable { get; }

    void Fix();
}
