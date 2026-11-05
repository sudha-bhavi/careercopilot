using System.ComponentModel.DataAnnotations;

namespace JobPilot.Api.Auth;

// Bound from the "Auth" configuration section. Values come from user-secrets locally
// and environment variables (Auth__Username ...) in production, never from git.
public class AuthOptions
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";

    // HMAC-SHA256 needs a key of at least 256 bits, so we insist on 32+ characters.
    [Required, MinLength(32)] public string JwtKey { get; set; } = "";

    public string Issuer { get; set; } = "jobpilot";
    public int ExpiryMinutes { get; set; } = 480;
}
