using Rysy.Extensions;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Rysy.Graphics;

public sealed class AnimatedTileBank {
    private Dictionary<string, AnimatedTileData> _tiles;

    public AnimatedTileBank() {
        _tiles = new();
    }
    
    public AnimatedTileBank(Dictionary<string, AnimatedTileData> tiles) {
        _tiles = tiles;
    }

    public AnimatedTileData? Get(string key) {
        return _tiles.GetValueOrDefault(key);
    }

    public void ReadFromXml(Stream xmlStream) {
        var xml = new XmlDocument();
        xml.Load(xmlStream);
        
        var data = xml["Data"] ?? throw new Exception("AnimatedTile .xml missing starting <Data> tag");

        var tiles = data.ChildNodes.OfType<XmlNode>()
            .Where(n => n.Name == "sprite")
            .Select(n => new AnimatedTileData(n))
            .ToDictionary(t => t.Name, t => t);

        _tiles = tiles;
    }

    public bool Empty() => _tiles.Count == 0;
}

public sealed class AnimatedTileData {
    public string Name { get; set; }

    public float Delay { get; set; }

    public Vector2 Offset { get; set; }

    public Vector2 Origin { get; set; }

    public IReadOnlyList<VirtTexture> Frames { get; set; }
    
    public AnimatedTileData() {
        
    }

    public AnimatedTileData(XmlNode xml) {
        if (xml.Attributes is not { } attrs)
            return;
        
        Name = attrs["name"]?.Value ?? throw new Exception("Missing name for Animated Tile");
        var path = attrs["path"]?.Value ?? throw new Exception("Missing path for Animated Tile");
        Frames = GFX.Atlas.GetSubtextures(path);

        Delay = attrs["delay"]?.Value.ToSingle() ?? 0f;
        Offset = new(
                attrs["posX"]?.Value.ToInt() ?? 0,
                attrs["posY"]?.Value.ToInt() ?? 0
            );
        
        Origin = new(
            attrs["origX"]?.Value.ToInt() ?? 0,
            attrs["origY"]?.Value.ToInt() ?? 0
        );
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderAt(SpriteRenderCtx ctx, SpriteBatch b, Vector2 pos, Color color) {
        var frames = Frames;
        var time = Time.Elapsed / Delay;
        time += pos.SeededRandomExclusive(frames.Count);
        var texture = frames[(int)time % frames.Count]; //pos.SeededRandomFrom(Frames);

        if (texture.Texture is { } t) {
            new Sprite(texture) {
                Pos = pos + Offset.Add(4f, 4f),
                Origin = Origin / new Vector2(texture.Width, texture.Height),
                Color = color
            }.Render(ctx);
        }
    }
}