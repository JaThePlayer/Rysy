using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

/// <summary>
/// Supplies a function that allows the <see cref="BindAttribute"/> to convert the value stored in the .bin file into <typeparamref name="T"/>
/// </summary>
public interface IFieldConvertible<T> {
    T ConvertMapDataValue(object value);
}

/// <summary>
/// Supplies a function that allows the <see cref="BindAttribute"/> to convert the value stored in the .bin file into an arbitrary type
/// </summary>
public interface IFieldConvertible {
    T ConvertMapDataValue<T>(object value);
}

/// <summary>
/// Supplies a function that allows the <see cref="BindAttribute"/> to convert the value stored in the .bin file into a collection of an arbitrary type
/// </summary>
public interface IFieldConvertibleToCollection {
    ReadOnlyArray<T> ConvertMapDataValueToArray<T>(object value);
    
    ReadOnlyHashSet<T> ConvertMapDataValueToHashSet<T>(object value);
}
