# Rysy

A WIP map viewer for Celeste written in C# and .NET 7.0 using Monogame.

If you wish to make maps, just use [Lönn](https://github.com/CelestialCartographers/Loenn) instead.

![image](https://user-images.githubusercontent.com/50085307/192340246-a6ba1e1f-c86e-452c-be2f-bd6d780113aa.png)

# Setup
Clone the repo, get a .NET 7 preview, and build. If you're on Windows, pick the net7.0-windows runtime to make Rysy use DirectX, giving *much* better texture load performance.

Run the program, and you'll be greeted with a black screen and a console window. The console window will tell you which config file to change to make Rysy actually run. If you got this far, you know what to do next :p

# Features
* Super fast load times (even faster than Lönn), thanks to multithreaded lazy loading.
* Renders all of 9D (at least the entities that have plugins :p) in ~1.5 seconds on my pc
* Colorful debug logs!
* yea, that's about it, just use lönn lol


# Why?
Because it's fun, and I also want to make some nice to work with rendering API for potential future projects.
