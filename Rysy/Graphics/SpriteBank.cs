using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Mods;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Rysy.Graphics;

public class SpriteBank {
    public CacheToken LoadToken = new();

    public Dictionary<string, SpriteDef> Entries = new();

    public bool Exists(string key, string anim, IAtlas atlas) {
        if (!Entries.TryGetValue(key, out var spriteDef)) 
            return false;

        if (!spriteDef.Animations.TryGetValue(anim, out var animDef))
            return false;

        return animDef.TextureExists(atlas);
    }

    public bool Loaded = false;

    public SpriteDef? Get(string id) {
        Entries.TryGetValue(id, out var sprite);

        return sprite;
    }

    public SpriteBank() {

    }

    public SpriteBank(Stream xmlStream, ModMeta? mod) {
        Load(xmlStream, mod);
    }

    public void Clear() {
        Entries.Clear();
    }

    public void Load(Stream xmlStream, ModMeta? mod) {
        var xml = new XmlDocument();
        xml.Load(xmlStream);

        var sprites = xml["Sprites"];
        if (sprites is null)
            return;

        foreach (var obj in sprites) {
            if (obj is not XmlElement spriteXml || spriteXml.Attributes["path"] is not { } path) {
                continue;
            }

            var spr = new SpriteDef();
            spr.Mod = mod;
            spr.Path = path.Value;

            if (spriteXml.Attributes["copy"] is { Value: not null } copyXml) {
                if (!Entries.TryGetValue(copyXml.Value, out var copyFrom)) {
                    Logger.Write("SpriteBank", LogLevel.Warning, $"Sprite {spriteXml.Name} tried to copy from {copyXml.Value}, which doesn't exist!");
                    continue;
                }

                spr.Animations = copyFrom.Animations.ToDictionary(kv => kv.Key, kv => new SpriteDef.Animation() {
                    FirstFrame = kv.Value.FirstFrame,
                    Path = spr.Path + kv.Value.Path.TrimStart(copyFrom.Path),
                });
            }

            foreach (var inner in spriteXml.OfType<XmlElement>()) {
                switch (inner.Name) {
                    case "Justify":
                        if (inner.Attributes["x"] is { } x && inner.Attributes["y"] is { } y) {
                            spr.Origin = new(float.Parse(x.Value, CultureInfo.InvariantCulture), float.Parse(y.Value, CultureInfo.InvariantCulture));
                        }
                        break;
                    case "Anim" or "Loop":
                        if (inner.Attributes["id"] is { } idXml) {
                            var pathXml = inner.Attributes["path"];

                            spr.Animations[idXml.Value] = new() {
                                Path = spr.Path + pathXml?.Value ?? "",
                                FirstFrame = inner.Attributes["frames"] is { } framesXml ? int.Parse(framesXml.Value.Split('-', '*', ',')[0], CultureInfo.InvariantCulture) : 0,
                            };
                        }
                        break;
                    default:
                        break;
                }
            }

            //Console.WriteLine(spr.ToJson());
            Entries[spriteXml.Name] = spr;
        }

        Loaded = true;
        LoadToken.Invalidate();
        LoadToken.Reset();
    }

    public Cache<List<FoundPath>> FindTextures(Regex regex) {
        var token = new CacheToken();
        var cache = new Cache<List<FoundPath>>(token, () => {
            var list = new List<FoundPath>();

            foreach (var (path, _) in Entries) {
                if (FoundPath.Create(path, regex) is {} found)
                    list.Add(found);
            }

            token.Reset();

            return list;
        });

        LoadToken.OnInvalidate += cache.Token.Invalidate;

        return cache;
    }

    public record class SpriteDef {
        public string Path;

        public Vector2 Origin;

        public Dictionary<string, Animation> Animations = new();

        [JsonIgnore]
        public ModMeta? Mod;

        public record class Animation {
            public string Path;
            public int FirstFrame;

            public VirtTexture GetTexture(IAtlas atlas) {
                return atlas[Path, FirstFrame];
            }

            public bool TextureExists(IAtlas atlas) {
                return atlas.Exists(Path, FirstFrame);
            }
        }
    }
}
