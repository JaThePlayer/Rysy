using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Scripting;

internal sealed class CheckDependenciesScript : Script {
    public override string Name => "Check Dependencies";

    public override IHistoryAction? Prerun(ScriptArgs args) {
        var map = args.Rooms.ElementAtOrDefault(0)?.Map;
        if (map is null)
            return null;

        var ctx = DependencyCheker.GetDependencies(map);

        ctx.Mods.LogAsJson();
        ctx.ModRequirementSources.Select(kv => new {
            Mod = kv.Key,
            Sources = kv.Value.Select(obj => obj switch {
                Decal d => $"{d.Name}:{d.Texture}",
                IName name => name.Name,
                _ => obj,
            }).Distinct()
        }).LogAsJson();

        if (map.Mod is { } mod)
            ctx.FindMissingDependencies(mod).LogAsJson();

        return null;
    }

    public override bool CallRun => false;
}
