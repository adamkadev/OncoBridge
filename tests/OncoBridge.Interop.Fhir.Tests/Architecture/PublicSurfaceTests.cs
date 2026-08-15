using System.Reflection;
using OncoBridge.Domain.Temporal;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Architecture;

public sealed class PublicSurfaceTests
{
    private const string Fhir = "Hl7.Fhir";

    [Fact]
    public void The_domain_exposes_no_FHIR_type_through_its_public_surface() =>
        AssertNoFhirInPublicSurface(typeof(PartialDate).Assembly);

    [Fact]
    public void Interop_Fhir_exposes_no_FHIR_type_through_its_public_surface() =>
        AssertNoFhirInPublicSurface(typeof(FhirNormalizer).Assembly);

    [Fact]
    public void The_normalizer_entry_point_accepts_and_returns_domain_types_only()
    {
        MethodInfo normalize = typeof(FhirNormalizer).GetMethod(nameof(FhirNormalizer.Normalize))!;

        Type[] signature =
        [
            .. normalize.GetParameters().Select(parameter => parameter.ParameterType),
            normalize.ReturnType,
        ];

        Assert.All(signature, type => Assert.False(IsFhir(NameOf(type))));
    }

    private static void AssertNoFhirInPublicSurface(Assembly assembly)
    {
        string[] offenders =
        [
            .. assembly.GetExportedTypes()
                .SelectMany(SignatureTypesOf)
                .Select(NameOf)
                .Where(IsFhir)
                .Distinct()
                .Order(),
        ];

        Assert.True(
            offenders.Length == 0,
            $"{assembly.GetName().Name} must not expose {Fhir}.* through its public surface, "
                + $"but exposes: {string.Join(", ", offenders)}.");
    }

    private static IEnumerable<Type> SignatureTypesOf(Type type)
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

        const BindingFlags PublicSurface = BindingFlags.Public | BindingFlags.Static
            | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (MemberInfo member in type.GetMembers(PublicSurface))
        {
            foreach (Type referenced in SignatureTypesOf(member))
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

    private static string NameOf(Type type) => type.FullName ?? type.Name;

    private static bool IsFhir(string typeName) =>
        typeName.StartsWith(Fhir, StringComparison.Ordinal)
        || typeName.Contains($"[{Fhir}", StringComparison.Ordinal);
}
