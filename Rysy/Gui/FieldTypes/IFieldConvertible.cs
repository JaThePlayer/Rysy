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
/// Supplies a function that allows the <see cref="BindAttribute"/> to convert the value stored in the .bin file into alist of an arbitrary type
/// </summary>
public interface IFieldConvertibleToList {
    IReadOnlyList<T> ConvertMapDataValueToList<T>(object value);
}
