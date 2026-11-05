using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobPilot.Api.Contracts;

namespace JobPilot.Api.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = ApiFactory.Username, password = ApiFactory.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = ApiFactory.Username, password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Applications_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Applications_WithGarbageToken_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
