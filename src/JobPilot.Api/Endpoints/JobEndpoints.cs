using JobPilot.Api.Contracts;
using JobPilot.Api.Services;

namespace JobPilot.Api.Endpoints;

public static class JobEndpoints
{
    private const int MaxResumeLength = 20_000;

    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        // POST (not GET) because the resume travels in the body; it is never stored or logged.
        // Rate limited separately, because every call spends the job source's free quota.
        app.MapPost("/api/jobs/search", async (
            JobSearchRequest request,
            IJobSearchProvider provider,
            IResumeMatcher matcher,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            IReadOnlyList<JobListing> jobs;
            try
            {
                var query = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();
                jobs = await provider.SearchAsync(request.City!.Trim(), query, ct);
            }
            catch (JobSearchUnavailableException ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Job search unavailable");
            }

            // Score every posting against the resume with the same matcher as the Resume match tab.
            var ranked = new List<JobMatch>();
            foreach (var job in jobs)
            {
                var match = await matcher.MatchAsync(request.ResumeText!, $"{job.Title}\n{job.Description}", ct);
                ranked.Add(new JobMatch(job, match.Score, match.Matched, match.Missing));
            }

            // Best fit first: higher score, then more skills actually matched, then newest.
            return Results.Ok(ranked
                .OrderByDescending(r => r.Score)
                .ThenByDescending(r => r.Matched.Count)
                .ThenByDescending(r => r.Job.PostedAt)
                .ToList());
        }).RequireAuthorization().RequireRateLimiting("search");

        return app;
    }

    private static Dictionary<string, string[]> Validate(JobSearchRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(r.City)) errors["city"] = ["City is required."];
        else if (r.City.Trim().Length > 100) errors["city"] = ["City must be 100 characters or fewer."];

        if (r.Query is { Length: > 100 }) errors["query"] = ["Job title must be 100 characters or fewer."];

        if (string.IsNullOrWhiteSpace(r.ResumeText)) errors["resumeText"] = ["Paste your resume to rank the jobs."];
        else if (r.ResumeText.Length > MaxResumeLength)
            errors["resumeText"] = [$"Resume must be {MaxResumeLength} characters or fewer."];

        return errors;
    }
}
