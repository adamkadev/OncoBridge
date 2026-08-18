using System.Reflection;

namespace OncoBridge.Api.Tests.Architecture;

internal static class ContractGraph
{
    private const BindingFlags PublicSurface =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    internal static string NameOf(Type type) => type.FullName ?? type.Name;

    internal static IEnumerable<Type> PropertyTypesOf(Type type)
    {
        foreach (PropertyInfo property in type.GetProperties(PublicSurface))
        {
            foreach (Type referenced in Expand(property.PropertyType))
            {
                yield return referenced;
            }
        }
    }

    internal static IEnumerable<Type> SignatureTypesOf(Type type)
    {
        foreach (Type argument in type.GetGenericArguments())
        {
            yield return argument;
        }

        if (type.BaseType is { } baseType)
        {
            yield return baseType;
        }

        foreach (Type contract in type.GetInterfaces())
        {
            yield return contract;
        }

        foreach (MemberInfo member in type.GetMembers(PublicSurface))
        {
            foreach (Type referenced in SignatureTypesOf(member))
            {
                yield return referenced;
            }
        }
    }

    private static IEnumerable<Type> Expand(Type type)
    {
        yield return type;

        foreach (Type argument in type.GetGenericArguments())
        {
            foreach (Type referenced in Expand(argument))
            {
                yield return referenced;
            }
        }
    }

    private static IEnumerable<Type> SignatureTypesOf(MemberInfo member)
    {
        switch (member)
        {
            case PropertyInfo property:
                yield return property.PropertyType;
                break;

            case FieldInfo field:
                yield return field.FieldType;
                break;

            case MethodBase method:
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    yield return parameter.ParameterType;
                }

                if (method is MethodInfo { ReturnType: { } returnType })
                {
                    yield return returnType;
                }

                break;

            case EventInfo { EventHandlerType: { } handler }:
                yield return handler;
                break;
        }
    }
}
