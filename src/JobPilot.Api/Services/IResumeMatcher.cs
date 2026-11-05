using JobPilot.Api.Contracts;

namespace JobPilot.Api.Services;

// The seam for the AI feature. The endpoint only knows this interface, so the keyword
// matcher can be swapped for an LLM-backed one by changing one line in Program.cs.
public interface IResumeMatcher
{
    Task<ResumeMatchResult> MatchAsync(string resumeText, string jobDescription, CancellationToken ct);
}
