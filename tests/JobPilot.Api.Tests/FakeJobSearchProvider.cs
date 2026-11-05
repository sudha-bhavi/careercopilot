using JobPilot.Api.Contracts;
using JobPilot.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace JobPilot.Api.Tests;

public class FakeJobSearchProvider : IJobSearchProvider
{
    public Task<IReadOnlyList<JobListing>> SearchAsync(string city, string? query, CancellationToken ct)
    {
        IReadOnlyList<JobListing> jobs =
        [
            new("1", "Frontend Engineer", "Acme", city, "We use React, TypeScript and GraphQL.", "https://example.com/1", null, null),
            new("2", "Backend Engineer", "Globex", city, "C#, .NET and PostgreSQL experience required.", "https://example.com/2", "$100,000 – $130,000", null),
            new("3", "Office Manager", "Initech", city, "Great communication skills.", "https://example.com/3", null, null)
        ];
        return Task.FromResult(jobs);
    }
}

public class UnavailableJobSearchProvider : IJobSearchProvider
{
    public Task<IReadOnlyList<JobListing>> SearchAsync(string city, string? query, CancellationToken ct) =>
        throw new JobSearchUnavailableException("Job search is not configured yet.");
}

// A factory whose job source is down, to test the 503 path.
public class UnavailableJobsApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => services.AddSingleton<IJobSearchProvider>(new UnavailableJobSearchProvider()));
    }
}
