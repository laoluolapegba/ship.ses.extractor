using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Application.Services.Extractors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Worker
{


    public class ObservationExtractorWorker : BackgroundService
    {
        private readonly ILogger<ObservationExtractorWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        public ObservationExtractorWorker(
            ILogger<ObservationExtractorWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Starting Observation Extractor Worker...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    //var extractor = scope.ServiceProvider.GetRequiredService<ObservationResourceExtractor>();
                    //await extractor.ExtractAndPersistAsync(stoppingToken);
                    _logger.LogInformation("✅ Observation extraction completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unhandled exception in ObservationExtractorWorker");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Observation Extractor Service is stopping.");

            if (_executingTask == null)
                return;

            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }
    }

}
