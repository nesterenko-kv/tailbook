using System.Text;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tailbook.Api.Host.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Time;
using Tailbook.SharedKernel.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHttpContextAccessor();
builder.Services.AddFastEndpoints(o => o.Assemblies = ModuleCatalog.EndpointAssemblies);
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
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

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

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "https://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Main")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgresql");

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IUtcClock, SystemUtcClock>();

builder.Services.AddTailbookModules(builder.Configuration);

var app = builder.Build();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment()) app.UseSwaggerGen();

app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c => c.Endpoints.RoutePrefix = null);

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    name = "Tailbook API",
    status = "ok",
    environment = app.Environment.EnvironmentName
})).AllowAnonymous();

app.MapTailbookModules();

app.Run();

