using System.Text.RegularExpressions;
using JobPilot.Api.Contracts;

namespace JobPilot.Api.Services;

/// <summary>
/// Deterministic matcher: finds known skills in the job description, then checks which of
/// them the resume also mentions. Free, instant and testable; no AI provider needed.
/// </summary>
public sealed class KeywordResumeMatcher : IResumeMatcher
{
    private static readonly (string Name, Regex Pattern)[] Catalog =
    [
        S("C#", "csharp"), S(".NET", "dotnet"), S("ASP.NET Core", "aspnet core", "asp.net"),
        S("Entity Framework", "EF Core"), S("LINQ"), S("Blazor"), S("WPF"),
        S("SQL Server", "mssql"), S("PostgreSQL", "postgres"), S("MySQL"), S("MongoDB"), S("Redis"), S("SQL"),
        S("Azure"), S("AWS"), S("GCP", "Google Cloud"), S("Azure DevOps"),
        S("Docker"), S("Kubernetes", "k8s"), S("Terraform"), S("CI/CD"), S("GitHub Actions"), S("Jenkins"),
        S("Git"), S("Linux"),
        S("REST APIs", "REST API", "RESTful"), S("GraphQL"), S("gRPC"), S("OpenAPI", "Swagger"),
        S("Microservices"), S("Clean Architecture"), S("Domain-Driven Design", "DDD"), S("SOLID"), S("Design Patterns"),
        S("RabbitMQ"), S("Kafka"), S("Service Bus"),
        S("xUnit"), S("NUnit"), S("Unit Testing", "unit tests"), S("TDD", "test-driven"),
        S("OAuth"), S("JWT"),
        S("React"), S("Angular"), S("Vue"), S("TypeScript"), S("JavaScript"), S("Node.js", "nodejs"),
        S("HTML"), S("CSS"), S("Tailwind"), S("Redux"), S("Next.js"),
        S("Java"), S("Python"), S("Kotlin"), S("Swift"), S("PHP"), S("Ruby"), S("Scala"), S("Rust"), S("Golang"), S("C++"),
        S("Agile"), S("Scrum"), S("Machine Learning"), S("Power BI")
    ];

    public Task<ResumeMatchResult> MatchAsync(string resumeText, string jobDescription, CancellationToken ct)
    {
        var wanted = Catalog.Where(s => s.Pattern.IsMatch(jobDescription)).ToList();

        var matched = wanted.Where(s => s.Pattern.IsMatch(resumeText)).Select(s => s.Name).ToList();
        var missing = wanted.Where(s => !s.Pattern.IsMatch(resumeText)).Select(s => s.Name).ToList();

        var score = wanted.Count == 0
            ? 0
            : (int)Math.Round(100.0 * matched.Count / wanted.Count, MidpointRounding.AwayFromZero);

        return Task.FromResult(new ResumeMatchResult(score, matched, missing));
    }

    // The lookarounds are word boundaries that also work for names like "C#" and ".NET",
    // where \b fails. They stop "Java" matching inside "JavaScript" and "SQL" inside "PostgreSQL".
    private static (string, Regex) S(string name, params string[] aliases)
    {
        var alternatives = new[] { name }.Concat(aliases).Select(Regex.Escape);
        var pattern = $@"(?<![A-Za-z0-9])(?:{string.Join("|", alternatives)})(?![A-Za-z0-9])";
        return (name, new Regex(pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
            TimeSpan.FromSeconds(1)));
    }
}
