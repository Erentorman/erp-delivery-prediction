using App.Api.ExceptionHandling;
using App.Api.Security;
using App.Application.IntegrationLogging;
using App.Infrastructure.Clock;
using App.Infrastructure.Security;
using App.Infrastructure.Shipping;
using App.Integration.MockErp;
using App.Persistence;
using App.Persistence.IntegrationLogging;
using App.Api.Configuration;
using App.Application.Contracts.Configuration;
using App.Application.Erp;
using App.Application.Prediction;
using App.Application.Prediction.Demo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Cors:AllowedOrigin lets the browser-served frontend call this API
// cross-origin (different host port under Docker Compose / local dev).
var corsAllowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
if (!string.IsNullOrWhiteSpace(corsAllowedOrigin))
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(corsAllowedOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IIntegrationLogWriter, IntegrationLogWriter>();

builder.Services.AddInfrastructureSecurity(builder.Configuration);
builder.Services.Configure<WhatIfShippingOptions>(
    builder.Configuration.GetSection(WhatIfShippingOptions.SectionName));
builder.Services.AddShippingLookup();
builder.Services.AddSystemClock();
builder.Services.AddMockErpDataProvider(builder.Configuration);
builder.Services.AddErpBatchReader();
builder.Services.AddPredictionServices();
builder.Services.AddDemoWorkOrderSupport(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtOptions>((bearerOptions, jwtOptions) =>
    {
        bearerOptions.TokenValidationParameters = JwtBearerOptionsFactory.BuildTokenValidationParameters(jwtOptions);
    });

builder.Configuration.AddMvpAssumptions();
builder.Services.AddMvpAssumptionsOptions(builder.Configuration);

var app = builder.Build();

// SAD §14.3: EF Core migrations are applied automatically at api startup
// so `docker compose up` reaches a usable state in a single command.
// Skipped under the "Testing" host environment: WebApplicationFactory-based
// tests boot the full pipeline against a dummy, unreachable connection
// string by design and never exercise persistence.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Run CategoryAFieldGuard
var options = app.Services.GetRequiredService<IOptions<MvpAssumptionsOptions>>().Value;
var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "mvp-assumptions.json");
var jsonContent = File.ReadAllText(jsonPath);
var logger = app.Services.GetRequiredService<ILogger<Program>>();
CategoryAFieldGuard.Validate(jsonContent, options, logger);

var demoOptions = app.Services.GetRequiredService<DemoWorkOrderOptions>();
if (demoOptions.EnableSyntheticWorkOrder)
{
    logger.LogWarning(
        "Demo:EnableSyntheticWorkOrder is enabled. Synthetic, non-ERP-verified DEMO-* work order data " +
        "may be injected for orders with no real routing data. This must not be used in production.");
}

// Global exception handling must run before any other middleware that could throw.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (!string.IsNullOrWhiteSpace(corsAllowedOrigin))
{
    app.UseCors("Frontend");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Docker/Compose healthcheck target. Explicitly anonymous so it never
// depends on JWT/auth configuration, even if a global auth policy is
// introduced later.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
