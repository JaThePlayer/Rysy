namespace Rysy;

public sealed class CommandlineArguments {
    private const string LogTag = "CommandlineArguments";
    
    public string? LoadIntoMap { get; set; }
    
    public string? Profile { get; set; }
    
    public bool Portable { get; set; }
    
    public CommandlineArguments(string[] args) {
        if (args is [var map] && File.Exists(map)) {
            LoadIntoMap = map;
            return;
        }

        for (int i = 0; i < args.Length; i++) {
            var option = args[i];

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
                case "--portable":
                    Portable = true;
                    break;
                default:
                    Logger.Write(LogTag, LogLevel.Error, $"Unknown cmd option option: {option}");
                    break;
            }
        }
    }
}