namespace Rysy;

public sealed class CommandlineArguments {
    public string? LoadIntoMap { get; set; }
    
    public string? Profile { get; set; }
    
    public CommandlineArguments(string[] args) {
        if (args is [var map]) {
            LoadIntoMap = map;
            return;
        }

        for (int i = 0; i + 1 < args.Length; i += 2) {
            var option = args[i];
            var arg = args[i + 1];

            switch (option) {
                case "--map" or "-m":
                    LoadIntoMap = arg;
                    break;
                case "--profile" or "-p":
                    Profile = arg;
                    break;
                default:
                    Logger.Write("CommandlineArguments", LogLevel.Error, $"Unknown cmd option option: {option}");
                    break;
            }
        }
    }
}