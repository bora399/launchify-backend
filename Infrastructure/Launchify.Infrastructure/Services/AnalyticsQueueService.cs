using System.Threading.Channels;

namespace Launchify.Infrastructure.Services
{
    public class AnalyticsQueueService
    {
        private readonly Channel<string> _pageVisitQueue;

        public AnalyticsQueueService()
        {
            _pageVisitQueue = Channel.CreateUnbounded<string>();
        }

        public async ValueTask EnqueueVisitAsync(string pageId)
        {
            await _pageVisitQueue.Writer.WriteAsync(pageId);
        }

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _pageVisitQueue.Reader.ReadAllAsync(cancellationToken);
        }
    }
}