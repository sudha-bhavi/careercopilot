using System.Net;
using System.Net.Http.Json;
using JobPilot.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobPilot.Api.Tests;

// The app with a real Postgres provider pointed at a port nothing listens on: a stand-in for an
// expired or stopped database.
public class DatabaseDownApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericArguments().Contains(typeof(JobPilotDbContext)))
                .ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            services.AddDbContext<JobPilotDbContext>(o =>
                o.UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2;Command Timeout=2"));
        });
    }
}

public class DatabaseDownTests
{
    [Fact]
    public async Task App_StartsWithoutDatabase_AndLivenessStaysHealthy()
    {
        using var factory = new DatabaseDownApiFactory();

        var live = await factory.CreateClient().GetAsync("/health");
        var ready = await factory.CreateClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode); // so the host does not kill the service
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
    }

    [Fact]
    public async Task DataEndpoints_Return503WithAFriendlyMessage()
    {
        using var factory = new DatabaseDownApiFactory();
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.GetAsync("/api/applications");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Database unavailable", body);
        Assert.Contains("not an application error", body);
    }

    [Fact]
    public async Task FeaturesThatNeedNoDatabase_StillWork()
    {
        using var factory = new DatabaseDownApiFactory();
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/resume/match",
            new { resumeText = "C# developer", jobDescription = "We need C# and Docker." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
