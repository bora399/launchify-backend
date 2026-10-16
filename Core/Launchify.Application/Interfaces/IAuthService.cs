using System.Security.Claims;

namespace Launchify.Application.Interfaces;

public interface IAuthService
{
    Task<ClaimsPrincipal> VerifyFirebaseTokenAsync(string idToken);
}
