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

    private static readonly string[] ModuleShortNames =
        ModuleAssemblyNames.Select(StripModulePrefix).ToArray();

    private static readonly string[] SubAssemblySuffixes =
        [".Domain", ".Application", ".Infrastructure", ".IntegrationEvents", ".Api.Contracts", ".Api"];

    private static readonly string[] ImplementationSubAssemblySuffixes =
        SubAssemblySuffixes.Where(s => s != ".Api.Contracts" && s != ".IntegrationEvents").ToArray();

    private static readonly string[] ModuleApiContractAssemblyNames =
        ModuleShortNames.Select(n => $"Tailbook.Modules.{n}.Api.Contracts").ToArray();

    private static readonly string SourceRoot = FindSourceRoot();

    private static readonly string[] AllowedPublishAsyncFiles =
    [
        "IntegrationOutboxPublisherBackgroundService.cs",
        "RabbitMqMessageBroker.cs",
        "NoOpMessageBroker.cs",
        "MessagingRegistration.cs",
        "IMessageBroker.cs"
    ];

    private static readonly string[] BrokerKeywords =
    [
        "IMessageBroker",
        ".PublishAsync(",
        "_messageBroker",
        "_eventBus",
        "_publisher"
    ];

    private static string StripModulePrefix(string assemblyName) =>
        assemblyName.Replace("Tailbook.Modules.", "");

    private static string ModuleSourcePath(string assemblyName) =>
        Path.Combine(SourceRoot, "Modules", StripModulePrefix(assemblyName));

    private static Assembly LoadSubAssembly(string assemblyName, string suffix) =>
        Assembly.Load($"{assemblyName}{suffix}");

    private static string[] AllModuleSubAssemblyNames(string assemblyName) =>
        SubAssemblySuffixes.Select(s => $"{assemblyName}{s}").ToArray();

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
        var violations = new List<string>();
        var otherModuleNames = ModuleAssemblyNames.Where(n => n != assemblyName).ToArray();

        foreach (var suffix in ImplementationSubAssemblySuffixes)
        {
            var subAssemblyName = $"{assemblyName}{suffix}";
            Assembly subAssembly;
            try
            {
                subAssembly = Assembly.Load(subAssemblyName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            var referencedModuleNames = subAssembly.GetReferencedAssemblies()
                .Select(x => x.Name)
                .Where(x => x is not null && (
                    ModuleAssemblyNames.Contains(x) ||
                    ModuleApiContractAssemblyNames.Contains(x)))
                .ToArray();

            var forbiddenRefs = referencedModuleNames
                .Where(x => x is not null && !x.EndsWith(".Api.Contracts"))
                .Where(x => otherModuleNames.Contains(x))
                .ToArray();

            foreach (var refName in forbiddenRefs)
            {
                violations.Add($"{subAssemblyName} references {refName}");
            }
        }

        Assert.Empty(violations);
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
            [$"{assemblyName}.Application", $"{assemblyName}.Infrastructure", $"{assemblyName}.Api", "FastEndpoints", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore"]);
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

        var apiPrefix = $"{assemblyName}.Api.";
        var contractsPrefix = $"{assemblyName}.Api.Contracts";

        AssertNoSourceReferences(
            applicationPath,
            [$"{assemblyName}.Infrastructure", apiPrefix, "Tailbook.BuildingBlocks.Infrastructure", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore"],
            [contractsPrefix]);

        AssertNoTypeReferences(
            $"{assemblyName}.Application",
            ".Application",
            [$"{assemblyName}.Infrastructure", apiPrefix, "Tailbook.BuildingBlocks.Infrastructure", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore"],
            [contractsPrefix]);
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
    public void Application_layer_should_only_use_fastendpoints_for_commands(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var applicationPath = Path.Combine(modulePath, "Application");
        if (!Directory.Exists(applicationPath))
        {
            return;
        }

        var sourceViolations = Directory.EnumerateFiles(applicationPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path => !IsCommandsPath(path))
            .Where(path => File.ReadAllText(path).Contains("FastEndpoints", StringComparison.Ordinal))
            .Select(path => $"{Path.GetRelativePath(SourceRoot, path)} references FastEndpoints outside Application/Commands")
            .ToArray();

        Assert.Empty(sourceViolations);

        var appAssemblyName = $"{assemblyName}.Application";
        Assembly appAssembly;
        try
        {
            appAssembly = Assembly.Load(appAssemblyName);
        }
        catch (FileNotFoundException)
        {
            return;
        }

        var typeViolations = GetLoadableTypes(appAssembly)
            .Where(type => type.Namespace?.Contains(".Application.", StringComparison.Ordinal) == true)
            .Where(type => type.Namespace?.Contains(".Commands", StringComparison.Ordinal) != true)
            .SelectMany(type => GetReferencedTypes(type)
                .Where(reference => IsForbidden(reference, ["FastEndpoints"]))
                .Select(reference => $"{type.FullName} references {reference.FullName} outside Application.Commands"))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        Assert.Empty(typeViolations);
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
    public void Query_services_should_not_expose_write_operations(string assemblyName)
    {
        var writePrefixes = new[]
        {
            "Create",
            "Update",
            "Add",
            "Assign",
            "Cancel",
            "Publish",
            "Process",
            "Register",
            "CheckIn",
            "Complete",
            "Close",
            "Record",
            "Apply",
            "Convert",
            "Attach",
            "Reschedule",
            "Revoke",
            "Request",
            "Reset"
        };

        var violations = new List<string>();

        foreach (var suffix in new[] { ".Application", ".Api.Contracts" })
        {
            var subAssemblyName = $"{assemblyName}{suffix}";
            Assembly subAssembly;
            try
            {
                subAssembly = Assembly.Load(subAssemblyName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            var subViolations = GetLoadableTypes(subAssembly)
                .Where(type => type.Name.EndsWith("Queries", StringComparison.Ordinal))
                .SelectMany(type => type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => writePrefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal)))
                    .Select(method => $"{type.FullName}.{method.Name} should be a command/use case, not a query service method"))
                .ToArray();

            violations.AddRange(subViolations);
        }

        Assert.Empty(violations);
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
    public void Command_records_should_live_under_application_commands(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var violations = Directory.EnumerateFiles(modulePath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path => !IsCommandsPath(path))
            .SelectMany(path =>
            {
                var lines = File.ReadAllLines(path);
                return lines
                    .Select((line, index) => new { Line = line.Trim(), LineNumber = index + 1 })
                    .Where(x => x.Line.StartsWith("public sealed record ", StringComparison.Ordinal) &&
                                x.Line.Contains("Command", StringComparison.Ordinal))
                    .Select(x => $"{Path.GetRelativePath(SourceRoot, path)}:{x.LineNumber} declares a command record outside Application/Commands");
            })
            .ToArray();

        Assert.Empty(violations);
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
    public void Required_actor_user_claim_bindings_should_not_be_nullable(string assemblyName)
    {
        var apiPath = Path.Combine(GetModuleSourcePath(assemblyName), "Api");
        if (!Directory.Exists(apiPath))
        {
            return;
        }

        var violations = Directory.EnumerateFiles(apiPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .SelectMany(path =>
            {
                var lines = File.ReadAllLines(path);
                return lines
                    .Select((line, index) => new { Line = line.Trim(), LineNumber = index + 1 })
                    .Where(x => x.Line.StartsWith("[FromClaim(TailbookClaimTypes.UserId", StringComparison.Ordinal) &&
                                !x.Line.Contains("isRequired: false", StringComparison.Ordinal))
                    .Where(x => x.LineNumber < lines.Length &&
                                lines[x.LineNumber].Trim().StartsWith("public Guid? ActorUserId", StringComparison.Ordinal))
                    .Select(x => $"{Path.GetRelativePath(SourceRoot, path)}:{x.LineNumber + 1} binds required ActorUserId as nullable");
            })
            .ToArray();

        Assert.Empty(violations);
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
            [$"{assemblyName}.Infrastructure", "Tailbook.BuildingBlocks.Infrastructure.Persistence", "Microsoft.EntityFrameworkCore", "AppDbContext"],
            [$"{assemblyName}.Api.Contracts"],
            ["Module.cs"]);

        AssertNoTypeReferences(
            $"{assemblyName}.Api",
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
        var modulePath = GetModuleSourcePath(assemblyName);

        foreach (var suffix in SubAssemblySuffixes)
        {
            // Skip Infrastructure project's own GlobalUsings — it naturally references its own namespace
            if (suffix == ".Infrastructure")
            {
                continue;
            }

            var globalUsingsPath = Path.Combine(modulePath, suffix.Replace(".", ""), "GlobalUsings.cs");
            if (!File.Exists(globalUsingsPath))
            {
                continue;
            }

            var text = File.ReadAllText(globalUsingsPath);
            Assert.DoesNotContain(".Infrastructure.", text, StringComparison.Ordinal);
        }
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

        var apiPrefix = $"{assemblyName}.Api.";
        var contractsPrefix = $"{assemblyName}.Api.Contracts";
        AssertNoSourceReferences(infrastructurePath, [apiPrefix], [contractsPrefix]);
        AssertNoTypeReferences($"{assemblyName}.Infrastructure", ".Infrastructure", new[] { apiPrefix }, new[] { contractsPrefix });
    }

    [Fact]
    public void BuildingBlocks_should_not_reference_module_assemblies()
    {
        var assembly = Assembly.Load("Tailbook.BuildingBlocks");
        var allModuleSubAssemblies = ModuleAssemblyNames
            .SelectMany(n => AllModuleSubAssemblyNames(n))
            .ToArray();

        var referencedModuleNames = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null && (
                ModuleAssemblyNames.Contains(x) ||
                ModuleApiContractAssemblyNames.Contains(x) ||
                allModuleSubAssemblies.Contains(x)))
            .ToArray();

        Assert.Empty(referencedModuleNames);
    }

    [Fact]
    public void SharedKernel_should_stay_framework_light_and_module_free()
    {
        var assembly = Assembly.Load("Tailbook.SharedKernel");
        var allModuleSubAssemblies = ModuleAssemblyNames
            .SelectMany(n => AllModuleSubAssemblyNames(n))
            .ToArray();

        var forbiddenReferences = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x =>
                x is not null &&
                (ModuleAssemblyNames.Contains(x) ||
                 ModuleApiContractAssemblyNames.Contains(x) ||
                 allModuleSubAssemblies.Contains(x) ||
                 x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                 x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
                 x.StartsWith("FastEndpoints", StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Theory]
    [InlineData("Tailbook.Modules.Identity.Api.Contracts")]
    [InlineData("Tailbook.Modules.Customer.Api.Contracts")]
    [InlineData("Tailbook.Modules.Pets.Api.Contracts")]
    [InlineData("Tailbook.Modules.Catalog.Api.Contracts")]
    [InlineData("Tailbook.Modules.Booking.Api.Contracts")]
    [InlineData("Tailbook.Modules.VisitOperations.Api.Contracts")]
    [InlineData("Tailbook.Modules.Staff.Api.Contracts")]
    [InlineData("Tailbook.Modules.Notifications.Api.Contracts")]
    [InlineData("Tailbook.Modules.Audit.Api.Contracts")]
    [InlineData("Tailbook.Modules.Reporting.Api.Contracts")]
    public void Api_contracts_should_not_reference_module_implementation_assemblies(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var forbiddenReferences = assembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x =>
                x is not null &&
                (ModuleAssemblyNames.Contains(x) ||
                 ImplementationSubAssemblySuffixes.Select(s => $"{assemblyName.Replace(".Api.Contracts", "")}{s}").Contains(x)))
            .ToArray();

        Assert.Empty(forbiddenReferences);
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
    public void CommandHandlers_should_not_publish_events_directly(string assemblyName)
    {
        var infrastructureAssemblyName = $"{assemblyName}.Infrastructure";
        Assembly infrastructureAssembly;
        try
        {
            infrastructureAssembly = Assembly.Load(infrastructureAssemblyName);
        }
        catch (FileNotFoundException)
        {
            return;
        }

        var handlerTypes = GetLoadableTypes(infrastructureAssembly)
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition().Name == "ICommandHandler`2"))
            .ToArray();

        if (handlerTypes.Length == 0)
        {
            return;
        }

        var modulePath = GetModuleSourcePath(assemblyName);
        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var filePath = GetHandlerSourceFilePath(modulePath, handler, assemblyName);
            if (filePath is null || !File.Exists(filePath))
            {
                violations.Add($"{handler.FullName}: source file not found at expected path");
                continue;
            }

            var text = File.ReadAllText(filePath);
            foreach (var keyword in BrokerKeywords)
            {
                if (text.Contains(keyword, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(SourceRoot, filePath)} contains {keyword}");
                }
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(string.Join("\n", violations.Distinct().OrderBy(x => x)));
        }
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
    public void Application_services_should_not_reference_message_broker(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var applicationPath = Path.Combine(modulePath, "Application");

        AssertNoSourceReferences(applicationPath, ["IMessageBroker", ".PublishAsync("]);

        var appAssemblyName = $"{assemblyName}.Application";
        Assembly appAssembly;
        try
        {
            appAssembly = Assembly.Load(appAssemblyName);
        }
        catch (FileNotFoundException)
        {
            return;
        }

        var typeViolations = GetLoadableTypes(appAssembly)
            .Where(type => type.Namespace?.Contains(".Application", StringComparison.Ordinal) == true)
            .SelectMany(type => GetReferencedTypes(type)
                .Where(reference => IsForbidden(reference, ["Tailbook.BuildingBlocks.Abstractions.IMessageBroker"]))
                .Select(reference => $"{type.FullName} references {reference.FullName}"))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        Assert.Empty(typeViolations);
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
    public void Domain_events_should_only_be_raised_from_aggregates(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var domainPath = Path.Combine(modulePath, "Domain");

        var violations = Directory.EnumerateFiles(modulePath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path => File.ReadAllText(path).Contains("RaiseDomainEvent(", StringComparison.Ordinal))
            .Where(path => !path.StartsWith(domainPath, StringComparison.Ordinal))
            .Select(path => $"{Path.GetRelativePath(SourceRoot, path)} calls RaiseDomainEvent outside Domain/")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Only_outbox_interceptor_should_call_ToIntegrationEvent()
    {
        var allowedFiles = new HashSet<string>
        {
            "OutboxPayloadProjector.cs",
            "IDomainEvent.cs"
        };

        var violations = Directory.EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path =>
            {
                var text = File.ReadAllText(path);
                return text.Contains(".ToIntegrationEvent(", StringComparison.Ordinal);
            })
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return !allowedFiles.Contains(fileName, StringComparer.Ordinal);
            })
            .Select(path => $"{Path.GetRelativePath(SourceRoot, path)} calls .ToIntegrationEvent()")
            .ToArray();

        Assert.Empty(violations);
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
    public void Integration_events_should_only_be_published_via_outbox(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        if (!Directory.Exists(modulePath))
        {
            return;
        }

        var violations = Directory.EnumerateFiles(modulePath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path =>
            {
                var text = File.ReadAllText(path);
                return text.Contains("IMessageBroker", StringComparison.Ordinal) ||
                       text.Contains("IOutboxPublisher", StringComparison.Ordinal);
            })
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return fileName != "IMessageBroker.cs";
            })
            .Select(path => $"{Path.GetRelativePath(SourceRoot, path)} references IMessageBroker or IOutboxPublisher")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Integration_events_should_only_be_published_via_outbox_building_blocks()
    {
        var bbPath = Path.Combine(SourceRoot, "Tailbook.BuildingBlocks");

        var violations = Directory.EnumerateFiles(bbPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path =>
            {
                var text = File.ReadAllText(path);
                return text.Contains(".PublishAsync(", StringComparison.Ordinal);
            })
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return !AllowedPublishAsyncFiles.Contains(fileName, StringComparer.Ordinal);
            })
            .Select(path => $"{Path.GetRelativePath(SourceRoot, path)} calls .PublishAsync()")
            .ToArray();

        Assert.Empty(violations);
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
    public void Domain_layer_should_not_reference_integration_event_dtos(string assemblyName)
    {
        var modulePath = GetModuleSourcePath(assemblyName);
        var domainPath = Path.Combine(modulePath, "Domain");

        if (!Directory.Exists(domainPath))
        {
            return;
        }

        var otherModuleIntegrationEventNamespaces = ModuleAssemblyNames
            .Where(name => name != assemblyName)
            .Select(name => $"Tailbook.Modules.{StripModulePrefix(name)}.IntegrationEvents")
            .ToArray();

        if (otherModuleIntegrationEventNamespaces.Length == 0)
        {
            return;
        }

        var violations = Directory.EnumerateFiles(domainPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .SelectMany(path =>
            {
                var text = File.ReadAllText(path);
                return otherModuleIntegrationEventNamespaces
                    .Where(ns => text.Contains(ns, StringComparison.Ordinal))
                    .Select(ns => $"{Path.GetRelativePath(SourceRoot, path)} references {ns}");
            })
            .ToArray();

        Assert.Empty(violations);
    }

    private static string GetModuleSourcePath(string assemblyName)
    {
        return ModuleSourcePath(assemblyName);
    }

    private static string? GetHandlerSourceFilePath(string modulePath, Type handlerType, string assemblyName)
    {
        var ns = handlerType.Namespace ?? "";
        var moduleNs = StripModulePrefix(assemblyName);
        var fullPrefix = $"Tailbook.Modules.{moduleNs}.";
        var relativeNs = ns.StartsWith(fullPrefix, StringComparison.Ordinal)
            ? ns[fullPrefix.Length..]
            : ns;
        var relativePath = relativeNs.Replace('.', Path.DirectorySeparatorChar);

        var filePath = Path.Combine(modulePath, relativePath, $"{handlerType.Name}.cs");
        if (File.Exists(filePath))
        {
            return filePath;
        }

        var searchPattern = $"{handlerType.Name}.cs";
        return Directory.EnumerateFiles(modulePath, searchPattern, SearchOption.AllDirectories)
            .FirstOrDefault(f => !IsGeneratedPath(f));
    }

    private static void AssertNoSourceReferences(string layerPath, string[] forbiddenPatterns, string[]? allowedPatterns = null, string[]? excludedFiles = null)
    {
        if (!Directory.Exists(layerPath))
        {
            return;
        }

        var violations = Directory.EnumerateFiles(layerPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path => excludedFiles is null || !excludedFiles.Any(e => path.EndsWith(e, StringComparison.Ordinal)))
            .SelectMany(path =>
            {
                var text = File.ReadAllText(path);
                return forbiddenPatterns
                    .Where(pattern => text.Contains(pattern, StringComparison.Ordinal))
                    .Where(pattern => allowedPatterns is null || !allowedPatterns.Any(a => text.Contains(a, StringComparison.Ordinal)))
                    .Select(pattern => $"{Path.GetRelativePath(SourceRoot, path)} references {pattern}");
            })
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool IsGeneratedPath(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCommandsPath(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoTypeReferences(string assemblyName, string layerSegment, string[] forbiddenNamespacePrefixes, string[]? allowedPrefixes = null)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyName);
        }
        catch (FileNotFoundException)
        {
            return;
        }

        var violations = GetLoadableTypes(assembly)
            .Where(type => type.Namespace?.Contains(layerSegment, StringComparison.Ordinal) == true)
            .SelectMany(type => GetReferencedTypes(type)
                .Where(reference => IsForbidden(reference, forbiddenNamespacePrefixes) && !IsForbidden(reference, allowedPrefixes ?? []))
                .Select(reference => $"{type.FullName} references {reference.FullName}"))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        Assert.Empty(violations);
    }

    private static void AssertNoTypeReferences(string assemblyName, string layerSegment, params string[] forbiddenNamespacePrefixes)
    {
        AssertNoTypeReferences(assemblyName, layerSegment, forbiddenNamespacePrefixes, null);
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
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var references = new List<Type?> { type.BaseType };

        references.AddRange(type.GetInterfaces());
        references.AddRange(type.GetFields(flags).Select(x => x.FieldType));
        references.AddRange(type.GetProperties(flags).Select(x => x.PropertyType));
        references.AddRange(type.GetEvents(flags).Select(x => x.EventHandlerType));
        references.AddRange(type.GetConstructors(flags).SelectMany(x => x.GetParameters()).Select(x => x.ParameterType));
        references.AddRange(type.GetMethods(flags).Select(x => x.ReturnType));
        references.AddRange(type.GetMethods(flags).SelectMany(x => x.GetParameters()).Select(x => x.ParameterType));

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
