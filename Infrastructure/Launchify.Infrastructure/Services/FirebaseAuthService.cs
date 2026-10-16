using FirebaseAdmin.Auth;
using Launchify.Application.Interfaces;
using System.Security.Claims;

namespace Launchify.Infrastructure.Services;

public class FirebaseAuthService(FirebaseAuth firebaseAuth) : IAuthService
{
    public async Task<ClaimsPrincipal> VerifyFirebaseTokenAsync(string idToken)
    {
        try
        {
            var token = await firebaseAuth.VerifyIdTokenAsync(idToken);
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, token.Uid) };
            if (token.Claims.TryGetValue("email", out var email) && email is string address)
                claims.Add(new Claim(ClaimTypes.Email, address));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase"));
        }
        catch (FirebaseAuthException ex)
        {
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş oturum anahtarı.", ex);
        }
    }
}
