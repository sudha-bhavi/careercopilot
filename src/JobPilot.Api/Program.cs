using System.Text;
using System.Threading.RateLimiting;
using JobPilot.Api.Auth;
using JobPilot.Api.Data;
using JobPilot.Api.Endpoints;
using JobPilot.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<JobPilotDbContext>(options =>
    options.UseNpgsql(ToNpgsqlConnectionString(builder.Configuration.GetConnectionString("JobPilot"))));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<JobPilotDbContext>("database", tags: ["ready"]);

// --- Auth: single user, credentials and signing key come from configuration ---
// ValidateOnStart makes a missing secret fail at startup with a clear message,
// instead of the first login failing mysteriously.
builder.Services.AddOptions<AuthOptions>()
    .BindConfiguration("Auth")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
// JWT settings are read from IOptions at runtime (not from builder.Configuration now),
// so tests and deployments can override the key after the builder is created.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AuthOptions>>((jwt, auth) =>
    {
        var a = auth.Value;
        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = a.Issuer,
            ValidateAudience = true,
            ValidAudience = a.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(a.JwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Max 10 login attempts per minute per client address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    // Job search spends the job source's free quota, so it gets its own, looser limit.
    options.AddPolicy("search", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// --- Resume matching: swap this one line to plug in an AI-backed matcher later ---
builder.Services.AddSingleton<IResumeMatcher, KeywordResumeMatcher>();

// --- Job search: Adzuna's official API. Optional config; see AdzunaOptions ---
builder.Services.AddOptions<AdzunaOptions>().BindConfiguration("Adzuna");
builder.Services.AddHttpClient<IJobSearchProvider, AdzunaJobProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.adzuna.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// Apply pending migrations on startup. Fine for a single-instance app; a larger system
// would run migrations as a separate deploy step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JobPilotDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

// Serves the built React app from wwwroot (present in the Docker image, absent in dev and tests).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Liveness: "is the process up?" Runs no checks, so a database outage doesn't get the app restarted.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
// Readiness: "can it serve traffic?" Includes the database check.
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapAuthEndpoints();
app.MapApplicationEndpoints();
app.MapDashboardEndpoints();
app.MapResumeEndpoints();
app.MapJobEndpoints();

// Any other path returns the React app so client-side routes survive a refresh.
// Two exclusions: /api/* (an unknown API route must be a real 404, not an HTML page) and any
// path containing a dot. Static-file middleware skips a request once a route has matched it,
// so a catch-all that also matched /assets/app.js would answer with index.html instead of the file.
app.MapFallbackToFile("{*path:regex(^(?!api/)[^.]*$)}", "index.html");

app.Run();

// Hosts such as Render hand out "postgres://user:pass@host:port/db". Npgsql wants key=value pairs,
// so convert the URL form; a connection string that is already key=value passes through unchanged.
static string ToNpgsqlConnectionString(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return "";
    if (!value.StartsWith("postgres://") && !value.StartsWith("postgresql://")) return value;

    var uri = new Uri(value);
    var credentials = uri.UserInfo.Split(':', 2);
    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        SslMode = SslMode.Prefer // encrypted when the server supports it, as on Render's external URL
    }.ConnectionString;
}

// Top-level statements generate an internal Program class. Making it public lets the
// test project use WebApplicationFactory<Program>.
public partial class Program;
