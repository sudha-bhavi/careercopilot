using System.Net;
using System.Text;
using JobPilot.Api.Services;
using Microsoft.Extensions.Options;

namespace JobPilot.Api.Tests;

public class AdzunaJobProviderTests
{
    private const string SampleJson = """
        {
          "results": [
            {
              "id": "123",
              "title": "Software Engineer",
              "description": "Build <strong>C#</strong> services &amp; APIs   for   Acme",
              "redirect_url": "https://www.adzuna.com/land/ad/123",
              "created": "2026-11-01T10:00:00Z",
              "salary_min": 90000,
              "salary_max": 120000,
              "company": { "display_name": "Acme" },
              "location": { "display_name": "Cincinnati, Ohio" }
            },
            {
              "id": 124,
              "title": "Unsafe link",
              "description": "x",
              "redirect_url": "javascript:alert(1)",
              "company": { "display_name": "Evil" },
              "location": { "display_name": "Nowhere" }
            }
          ]
        }
        """;

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    private static AdzunaJobProvider CreateProvider(StubHandler handler, string appId = "id", string appKey = "key") =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.adzuna.com/") },
            Options.Create(new AdzunaOptions { AppId = appId, AppKey = appKey }));

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Search_MapsResults_CleansText_AndDropsUnsafeLinks()
    {
        var handler = new StubHandler(_ => Json(SampleJson));

        var jobs = await CreateProvider(handler).SearchAsync("Cincinnati", "developer", CancellationToken.None);

        var job = Assert.Single(jobs); // the javascript: listing is dropped
        Assert.Equal("123", job.Id);
        Assert.Equal("Software Engineer", job.Title);
        Assert.Equal("Acme", job.Company);
        Assert.Equal("Cincinnati, Ohio", job.Location);
        Assert.Equal("Build C# services & APIs for Acme", job.Description);
        Assert.Equal("https://www.adzuna.com/land/ad/123", job.Url);
        Assert.Equal("$90,000 – $120,000", job.Salary);
        Assert.NotNull(job.PostedAt);
    }

    [Fact]
    public async Task Search_AcceptsAdzunasNonStandardCharsetHeader()
    {
        // Adzuna really sends "charset=utf8" (no hyphen); this must not crash the search.
        var handler = new StubHandler(_ =>
        {
            var response = Json(SampleJson);
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json;charset=utf8");
            return response;
        });

        var jobs = await CreateProvider(handler).SearchAsync("Cincinnati", null, CancellationToken.None);

        Assert.Single(jobs);
    }

    [Fact]
    public async Task Search_EscapesCityAndQuery()
    {
        var handler = new StubHandler(_ => Json("""{ "results": [] }"""));

        await CreateProvider(handler).SearchAsync("St. Louis&x=1", "c# dev", CancellationToken.None);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("where=St.%20Louis%26x%3D1", query);
        Assert.Contains("what=c%23%20dev", query);
    }

    [Fact]
    public async Task Search_WithoutKeys_ThrowsUnavailable()
    {
        var handler = new StubHandler(_ => Json(SampleJson));

        await Assert.ThrowsAsync<JobSearchUnavailableException>(() =>
            CreateProvider(handler, appId: "", appKey: "").SearchAsync("Cincinnati", null, CancellationToken.None));
    }

    [Fact]
    public async Task Search_WhenSourceFails_ThrowsUnavailable_WithoutLeakingTheKey()
    {
        var handler = new StubHandler(_ => Json("{}", HttpStatusCode.InternalServerError));

        var ex = await Assert.ThrowsAsync<JobSearchUnavailableException>(() =>
            CreateProvider(handler, appKey: "super-secret-key").SearchAsync("Cincinnati", null, CancellationToken.None));

        Assert.DoesNotContain("super-secret-key", ex.Message);
    }
}
