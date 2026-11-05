using JobPilot.Api.Auth;
using JobPilot.Api.Contracts;

namespace JobPilot.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, TokenService tokens) =>
                tokens.ValidateCredentials(request.Username, request.Password)
                    ? Results.Ok(new LoginResponse(tokens.CreateToken()))
                    : Results.Unauthorized())
            .AllowAnonymous()
            .RequireRateLimiting("login"); // slows down password guessing

        return app;
    }
}
