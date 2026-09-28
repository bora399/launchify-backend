using FirebaseAdmin.Auth;
using Launchify.Application.Interfaces;
using System.Threading.Tasks;
using System;
using Launchify.Application.Interfaces;
using Launchify.Application.Interfaces;

namespace Launchify.Infrastructure.Services
{
    public class FirebaseAuthService : IAuthService
    {
        public async Task<string> VerifyFirebaseTokenAsync(string idToken)
        {
            try
            {
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

                return decodedToken.Uid;
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş oturum anahtarı.", ex);
            }
        }
    }
}