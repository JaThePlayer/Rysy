using Microsoft.Xna.Framework.Graphics.PackedVector;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Selections;
using System.Runtime.InteropServices;

#pragma warning disable CS0649

namespace Rysy.Stylegrounds.Modded;

[CustomEntity("CommunalHelper/Cloudscape")]
internal sealed class Cloudscape : LuaStyle, IPlaceable {
    [Bind("seed")] 
    internal string Seed = "";

    [Bind("hasBackgroundColor")]
    internal bool HasBackgroundColor;

    [Bind("bgColor")]
    private Color _sky;

    [Bind("lightning")]
    internal bool Lightning;
    
    [Bind("lightningMinDelay")]
    internal float LightningMinDelay;
    
    [Bind("lightningMaxDelay")]
    internal float LightningMaxDelay;

    [Bind("lightningFlashColor")]
    internal Color LightningFlashColor;

    [Bind("lightningMinDuration")]
    internal float LightningMinDuration;
    
    [Bind("lightningMaxDuration")]
    internal float LightningMaxDuration;
    
    [Bind("lightningIntensity")]
    internal float LightningIntensity;

    [Bind("innerRadius")]
    internal float InnerRadius;
    
    [Bind("outerRadius")]
    internal float OuterRadius;

    [Bind("innerDensity")]
    internal float InnerDensity;
    
    [Bind("outerDensity")]
    internal float OuterDensity;
    
    [Bind("innerRotation")]
    internal float InnerRotation;
    
    [Bind("outerRotation")]
    internal float OuterRotation;
    
    [Bind("rotationExponent")]
    internal float RotationExponent;

    [Bind("rings")]
    internal int Count;
    
    [Bind("colors")]
    internal ReadOnlyArray<Color> Colors;
    
    [Bind("lightningColors")]
    internal ReadOnlyArray<Color> LightningColors;

    [Bind("offsetX")] internal float _offsetX;
    [Bind("offsetY")] internal float _offsetY;
    internal Vector2 Offset => new(_offsetX, _offsetY);
    
    
    [Bind("parallaxX")] internal float _parallaxX;
    [Bind("parallaxY")] internal float _parallaxY;
    internal Vector2 Parallax => new(_parallaxX, _parallaxY);

    [Bind("alpha")]
    internal float BufferAlpha;
    
    public enum ZoomBehaviors
    {
        StaySame,
        Adjust,
    }


    [Bind("zoomBehavior")]
    internal ZoomBehaviors ZoomBehavior;
    
    internal Color Sky => HasBackgroundColor
        ? _sky
        : Color.Transparent;

    public static FieldList GetFields() {
        var baseFields = EntityRegistry.GetInfo("CommunalHelper/Cloudscape", RegisteredEntityType.Style)!
            .LonnStylePlugin!.FieldList!(null!);

        baseFields["bgColor"] = Fields.RGBA("4f9af7ff");
        baseFields["lightningFlashColor"] = Fields.RGBA("ffffff");
        baseFields["colors"] = new ListField(Fields.RGBA("ffffff"), "6d8adaff,aea0c1ff,d9cbbcff");
        baseFields["zoomBehavior"] = Fields.EnumNamesDropdown(ZoomBehaviors.StaySame);
        
        return baseFields;
    }

    public static PlacementList GetPlacements() => [];

    public override IEnumerable<ISprite> GetPreviewSprites() {
        _preview ??= new CloudscapeSprite(this);
        _preview.StylegroundCtx = null;
        return [ _preview ];
    }

    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        _sprite ??= new CloudscapeSprite(this);
        _sprite.StylegroundCtx = ctx;
        return [ _sprite ];
    }

    public override void OnChanged(EntityDataChangeCtx ctx) {
        base.OnChanged(ctx);
        _sprite = null;
        _preview = null;
    }

    private CloudscapeSprite? _sprite;
    private CloudscapeSprite? _preview;
}

internal static class CloudscapeResources {
    public static bool CanRender { get; private set; }

    private static bool _loaded;
    
    public static void LoadIfNeeded() {
        if (_loaded)
            return;
        _loaded = true;

        if (ModRegistry.GetModByName("CommunalHelper") is not { } ch) {
            CanRender = false;
            return;
        }

        var gd = GFX.Batch.GraphicsDevice;

        if (ch.Filesystem.TryReadAllBytes("Effects/CommunalHelper/cloudscape.cso") is not { } bytes) {
            Logger.Write("CommunalHelper.Cloudscape", LogLevel.Error, $"Failed to find cloudscape shader");
            CanRender = false;
            return;
        }
        
        Effect = new Effect(gd, bytes);
        Atlas = new Atlas();
        Atlas.LoadFromCrunchXml(ch, "Graphics/Atlases/CommunalHelper/Cloudscape/atlas.xml");

        CanRender = true;
    }
    
    public static Effect Effect { get; private set; }
    
    public static IAtlas Atlas { get; private set; }
}

internal sealed record CloudscapeSprite(Cloudscape scape) : ISprite {
    public StylegroundRenderCtx? StylegroundCtx { get; set; }
    
    private const uint LevelOfDetail = 16;
    
    public int? Depth { get; set; }
    public Color Color { get; set; }

    public ISprite WithMultipliedAlpha(float alpha) => this with { Color = Color * alpha, };
    
    public bool IsLoaded => true;
    
    public void Render(SpriteRenderCtx ctx) {
        CloudscapeResources.LoadIfNeeded();
        if (!CloudscapeResources.CanRender)
            return;
        
        var zoom = (StylegroundCtx?.Camera?.Scale ?? 1f);
        if (StylegroundCtx is { }) {
            zoom /= 6f;
        }

        if (zoom < (1f / 6f) && scape.ZoomBehavior == Cloudscape.ZoomBehaviors.Adjust) {
            return;
        }
        
        if (_mesh is null) {
            CreateMesh();
        }
        
        var bounds = StylegroundCtx?.FullScreenBounds ?? scape.PreviewRectangle();

        var zoomMode = scape.ZoomBehavior;

        var bufferSize = zoomMode switch {
            Cloudscape.ZoomBehaviors.StaySame => new Point(320, 180),
            Cloudscape.ZoomBehaviors.Adjust => new Point(bounds.Width.AtMost(1920), bounds.Height.AtMost(1080)),
            _ => new Point(320, 180),
        };

        using var buffer = RenderTargetPool.Get(bufferSize.X, bufferSize.Y);
        using var colorBuffer = RenderTargetPool.Get(_clouds.Length, 1);
        
        var gd = GFX.Batch.GraphicsDevice;

        var st = GFX.EndBatch();
        
        var prev = gd.GetRenderTargets();
        
        gd.SetRenderTarget(buffer.Target);
        gd.Clear(scape.Sky);
        gd.BlendState = BlendState.AlphaBlend;

        var effect = CloudscapeResources.Effect;

        EffectParameterCollection parameters = effect.Parameters;

        parameters["atlas_texture"].SetValue(CloudscapeResources.Atlas.GetTextures().First().texture.Texture);
        parameters["ring_count"].SetValue(_rings.Length);
        
        // calculate colors once for each cloud, and store them in the color buffer texture.
        // it will be sent to the gpu so it can be sampled, instead of changing the color of each vertex (old & slow method)
        for (int i = 0; i < _clouds.Length; i++)
        {
            var cloud = _clouds[i];
            cloud.Update(scape.Lightning, ctx.Animate ? Time.Delta : 0f, Random.Shared);
            _colors[i] = cloud.CalculateColor();
        }
        colorBuffer.Target.SetData(_colors);
        
        parameters["color_buffer_size"].SetValue(colorBuffer.Target.Width);
        parameters["color_texture"].SetValue(colorBuffer.Target);
        
        var translate = scape.Offset - (ctx.Camera?.ScreenToReal(Vector2.Zero).Floored() ?? new()) * scape.Parallax;
        
        
        //Console.WriteLine((zoom, 1f/6f, zoom.AtLeast(1f / 6f)));
        parameters["offset"].SetValue(scape.ZoomBehavior switch
        {
            Cloudscape.ZoomBehaviors.Adjust => translate / zoom,
            Cloudscape.ZoomBehaviors.StaySame => translate,
        });
        parameters["inner_rotation"].SetValue(scape.InnerRotation);
        parameters["outer_rotation"].SetValue(scape.OuterRotation);
        parameters["rotation_exponent"].SetValue(scape.RotationExponent);
        parameters["time"].SetValue(ctx.Time);
        parameters["dimensions"].SetValue(new Vector2(bufferSize.X, bufferSize.Y));

        var technique = effect.Techniques[0];
        foreach (var pass in technique.Passes)
        {
            pass.Apply();
            _mesh!.Draw();
        }

        // important because used by some vanilla celeste shader
        gd.SamplerStates[0] = SamplerState.LinearWrap;
        gd.SamplerStates[1] = SamplerState.LinearWrap;

        // present onto RT
        gd.SetRenderTargets(prev);

        GFX.BeginBatch(st);

        switch (zoomMode) {
            case Cloudscape.ZoomBehaviors.Adjust:
                GFX.Batch.Draw(buffer.Target, bounds, null, Color.White * scape.BufferAlpha);
                break;
            case Cloudscape.ZoomBehaviors.StaySame:
                GFX.Batch.Draw(buffer.Target, Vector2.Zero, null, Color.White * scape.BufferAlpha, 0f, Vector2.Zero, 1f / zoom, SpriteEffects.None, 0f);
                break;
        }
        
    }
    
    private Mesh<CloudscapeVertex>? _mesh;
    private WarpedCloud[] _clouds = [];
    private Ring[] _rings = [];
    private Color[] _colors = [];
    
    private void CreateMesh() {
        _mesh = new();

        var rng = new Random(scape.Seed.GetHashCode());

        List<WarpedCloud> clouds = new();
        List<Ring> rings = new();

        int count = scape.Count;

        float a = MathHelper.Min(scape.InnerRadius, scape.OuterRadius);
        float b = MathHelper.Max(scape.InnerRadius, scape.OuterRadius);
        float d = b - a;

        short id = 0; // cloud ID for color lookup

        // ring iteration
        for (short r = 0; r < count; r++)
        {
            float percent = (float) r / count;

            Color color = Util.ColorArrayLerp(percent * (scape.Colors.Count - 1), scape.Colors);
            float radius = a + (d * percent);
            float density = MathHelper.Lerp(scape.InnerDensity, scape.OuterDensity, percent);

            if (density == 0)
                continue;

            List<WarpedCloud> cloudsInRing = new();

            float rotation = rng.NextSingle() * MathHelper.TwoPi;

            // cloud iteration
            float angle = 0f;
            while (angle < MathHelper.TwoPi)
            {
                WarpedCloud cloud = new(scape, color);
                clouds.Add(cloud);
                cloudsInRing.Add(cloud);

                int index = _mesh.VertexCount;

                CloudscapeResources.LoadIfNeeded();
                if (!CloudscapeResources.CanRender)
                    return;
                var atlas = CloudscapeResources.Atlas;
                
                var texture = (ModSubtexture)rng.ChooseFrom(atlas.GetSubtextures("").ToList());
                float halfHeight = texture.Height / 2f;

                float centralAngle = texture.Width / radius;
                float step = centralAngle / LevelOfDetail;

                for (int i = 0; i < LevelOfDetail; i++)
                {
                    float th = rotation + angle + (step * i);

                    // custom vertices hold polar coordinates. cartesian coordinates are computed in the shader.
                    var clip = texture.ClipRect;
                    var tp = texture.ClipRect.Location.ToVector2();
                    
                    var leftUv = tp.X / texture.Parent.Width;
                    var rightUv = (tp.X + clip.Width) / texture.Parent.Width;
                    var topUv = tp.Y / texture.Parent.Height;
                    var bottomUv = (tp.Y + clip.Height) / texture.Parent.Height;
                    
                    float uvx = MathHelper.Lerp(
                        leftUv, rightUv,
                        (float) i / (LevelOfDetail - 1));
                    CloudscapeVertex closer = new(th, radius - halfHeight, new(uvx, topUv), id, r);
                    CloudscapeVertex farther = new(th, radius + halfHeight, new(uvx, bottomUv), id, r);
                    _mesh.AddVertices(closer, farther);

                    if (i != LevelOfDetail - 1)
                    {
                        int o = index + (i * 2);
                        _mesh.AddTriangle(o + 0, o + 1, o + 2);
                        _mesh.AddTriangle(o + 1, o + 2, o + 3);
                    }
                }

                ++id;
                angle += centralAngle / density;
            }

            // add ring to regroup clouds
            rings.Add(new(percent, cloudsInRing.ToArray()));
        }

        _mesh.Bake();

        _clouds = clouds.ToArray();
        _rings = rings.ToArray();
        _colors = new Color[_clouds.Length];

        /*
        int bytes = _mesh.VertexCount * CloudscapeVertex.VertexDeclaration.VertexStride;
        Logger.Write("Cloudscape", LogLevel.Info, $"[NEW-IMPL] Cloudscape meshes baked:");
        Logger.Write("Cloudscape", LogLevel.Info, $"  * {_mesh.VertexCount} vertices and {_mesh.Triangles} triangles ({_mesh.Triangles * 3} indices)");
        Logger.Write("Cloudscape", LogLevel.Info, $"  * Size of {bytes * 1e-3} kB = {bytes * 1e-6} MB ({bytes}o)");
        */
    }
    
    public ISelectionCollider GetCollider() => ISelectionCollider.FromRect(0, 0, 320, 180);
}

internal sealed class Ring
{
    public float Lerp { get; }
    private readonly WarpedCloud[] clouds;

    public Ring(float lerp, WarpedCloud[] clouds)
    {
        Lerp = lerp;
        this.clouds = clouds;
    }

    public void ApplyIdleColor(Color color, Color[] array)
    {
        for (int i = 0; i < clouds.Length; i++)
        {
            var cloud = clouds[i];
            cloud.IdleColor = color;
            array[i] = cloud.CalculateColor(force: true);
        }
    }
}

sealed class WarpedCloud {
    private readonly Cloudscape _parent;

    public Color IdleColor { get; set; }
    private Color _targetColorA, _targetColorB, _flashColor;

    private float _timer = 0f;
    private float _flashDuration = 1f, _flashTimer = 0f;
    private float _intensity;

    private float _oldPercent;

    private Color _color;

    public WarpedCloud(Cloudscape parent, Color idleColor) {
        _parent = parent;
        IdleColor = _color = idleColor;
        _timer = Random.Shared.Range(parent.LightningMinDelay, parent.LightningMaxDelay) * Random.Shared.NextSingle();
    }

    public Color CalculateColor(bool force = false) {
        float percent = _flashTimer / _flashDuration;
        if (_oldPercent == percent && !force)
            return _color;

        float sin = ((float) Math.Sin(percent * 10) + 1) / 2f;
        Color target = Color.Lerp(_targetColorA, _targetColorB, sin);
        Color lightning = Color.Lerp(IdleColor, target, Easing.BounceIn(percent) * (1 - Easing.CubeIn(percent)));
        _color = _intensity > 0 ? Color.Lerp(lightning, _flashColor, _intensity * Easing.ExpoIn(percent)) : lightning;

        _oldPercent = percent;

        return _color;
    }

    public void Update(bool allowLightning, float deltaTime, Random rng) {
        if (allowLightning) {
            _timer -= deltaTime;
            if (_timer <= 0) {
                _timer = rng.Range(_parent.LightningMinDelay, _parent.LightningMaxDelay);
                _flashColor = _parent.LightningFlashColor;
                _flashTimer = _flashDuration = rng.Range(_parent.LightningMinDuration, _parent.LightningMaxDuration);
                _intensity = /* Settings.Instance.DisableFlashes ? 0 :*/
                    _parent.LightningIntensity * Easing.CubeIn(rng.NextSingle());
                _targetColorA = Util.ColorArrayLerp(rng.NextSingle() * (_parent.LightningColors.Count - 1),
                    _parent.LightningColors);
                _targetColorB = Util.ColorArrayLerp(rng.NextSingle() * (_parent.LightningColors.Count - 1),
                    _parent.LightningColors);
            }
        }

        if (_flashTimer > 0)
            _flashTimer = _flashTimer.Approach(0, deltaTime);
    }
}

/// <summary>
/// Small utility class to handle groups of vertices and indices, and draw them.<br/>
/// Was originally written for another mod.
/// </summary>
/// <typeparam name="T">The <see cref="IVertexType"/> used in this mesh.</typeparam>
internal sealed class Mesh<T> where T : struct, IVertexType
{
    private List<T> _vertices = new();
    private List<int> _indices = new();

    /// <summary>
    /// The array of vertices.<br/>
    /// Remains <c>null</c> until this mesh is baked.
    /// </summary>
    public T[] Vertices { get; private set; }

    /// <summary>
    /// The array of indices.<br/>
    /// Remains <c>null</c> until this mesh is baked.
    /// </summary>
    public int[] Indices { get; private set; }

    /// <summary>
    /// The current amount of vertices in this mesh.
    /// </summary>
    public int VertexCount => Baked ? Vertices.Length : _vertices.Count;

    /// <summary>
    /// The current amount of triangles in this mesh.<br/>
    /// Always the number of indices divided by 3.
    /// </summary>
    public int Triangles { get; private set; }

    /// <summary>
    /// Whether this mesh has been baked or not.
    /// </summary>
    public bool Baked { get; private set; }

    /// <summary>
    /// Adds a single vertex to this mesh.<br/>
    /// This can only be done if the mesh is unbaked.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public void AddVertex(T vertex)
    {
        if (Baked)
            throw new InvalidOperationException("Cannot add a vertex to a baked mesh!");

        _vertices.Add(vertex);
    }

    /// <summary>
    /// Adds an array of vertices to this mesh.<br/>
    /// This can only be done if the mesh is unbaked.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public void AddVertices(params T[] vertices)
    {
        if (Baked)
            throw new InvalidOperationException("Cannot add vertices to a baked mesh!");

        _vertices.AddRange(vertices);
    }

    /// <summary>
    /// Adds the vertices and indices to make a quadrilated mesh.
    /// </summary>
    /// <param name="a">The 1st point.</param>
    /// <param name="b">The 2nd point.</param>
    /// <param name="c">The 3rd point.</param>
    /// <param name="d">The 4th point.</param>
    public void AddQuad(T a, T b, T c, T d)
    {
        if (Baked)
            throw new InvalidOperationException("Cannot add vertices to a baked mesh!");

        int o = VertexCount;

        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _vertices.Add(d);

        _indices.Add(o + 1);
        _indices.Add(o + 0);
        _indices.Add(o + 2);
        _indices.Add(o + 1);
        _indices.Add(o + 2);
        _indices.Add(o + 3);

        Triangles += 2;
    }

    /// <summary>
    /// Adds the vertices and indices to make a quadrilated mesh. Winding order is inverted.
    /// </summary>
    /// <param name="a">The 1st point.</param>
    /// <param name="b">The 2nd point.</param>
    /// <param name="c">The 3rd point.</param>
    /// <param name="d">The 4th point.</param>
    public void AddQuadInverted(T a, T b, T c, T d)
    {
        if (Baked)
            throw new InvalidOperationException("Cannot add vertices to a baked mesh!");

        int o = VertexCount;

        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _vertices.Add(d);

        _indices.Add(o + 2);
        _indices.Add(o + 0);
        _indices.Add(o + 1);
        _indices.Add(o + 3);
        _indices.Add(o + 2);
        _indices.Add(o + 1);

        Triangles += 2;
    }

    /// <summary>
    /// Adds a triangle to this mesh given the three indices of the triangle's vertices.<br/>
    /// This can only be done if the mesh is unbaked.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public void AddTriangle(int a, int b, int c)
    {
        if (Baked)
            throw new InvalidOperationException("Cannot add indices to a baked mesh!");

        _indices.Add(a);
        _indices.Add(b);
        _indices.Add(c);

        ++Triangles;
    }

    /// <summary>
    /// Creates the <see cref="Vertices"/> and <see cref="Indices"/> arrays that will be used for drawing.<br/>
    /// This can only be called once, and makes this mesh uneditable.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public void Bake()
    {
        if (Baked)
            throw new InvalidOperationException("Cannot bake mesh that was already baked!");

        Vertices = _vertices.ToArray();
        Indices = _indices.ToArray();

        Baked = true;
    }

    /// <summary>
    /// Draws the vertices.
    /// This can only be done if the mesh is baked.
    /// </summary>
    public void Draw()
    {
        if (!Baked)
            throw new InvalidOperationException("A mesh must be baked in order for its vertices to be drawn!");

        if (VertexCount == 0)
            return;

        GFX.Batch.GraphicsDevice.DrawUserIndexedPrimitives
        (
            PrimitiveType.TriangleList,
            Vertices, 0, VertexCount,
            Indices, 0, Triangles
        );
    }
}

/// <summary>
/// Custom vertex for Cloudscape meshes. (20 bytes)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
struct CloudscapeVertex : IVertexType
{
    public Vector2 Polar; // { angle, distance }
    public Vector2 Texture; // uv
    public Short2 IndexRing; // { cloud_id, ring_idx }

    public static readonly VertexDeclaration VertexDeclaration = new
    (
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(16, VertexElementFormat.Short2, VertexElementUsage.TextureCoordinate, 2)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public CloudscapeVertex(float angle, float distance, Vector2 texture, short index, short ring)
    {
        Polar = new(angle, distance);
        Texture = texture;
        IndexRing = new(index, ring);
    }
}

file sealed class Util 
{
    public static Color ColorArrayLerp(float lerp, ReadOnlyArray<Color> colors)
    {
        float m = Mod(lerp, colors.Count);
        int fromIndex = (int) Math.Floor(m);
        int toIndex = Mod(fromIndex + 1, colors.Count);
        float clampedLerp = m - fromIndex;

        return Color.Lerp(colors[fromIndex], colors[toIndex], clampedLerp);
    }
    
    public static float Mod(float x, float m)
    {
        return ((x % m) + m) % m;
    }

    public static int Mod(int x, int m)
    {
        return ((x % m) + m) % m;
    }
}