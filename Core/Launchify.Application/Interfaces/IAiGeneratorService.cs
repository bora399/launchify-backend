using Launchify.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Launchify.Application.Interfaces
{
    public interface IAiGeneratorService
    {
        Task<AiPageConfig> GenerateContentAsync(
            string productName,
            string themeType,
            string productDescription,
            Func<string, Task> logCallback
        );
    }
}