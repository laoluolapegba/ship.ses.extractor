using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Application.Services.Extractors;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Ship.Ses.Extractor.Worker
{


    public class EncounterExtractorWorker : BackgroundService
    {
        private readonly ILogger<EncounterExtractorWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        public EncounterExtractorWorker(
            ILogger<EncounterExtractorWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Encounter Extractor Worker...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var extractor = scope.ServiceProvider.GetRequiredService<EncounterResourceExtractor>();
                    await extractor.ExtractAndPersistAsync(stoppingToken);
                    _logger.LogInformation("✅ Encounter extraction completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unhandled exception in EncounterExtractorWorker");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Encounter Extractor Service is stopping.");

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
