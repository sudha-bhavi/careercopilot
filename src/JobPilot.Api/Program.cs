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

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
builder.Services.AddDbContext<JobPilotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("JobPilot")));

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
});

// --- Resume matching: swap this one line to plug in an AI-backed matcher later ---
builder.Services.AddSingleton<IResumeMatcher, KeywordResumeMatcher>();

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

app.Run();

// Top-level statements generate an internal Program class. Making it public lets the
// test project use WebApplicationFactory<Program>.
public partial class Program;
