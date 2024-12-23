namespace Rysy.Helpers;

public sealed record FileStructureInfo(
    string Name,
    List<FileStructureInfo>? ChildFiles = null,
    string? Contents = null) {

    public static FileStructureInfo FromPath(string path) {
        var pathSplits = path.Split('/');
        
        var info = new FileStructureInfo(pathSplits[0], []);
        var nextDir = info;
        for (int i = 1; i < pathSplits.Length; i++) {
            nextDir.ChildFiles!.Add(new(pathSplits[i], i + 1 < pathSplits.Length ? [] : null));
            nextDir = nextDir.ChildFiles[0];
        }

        return info;
    }
}