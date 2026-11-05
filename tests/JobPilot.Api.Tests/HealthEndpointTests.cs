using System.Net;

namespace JobPilot.Api.Tests;

public class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_IsAnonymousAndHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_ChecksTheDatabase()
    {
        var response = await factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
