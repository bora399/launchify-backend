using System.Threading.Tasks;
using Launchify.Domain.Entities;
using System.Collections.Generic;

namespace Launchify.Application.Interfaces
{
    public interface IWaitlistRepository
    {
        Task AddAsync(WaitlistEntry entry);
        Task<IEnumerable<WaitlistEntry>> GetByPageIdAsync(string pageId); 
    }
}