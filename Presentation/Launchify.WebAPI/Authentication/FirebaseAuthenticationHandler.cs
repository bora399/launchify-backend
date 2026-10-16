using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Launchify.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Launchify.WebAPI.Authentication;

public sealed class FirebaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAuthService authService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Firebase";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            && string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            token = header.Parameter;
        }
        // SignalR browser transports cannot always attach an Authorization header.
        if (string.IsNullOrWhiteSpace(token) && Request.Path.StartsWithSegments("/generationHub"))
            token = Request.Query["access_token"];

        if (string.IsNullOrWhiteSpace(token)) return AuthenticateResult.NoResult();

        try
        {
            var identity = await authService.VerifyFirebaseTokenAsync(token);
            return AuthenticateResult.Success(new AuthenticationTicket(identity, SchemeName));
        }
        catch (UnauthorizedAccessException)
        {
            return AuthenticateResult.Fail("Geçersiz veya süresi dolmuş oturum.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Response.WriteAsJsonAsync(new { message = "Bu işlem için giriş yapmanız gerekiyor." });
    }
}
