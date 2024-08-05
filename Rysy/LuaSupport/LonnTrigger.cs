using Rysy.Graphics;
using Rysy.Helpers;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public sealed class LonnTrigger : Trigger, IHasLonnPlugin {
    private ListenableDictionaryRef<string, RegisteredEntity> _lonnPluginRef;
    
    ListenableDictionaryRef<string, RegisteredEntity> IHasLonnPlugin.LonnPluginRef { 
        get => _lonnPluginRef;
        set => _lonnPluginRef = value;
    }

    [JsonIgnore] 
    public LonnEntityPlugin? Plugin => _lonnPluginRef.TryGetValue(out var plugin, out var changed) 
        ? plugin.LonnPlugin
        : null;
    
    public override List<string>? AssociatedMods
        => Plugin?.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    public override Range NodeLimits => Plugin?.GetNodeLimits(Room, this) ?? base.NodeLimits;

    public override Point MinimumSize => Plugin?.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;

    public override string Text => Plugin?.TriggerText?.Invoke(Room, this) ?? base.Text;

    public string Category => Plugin?.TriggerCategory?.Invoke(Room, this) ?? "default";
    
    public override IEnumerable<ISprite> GetNodePathSprites() {
        var pl = Plugin;
        if (pl is null)
            return base.GetNodePathSprites();
        
        return pl.GetNodePathSprites(this);
    }
    
    public override IEnumerable<ISprite> GetAllNodeSprites() {
        if (Plugin is null)
            return [];

        return Plugin.PushToStack((pl) => {
            var visibility = pl.GetNodeVisibility(this);

            var visible = visibility switch {
                "always" => true,
                "selected" => true,
                var other => false,
            };

            if (!visible) {
                return [];
            }

            var sprites = base.GetAllNodeSprites();

            return sprites;
        });
    }
}
