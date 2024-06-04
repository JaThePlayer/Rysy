#if !LuaSharpener
namespace LuaSharpener;

public interface ILuaTable {
    public object? this[object? key] { get; set; }
    public int Length { get; }
}
#endif