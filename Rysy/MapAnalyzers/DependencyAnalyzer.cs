using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Stylegrounds;
using Rysy.Tools;

namespace Rysy.MapAnalyzers;

public class DependencyAnalyzer : MapAnalyzer {
    public override void Analyze(AnalyzerCtx ctx) {
        if (ctx.Map.Mod is not { } mod) {
            ctx.AddResult(new NotInModResult());
            return;
        }

        var depCtx = DependencyChecker.GetDependencies(ctx.Map);

        var missing = depCtx.FindMissingDependencies(mod).ToListIfNotList();

        foreach (var dep in missing) {
            ctx.AddResult(new MissingDepResult(mod, dep, ModRegistry.GetModByName(dep), depCtx.GetDrawableDetailsFor(dep)));
        }
    }

    sealed record class NotInModResult() : IAnalyzerResult {
        public LogLevel Level => LogLevel.Info;

        public string Message => "rysy.analyzers.dependency.not_in_mod".Translate();

        public bool AutoFixable => false;

        public void Fix() {
            throw new NotImplementedException();
        }

        public void RenderDetailImgui() {
            
        }
    }

    sealed record class MissingDepResult(ModMeta BaseMod, string DepModName, ModMeta? DepModMeta, IImGuiDrawable DetailsDrawable) : IAnalyzerResult {
        private bool IsUnknown => DepModName == DependencyChecker.UnknownModName;

        public LogLevel Level => IsUnknown ? LogLevel.Warning : LogLevel.Error;

        public string Message => IsUnknown 
            ? "rysy.analyzers.dependency.missing.unknown".Translate()
            : "rysy.analyzers.dependency.missing".TranslateFormatted(ModMeta.ModNameToDisplayName(DepModName));

        public bool AutoFixable => !IsUnknown && DepModMeta is { } && BaseMod.Filesystem is IWriteableModFilesystem;

        public void RenderDetailImgui() {
            DetailsDrawable.DrawImGui();
        }

        public void Fix() {
            if (DepModMeta is null || BaseMod.Filesystem is not IWriteableModFilesystem)
                return;

            BaseMod.EverestYaml.First().Dependencies.Add(new() {
                Name = DepModMeta.Name,
                Version = DepModMeta.Version,
            });

            BaseMod.TrySaveEverestYaml();
        }
    }
}
