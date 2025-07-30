using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Application.Services.Extractors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Worker
{

    public class ConditionExtractorWorker : BackgroundService
    {
        private readonly ILogger<ConditionExtractorWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        public ConditionExtractorWorker(
            ILogger<ConditionExtractorWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Starting Condition Extractor Worker...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    //var extractor = scope.ServiceProvider.GetRequiredService<ConditionResourceExtractor>();
                    //await extractor.ExtractAndPersistAsync(stoppingToken);
                    _logger.LogInformation("✅ Condition extraction completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Unhandled exception in ConditionExtractorWorker");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Condition Extractor Service is stopping.");

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
