using Moq;
using Xunit;
using Launchify.Application.Interfaces;
using System.Threading.Tasks;

namespace Launchify.Tests
{
    public class AnalyticsTests
    {
        [Fact]
        public async Task IncrementVisitCount_Should_Call_Repository_Once()
        {
            var mockRepository = new Mock<ILandingPageRepository>();

            string testPageId = "test-project-123";
            int newVisits = 5;

            await mockRepository.Object.IncrementVisitCountAsync(testPageId, newVisits);

            mockRepository.Verify(repo => repo.IncrementVisitCountAsync(testPageId, newVisits), Times.Once);
        }
    }
}