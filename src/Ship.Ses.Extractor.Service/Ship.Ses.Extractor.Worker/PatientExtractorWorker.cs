using Ship.Ses.Extractor.Application.Services.Extractors;
using Ship.Ses.Extractor.Infrastructure.Persistance.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Worker
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using System.Threading.Tasks;

    public class PatientExtractorWorker : BackgroundService
    {
        private readonly ILogger<PatientExtractorWorker> _logger;
        private readonly PatientResourceExtractor _extractor;

        public PatientExtractorWorker(ILogger<PatientExtractorWorker> logger, PatientResourceExtractor extractor)
        {
            _logger = logger;
            _extractor = extractor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Starting Patient Extractor Worker...");
            try
            {
                await _extractor.ExtractAndPersistAsync(stoppingToken);
                _logger.LogInformation("✅ Patient extraction completed:");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unhandled exception in PatientExtractorWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            
        }
    }


}
