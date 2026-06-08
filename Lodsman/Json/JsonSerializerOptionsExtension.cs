using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Lodsman.Json;

internal static class JsonSerializerOptionsExtension
{
    public static JsonSerializerOptions WithBaseTypeAttribute(this JsonSerializerOptions options)
    {
        var resolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = resolver.WithAddedModifier(static typeInfo =>
        {
            if (typeInfo.Type.IsValueType || typeInfo.Type.IsSealed)
                return;

            var childTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeInfo.Type));

            foreach (var childType in childTypes)
            {
                var attribute = childType.GetCustomAttribute<JsonBaseTypeAttribute>();
                if (attribute == null || attribute.BaseType != typeInfo.Type)
                    continue;

                typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions();
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(childType, attribute.TypeDiscriminator));
            }
        });

        return options;
    }
}
