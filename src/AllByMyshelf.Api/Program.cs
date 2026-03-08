using AllByMyshelf.Api.Configuration;
using AllByMyshelf.Api.Infrastructure.Data;
using AllByMyshelf.Api.Infrastructure.ExternalApis;
using AllByMyshelf.Api.Repositories;
using AllByMyshelf.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

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

// ── Repositories & services ───────────────────────────────────────────────────
builder.Services.AddScoped<IReleasesRepository, ReleasesRepository>();
builder.Services.AddScoped<IReleasesService, ReleasesService>();

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

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
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

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
