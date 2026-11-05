using JobPilot.Api.Contracts;
using JobPilot.Api.Services;

namespace JobPilot.Api.Endpoints;

public static class ResumeEndpoints
{
    private const int MaxLength = 20_000;

    public static IEndpointRouteBuilder MapResumeEndpoints(this IEndpointRouteBuilder app)
    {
        // Stateless: the resume text is sent with each request and never stored.
        app.MapPost("/api/resume/match", async (ResumeMatchRequest request, IResumeMatcher matcher, CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            Check(errors, "resumeText", request.ResumeText);
            Check(errors, "jobDescription", request.JobDescription);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            return Results.Ok(await matcher.MatchAsync(request.ResumeText!, request.JobDescription!, ct));
        }).RequireAuthorization();

        return app;
    }

    // The length cap also bounds the work the regex matcher (or a paid AI call) can be made to do.
    private static void Check(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[field] = [$"{field} is required."];
        else if (value.Length > MaxLength) errors[field] = [$"{field} must be {MaxLength} characters or fewer."];
    }
}
