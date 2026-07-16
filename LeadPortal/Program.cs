using System.Text.Json.Serialization;
using LeadPortal.Configuration;
using LeadPortal.Data;
using LeadPortal.Endpoints;
using LeadPortal.HealthChecks;
using LeadPortal.Hubs;
using LeadPortal.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ── Strongly-typed configuration ─────────────────────────────────────────────
builder.Services.AddOptions<HubSpotOptions>()
    .Bind(builder.Configuration.GetSection(HubSpotOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<LeadValidationOptions>(
    builder.Configuration.GetSection(LeadValidationOptions.SectionName));
builder.Services.Configure<BudgetValuationOptions>(
    builder.Configuration.GetSection(BudgetValuationOptions.SectionName));
builder.Services.Configure<SeedingOptions>(
    builder.Configuration.GetSection(SeedingOptions.SectionName));

// ── Persistence ──────────────────────────────────────────────────────────────
// Connection string comes from config; on Azure App Service it is overridden with
// a persistent path via the ConnectionStrings__Default app setting.
var sqliteConn = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing connection string 'Default'. Set ConnectionStrings:Default in configuration.");

// SQLite creates the DB file but not its parent directory — ensure it exists.
var dbPath = new SqliteConnectionStringBuilder(sqliteConn).DataSource;
var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqliteConn));

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<LeadService>();
builder.Services.AddSingleton<IHubSpotClient, MockHubSpotClient>();

builder.Services.AddSignalR();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// ── CORS (config-driven; same-origin SPA needs none) ─────────────────────────
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
                  ?? new CorsOptions();
builder.Services.AddCors(o => o.AddDefaultPolicy(policy =>
{
    if (corsOptions.AllowedOrigins.Length > 0)
        policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

// ── Database migration + optional seeding ────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (services.GetRequiredService<IOptions<SeedingOptions>>().Value.Enabled)
        await Seed.RunAsync(db);
}

// ── Middleware pipeline ──────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

app.UseSerilogRequestLogging();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapLeadApi();
app.MapHub<LeadsHub>("/hub/leads");
app.MapHealthChecks("/health");

app.Run();
