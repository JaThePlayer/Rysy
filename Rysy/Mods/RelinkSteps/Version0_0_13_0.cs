using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace Rysy.Mods.RelinkSteps;

#pragma warning disable CS0618 // Type or member is obsolete

// ReSharper disable once InconsistentNaming
internal sealed class Version0_0_13_0 : IRelinkStepVisitCilMethods {
    private IMethodDescriptor? _mapGetRooms;
    
    public void Begin(RelinkCtx ctx) {
    }
    
    public void Visit(RelinkCtx ctx, CilMethodBody cilMethodBody) {
        foreach (var instr in cilMethodBody.Instructions) {
            if (instr.OpCode.Code != CilCode.Callvirt || instr.Operand is not MemberReference {
                    FullName: "System.Collections.Generic.List`1<Rysy.Room> Rysy.Map::get_Rooms()"
                }) {
                continue;
            }
            
            ctx.Logger.Info($"Relinking {instr} in {cilMethodBody.Owner}.");
            
            var newGetRooms = _mapGetRooms ??= ctx.AssemblyDefinition.ManifestModule!.DefaultImporter
                .ImportMethod(typeof(Version0_0_13_0_Backports).GetMethod(nameof(Version0_0_13_0_Backports.MapGetRooms))!);

            instr.Operand = newGetRooms;
            instr.OpCode = CilOpCodes.Call;
        }
    }
}

// ReSharper disable once InconsistentNaming
[Obsolete("Should only be used by the Relinker")]
public static class Version0_0_13_0_Backports {
    public static List<Room> MapGetRooms(Map map) {
        return map.Rooms.ToList();
    }
}
