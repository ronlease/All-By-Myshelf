using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Infrastructure.ExternalApis;
using AllByMyshelf.Api.Repositories;
using AllByMyshelf.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AllByMyshelfDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Discogs configuration (fail-fast on startup if token is missing) ──────────
builder.Services.AddOptions<DiscogsOptions>()
    .Bind(builder.Configuration.GetSection(DiscogsOptions.SectionName))
    .Validate(
        opts => !string.IsNullOrWhiteSpace(opts.PersonalAccessToken),
        "Discogs:PersonalAccessToken must be set. Run: dotnet user-secrets set \"Discogs:PersonalAccessToken\" \"<token>\"")
    .Validate(
        opts => !string.IsNullOrWhiteSpace(opts.Username),
        "Discogs:Username must be set in configuration.")
    .ValidateOnStart();

// ── Discogs HTTP client ───────────────────────────────────────────────────────
builder.Services.AddHttpClient<DiscogsClient>(client =>
{
    client.BaseAddress = new Uri("https://api.discogs.com");
    client.DefaultRequestHeaders.Add("User-Agent", "AllByMyshelf/1.0");
});

// ── Hardcover configuration (fail-fast on startup if token is missing) ────────
builder.Services.AddOptions<HardcoverOptions>()
    .Bind(builder.Configuration.GetSection(HardcoverOptions.SectionName))
    .Validate(
        opts => !string.IsNullOrWhiteSpace(opts.ApiToken),
        "Hardcover:ApiToken must be set. Run: dotnet user-secrets set \"Hardcover:ApiToken\" \"<token>\"")
    .ValidateOnStart();

// ── Hardcover HTTP client ─────────────────────────────────────────────────────
builder.Services.AddHttpClient("Hardcover");

builder.Services.AddScoped<HardcoverClient>();

// ── Repositories & services ───────────────────────────────────────────────────
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IReleasesRepository, ReleasesRepository>();
builder.Services.AddScoped<IReleasesService, ReleasesService>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// BooksSyncService is a singleton BackgroundService; also exposed as IBooksSyncService.
builder.Services.AddSingleton<BooksSyncService>();
builder.Services.AddSingleton<IBooksSyncService>(sp => sp.GetRequiredService<BooksSyncService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BooksSyncService>());

// SyncService is a singleton BackgroundService; also exposed as ISyncService.
builder.Services.AddSingleton<SyncService>();
builder.Services.AddSingleton<ISyncService>(sp => sp.GetRequiredService<SyncService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SyncService>());

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

// ── Health checks ──────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins("https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── MVC / Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "All By Myshelf API",
        Version = "v1",
        Description = "Personal collection dashboard API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme",
        Scheme = "bearer",
        Type = SecuritySchemeType.Http
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build & middleware ────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "All By Myshelf API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAngularDev");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers().RequireAuthorization();

app.Run();
