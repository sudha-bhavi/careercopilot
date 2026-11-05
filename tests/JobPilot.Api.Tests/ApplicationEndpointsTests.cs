using System.Net;
using System.Net.Http.Json;
using JobPilot.Api.Contracts;
using JobPilot.Api.Domain;

namespace JobPilot.Api.Tests;

public class ApplicationEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // Unique names keep tests independent even though they share one in-memory database.
    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<ApplicationResponse> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/applications", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApplicationResponse>())!;
    }

    private static string Day(int offsetFromToday) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offsetFromToday).ToString("yyyy-MM-dd");

    [Fact]
    public async Task Create_ReturnsCreated_AndDefaultsToSaved()
    {
        var client = await factory.CreateAuthedClientAsync();

        var created = await CreateAsync(client, new { company = Unique("Acme"), role = "Backend Developer" });

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(ApplicationStatus.Saved, created.Status);
    }

    [Fact]
    public async Task Create_WithoutCompany_Returns400()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/applications", new { role = "Developer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithJavascriptLink_Returns400()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.PostAsJsonAsync("/api/applications",
            new { company = Unique("Evil"), role = "Dev", link = "javascript:alert(1)" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.GetAsync($"/api/applications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var client = await factory.CreateAuthedClientAsync();
        var interview = await CreateAsync(client, new { company = Unique("A"), role = "Dev", status = "Interview" });
        var saved = await CreateAsync(client, new { company = Unique("B"), role = "Dev", status = "Saved" });

        var result = await client.GetFromJsonAsync<List<ApplicationResponse>>("/api/applications?status=interview");

        Assert.Contains(result!, a => a.Id == interview.Id);
        Assert.DoesNotContain(result!, a => a.Id == saved.Id);
        Assert.All(result!, a => Assert.Equal(ApplicationStatus.Interview, a.Status));
    }

    [Fact]
    public async Task List_WithUnknownStatus_Returns400()
    {
        var client = await factory.CreateAuthedClientAsync();

        var response = await client.GetAsync("/api/applications?status=nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesStatusAndNotes()
    {
        var client = await factory.CreateAuthedClientAsync();
        var company = Unique("Update");
        var created = await CreateAsync(client, new { company, role = "Dev" });

        var response = await client.PutAsJsonAsync($"/api/applications/{created.Id}",
            new { company, role = "Dev", status = "Applied", appliedDate = Day(0), notes = "Sent CV" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ApplicationResponse>())!;
        Assert.Equal(ApplicationStatus.Applied, updated.Status);
        Assert.Equal("Sent CV", updated.Notes);
    }

    [Fact]
    public async Task Delete_RemovesTheApplication()
    {
        var client = await factory.CreateAuthedClientAsync();
        var created = await CreateAsync(client, new { company = Unique("Delete"), role = "Dev" });

        var delete = await client.DeleteAsync($"/api/applications/{created.Id}");
        var get = await client.GetAsync($"/api/applications/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task FollowUps_ReturnsOnlyDueApplicationsStillInPlay()
    {
        var client = await factory.CreateAuthedClientAsync();
        var due = await CreateAsync(client, new { company = Unique("Due"), role = "Dev", status = "Applied", followUpDate = Day(-1) });
        var future = await CreateAsync(client, new { company = Unique("Future"), role = "Dev", status = "Applied", followUpDate = Day(30) });
        var rejected = await CreateAsync(client, new { company = Unique("Rejected"), role = "Dev", status = "Rejected", followUpDate = Day(-1) });
        var saved = await CreateAsync(client, new { company = Unique("Saved"), role = "Dev", status = "Saved", followUpDate = Day(-1) });

        var result = await client.GetFromJsonAsync<List<ApplicationResponse>>("/api/applications/follow-ups");

        Assert.Contains(result!, a => a.Id == due.Id);
        Assert.DoesNotContain(result!, a => a.Id == future.Id);
        Assert.DoesNotContain(result!, a => a.Id == rejected.Id);
        Assert.DoesNotContain(result!, a => a.Id == saved.Id);
    }

    [Fact]
    public async Task Dashboard_CountsByStatus_AndFollowUps()
    {
        // Own factory (and so own empty database) so the totals are exact.
        using var isolated = new ApiFactory();
        var client = await isolated.CreateAuthedClientAsync();
        await CreateAsync(client, new { company = "One", role = "Dev", status = "Applied", followUpDate = Day(-2) });
        await CreateAsync(client, new { company = "Two", role = "Dev", status = "Applied" });
        await CreateAsync(client, new { company = "Three", role = "Dev", status = "Offer" });

        var dashboard = await client.GetFromJsonAsync<DashboardResponse>("/api/dashboard");

        Assert.Equal(3, dashboard!.Total);
        Assert.Equal(2, dashboard.ByStatus["Applied"]);
        Assert.Equal(1, dashboard.ByStatus["Offer"]);
        Assert.Equal(0, dashboard.ByStatus["Rejected"]);
        Assert.Equal(1, dashboard.FollowUpsDue);
    }
}
