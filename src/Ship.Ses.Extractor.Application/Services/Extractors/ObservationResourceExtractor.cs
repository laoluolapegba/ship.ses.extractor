using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Ship.Ses.Extractor.Application.Services.Transformers;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Repositories.Validator;
using Ship.Ses.Extractor.Domain.Shared;
using System.Text.Json.Nodes;
using Ship.Ses.Extractor.Domain.Entities.Observation;
using Serilog.Context;
using System.Security.Cryptography;
namespace Ship.Ses.Extractor.Application.Services.Extractors
{


    public class ObservationResourceExtractor
    {
        private readonly ITableMappingService _mappingService;
        private readonly IDataExtractorService _dataExtractor;
        private readonly ObservationTransformer _transformer;
        private readonly IFhirResourceValidator _validator;
        private readonly IFhirSyncRepository<ObservationSyncRecord> _repository;
        private readonly ISyncTrackingRepository _syncTrackingRepository;
        private readonly ILogger<ObservationResourceExtractor> _logger;
        private readonly string _facilityId;
        const string prefix = "Organization/";

        public ObservationResourceExtractor(
            ITableMappingService mappingService,
            IDataExtractorService dataExtractor,
            ObservationTransformer transformer,
            IFhirResourceValidator validator,
            IFhirSyncRepository<ObservationSyncRecord> repository,
            ISyncTrackingRepository syncTrackingRepository,
            ILogger<ObservationResourceExtractor> logger,
            IConfiguration configuration)
        {
            _mappingService = mappingService;
            _dataExtractor = dataExtractor;
            _transformer = transformer;
            _validator = validator;
            _repository = repository;
            _syncTrackingRepository = syncTrackingRepository;
            _logger = logger;

            var envDefaults = configuration.GetSection("EnvironmentDefaults").Get<EnvironmentDefaults>();
            string? rawReference = envDefaults?.ManagingOrganization?.Reference;

            if (string.IsNullOrWhiteSpace(rawReference) || !rawReference.StartsWith(prefix))
            {
                var msg = $"Extraction startup failed: Invalid ManagingOrganization.Reference - must start with '{prefix}'.";
                logger.LogError(msg);
                throw new ApplicationException(msg);
            }

            string potentialFacilityId = rawReference.Substring(prefix.Length);
            if (!Guid.TryParse(potentialFacilityId, out _))
            {
                logger.LogWarning($"Extracted Facility ID '{potentialFacilityId}' is not a valid GUID. Proceeding anyway.");
            }

            _facilityId = potentialFacilityId;
        }

        public async Task ExtractAndPersistAsync(CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation("📊 Starting Observation extraction... [{CorrelationId}]", correlationId);

                var mapping = await _mappingService.GetMappingForResourceAsync("Observation", cancellationToken);
                var rawRows = await _dataExtractor.ExtractAsync(mapping, cancellationToken);

                _logger.LogInformation("Extracted {Count} observation rows from {Table}", rawRows.Count(), mapping.TableName);

                foreach (var row in rawRows)
                {
                    var sourceId = row["observation_id"]?.ToString();
                    var lastUpdated = row.TryGetValue("created_at", out var created) ? DateTime.Parse(created.ToString()!) : (DateTime?)null;
                    var rowHash = ComputeRowHash(row);

                    if (string.IsNullOrWhiteSpace(sourceId))
                    {
                        _logger.LogWarning("⚠️ Skipping observation with null ID");
                        continue;
                    }

                    if (await _syncTrackingRepository.ExistsAsync("Observation", sourceId, cancellationToken))
                    {
                        _logger.LogInformation("🔁 Skipping already tracked Observation {SourceId}", sourceId);
                        continue;
                    }

                    var tracking = new SyncTracking
                    {
                        ResourceType = "Observation",
                        SourceId = sourceId,
                        SourceHash = rowHash,
                        LastUpdated = lastUpdated,
                        CreatedAt = DateTime.UtcNow,
                        RetryCount = 0
                    };

                    try
                    {
                        var errors = new List<string>();
                        var json = _transformer.Transform(row, mapping, errors);
                        var normalized = _transformer.NormalizeEnumFields(json);

                        if (errors.Any())
                        {
                            tracking.ExtractStatus = "Failed";
                            tracking.ErrorMessage = string.Join("; ", errors);
                            await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                            continue;
                        }

                        var record = new ObservationSyncRecord
                        {
                            ResourceId = sourceId,
                            FhirJson = BsonDocument.Parse(normalized.ToJsonString()),
                            CreatedDate = DateTime.UtcNow,
                            Status = "Pending",
                            LastAttemptAt = DateTime.UtcNow,
                            ExtractSource = "extractor",
                            RetryCount = 0,
                            FacilityId = _facilityId
                        };

                        var validation = await _validator.ValidateAsync(normalized);
                        if (!validation.IsValid)
                        {
                            tracking.ExtractStatus = "Failed";
                            tracking.ErrorMessage = string.Join("; ", validation.Errors);
                            await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                            continue;
                        }

                        await _repository.InsertAsync(record, cancellationToken);
                        tracking.ExtractStatus = "Success";
                        await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled exception while processing observation {SourceId}", sourceId);
                        tracking.ExtractStatus = "Failed";
                        tracking.ErrorMessage = ex.Message;
                        await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                    }
                }
            }
        }

        private string ComputeRowHash(IDictionary<string, object> row)
        {
            using var sha256 = SHA256.Create();
            var raw = string.Join("|", row.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }

}
