using Rysy.Graphics;

namespace Rysy.Helpers;

public static class NodeHelper
{
    /// <summary>
    /// Returns all sprites needed to render nodes for a given entity.
    /// This will take into account interfaces implemented on the entity type.
    /// </summary>
    public static IEnumerable<ISprite> GetNodeSpritesFor(Entity entity)
    {
        if (entity.Nodes is not { } nodes)
        {
            yield break;
        }


        var depth = entity.Depth;

        switch (entity)
        {
            case ICustomNodeHandler customHandler:
                foreach (var item in customHandler.GetNodeSprites())
                {
                    item.Depth ??= depth;
                    yield return item;
                }
                break;

            case INodeSpriteProvider nodeProvider:
                foreach (var item in GetNodeConnectors(entity))
                    yield return item;

                for (int i = 0; i < entity.Nodes.Length; i++)
                {
                    foreach (var item in nodeProvider.GetNodeSprites(i))
                    {
                        item.Depth ??= depth;

                        yield return item;
                    }
                }
                break;

            default:
                // no custom node implementation, do it ourselves
                foreach (var item in GetNodeConnectors(entity))
                    yield return item;

                foreach (var node in entity.Nodes)
                {
                    var oldPos = entity.Pos;
                    entity.Pos = node;
                    try
                    {
                        var spr = entity.GetSprites();
                        foreach (var item in spr)
                        {
                            item.Alpha *= .5f;
                            item.Depth ??= depth;
                            yield return item;
                        }
                    }
                    finally
                    {
                        entity.Pos = oldPos;
                    }
                }
                break;
        }


    }

    private static IEnumerable<LineSprite> GetNodeConnectors(Entity entity)
    {
        // TODO: Alternative options

        var start = entity.Center;
        if (entity.Nodes is { } nodes)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                var end = entity.GetNodeCentered(i);

                yield return ISprite.Line(start, end, Color.White * .5f) with
                {
                    Depth = entity.Depth + 1
                };
            }
        }
    }
}