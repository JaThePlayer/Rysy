namespace Rysy.Graphics;

public static class NodePathTypes
{
    /// <summary>
    /// Each node has a line connecting it to the main entity.
    /// </summary>
    public static IEnumerable<ISprite> Fan(Entity entity)
    {
        var nodes = entity.Nodes!;
        var start = entity.Center;

        for (int i = 0; i < nodes.Length; i++)
        {
            var end = entity.GetNodeCentered(i);

            yield return ISprite.Line(start, end, Color.White * .5f);
        }
    }

    /// <summary>
    /// Nodes are connected in one line, going from the main entity to the last node while passing by each other node
    /// </summary>
    public static IEnumerable<ISprite> Line(Entity entity)
    {
        var nodes = entity.Nodes!;
        var start = entity.Center;

        for (int i = 0; i < nodes.Length; i++)
        {
            var end = entity.GetNodeCentered(i);

            yield return ISprite.Line(start, end, Color.White * .5f);

            start = end;
        }
    }

    /// <summary>
    /// Nodes don't have any path connecting them.
    /// </summary>
    public static IEnumerable<ISprite> None = Array.Empty<ISprite>();
}