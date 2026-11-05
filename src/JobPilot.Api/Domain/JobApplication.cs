using System.Text.Json.Serialization;

namespace JobPilot.Api.Domain;

// Serialised as text ("Applied"), not a number, so the API and database stay readable.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Saved,
    Applied,
    Interview,
    Offer,
    Rejected
}

public class JobApplication
{
    public Guid Id { get; set; }
    public required string Company { get; set; }
    public required string Role { get; set; }
    public string? Link { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Saved;
    public DateOnly? AppliedDate { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
