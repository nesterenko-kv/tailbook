using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
using Tailbook.Api.Host.Infrastructure;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Jobs;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;
using Tailbook.BuildingBlocks.Infrastructure.Security;
using Tailbook.Modules.Audit.Infrastructure.Telemetry;
using Tailbook.Modules.Notifications.Infrastructure.Telemetry;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddHttpContextAccessor();
builder.Services.AddFastEndpoints(o => o.Assemblies = ModuleCatalog.ModuleAssemblies);
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton(multiplexer);
    builder.Services.AddJobQueues<JobRecord, RedisJobProvider>();
}
else
{
    builder.Services.AddJobQueues<JobRecord, JobProvider>();
}
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
});

builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Tailbook API";
        s.Version = "v1";
    };
});

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.SigningKey) && x.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters long.")
    .Validate(x => x.ExpirationMinutes > 0, "Jwt:ExpirationMinutes must be greater than zero.")
    .ValidateOnStart();

builder.Services
    .AddOptions<AppCorsOptions>()
    .Bind(builder.Configuration.GetSection(AppCorsOptions.SectionName))
    .Validate(options => options.AllowedOrigins.All(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)), "AppCors:AllowedOrigins must contain valid absolute HTTP/HTTPS origins.")
    .Validate(options => !options.AllowCredentials || options.AllowedOrigins.Length > 0, "AppCors:AllowedOrigins must be configured when AppCors:AllowCredentials is true.")
    .ValidateOnStart();

builder.Services
    .AddOptions<HttpTransportOptions>()
    .Bind(builder.Configuration.GetSection(HttpTransportOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<TelemetryOptions>()
    .Bind(builder.Configuration.GetSection(TelemetryOptions.SectionName))
    .Validate(TelemetryOptions.HasValidServiceName, "Telemetry:ServiceName is required.")
    .Validate(TelemetryOptions.HasValidDatabasePoolName, "Telemetry:DatabasePoolName is required.")
    .Validate(TelemetryOptions.HasValidOtlpEndpoint, "Telemetry:OtlpEndpoint must be a valid absolute HTTP/HTTPS URI.")
    .ValidateOnStart();

var mainConnectionString = builder.Configuration.GetConnectionString(DatabaseConnectionOptions.MainConnectionStringName);
var telemetryOptions = builder.Configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();
var telemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService(telemetryOptions.ServiceName);
builder.Services.AddOptions<DatabaseConnectionOptions>()
    .Configure(options => options.Main = mainConnectionString)
    .Validate(DatabaseConnectionOptions.HasValidMainConnectionString,
        "ConnectionStrings:Main must be a valid PostgreSQL connection string with Host, Database, and Username.")
    .ValidateOnStart();

builder.Services.AddOptions<SensitivePayloadProtectionOptions>()
    .Bind(builder.Configuration.GetSection(SensitivePayloadProtectionOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.Key) && x.Key.Length >= 32, "SensitivePayloadProtection:Key must be at least 32 characters long.")
    .Validate(x => string.IsNullOrWhiteSpace(x.PreviousKey) || x.PreviousKey.Length >= 32, "SensitivePayloadProtection:PreviousKey must be null or at least 32 characters long.")
    .Validate(x => !string.Equals(x.Key, x.PreviousKey, StringComparison.Ordinal), "SensitivePayloadProtection:Key and PreviousKey must be different.")
    .ValidateOnStart();
builder.Services.AddSingleton<ISensitivePayloadProtector, AesGcmSensitivePayloadProtector>();

builder.Services.AddOptions<JwtSigningOptions>()
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        options.SigningKey = jwtOptions.SigningKey;
    });

builder.Services.AddOptions<JwtCreationOptions>()
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        options.SigningKey = jwtOptions.SigningKey;
    });

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services
    .AddAuthenticationJwtBearer(_ =>
    {

    })
    .AddAuthorization();

builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>()
    .Configure<IOptions<AppCorsOptions>>((options, appCorsOptionsAccessor) =>
    {
        var appCorsOptions = appCorsOptionsAccessor.Value;

        options.AddPolicy("AppCors", policy =>
        {
            if (appCorsOptions.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(appCorsOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                if (appCorsOptions.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            }
        });
    });

builder.Services.AddSingleton(_ =>
{
    var connStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(mainConnectionString!)
    {
        MaxPoolSize = 300,
        ConnectionIdleLifetime = 30,
        ConnectionPruningInterval = 10
    };

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStringBuilder.ConnectionString)
    {
        Name = telemetryOptions.DatabasePoolName
    };

    return dataSourceBuilder.Build();
});

if (telemetryOptions.ShouldExportLogs)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.SetResourceBuilder(telemetryResourceBuilder);
        if (telemetryOptions.HasExportableOtlpEndpoint)
        {
            logging.AddOtlpExporter(exporter =>
            {
                exporter.Protocol = OtlpExportProtocol.Grpc;
                exporter.Endpoint = new Uri(telemetryOptions.OtlpEndpoint!);
            });
        }
    });
}

ThreadPool.SetMinThreads(workerThreads: 200, completionPortThreads: 200);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = null;
    options.Limits.MaxConcurrentUpgradedConnections = null;
});

builder.Services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
    options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
        .AddInterceptors(serviceProvider.GetRequiredService<DomainEventToOutboxInterceptor>())
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "tailbook:";
    });
}

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgresql");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", HealthStatus.Unhealthy, ["cache"]);
}

builder.Services.AddOptions<RateLimitOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitOptions.SectionName))
    .ValidateOnStart();
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(15);
    options.Period = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IHealthCheckPublisher, HealthCheckTelemetryPublisher>();

if (telemetryOptions.Enabled)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(telemetryOptions.ServiceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(ApiDiagnosticsTelemetry.MeterName)
                    .AddMeter(AuditTelemetry.MeterName)
                    .AddMeter(JobQueueTelemetry.MeterName)
                    .AddMeter(OutboxTelemetry.MeterName)
                    .AddMeter(InboxTelemetry.MeterName)
                    .AddMeter("Npgsql")
                    .AddMeter(NotificationTelemetry.MeterName)
                    .AddMeter(RabbitMqTelemetry.MeterName)
                    .AddPrometheusExporter();
                if (telemetryOptions.HasExportableOtlpEndpoint)
                {
                    metrics.AddOtlpExporter(exporter =>
                    {
                        exporter.Protocol = OtlpExportProtocol.Grpc;
                        exporter.Endpoint = new Uri(telemetryOptions.OtlpEndpoint!);
                    });
                }
            })
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("tailbook.request_id", request.HttpContext.TraceIdentifier);
                    };
                    options.RecordException = true;
                })
                .AddNpgsql()
                .AddSource(AuditTelemetry.ActivitySourceName)
                .AddSource(JobQueueTelemetry.ActivitySourceName)
                .AddSource(OutboxTelemetry.ActivitySourceName)
                .AddSource(NotificationTelemetry.ActivitySourceName)
                .AddSource(RabbitMqTelemetry.ActivitySourceName);
            if (telemetryOptions.HasExportableOtlpEndpoint)
            {
                tracing.AddOtlpExporter(exporter =>
                {
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                    exporter.Endpoint = new Uri(telemetryOptions.OtlpEndpoint!);
                });
            }
        });
}

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IEntityScopeService, EntityScopeService>();
builder.Services.AddScoped<IResourceScopeResolver, DirectMatchResourceScopeResolver>();
builder.Services.AddScoped<IIdempotencyStore, IdempotencyStore>();
builder.Services.AddOptions<IdempotencyRequestOptions>()
    .Bind(builder.Configuration.GetSection(IdempotencyRequestOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddScoped<IInboxStore, InboxStore>();
builder.Services.AddOptions<InboxOptions>()
    .Bind(builder.Configuration.GetSection(InboxOptions.SectionName))
    .Validate(x => x.PollIntervalSeconds >= 5, "Inbox:PollIntervalSeconds must be at least 5 seconds.")
    .Validate(x => x.MaxRetryAttempts >= 1, "Inbox:MaxRetryAttempts must be at least 1.")
    .Validate(x => x.BackoffBaseDelaySeconds >= 1, "Inbox:BackoffBaseDelaySeconds must be at least 1 second.")
    .ValidateOnStart();
builder.Services.AddHostedService<InboxProcessorBackgroundService>();
builder.Services.AddRabbitMqMessageBroker(builder.Configuration);
builder.Services.AddIntegrationOutboxPublisher(builder.Configuration);
builder.Services.AddSingleton<DomainEventToOutboxInterceptor>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDataSeeder, DevelopmentDemoSalonSeeder>();

builder.Services.AddTailbookModules(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();
StartupDiagnosticsLogger.Log(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

var httpTransportOptions = app.Services.GetRequiredService<IOptions<HttpTransportOptions>>().Value;
if (!app.Environment.IsDevelopment() && httpTransportOptions is { UseHsts: true, EnforceHttpsRedirection: true })
{
    app.UseHsts();
}
if (httpTransportOptions.EnforceHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ApiSecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<UnhandledExceptionMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseCors("AppCors");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    app.UseMiddleware<DistributedRateLimitMiddleware>();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = null;
    c.Security.PermissionsClaimType = TailbookClaimTypes.Permission;
    c.Serializer.Options.Converters.Add(new UtcDateTimeOffsetJsonConverter());
});

app.UseJobQueues();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapPrometheusScrapingEndpoint();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapGet("/", () => Results.Ok(new
{
    name = "Tailbook API",
    status = "ok",
    environment = app.Environment.EnvironmentName
})).AllowAnonymous();

app.MapTailbookModules();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();
