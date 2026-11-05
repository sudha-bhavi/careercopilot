using JobPilot.Api.Domain;

namespace JobPilot.Api.Contracts;

// Request/response shapes are separate from the EF entity so the database model
// can change without breaking the API, and clients can't set fields like Id.
public record ApplicationRequest(
    string? Company,
    string? Role,
    string? Link,
    ApplicationStatus? Status,
    DateOnly? AppliedDate,
    DateOnly? FollowUpDate,
    string? Notes);

public record ApplicationResponse(
    Guid Id,
    string Company,
    string Role,
    string? Link,
    ApplicationStatus Status,
    DateOnly? AppliedDate,
    DateOnly? FollowUpDate,
    string? Notes,
    DateTimeOffset CreatedAt)
{
    public static ApplicationResponse From(JobApplication a) => new(
        a.Id, a.Company, a.Role, a.Link, a.Status, a.AppliedDate, a.FollowUpDate, a.Notes, a.CreatedAt);
}

public record DashboardResponse(int Total, Dictionary<string, int> ByStatus, int FollowUpsDue);

public record LoginRequest(string? Username, string? Password);

public record LoginResponse(string Token);

public record ResumeMatchRequest(string? ResumeText, string? JobDescription);

public record ResumeMatchResult(int Score, IReadOnlyList<string> Matched, IReadOnlyList<string> Missing);

public record JobSearchRequest(string? City, string? Query, string? ResumeText);

public record JobListing(
    string Id,
    string Title,
    string Company,
    string Location,
    string Description,
    string Url,
    string? Salary,
    DateTimeOffset? PostedAt);

public record JobMatch(JobListing Job, int Score, IReadOnlyList<string> Matched, IReadOnlyList<string> Missing);
