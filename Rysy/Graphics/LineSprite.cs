﻿using Rysy.Extensions;
using System.Collections;

namespace Rysy.Graphics;

public record struct LineSprite : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
        };
    }

    public bool IsLoaded => true;

    public Vector2[] Positions { get; set; }

    public int Thickness { get; set; } = 1;
    public float MagnitudeOffset { get; set; } = 0f;

    public Vector2 Offset { get; set; } = default;

    public LineSprite(Vector2[] positions) {
        Positions = positions;
    }

    public LineSprite MovedBy(Vector2 by) => MovedBy(by.X, by.Y);

    public LineSprite MovedBy(float x, float y) {
        var positions = Positions;
        var newPositions = new Vector2[positions.Length];
        for (int i = 0; i < newPositions.Length; i++) {
            newPositions[i] = positions[i].Add(x, y);
        }

        return this with {
            Positions = newPositions,
        };
    }

    public void Render() {
        var b = GFX.Batch;
        var c = Color;
        for (int i = 0; i < Positions.Length - 1; i++) {
            var start = Positions[i];
            var end = Positions[i + 1];
            b.DrawLine(start, end, c, Thickness, Offset, MagnitudeOffset);
        }
    }

    public void Render(Camera? cam, Vector2 offset) {
        Render();
    }

    public ISelectionCollider GetCollider() {
        return ISelectionCollider.FromRect(RectangleExt.FromPoints(Positions));
    }
}
