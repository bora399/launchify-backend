using Launchify.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Launchify.Infrastructure.Services
{
    public class AnalyticsBackgroundWorker : BackgroundService
    {
        private readonly AnalyticsQueueService _queueService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AnalyticsBackgroundWorker> _logger;

        private readonly ConcurrentDictionary<string, int> _visitCounts = new();

        public AnalyticsBackgroundWorker(
            AnalyticsQueueService queueService,
            IServiceProvider serviceProvider,
            ILogger<AnalyticsBackgroundWorker> logger)
        {
            _queueService = queueService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Analitik Arka Plan İşçisi (Worker) ayağa kalktı.");

            _ = Task.Run(async () =>
            {
                await foreach (var pageId in _queueService.ReadAllAsync(stoppingToken))
                {
                    _visitCounts.AddOrUpdate(pageId, 1, (_, count) => count + 1);
                }
            }, stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushToDatabaseAsync();
            }
        }

        private async Task FlushToDatabaseAsync()
        {
            if (_visitCounts.IsEmpty) return;

            var snapshot = _visitCounts.ToArray();
            _visitCounts.Clear();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ILandingPageRepository>();

                foreach (var item in snapshot)
                {
                    string pageId = item.Key;
                    int newVisits = item.Value;

                    await repository.IncrementVisitCountAsync(pageId, newVisits);

                    _logger.LogInformation("{Count} yeni ziyaret {PageId} ID'li platform için Firestore'a toplu yazıldı.", newVisits, pageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu ziyaret verileri veritabanına yazılırken hata oluştu!");
                foreach (var item in snapshot)
                {
                    _visitCounts.AddOrUpdate(item.Key, item.Value, (_, count) => count + item.Value);
                }
            }
        }
    }
}