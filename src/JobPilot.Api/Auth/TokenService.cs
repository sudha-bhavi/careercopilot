using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JobPilot.Api.Auth;

public class TokenService(IOptions<AuthOptions> options, TimeProvider time)
{
    private readonly AuthOptions _options = options.Value;

    // Hashing both sides first gives equal-length inputs, so FixedTimeEquals takes the
    // same time whether the guess is close or not (no timing side channel).
    public bool ValidateCredentials(string? username, string? password)
    {
        var userOk = SafeEquals(username ?? "", _options.Username);
        var passOk = SafeEquals(password ?? "", _options.Password);
        return userOk & passOk; // single '&' on purpose: both checks always run
    }

    public string CreateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, _options.Username)]),
            Issuer = _options.Issuer,
            Audience = _options.Issuer,
            Expires = time.GetUtcNow().AddMinutes(_options.ExpiryMinutes).UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static bool SafeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
