﻿using Rysy.Graphics;

namespace Rysy.Gui;

public record struct XnaWidgetDef(string ID, int W, int H, Action RenderFunc, Camera? Camera = null, bool Rerender = true);
