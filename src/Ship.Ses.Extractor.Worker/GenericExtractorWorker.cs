using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Worker
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Ship.Ses.Extractor.Application.Contracts;

    public sealed class GenericExtractorWorker : BackgroundService
    {
        private readonly ILogger<GenericExtractorWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Tunables
        private readonly TimeSpan _idleDelay = TimeSpan.FromSeconds(10);  // when nothing to ingest
        private readonly TimeSpan _loopDelay = TimeSpan.FromSeconds(2);   // small breather after work
        private readonly TimeSpan _errorBackoff = TimeSpan.FromSeconds(60);  // on unhandled errors

        public GenericExtractorWorker(
            ILogger<GenericExtractorWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("▶️ Starting Generic Extractor Worker…");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("🔁 Extractor loop begin");

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IGenericFhirStagingIngestService>();

                        // Patients → transformed_pool_patients
                        var patients = await svc.IngestPatientsAsync(stoppingToken);
                        if (patients > 0)
                            _logger.LogInformation("⬆️ Inserted {Count} Patient records to Mongo (patients pool).", patients);
                        else
                            _logger.LogDebug("No Patient rows to ingest this cycle.");

                        // Everything else → transformed_pool_resources
                        var nonPatients = await svc.IngestNonPatientsAsync(stoppingToken);
                        if (nonPatients > 0)
                            _logger.LogInformation("⬆️ Inserted {Count} non-Patient records to Mongo (generic pool).", nonPatients);
                        else
                            _logger.LogDebug("No non-Patient rows to ingest this cycle.");

                        // Idle/small delay
                        if (patients == 0 && nonPatients == 0)
                            await Task.Delay(_idleDelay, stoppingToken);
                        else
                            await Task.Delay(_loopDelay, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("🛑 Host stop signal received. Stopping Generic Extractor Worker.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Unhandled exception in GenericExtractorWorker. Backing off…");
                        await Task.Delay(_errorBackoff, stoppingToken);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("⏹️ Generic Extractor Worker stopped.");
            }
        }
    }


}
