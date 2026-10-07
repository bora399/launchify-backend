using FluentAssertions;
using Xunit;
using Launchify.Infrastructure.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Launchify.Tests.Infrastructure
{
    public class AnalyticsQueueTests
    {
        [Fact]
        public async Task Queue_Ayni_Anda_Gelen_Yuzlerce_Ziyareti_Kayıpsız_Sıraya_Almali()
        {
            var queueService = new AnalyticsQueueService();
            string testPageId = "vios-coffee-123";
            int totalRequests = 100;

            var tasks = new List<Task>();
            for (int i = 0; i < totalRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await queueService.EnqueueVisitAsync(testPageId);
                }));
            }

            await Task.WhenAll(tasks); 

            int count = 0;
            var cts = new CancellationTokenSource(System.TimeSpan.FromSeconds(2)); 

            await foreach (var item in queueService.ReadAllAsync(cts.Token))
            {
                count++;
                if (count == totalRequests) break;
            }

            count.Should().Be(totalRequests, "çünkü Thread-Safe kullandık ve hiçbir ziyaret kaybolmamalı");
        }
    }
}