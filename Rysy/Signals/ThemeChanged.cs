using Rysy.Gui;

namespace Rysy.Signals;

public record struct ThemeChanged(Theme NewTheme) : ISignal;