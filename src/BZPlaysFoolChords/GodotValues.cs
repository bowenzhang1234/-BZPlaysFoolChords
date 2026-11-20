using System.Reflection;

namespace BZPlaysFoolChords;

/// <summary>
/// Godot method and property parameters are often StringName or NodePath, not System.String.
/// Reflection does not apply the implicit conversion, so callers must construct the Godot type.
/// </summary>
public static class GodotValues
{
    public static object? Adapt(object? value, Type target)
    {
        if (value == null || target.IsInstanceOfType(value))
            return value;

        if (value is not string text || !IsGodotText(target))
            return value;

        var constructed = target.GetConstructor([typeof(string)])?.Invoke([text]);
        if (constructed != null)
            return constructed;

        foreach (var method in target.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "op_Implicit")
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string) && target.IsAssignableFrom(method.ReturnType))
                return method.Invoke(null, [text]);
        }

        return value;
    }

    public static object?[] AdaptArguments(ParameterInfo[] parameters, params object?[] provided)
    {
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < provided.Length && provided[i] != null)
                args[i] = Adapt(provided[i], parameters[i].ParameterType);
            else if (parameters[i].HasDefaultValue)
                args[i] = parameters[i].DefaultValue;
            else if (parameters[i].ParameterType.IsValueType)
                args[i] = Activator.CreateInstance(parameters[i].ParameterType);
            else
                args[i] = null;
        }

        return args;
    }

    public static void Set(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName);
        if (property == null || !property.CanWrite || value == null)
            return;

        property.SetValue(target, Adapt(value, property.PropertyType));
    }

    static bool IsGodotText(Type target) =>
        target.Name is "StringName" or "NodePath" && target.Namespace == "Godot";
}
