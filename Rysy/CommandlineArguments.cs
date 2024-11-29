namespace Rysy;

public sealed class CommandlineArguments {
    private const string LogTag = "CommandlineArguments";
    
    public string? LoadIntoMap { get; set; }
    
    public string? Profile { get; set; }
    
    public string? CelesteDir { get; set; }
    
    public bool Portable { get; set; }
    
    public bool Headless { get; set; }
    
    public string? HeadlessScriptFile { get; set; }
    
    public CommandlineArguments(string[] args) {
        for (int i = 0; i < args.Length; i++) {
            var option = args[i];
            var isLast = i == args.Length - 1;

            switch (option) {
                case "--map" or "-m":
                    if (i + 1 < args.Length) {
                        LoadIntoMap = args[i + 1];
                        i++;
                    } else {
                        Logger.Write(LogTag, LogLevel.Error, $"Missing map path for argument: {option}");
                    }
                    break;
                case "--profile" or "-p":
                    if (i + 1 < args.Length) {
                        Profile = args[i + 1];
                        i++;
                    } else {
                        Logger.Write(LogTag, LogLevel.Error, $"Missing profile name for argument: {option}");
                    }
                    break;
                case "--celeste-exe":
                    if (i + 1 < args.Length) {
                        CelesteDir = Path.GetDirectoryName(args[i + 1]);
                        i++;
                    } else {
                        Logger.Write(LogTag, LogLevel.Error, $"Missing Celeste.exe path for argument: {option}");
                    }
                    break;
                case "--portable":
                    Portable = true;
                    break;
                default:
                    // If the last arg is a bin filename, treat it as the map to open
                    if (isLast && option.EndsWith(".bin", StringComparison.Ordinal) && File.Exists(option)) {
                        LoadIntoMap = option;
                        break;
                    }

                    if (isLast && option.EndsWith(".cs", StringComparison.Ordinal) && File.Exists(option)) {
                        Headless = true;
                        HeadlessScriptFile = option;
                        break;
                    }
                    
                    Logger.Write(LogTag, LogLevel.Error, $"Unknown cmd option option: {option}");
                    break;
            }
        }
    }
}