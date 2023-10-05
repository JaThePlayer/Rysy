using Neo.IronLua;
using Rysy.Graphics;
using System.Collections;
using System.Dynamic;

namespace Rysy.NeoLuaSupport;

public interface INeoLonnObject {
    public NeoLonnEntityHandler Handler { get; set; }
}

public abstract class NeoWrapper : DynamicObject {
    public abstract object? Index(string name);

    public override bool TryGetMember(GetMemberBinder binder, out object? result) {
        var name = binder.Name;

        result = Index(name);

        return true;
    }

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result) {
        if (indexes is not [string key]) {
            result = null;
            return true;
        }

        result = Index(key);
        return true;
    }
}

public sealed class NeoNodeWrapper : NeoWrapper {
    public Node Node { get; private set; }

    public NeoNodeWrapper(Node node) {
        Node = node;
    }

    public override object? Index(string name) {
        return name switch {
            "x" => Node.X,
            "y" => Node.Y,
            _ => null,
        };
    }
}

public sealed class NeoEntityWrapper : NeoWrapper {
    public Entity Entity { get; private set; }

    public NeoEntityWrapper(Entity entity) {
        Entity = entity;
    }

    public override object? Index(string name) {
        return name switch {
            "x" => Entity.X,
            "y" => Entity.Y,
            "_name" => Entity.Name,
            "_id" => Entity.ID,
            "nodes" => Entity.Nodes.Select(n => new NeoNodeWrapper(n)).ToLuaTable(),
            _ => Entity.EntityData.TryGetValue(name, out var ret) ? ret : null,
        };
    }
}

public class NeoLonnEntity : Entity, INeoLonnObject {
    internal NeoEntityWrapper Wrapper;

    public NeoLonnEntity() {
        Wrapper = new(this);
    }

    public override int Depth => 0;

    public NeoLonnEntityHandler Handler { get; set; }

    public override IEnumerable<ISprite> GetSprites() {
        NeoRoomWrapper room = new(Room);

        if (Handler.GetSprites is { } getSprites && getSprites(room, Wrapper) is { } sprites)
            return sprites;

        if (Handler.GetTexture is { } getTexture && getTexture(room, Wrapper) is { } texture) {
            return ISprite.FromTexture(Pos, texture) with {
                Origin = Handler.GetOrigin(room, Wrapper),
                Color = Handler.GetColor(room, Wrapper),
                Scale = Handler.GetScale(room, Wrapper),
                Rotation = Handler.GetRotation(room, Wrapper),
            };
        }

        return base.GetSprites();
    }
}

internal class NeoLonnTrigger : Trigger, INeoLonnObject {
    public NeoLonnEntityHandler Handler { get; set; }
}