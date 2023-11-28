using Rysy.Extensions;

namespace Rysy.Graphics;

public static class NodePathTypes {
    public static Color Color => Color.White * .3f;

    /// <summary>
    /// Each node has a line connecting it to the main entity.
    /// </summary>
    public static IEnumerable<ISprite> Fan(Entity entity) {
        var nodes = entity.Nodes!;
        var start = entity.Center;

        for (int i = 0; i < nodes.Count; i++) {
            var end = entity.GetNodeCentered(i);

            yield return ISprite.Line(start, end, Color);
        }
    }

    /// <summary>
    /// Nodes are connected in one line, going from the main entity to the last node while passing by each other node
    /// </summary>
    public static IEnumerable<ISprite> Line(Entity entity) {
        var nodes = entity.Nodes!;
        var start = entity.Center;

        for (int i = 0; i < nodes.Count; i++) {
            var end = entity.GetNodeCentered(i);

            yield return ISprite.Line(start, end, Color);

            start = end;
        }
    }

    /// <summary>
    /// Nodes are connected in one line, going from the main entity to the last node while passing by each other node.
    /// <paramref name="nodeToPos"/> gets called with the entity and node index for each node to calculate its position
    /// </summary>
    public static IEnumerable<ISprite> Line<TEntity>(TEntity entity, Func<TEntity, int, Vector2> nodeToPos)
        where TEntity : Entity {
        var nodes = entity.Nodes!;
        var start = entity.Center;

        for (int i = 0; i < nodes.Count; i++) {
            var end = nodeToPos(entity, i);

            yield return ISprite.Line(start, end, Color);

            start = end;
        }
    }

    public static IEnumerable<ISprite> Circle(Entity entity) {
        if (entity.Nodes is not [var node, ..])
            yield break;

        var pos = entity.Pos;
        var radius = Vector2.Distance(pos, node);

        yield return ISprite.Circle(pos, radius, Color, ((int) radius).AtLeast(12));
    }

    /// <summary>
    /// Nodes don't have any path connecting them.
    /// </summary>
    public static IEnumerable<ISprite> None => Array.Empty<ISprite>();
}