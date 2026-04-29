using System.Text;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tailbook.Api.Host.Infrastructure;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Time;
using Tailbook.SharedKernel.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    options.IncludeScopes = true;
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddFastEndpoints(o => o.Assemblies = ModuleCatalog.EndpointAssemblies)
    .AddJobQueues<JobRecord, JobProvider>();

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
    .ValidateOnStart();

builder.Services
    .AddOptions<HttpTransportOptions>()
    .Bind(builder.Configuration.GetSection(HttpTransportOptions.SectionName))
    .ValidateOnStart();

var mainConnectionString = builder.Configuration.GetConnectionString(DatabaseConnectionOptions.MainConnectionStringName);
builder.Services.AddOptions<DatabaseConnectionOptions>()
    .Configure(options => options.Main = mainConnectionString)
    .Validate(DatabaseConnectionOptions.HasValidMainConnectionString,
        "ConnectionStrings:Main must be a valid PostgreSQL connection string with Host, Database, and Username.")
    .ValidateOnStart();

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

var appCorsOptions = builder.Configuration.GetSection(AppCorsOptions.SectionName).Get<AppCorsOptions>() ?? new AppCorsOptions();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (appCorsOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(appCorsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(mainConnectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgresql");

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher>();
builder.Services.AddSingleton<IUtcClock, SystemUtcClock>();
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
app.UseCors("AppCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = null;
    c.Security.PermissionsClaimType = TailbookClaimTypes.Permission;
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

app.Run();
