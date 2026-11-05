using System.Net;
using System.Net.Http.Json;
using JobPilot.Api.Contracts;

namespace JobPilot.Api.Tests;

public class JobSearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Resume = "Senior developer with C#, .NET and PostgreSQL experience.";

    [Fact]
    public async Task Search_RanksBestFitFirst()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/jobs/search",
            new { city = "Cincinnati", query = "engineer", resumeText = Resume });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = (await response.Content.ReadFromJsonAsync<List<JobMatch>>())!;

        Assert.Equal(3, results.Count);
        Assert.Equal("2", results[0].Job.Id);
        Assert.Equal(100, results[0].Score);
        Assert.Equal(new[] { "C#", ".NET", "PostgreSQL" }, results[0].Matched);
        Assert.Equal("Cincinnati", results[0].Job.Location);
        // The frontend job needs skills the resume doesn't show.
        var frontend = results.Single(r => r.Job.Id == "1");
        Assert.Equal(0, frontend.Score);
        Assert.Contains("React", frontend.Missing);
    }

    [Fact]
    public async Task Search_WithoutCity_Returns400()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/jobs/search", new { resumeText = Resume });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithoutResume_Returns400()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/jobs/search", new { city = "Cincinnati" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/jobs/search",
            new { city = "Cincinnati", resumeText = Resume });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenSourceIsDown_Returns503()
    {
        using var down = new UnavailableJobsApiFactory();
        var client = await down.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/jobs/search",
            new { city = "Cincinnati", resumeText = Resume });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
