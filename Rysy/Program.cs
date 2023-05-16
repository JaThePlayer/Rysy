Environment.CurrentDirectory = Path.GetDirectoryName(typeof(RysyEngine).Assembly.Location) ?? Environment.CurrentDirectory;

new RysyEngine().Run();
