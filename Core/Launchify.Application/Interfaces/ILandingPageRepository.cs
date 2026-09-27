using Launchify.Domain.Entities;
using System.Threading.Tasks;

namespace Launchify.Application.Interfaces
{
    public interface ILandingPageRepository
    {
        Task AddAsync(LandingPage page);
        Task<LandingPage> GetByIdAsync(string id);
    }
}