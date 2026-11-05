using JobPilot.Api.Contracts;

namespace JobPilot.Api.Services;

// The seam for job sources. The endpoint only knows this interface, so Adzuna can be
// swapped for or combined with another source without touching the endpoint or the UI.
public interface IJobSearchProvider
{
    Task<IReadOnlyList<JobListing>> SearchAsync(string city, string? query, CancellationToken ct);
}

// Thrown when the source is not configured or cannot be reached. The endpoint turns it into a
// clean 503 instead of leaking an exception (or anything about the request URL, which holds the key).
public class JobSearchUnavailableException(string message) : Exception(message);
