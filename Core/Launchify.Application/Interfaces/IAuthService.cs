using System.Threading.Tasks;

namespace Launchify.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> VerifyFirebaseTokenAsync(string idToken);
    }
}