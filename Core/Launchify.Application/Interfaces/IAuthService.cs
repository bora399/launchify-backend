using System.Threading.Tasks;

namespace Launchify.Application.Interfaces
{
    public interface IAuthService
    {
        // React tarafından bize gönderilen Firebase ID Token'ı alıp geçerliliğini doğrular, 
        // geçerliyse Firebase UID'sini (Kullanıcı ID'sini) döner.
        Task<string> VerifyFirebaseTokenAsync(string idToken);
    }
}