using Launchify.Domain.Entities;
using System.Threading.Tasks;

namespace Launchify.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<AppUser> GetByIdAsync(string firebaseUid);
        Task<AppUser> GetByEmailAsync(string email);
        Task AddAsync(AppUser user);
        Task UpdateAsync(AppUser user);
    }
}