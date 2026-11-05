using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobPilot.Api.Contracts;
using JobPilot.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobPilot.Api.Tests;

// Starts the real API in memory, but with an in-memory database and fixed test credentials,
// so tests need no Postgres, no secrets and no network.
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string Username = "test-user";
    public const string Password = "test-password";

    // One name per factory: all requests in a test class share data, different classes don't.
    private readonly string _databaseName = $"jobpilot-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Username"] = Username,
                ["Auth:Password"] = Password,
                ["Auth:JwtKey"] = "test-signing-key-that-is-at-least-32-chars"
            }));

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql registrations (DbContextOptions<T> and its configuration)...
            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericArguments().Contains(typeof(JobPilotDbContext)))
                .ToList();
            foreach (var descriptor in toRemove) services.Remove(descriptor);

            // ...and use an in-memory database instead.
            services.AddDbContext<JobPilotDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}
