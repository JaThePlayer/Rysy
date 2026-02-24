using Rysy.Mods;
using System.Reflection;

namespace Rysy.Signals;

public record struct ModAssemblyReloaded(ModMeta Mod, Assembly? OldAssembly, Assembly? NewAssembly) : ISignal;