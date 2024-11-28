﻿using Rysy;

RysyState.CmdArguments = new(args);

Environment.CurrentDirectory = Path.GetDirectoryName(typeof(RysyEngine).Assembly.Location) ?? Environment.CurrentDirectory;

using var engine = new RysyEngine();
engine.Run();
