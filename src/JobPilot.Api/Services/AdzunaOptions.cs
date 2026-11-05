namespace JobPilot.Api.Services;

// Bound from the "Adzuna" configuration section (user-secrets locally, environment variables
// Adzuna__AppId / Adzuna__AppKey in production). Both are optional so the rest of the app
// still starts without them; job search simply reports that it is not configured.
public class AdzunaOptions
{
    public string AppId { get; set; } = "";
    public string AppKey { get; set; } = "";

    // Adzuna country code in the URL path (us, gb, ca, au ...).
    public string Country { get; set; } = "us";
}
