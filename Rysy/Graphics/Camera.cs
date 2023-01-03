using Microsoft.Xna.Framework.Graphics;

namespace Rysy.Graphics;

public class Camera
{
    private Vector2 _pos;
    public Vector2 Pos => _pos;
    public float Scale = 1f;

    public Viewport Viewport = RysyEngine.GDM.GraphicsDevice.Viewport;

    public Camera()
    {
        RysyEngine.OnViewportChanged += (v) => Viewport = v;
    }

    /// <summary>
    /// The inverse of <see cref="Matrix"/>
    /// </summary>
    public Matrix Inverse => Matrix.Invert(Matrix);

    public Matrix Matrix =>
          Matrix.CreateTranslation(-MathF.Floor(Pos.X), -MathF.Floor(Pos.Y), 0f)
        * Matrix.CreateScale(Scale);

    /// <summary>
    /// Moves the camera the specified amount.
    /// </summary>
    public void Move(Vector2 amount)
    {
        _pos += amount;
    }

    /// <summary>
    /// Moves the camera to the specified position.
    /// </summary>
    public void Goto(Vector2 position)
    {
        _pos = position;
    }

    /// <summary>
    /// Centers the camera on the specified REAL position.
    /// </summary>
    /// <param name="position">The position to center on.</param>
    public void CenterOnRealPos(Vector2 position)
    {
        _pos = Vector2.Floor(position - new Vector2(Viewport.Width / 2, Viewport.Height / 2) / Scale);
    }

    public void CenterOnScreenPos(Vector2 position)
    {
        CenterOnRealPos(ScreenToReal(position));
    }

    public void CenterOnMousePos()
    {
        CenterOnScreenPos(Input.Mouse.Pos.ToVector2());
    }

    /// <summary>
    /// Zooms the camera in.
    /// </summary>
    public void ZoomIn()
    {
        DoZoom(Scale * 2f);
    }

    private void DoZoom(float newZoom)
    {
        var rp = ScreenToReal(Input.Mouse.Pos.ToVector2());
        Scale = newZoom;
        var rp2 = ScreenToReal(Input.Mouse.Pos.ToVector2());

        _pos += rp - rp2;
    }

    /// <summary>
    /// Zooms the camera out.
    /// </summary>
    /// <param name="pos">The position to center the camera on when zooming.</param>
    public void ZoomOut()
    {
        DoZoom(Scale / 2f);
    }

    /// <summary>
    /// Converts a real (level/map) position to a position on the screen.
    /// </summary>
    /// <param name="pos">The position to convert.</param>
    /// <returns>The converted position.</returns>
    public Vector2 RealToScreen(Vector2 pos) => Vector2.Transform(pos, Matrix);

    /// <summary>
    /// Converts a screen position to a real (level/map) position.
    /// </summary>
    /// <param name="pos">The position to convert.</param>
    /// <returns>The converted position.</returns>
    public Vector2 ScreenToReal(Vector2 pos) => Vector2.Transform(pos, Inverse);

    /// <summary>
    /// Checks whether the given rectangle is visible inside of the camera at a given camera position
    /// </summary>
    public bool IsRectVisible(Rectangle rect)
    {
        var (x, y) = RealToScreen(rect.Location.ToVector2());
        var w = rect.Width * Scale;
        var h = rect.Height * Scale;

        return Viewport.Bounds.Intersects(new Rectangle((int)x, (int)y, (int)w, (int)h));
    }

    /// <summary>
    /// Checks whether the given rectangle is visible inside of the camera at a given camera position
    /// </summary>
    public bool IsRectVisible(Vector2 pos, int width, int height)
    {
        var (x, y) = RealToScreen(pos);
        var w = width * Scale;
        var h = height * Scale;

        return Viewport.Bounds.Intersects(new Rectangle((int)x, (int)y, (int)w, (int)h));
    }

    public void HandleMouseMovement()
    {
        // Right click drag - move camera
        if (Input.Mouse.Right.Held() && Input.Mouse.PositionDelta != default)
        {
            Move(-Input.Mouse.PositionDelta.ToVector2() / Scale);
        }

        // Scrolled - zoom camera
        switch (Input.Mouse.ScrollDelta)
        {
            case > 0: ZoomIn(); break;
            case < 0: ZoomOut(); break;
        }
    }
}