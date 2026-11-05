using JobPilot.Api.Contracts;
using JobPilot.Api.Data;
using JobPilot.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobPilot.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (JobPilotDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            // GROUP BY runs in the database, so we never load every row just to count them.
            var counts = await db.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            // Every status appears in the result, even with zero, so the UI needs no special cases.
            var byStatus = Enum.GetValues<ApplicationStatus>()
                .ToDictionary(s => s.ToString(), s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);

            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            var due = await db.Applications.DueFollowUps(today).CountAsync(ct);

            return Results.Ok(new DashboardResponse(byStatus.Values.Sum(), byStatus, due));
        }).RequireAuthorization();

        return app;
    }
}
