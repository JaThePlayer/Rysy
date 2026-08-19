using AsmResolver.DotNet.Code.Cil;

namespace Rysy.Mods.RelinkSteps;

internal interface IRelinkStep {
    void Begin(RelinkCtx ctx);
}

internal interface IRelinkStepVisitCilMethods : IRelinkStep {
    void Visit(RelinkCtx ctx, CilMethodBody cilMethodBody);
}
