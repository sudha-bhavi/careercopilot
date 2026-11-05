using JobPilot.Api.Contracts;
using JobPilot.Api.Data;
using JobPilot.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobPilot.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        // RequireAuthorization on the group protects every route below in one place,
        // so a new endpoint can't be left open by accident.
        var group = app.MapGroup("/api/applications").RequireAuthorization();

        group.MapGet("/", async (string? status, JobPilotDbContext db, CancellationToken ct) =>
        {
            var query = db.Applications.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]> { ["status"] = [$"Unknown status '{status}'."] });
                }
                query = query.Where(a => a.Status == parsed);
            }

            var items = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
            return Results.Ok(items.Select(ApplicationResponse.From));
        });

        // Declared before "/{id}" for readability; the :guid constraint also keeps them apart.
        group.MapGet("/follow-ups", async (JobPilotDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            var items = await db.Applications.AsNoTracking()
                .DueFollowUps(today)
                .OrderBy(a => a.FollowUpDate)
                .ToListAsync(ct);
            return Results.Ok(items.Select(ApplicationResponse.From));
        });

        group.MapGet("/{id:guid}", async (Guid id, JobPilotDbContext db, CancellationToken ct) =>
            await db.Applications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct) is { } found
                ? Results.Ok(ApplicationResponse.From(found))
                : Results.NotFound());

        group.MapPost("/", async (ApplicationRequest request, JobPilotDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var entity = new JobApplication
            {
                Company = request.Company!.Trim(),
                Role = request.Role!.Trim(),
                CreatedAt = time.GetUtcNow()
            };
            Apply(entity, request);

            db.Applications.Add(entity);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/applications/{entity.Id}", ApplicationResponse.From(entity));
        });

        group.MapPut("/{id:guid}", async (Guid id, ApplicationRequest request, JobPilotDbContext db, CancellationToken ct) =>
        {
            var entity = await db.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity is null) return Results.NotFound();

            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            entity.Company = request.Company!.Trim();
            entity.Role = request.Role!.Trim();
            Apply(entity, request);

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApplicationResponse.From(entity));
        });

        group.MapDelete("/{id:guid}", async (Guid id, JobPilotDbContext db, CancellationToken ct) =>
        {
            var entity = await db.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity is null) return Results.NotFound();

            db.Applications.Remove(entity);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }

    private static void Apply(JobApplication entity, ApplicationRequest r)
    {
        entity.Link = Clean(r.Link);
        entity.Status = r.Status ?? entity.Status;
        entity.AppliedDate = r.AppliedDate;
        entity.FollowUpDate = r.FollowUpDate;
        entity.Notes = Clean(r.Notes);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Dictionary<string, string[]> Validate(ApplicationRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(r.Company)) errors["company"] = ["Company is required."];
        else if (r.Company.Trim().Length > 200) errors["company"] = ["Company must be 200 characters or fewer."];

        if (string.IsNullOrWhiteSpace(r.Role)) errors["role"] = ["Role is required."];
        else if (r.Role.Trim().Length > 200) errors["role"] = ["Role must be 200 characters or fewer."];

        // Only http(s) links are accepted. A "javascript:" link stored here would become
        // an XSS vector the moment the UI renders it as <a href>.
        if (!string.IsNullOrWhiteSpace(r.Link) &&
            !(Uri.TryCreate(r.Link.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
        {
            errors["link"] = ["Link must be a valid http or https URL."];
        }

        if (r.Link is { Length: > 2000 }) errors["link"] = ["Link must be 2000 characters or fewer."];
        if (r.Notes is { Length: > 4000 }) errors["notes"] = ["Notes must be 4000 characters or fewer."];

        return errors;
    }
}

public static class ApplicationQueries
{
    // One definition of "follow-up due", shared by the list endpoint and the dashboard count.
    // Only applications still in play (Applied, Interview) need a nudge.
    public static IQueryable<JobApplication> DueFollowUps(this IQueryable<JobApplication> query, DateOnly today) =>
        query.Where(a => a.FollowUpDate != null
                         && a.FollowUpDate <= today
                         && (a.Status == ApplicationStatus.Applied || a.Status == ApplicationStatus.Interview));
}
