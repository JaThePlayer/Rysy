using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Mods;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Rysy.Graphics;

public class SpriteBank {
    public CacheToken LoadToken = new();

    public Dictionary<string, SpriteDef> Entries = new();

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

                            spr.Animations.Add(idXml.Value, new() {
                                Path = spr.Path + pathXml?.Value ?? "",
                                FirstFrame = inner.Attributes["frames"] is { } framesXml ? int.Parse(framesXml.Value.Split('-', '*', ',')[0]) : 0,
                            });
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

    public Cache<List<FoundTexture>> FindTextures(Regex regex) {
        var token = new CacheToken();
        var cache = new Cache<List<FoundTexture>>(token, () => {
            var list = new List<FoundTexture>();

            foreach (var (path, _) in Entries) {
                if (regex.Match(path) is { Success: true, Groups: [_, var secondGroup, ..] } match) {
                    list.Add(new(path, secondGroup.Value));
                }
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
        }
    }
}
