using Launchify.Domain.Entities;

namespace Launchify.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(string id);
        Task<User> SyncUserAsync(string id, string email);
        Task<bool> DeductCreditAsync(string id);
        Task<bool> RefundCreditAsync(string id);
    }
}