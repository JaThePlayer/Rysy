using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

namespace Rysy.Graphics;

public sealed class AnimatedTileBank {
    private static AnimatedTileData? _missingTile;
    
    private Dictionary<string, AnimatedTileData> _tiles;
    
    public XmlDocument Xml { get; private set; }
    
    public static AnimatedTileData MissingTile => _missingTile ??= new() {
        Name = "Missing",
        Delay = 1f,
        Frames = [ Gfx.Atlas["Rysy:tilesets/missingAnimatedTile"] ],
        Offset = new Vector2(-4f, -12f),
        Origin = new Vector2(0.0f),
    };
    
    public IReadOnlyDictionary<string, AnimatedTileData> Tiles => _tiles;

    public AnimatedTileBank() {
        _tiles = new();
    }

    public AnimatedTileData Get(string key) {
        ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(_tiles, key, out var existed);
        if (!existed || tile is null) {
            tile = new(MissingTile);
        }
        return tile;
    }

    public bool Has(string key) {
        if (!_tiles.TryGetValue(key, out AnimatedTileData? tile)) {
            return false;
        }

        return tile.Xml is { };
    }
    
    private string Name(XmlNode node)
        => node.Attributes?["name"]?.Value ?? "";

    public void ReadFromXml(Stream xmlStream) {
        var xml = new XmlDocument();
        xml.Load(xmlStream);

        Xml = xml;
        
        var data = xml["Data"] ?? throw new Exception("AnimatedTile .xml missing starting <Data> tag");
        
        HashSet<string> added = [];
        foreach (var n in data.ChildNodes.OfType<XmlNode>()) {
            added.Add(Name(n));
            ReadXmlNode(n);
        }

        foreach (var k in _tiles.Keys.ToList()) {
            if (!added.Contains(k))
                _tiles[k].Copy(MissingTile);
        }
    }

    public AnimatedTileData? ReadXmlNode(XmlNode node, AnimatedTileData? into = null) {
        var name = Name(node);
        var tile = into ?? (_tiles.TryGetValue(name, out var existing) ? existing : new AnimatedTileData() { Name = name, });
        var prevName = tile.Name;
        
        if (node.OwnerDocument != Xml)
        {
            using var reader = new XmlNodeReader(node);
            node = Xml.ReadNode(reader)!;
            tile.Xml = node;
        }

        if (AnimatedTileData.TryParse(node, tile)) {
            tile.Bank = this;
            if (prevName != tile.Name)
                _tiles.Remove(prevName);
            _tiles[tile.Name] = tile;
            return tile;
        }

        return null;
    }

    public bool Remove(string name) {
        if (!_tiles.TryGetValue(name, out var tile))
            return false;
        if (tile.Xml is {})
            Xml.DocumentElement!.RemoveChild(tile.Xml);
        
        tile.Copy(MissingTile);

        return true;
    }

    public bool Empty() => _tiles.Count == 0;
}

public sealed class AnimatedTileData : IXmlBackedEntityData {
    public string Name { get; set; } = "";

    public float Delay { get; set; }

    public Vector2 Offset { get; set; }

    public Vector2 Origin { get; set; }

    public IReadOnlyList<VirtTexture> Frames { get; set; }
    
    public XmlNode? Xml { get; set; }
    
    public AnimatedTileBank Bank { get; set; }
    
    string IXmlBackedEntityData.EntityDataName => Name;

    private AnimatedSpriteTemplate? _spriteTemplate;
    
    private EntityData? _fakeData;
    
    public AnimatedTileData() {
        
    }

    public AnimatedTileData(AnimatedTileData source) {
        Copy(source);
    }

    public void Copy(AnimatedTileData source) {
        Name = source.Name;
        Delay = source.Delay;
        Offset = source.Offset;
        Origin = source.Origin;
        Frames = source.Frames;
        Xml = source.Xml;
        Bank = source.Bank;

        _spriteTemplate = null;
        _fakeData = null;
    }

    public static bool TryParse(XmlNode xml, AnimatedTileData into) {
        into.Xml = xml;
        
        if (xml.Attributes is not { } attrs)
            return false;

        var data = new XmlNodeUntypedData(xml);
        
        into.Name = data.Attr("name");
        if (into.Name.IsNullOrWhitespace())
            return false;
        
        var path = data.Attr("path");
        if (path.IsNullOrWhitespace())
            return false;
        into.Frames = Gfx.Atlas.GetSubtextures(path);

        into.Delay = data.Float("delay");
        into.Offset = new(
            data.Int("posX"),
            data.Int("posY")
        );
        into.Origin = new(
            data.Int("origX"),
            data.Int("origY")
        );

        into._spriteTemplate = null;
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderAt(SpriteRenderCtx ctx, SpriteBatch b, Vector2 pos, Color color) {
        if (Frames is not [{ Texture: { } } firstTexture, ..] frames)
            return;
        
        if (_spriteTemplate is not { } template) {
            var animation = new SimpleAnimation(Frames, 1f / Delay);
            
            _spriteTemplate = template = new(SpriteTemplate.FromTexture(firstTexture, 0) with {
                Origin = Origin / new Vector2(firstTexture.Width, firstTexture.Height),
            }, animation);
        }
        
        template.RenderAt(ctx, pos + Offset.Add(4f, 4f), color, default, timeOffset: pos.SeededRandomExclusive(frames.Count) / Delay);
    }

    public void OnXmlChanged() {
        if (Xml is {})
            Bank.ReadXmlNode(Xml, this);
    }
    
    public EntityData FakeData => _fakeData ??= this.CreateFakeData();
    
    public FieldList GetFields() {
        FieldList fieldInfo = new(new {
            //name = "",
            path = Fields.AtlasPath("", "^(animatedTiles/.*)00$"),
            delay = 0f,
            posX = 0f,
            posY = 0f,
            origX = 0f,
            origY = 0f,
        });

        var fields = new FieldList();
        var order = new List<string>();

        foreach (var (k, f) in fieldInfo.OrderedEnumerable(this)) {
            fields.Add(k, f.CreateClone());
            order.Add(k);
        }

        // Take into account properties defined on this animated tile, even if not present in FieldInfo
        foreach (var (k, v) in FakeData) {
            if (k is "name")
                continue;
            
            if (fields.TryGetValue(k, out var knownFieldType)) {
                fields[k].SetDefault(v);
            } else {
                fields[k] = Fields.GuessFromValue(v, fromMapData: true)!;
                order.Add(k);
            }
        }
        
        fields.AddTranslations("rysy.tilesetWindow.anim.description", "rysy.tilesetWindow.anim.attribute");

        return fields.Ordered(order);
    }
}