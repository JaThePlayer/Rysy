# Rysy

A WIP map viewer for Celeste written in C# using Monogame.

If you wish to make maps, just use [Lönn](https://github.com/CelestialCartographers/Loenn) instead.

![image](https://user-images.githubusercontent.com/50085307/192340246-a6ba1e1f-c86e-452c-be2f-bd6d780113aa.png)

# Setup
Clone the repo, get .NET 8, and build. If you're on Windows, pick the net7.0-windows runtime to make Rysy use DirectX, giving *much* better texture load performance.

Run the program, and it'll ask you to select a Celeste install, then a map .bin to load. You can drop both of these files onto Rysy's window.

# Features
* Super fast load times (even faster than Lönn), thanks to multithreaded lazy loading.
* Renders all of 9D (at least the entities that have plugins :p) in ~1.5 seconds on my pc
* Colorful debug logs!
* Doesn't lock mod zips forever :p
* yea, that's about it, just use lönn lol


# Why?
Because it's fun, and I also want to make some nice to work with rendering API for potential future projects.
