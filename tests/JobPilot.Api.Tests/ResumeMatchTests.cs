using System.Net;
using System.Net.Http.Json;
using JobPilot.Api.Contracts;
using JobPilot.Api.Services;

namespace JobPilot.Api.Tests;

public class ResumeMatchTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly KeywordResumeMatcher Matcher = new();

    [Fact]
    public async Task Match_SplitsMatchedAndMissing_AndScores()
    {
        var result = await Matcher.MatchAsync(
            resumeText: "Senior engineer. I use c# and Docker daily.",
            jobDescription: "We need C#, .NET, PostgreSQL and Docker experience.",
            CancellationToken.None);

        Assert.Equal(["C#", "Docker"], result.Matched);
        Assert.Equal([".NET", "PostgreSQL"], result.Missing);
        Assert.Equal(50, result.Score);
    }

    [Fact]
    public async Task Match_DoesNotConfuseJavaWithJavaScript()
    {
        var result = await Matcher.MatchAsync("Java developer", "Looking for JavaScript skills", CancellationToken.None);

        Assert.Empty(result.Matched);
        Assert.Equal(["JavaScript"], result.Missing);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public async Task Match_DoesNotFindSqlInsidePostgreSql()
    {
        var result = await Matcher.MatchAsync("I know PostgreSQL", "Strong SQL skills required", CancellationToken.None);

        Assert.Equal(["SQL"], result.Missing);
    }

    [Fact]
    public async Task Match_WithNoKnownSkills_ScoresZero()
    {
        var result = await Matcher.MatchAsync("Anything", "We value teamwork and curiosity.", CancellationToken.None);

        Assert.Equal(0, result.Score);
        Assert.Empty(result.Matched);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public async Task Endpoint_ReturnsResult()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/resume/match",
            new { resumeText = "React and TypeScript", jobDescription = "React, TypeScript, GraphQL" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ResumeMatchResult>())!;
        Assert.Equal(67, body.Score);
        Assert.Equal(["GraphQL"], body.Missing);
    }

    [Fact]
    public async Task Endpoint_WithEmptyText_Returns400()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/resume/match",
            new { resumeText = "", jobDescription = "React" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/resume/match",
            new { resumeText = "a", jobDescription = "b" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
