using JetBrains.Annotations;
using Rysy.Gui.FieldTypes;
using System.Reflection;
using System.Reflection.Emit;

namespace Rysy;

/// <summary>
/// Provides methods necessary for <see cref="BindAttribute"/> to work
/// </summary>
public interface IBindTarget {
    string Name { get; }

    public FieldList GetFields();

    public object GetValueForField(Field field, string key);
}

/// <summary>
/// Binds values from EntityData to fields declared on your type, with caching.
/// The value gets converted to your field's type by using the <see cref="Field"/> defined in your <see cref="FieldInfo"/> for this field name.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
[MeansImplicitUse]
public sealed class BindAttribute : Attribute {
    public string FieldName { get; }

    public BindAttribute(string fieldName) {
        FieldName = fieldName;
    }

    static Dictionary<string, object> ContextCache = new();

    internal static List<Field> _boundFields = new();

    internal static Ctx<BaseT> GetBindContext<BaseT>(IBindTarget entity) where BaseT : IBindTarget {
        var entityType = entity.GetType();

        if (ContextCache.TryGetValue(entity.Name, out var cached) && cached is Ctx<BaseT> cachedCtx && cachedCtx.Type == entityType) return cachedCtx;

        var ctx = new Ctx<BaseT>();
        ctx.Type = entityType;

        var bindAttrs = entityType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(f => f.GetCustomAttribute<BindAttribute>() is { } attr ? (f, attr) : (null, null))
            .Where(p => p.Item1 is not null)
            .ToList();

        if (bindAttrs.Count > 0) {
            var fieldList = entity.GetFields();

            foreach (var bind in bindAttrs) {
                var fieldInfo = bind.Item1!;
                var attr = bind.Item2!;

                var method = new DynamicMethod($"Rysy.Attributes.BindAttribute.Glue<{entityType.Name}>.{fieldInfo.Name}", null, new Type[] { typeof(BaseT) });
                var il = method.GetILGenerator();

                if (!fieldList.TryGetValue(attr.FieldName, out var field)) {
                    throw new Exception($"{entityType} tried to [Bind] field {attr.FieldName}, which is not defined by {nameof(IPlaceable.GetFields)}");
                }

                var converterMethod = FindConverterMethod(entityType, fieldInfo, attr, field);

                var loadValueFromEntity = entity.GetValueForField;
                var loadField = LoadFieldByBindId;

                _boundFields.Add(field);
                var fieldLocal = il.DeclareLocal(typeof(Field));

                il.Emit(OpCodes.Ldarg_0);

                // load the right Field instance
                il.Emit(OpCodes.Ldc_I4, _boundFields.Count - 1);
                il.Emit(OpCodes.Call, loadField.Method);
                il.Emit(OpCodes.Stloc, fieldLocal);
                il.Emit(OpCodes.Ldloc, fieldLocal);

                // load right value from entity data
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc, fieldLocal);
                il.Emit(OpCodes.Ldstr, attr.FieldName);
                il.Emit(OpCodes.Call, loadValueFromEntity.Method);

                // call the converter function to map to T
                il.Emit(OpCodes.Callvirt, converterMethod);

                // set the right field on the entity
                il.Emit(OpCodes.Stfld, fieldInfo);
                il.Emit(OpCodes.Ret);

                ctx.UpdateFuncs[attr.FieldName] = method.CreateDelegate<Action<BaseT>>();
            }
        }

        ContextCache[entity.Name] = ctx;

        return ctx;
    }

    private static MethodInfo FindConverterMethod(Type entityType, FieldInfo fieldInfo, BindAttribute attr, Field field) {
        MethodInfo? converterMethod = null;
        var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        var argumentTypeList = new Type[] { typeof(object) };

        var convertibleType = typeof(IFieldConvertible<>).MakeGenericType(fieldInfo.FieldType);
        if (field.GetType().IsAssignableTo(convertibleType)) {
            converterMethod =
                 convertibleType.GetMethod(nameof(IFieldConvertible<int>.ConvertMapDataValue), bindingFlags, argumentTypeList)
              ?? field.GetType().GetMethod(nameof(IFieldConvertible<int>.ConvertMapDataValue), bindingFlags, argumentTypeList);

        }

        if (converterMethod is null && field.GetType().IsAssignableTo(typeof(IFieldConvertible))) {
            converterMethod =
              typeof(IFieldConvertible).GetMethod(nameof(IFieldConvertible.ConvertMapDataValue), bindingFlags, argumentTypeList)
              ?? field.GetType().GetMethod(nameof(IFieldConvertible.ConvertMapDataValue), bindingFlags, argumentTypeList);

            converterMethod = converterMethod?.MakeGenericMethod(fieldInfo.FieldType);
        }

        if (converterMethod is null && field.GetType().IsAssignableTo(typeof(IFieldConvertibleToList))) {
            if (fieldInfo.FieldType.GetGenericTypeDefinition() != typeof(IReadOnlyList<>)) {
                throw new Exception($"""
                    {entityType} tried to [Bind] field {attr.FieldName} to type {fieldInfo.FieldType}, but {field.GetType()} does not implement {convertibleType} or {typeof(IFieldConvertible)}
                    It implements {typeof(IFieldConvertibleToList)}, but the type of {attr.FieldName} does not extend {typeof(IReadOnlyList<>)}.
                    """);
            }

            converterMethod =
                 typeof(IFieldConvertibleToList).GetMethod(nameof(IFieldConvertibleToList.ConvertMapDataValueToList), bindingFlags, argumentTypeList)
              ?? field.GetType().GetMethod(nameof(IFieldConvertibleToList.ConvertMapDataValueToList), bindingFlags, argumentTypeList);

            converterMethod = converterMethod?.MakeGenericMethod(fieldInfo.FieldType.GetGenericArguments()[0]);
        }

        if (converterMethod is null) {
            throw new Exception($"{entityType} tried to [Bind] field {attr.FieldName} to type {fieldInfo.FieldType}, but {field.GetType()} does not implement {convertibleType} or {typeof(IFieldConvertible)} or {typeof(IFieldConvertibleToList)}");
        }

        return converterMethod;
    }

    internal static Field LoadFieldByBindId(int id) => _boundFields[id];

    internal static object LoadValueFromEntity(Field field, Entity entity, string key) {
        if (entity.EntityData.TryGetValue(key, out var value))
            return value;

        return field.GetDefault();
    }

    internal class Ctx<T> where T : IBindTarget {
        internal Dictionary<string, Action<T>> UpdateFuncs = new(StringComparer.Ordinal);

        internal Type Type;

        public void UpdateBoundFields(T entity, EntityDataChangeCtx changed) {
            if (entity.GetType() != Type) return;

            if (changed.AllChanged) {
                foreach (var (f, v) in UpdateFuncs) {
                    v(entity);
                }
                return;
            }

            if (changed.ChangedFieldName is { } field && UpdateFuncs.TryGetValue(field, out var updater)) {
                updater(entity);
            }
        }
    }
}
