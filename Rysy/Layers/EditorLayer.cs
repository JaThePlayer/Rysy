using Rysy.Components;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public interface IEditorLayer
{
    string Name { get; }
    string LocalizedName { get; }
    SelectionLayer SelectionLayer { get; }

    /// <summary>
    /// Prefix to use for language entries for materials. Leave null to not use translations.
    /// </summary>
    string? MaterialLangPrefix { get; }

    bool SupportsPreciseMoveMode { get; }
    int? ForcedGridSize { get; }

    IEnumerable<Placement> GetMaterials();

    Searchable GetMaterialSearchable(object material);

    ITooltip? GetMaterialTooltip(object material);
}

public abstract class EditorLayer : IHasComponentRegistry, IEditorLayer {
    public abstract string Name { get; }

    public virtual string LocalizedName => Name;
    
    public abstract SelectionLayer SelectionLayer { get; }

    public abstract IEnumerable<Placement> GetMaterials();

    public virtual Searchable GetMaterialSearchable(object material) {
        return material switch {
            Placement pl => new Searchable(GetPlacementName(pl), pl.GetAssociatedMods(), pl.GetTags(), pl.GetDefiningMod()?.Name) {
                AlternativeNames = GetPlacementAlternativeNames(pl),
            },
            IName name => new Searchable(name.Name),
            _ => new Searchable(material.ToStringInvariant())
        };
    }

    public virtual ITooltip? GetMaterialTooltip(object material) {
        if (material is not Placement pl)
            return null;

        if (pl.Tooltip is { } tooltip)
            return new Tooltip(tooltip);
        
        var prefix = MaterialLangPrefix;
        if (prefix is null)
            return null;

        return Tooltip.CreateTranslatedOrNull($"{prefix}.{pl.Sid}.placements.description.{pl.Name}");
    }

    private string GetPlacementName(Placement pl) {
        var prefix = MaterialLangPrefix;
        return prefix is null
            ? pl.Name 
            : pl.Name.TranslateOrHumanize(Interpolator.Temp($"{prefix}.{pl.Sid ?? ""}.placements.name"));
    }
    
    public IReadOnlyList<string> GetPlacementAlternativeNames(Placement pl) {
        var prefix = MaterialLangPrefix;
        if (prefix is null)
            return pl.AlternativeNames;
        return new TranslatedList<string>(pl.AlternativeNames, x => x, $"{prefix}.{pl.Sid ?? ""}.placements.name");
    }

    /// <summary>
    /// Prefix to use for language entries for materials. Leave null to not use translations.
    /// </summary>
    public virtual string? MaterialLangPrefix => null;
    
    public virtual bool SupportsPreciseMoveMode => true;

    public virtual int? ForcedGridSize => null;
    
    public IComponentRegistry? Registry { get; set; }
}