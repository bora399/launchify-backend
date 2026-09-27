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
                // Firebase Admin SDK, gelen token'ı doğrular ve çözer
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

                // Eğer geçerliyse, bu kullanıcının eşsiz Firebase UID'sini geri dönüyoruz
                return decodedToken.Uid;
            }
            catch (Exception ex)
            {
                // Token geçersizse, süresi dolmuşsa veya sahteyse buraya düşer
                throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş oturum anahtarı.", ex);
            }
        }
    }
}