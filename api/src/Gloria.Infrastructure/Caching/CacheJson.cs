using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Gloria.Infrastructure.Caching;

/// <summary>
/// JSON options that can round-trip our flat domain entities (private parameterless constructor,
/// private property setters). Only used for the cache — the domain stays free of serialization attributes.
/// </summary>
internal static class CacheJson
{
    public static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(UsePrivateParameterlessCtor);
        resolver.Modifiers.Add(IncludePrivateSetters);

        return new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            PropertyNameCaseInsensitive = true,
        };
    }

    private static void UsePrivateParameterlessCtor(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.CreateObject is not null)
            return;

        var ctor = typeInfo.Type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, Type.EmptyTypes, modifiers: null);

        if (ctor is not null)
            typeInfo.CreateObject = () => ctor.Invoke(null);
    }

    private static void IncludePrivateSetters(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var prop in typeInfo.Properties)
        {
            if (prop.Set is not null) continue;

            var clrProp = typeInfo.Type.GetProperty(
                prop.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var setter = clrProp?.GetSetMethod(nonPublic: true);
            if (setter is null) continue;

            prop.Set = (obj, value) => setter.Invoke(obj, new[] { value });
        }
    }
}
