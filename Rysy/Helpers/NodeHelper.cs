using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Helpers;

public static class NodeHelper {
    private static DefaultNodeSpriteProvider defaultNodeSpriteProvider = new();

    /// <summary>
    /// Returns all sprites needed to render a given node for a given entity.
    /// This will take into account interfaces implemented on the entity type.
    /// </summary>
    public static IEnumerable<ISprite> GetNodeSpritesForNode(Entity entity, int nodeId) {
        if (entity.Nodes is not { } nodes) {
            yield break;
        }

        var depth = entity.Depth;

        switch (entity) {
            case ICustomNodeHandler customHandler:
                foreach (var item in customHandler.GetNodeSprites()) {
                    item.Depth ??= depth;
                    yield return item;
                }
                break;

            default:
                INodeSpriteProvider provider = entity switch {
                    INodeSpriteProvider p => p,
                    _ => defaultNodeSpriteProvider.For(entity)
                };

                foreach (var item in provider.GetNodeSprites(nodeId)) {
                    item.Depth ??= depth;

                    yield return item;
                }
                break;
        }
    }

    /// <summary>
    /// Returns all sprites needed to render nodes for a given entity.
    /// This will take into account interfaces implemented on the entity type.
    /// </summary>
    public static IEnumerable<ISprite> GetNodeSpritesFor(Entity entity, bool includeConnectors = true) {
        if (entity.Nodes is not { } nodes) {
            return Array.Empty<ISprite>();
        }


        var depth = entity.Depth;

        return entity switch {
            ICustomNodeHandler customHandler => customHandler.GetNodeSprites().Apply(s => s.Depth ??= depth),
            _ => GetGuessedNodeSpritesFor(entity, includeConnectors),
        };
    }

    /// <summary>
    /// Returns all sprites needed to render nodes for a given entity.
    /// Rysy will guess how this should be done, bypassing <see cref="ICustomNodeHandler"/>
    /// </summary>
    public static IEnumerable<ISprite> GetGuessedNodeSpritesFor(Entity entity, bool includeConnectors = true) {
        if (entity.Nodes is not { } nodes) {
            yield break;
        }

        var depth = entity.Depth;

        INodeSpriteProvider provider = entity switch {
            INodeSpriteProvider p => p,
            _ => defaultNodeSpriteProvider.For(entity)
        };

        if (includeConnectors && GetNodeConnectors(entity) is { } connectors)
            foreach (var item in connectors) {
                item.Depth = depth + 1;
                yield return item;
            }

        for (int i = 0; i < entity.Nodes.Count; i++) {
            foreach (var item in provider.GetNodeSprites(i)) {
                item.Depth ??= depth;

                yield return item;
            }
        }
    }

    private static IEnumerable<ISprite>? GetNodeConnectors(Entity entity) {
        if (entity.Nodes is { } nodes) {
            return entity switch {
                INodePathProvider p => p.GetNodePathSprites(),
                _ => NodePathTypes.Line(entity),
            };
        } else {
            return null;
        }
    }


    private sealed class DefaultNodeSpriteProvider : INodeSpriteProvider {
        private Entity _entity;

        /// <summary>
        /// Swaps this instance of the provider to use this entity. 
        /// Make sure to call GetNodeSprites before calling this again so that the enumerable can grab the reference to the entity!
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public INodeSpriteProvider For(Entity entity) {
            _entity = entity;
            return this;
        }

        public IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
            var entity = _entity;
            var node = entity.Nodes![nodeIndex];
            var oldPos = entity.Pos;
            entity.Pos = node;
            try {
                var spr = entity.GetSprites();
                foreach (var item in spr) {
                    item.MultiplyAlphaBy(.5f);
                    yield return item;
                }
            } finally {
                entity.Pos = oldPos;
            }
        }
    }

}