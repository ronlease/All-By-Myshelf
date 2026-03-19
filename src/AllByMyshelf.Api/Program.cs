using AllByMyshelf.Api.Features.Bgg;
using AllByMyshelf.Api.Features.Discogs;
using AllByMyshelf.Api.Features.Hardcover;
using AllByMyshelf.Api.Features.Statistics;
using AllByMyshelf.Api.Features.Wantlist;
using AllByMyshelf.Api.Infrastructure.Configuration;
using AllByMyshelf.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Database-backed configuration ─────────────────────────────────────────
// Add DB configuration provider LAST so database values override user-secrets
((IConfigurationBuilder)builder.Configuration).Add(new DbConfigurationSource(
    builder.Configuration.GetConnectionString("Default")!));
builder.Services.AddSingleton<IConfigurationRoot>(builder.Configuration);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AllByMyshelfDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── BGG configuration ─────────────────────────────────────────────────────────
builder.Services.AddOptions<BggOptions>()
    .Bind(builder.Configuration.GetSection(BggOptions.SectionName));

// ── BGG HTTP client ───────────────────────────────────────────────────────────
builder.Services.AddHttpClient<BggClient>(client =>
{
    client.BaseAddress = new Uri("https://boardgamegeek.com");
    client.DefaultRequestHeaders.Add("User-Agent", "AllByMyshelf/1.0");
});

// ── Discogs configuration ─────────────────────────────────────────────────────
builder.Services.AddOptions<DiscogsOptions>()
    .Bind(builder.Configuration.GetSection(DiscogsOptions.SectionName));

// ── Discogs HTTP client ───────────────────────────────────────────────────────
builder.Services.AddHttpClient<DiscogsClient>(client =>
{
    client.BaseAddress = new Uri("https://api.discogs.com");
    client.DefaultRequestHeaders.Add("User-Agent", "AllByMyshelf/1.0");
});

// ── Hardcover configuration ───────────────────────────────────────────────────
builder.Services.AddOptions<HardcoverOptions>()
    .Bind(builder.Configuration.GetSection(HardcoverOptions.SectionName));

// ── Hardcover HTTP client ─────────────────────────────────────────────────────
builder.Services.AddHttpClient("Hardcover");

builder.Services.AddScoped<HardcoverClient>();

// ── Repositories & services ───────────────────────────────────────────────────
builder.Services.AddScoped<IBoardGamesRepository, BoardGamesRepository>();
builder.Services.AddScoped<IBoardGamesService, BoardGamesService>();
builder.Services.AddScoped<IBooksRepository, BooksRepository>();
builder.Services.AddScoped<IBooksService, BooksService>();
builder.Services.AddScoped<IReleasesRepository, ReleasesRepository>();
builder.Services.AddScoped<IReleasesService, ReleasesService>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<IWantlistRepository, WantlistRepository>();

// BoardGamesSyncService is a singleton BackgroundService; also exposed as IBoardGamesSyncService.
builder.Services.AddSingleton<BoardGamesSyncService>();
builder.Services.AddSingleton<IBoardGamesSyncService>(sp => sp.GetRequiredService<BoardGamesSyncService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<BoardGamesSyncService>());

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

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
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
