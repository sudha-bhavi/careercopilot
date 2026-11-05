using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JobPilot.Api.Contracts;
using Microsoft.Extensions.Options;

namespace JobPilot.Api.Services;

/// <summary>Searches open jobs through Adzuna's official API (no scraping).</summary>
public sealed partial class AdzunaJobProvider(HttpClient http, IOptions<AdzunaOptions> options) : IJobSearchProvider
{
    public async Task<IReadOnlyList<JobListing>> SearchAsync(string city, string? query, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.AppId) || string.IsNullOrWhiteSpace(o.AppKey))
        {
            throw new JobSearchUnavailableException("Job search is not configured yet. Add the Adzuna app id and key.");
        }

        // Every user-supplied value is escaped, so it cannot add or change query parameters.
        var url = $"v1/api/jobs/{o.Country}/search/1?app_id={Esc(o.AppId)}&app_key={Esc(o.AppKey)}" +
                  $"&results_per_page=30&distance=40&where={Esc(city)}";
        if (!string.IsNullOrWhiteSpace(query)) url += $"&what={Esc(query)}";

        try
        {
            using var response = await http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            // Read the raw bytes and parse them ourselves. Adzuna labels its reply "charset=utf8"
            // (no hyphen), which makes ReadFromJsonAsync throw even though the body is valid UTF-8 JSON.
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var body = await JsonSerializer.DeserializeAsync<AdzunaResponse>(stream, cancellationToken: ct);
            return (body?.Results ?? [])
                .Select(Map)
                .OfType<JobListing>() // drops the nulls (listings with an unsafe link)
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // The exception message can contain request details, so it is not passed on.
            throw new JobSearchUnavailableException("The job source is unavailable right now. Try again in a minute.");
        }
    }

    private static string Esc(string value) => Uri.EscapeDataString(value);

    private static JobListing? Map(AdzunaJob job)
    {
        // This URL comes from a third party and ends up in an <a href>. Accept only http(s),
        // otherwise a "javascript:" link in the data would become an XSS vector.
        if (!Uri.TryCreate(job.RedirectUrl, UriKind.Absolute, out var link) || link.Scheme is not ("http" or "https"))
        {
            return null;
        }

        var description = WebUtility.HtmlDecode(HtmlTag().Replace(job.Description ?? "", ""));
        description = Whitespace().Replace(description, " ").Trim();

        return new JobListing(
            Id: job.Id?.ToString() ?? link.ToString(),
            Title: Clean(job.Title) ?? "Untitled role",
            Company: Clean(job.Company?.DisplayName) ?? "Unknown company",
            Location: Clean(job.Location?.DisplayName) ?? "",
            Description: description,
            Url: link.ToString(),
            Salary: FormatSalary(job.SalaryMin, job.SalaryMax),
            PostedAt: DateTimeOffset.TryParse(job.Created, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var posted)
                ? posted
                : null);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();

    private static string? FormatSalary(double? min, double? max)
    {
        static string Money(double v) => "$" + v.ToString("N0", CultureInfo.InvariantCulture);
        return (min, max) switch
        {
            (> 0, > 0) => $"{Money(min!.Value)} – {Money(max!.Value)}",
            (> 0, _) => $"From {Money(min!.Value)}",
            (_, > 0) => $"Up to {Money(max!.Value)}",
            _ => null
        };
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

// Shapes of Adzuna's JSON response. Only the fields we use are declared.
internal sealed record AdzunaResponse(
    [property: JsonPropertyName("results")] List<AdzunaJob>? Results);

internal sealed record AdzunaJob(
    [property: JsonPropertyName("id")] JsonElement? Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("redirect_url")] string? RedirectUrl,
    [property: JsonPropertyName("created")] string? Created,
    [property: JsonPropertyName("salary_min")] double? SalaryMin,
    [property: JsonPropertyName("salary_max")] double? SalaryMax,
    [property: JsonPropertyName("company")] AdzunaName? Company,
    [property: JsonPropertyName("location")] AdzunaName? Location);

internal sealed record AdzunaName(
    [property: JsonPropertyName("display_name")] string? DisplayName);
