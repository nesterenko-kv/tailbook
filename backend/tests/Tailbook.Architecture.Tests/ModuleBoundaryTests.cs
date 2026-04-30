using System.Reflection;
using Xunit;

namespace Tailbook.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    private static readonly string[] ModuleAssemblyNames =
    [
        "Tailbook.Modules.Identity",
        "Tailbook.Modules.Customer",
        "Tailbook.Modules.Pets",
        "Tailbook.Modules.Catalog",
        "Tailbook.Modules.Booking",
        "Tailbook.Modules.VisitOperations",
        "Tailbook.Modules.Staff",
        "Tailbook.Modules.Notifications",
        "Tailbook.Modules.Audit",
        "Tailbook.Modules.Reporting"
    ];

    private static readonly string SourceRoot = FindSourceRoot();

    [Theory]
    [InlineData("Tailbook.Modules.Identity")]
    [InlineData("Tailbook.Modules.Customer")]
    [InlineData("Tailbook.Modules.Pets")]
    [InlineData("Tailbook.Modules.Catalog")]
    [InlineData("Tailbook.Modules.Booking")]
    [InlineData("Tailbook.Modules.VisitOperations")]
    [InlineData("Tailbook.Modules.Staff")]
    [InlineData("Tailbook.Modules.Notifications")]
    [InlineData("Tailbook.Modules.Audit")]
    [InlineData("Tailbook.Modules.Reporting")]
    public void Modules_should_not_reference_other_modules_directly(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var referencedModuleNames = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null && ModuleAssemblyNames.Contains(x))
            .ToArray();

        Assert.DoesNotContain(referencedModuleNames, x => x != assemblyName);
    }

    [Theory]
    [InlineData("Tailbook.Modules.Identity")]
    [InlineData("Tailbook.Modules.Customer")]
    [InlineData("Tailbook.Modules.Pets")]
    [InlineData("Tailbook.Modules.Catalog")]
    [InlineData("Tailbook.Modules.Booking")]
    [InlineData("Tailbook.Modules.VisitOperations")]
    [InlineData("Tailbook.Modules.Staff")]
    [InlineData("Tailbook.Modules.Notifications")]
    [InlineData("Tailbook.Modules.Audit")]
    [InlineData("Tailbook.Modules.Reporting")]
    public void Domain_layer_should_not_reference_outer_layers_or_frameworks(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var domainPath = Path.Combine(modulePath, "Domain");

        AssertNoSourceReferences(
            domainPath,
            $"{assemblyName}.Application",
            $"{assemblyName}.Infrastructure",
            $"{assemblyName}.Api",
            "FastEndpoints",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Theory]
    [InlineData("Tailbook.Modules.Identity")]
    [InlineData("Tailbook.Modules.Customer")]
    [InlineData("Tailbook.Modules.Pets")]
    [InlineData("Tailbook.Modules.Catalog")]
    [InlineData("Tailbook.Modules.Booking")]
    [InlineData("Tailbook.Modules.VisitOperations")]
    [InlineData("Tailbook.Modules.Staff")]
    [InlineData("Tailbook.Modules.Notifications")]
    [InlineData("Tailbook.Modules.Audit")]
    [InlineData("Tailbook.Modules.Reporting")]
    public void Application_layer_should_not_reference_infrastructure_api_or_http_frameworks(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var applicationPath = Path.Combine(modulePath, "Application");

        AssertNoSourceReferences(
            applicationPath,
            $"{assemblyName}.Infrastructure",
            $"{assemblyName}.Api",
            "Tailbook.BuildingBlocks.Infrastructure",
            "FastEndpoints",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");

        AssertNoTypeReferences(
            assemblyName,
            ".Application",
            $"{assemblyName}.Infrastructure",
            $"{assemblyName}.Api",
            "Tailbook.BuildingBlocks.Infrastructure",
            "FastEndpoints",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Theory]
    [InlineData("Tailbook.Modules.Identity")]
    [InlineData("Tailbook.Modules.Customer")]
    [InlineData("Tailbook.Modules.Pets")]
    [InlineData("Tailbook.Modules.Catalog")]
    [InlineData("Tailbook.Modules.Booking")]
    [InlineData("Tailbook.Modules.VisitOperations")]
    [InlineData("Tailbook.Modules.Staff")]
    [InlineData("Tailbook.Modules.Notifications")]
    [InlineData("Tailbook.Modules.Audit")]
    [InlineData("Tailbook.Modules.Reporting")]
    public void Api_layer_should_not_reference_module_infrastructure_or_persistence(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var apiPath = Path.Combine(modulePath, "Api");

        AssertNoSourceReferences(
            apiPath,
            $"{assemblyName}.Infrastructure",
            "Tailbook.BuildingBlocks.Infrastructure.Persistence",
            "Microsoft.EntityFrameworkCore",
            "AppDbContext");

        AssertNoTypeReferences(
            assemblyName,
            ".Api",
            $"{assemblyName}.Infrastructure",
            "Tailbook.BuildingBlocks.Infrastructure.Persistence",
            "Microsoft.EntityFrameworkCore");
    }

    [Theory]
    [InlineData("Tailbook.Modules.Identity")]
    [InlineData("Tailbook.Modules.Customer")]
    [InlineData("Tailbook.Modules.Pets")]
    [InlineData("Tailbook.Modules.Catalog")]
    [InlineData("Tailbook.Modules.Booking")]
    [InlineData("Tailbook.Modules.VisitOperations")]
    [InlineData("Tailbook.Modules.Staff")]
    [InlineData("Tailbook.Modules.Notifications")]
    [InlineData("Tailbook.Modules.Audit")]
    [InlineData("Tailbook.Modules.Reporting")]
    public void Module_global_usings_should_not_import_infrastructure_namespaces(string assemblyName)
    {
        var globalUsingsPath = Path.Combine(GetModuleSourcePath(assemblyName), "GlobalUsings.cs");
        if (!File.Exists(globalUsingsPath))
        {
            return;
        }

        var text = File.ReadAllText(globalUsingsPath);
        Assert.DoesNotContain(".Infrastructure.", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Tailbook.Modules.Identity")]
    [InlineData("Tailbook.Modules.Customer")]
    [InlineData("Tailbook.Modules.Pets")]
    [InlineData("Tailbook.Modules.Catalog")]
    [InlineData("Tailbook.Modules.Booking")]
    [InlineData("Tailbook.Modules.VisitOperations")]
    [InlineData("Tailbook.Modules.Staff")]
    [InlineData("Tailbook.Modules.Notifications")]
    [InlineData("Tailbook.Modules.Audit")]
    [InlineData("Tailbook.Modules.Reporting")]
    public void Infrastructure_layer_should_not_reference_api_layer(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var infrastructurePath = Path.Combine(modulePath, "Infrastructure");

        AssertNoSourceReferences(infrastructurePath, $"{assemblyName}.Api");
        AssertNoTypeReferences(assemblyName, ".Infrastructure", $"{assemblyName}.Api");
    }

    [Fact]
    public void BuildingBlocks_should_not_reference_module_assemblies()
    {
        var assembly = Assembly.Load("Tailbook.BuildingBlocks");
        var referencedModuleNames = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null && ModuleAssemblyNames.Contains(x))
            .ToArray();

        Assert.Empty(referencedModuleNames);
    }

    [Fact]
    public void SharedKernel_should_stay_framework_light_and_module_free()
    {
        var assembly = Assembly.Load("Tailbook.SharedKernel");
        var forbiddenReferences = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x =>
                x is not null &&
                (ModuleAssemblyNames.Contains(x) ||
                 x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                 x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
                 x.StartsWith("FastEndpoints", StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    private static string GetModuleSourcePath(string assemblyName)
    {
        return Path.Combine(SourceRoot, assemblyName);
    }

    private static void AssertNoSourceReferences(string layerPath, params string[] forbiddenPatterns)
    {
        if (!Directory.Exists(layerPath))
        {
            return;
        }

        var violations = Directory.EnumerateFiles(layerPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path =>
            {
                var text = File.ReadAllText(path);
                return forbiddenPatterns
                    .Where(pattern => text.Contains(pattern, StringComparison.Ordinal))
                    .Select(pattern => $"{Path.GetRelativePath(SourceRoot, path)} references {pattern}");
            })
            .ToArray();

        Assert.Empty(violations);
    }

    private static void AssertNoTypeReferences(string assemblyName, string layerSegment, params string[] forbiddenNamespacePrefixes)
    {
        var assembly = Assembly.Load(assemblyName);
        var violations = GetLoadableTypes(assembly)
            .Where(type => type.Namespace?.Contains(layerSegment, StringComparison.Ordinal) == true)
            .SelectMany(type => GetReferencedTypes(type)
                .Where(reference => IsForbidden(reference, forbiddenNamespacePrefixes))
                .Select(reference => $"{type.FullName} references {reference.FullName}"))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var references = new List<Type?>();

        references.Add(type.BaseType);
        references.AddRange(type.GetInterfaces());
        references.AddRange(type.GetFields(Flags).Select(x => x.FieldType));
        references.AddRange(type.GetProperties(Flags).Select(x => x.PropertyType));
        references.AddRange(type.GetEvents(Flags).Select(x => x.EventHandlerType));
        references.AddRange(type.GetConstructors(Flags).SelectMany(x => x.GetParameters()).Select(x => x.ParameterType));
        references.AddRange(type.GetMethods(Flags).Select(x => x.ReturnType));
        references.AddRange(type.GetMethods(Flags).SelectMany(x => x.GetParameters()).Select(x => x.ParameterType));

        return references
            .OfType<Type>()
            .SelectMany(ExpandType)
            .Distinct();
    }

    private static IEnumerable<Type> ExpandType(Type type)
    {
        if (type.IsArray && type.GetElementType() is { } elementType)
        {
            foreach (var expanded in ExpandType(elementType))
            {
                yield return expanded;
            }
        }

        if (type.IsGenericType)
        {
            yield return type.GetGenericTypeDefinition();
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var expanded in ExpandType(argument))
                {
                    yield return expanded;
                }
            }
        }

        yield return type;
    }

    private static bool IsForbidden(Type type, string[] forbiddenNamespacePrefixes)
    {
        var fullName = type.FullName ?? type.Name;
        return forbiddenNamespacePrefixes.Any(prefix => fullName.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static string FindSourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var sourceRoot = Path.Combine(current.FullName, "backend", "src");
            if (Directory.Exists(sourceRoot))
            {
                return sourceRoot;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate backend source root.");
    }
}
