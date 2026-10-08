using Launchify.Domain.Entities;
using System.Threading.Tasks;

namespace Launchify.Application.Interfaces
{
    public interface ILandingPageRepository
    {
        Task AddAsync(LandingPage page);
        Task<LandingPage> GetByIdAsync(string id);
        Task<LandingPage> GetBySlugAsync(string slug);
        Task<IEnumerable<LandingPage>> GetByUserIdAsync(string userId);
        Task IncrementVisitCountAsync(string pageId, int visitCount);
        Task UpdateAsync(LandingPage landingPage);
        Task DeleteAsync(string id);
    }
}