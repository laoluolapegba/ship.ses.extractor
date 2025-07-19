using MongoDB.Bson;
using Ship.Ses.Extractor.Application.Services;
using Ship.Ses.Extractor.Domain.Entities.Encounter;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Repositories.Validator;
using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Serilog.Context;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Ship.Ses.Extractor.Application.Services.Transformers;

namespace Ship.Ses.Extractor.Application.Services.Extractors
{
    public class EncounterResourceExtractor
    {
        private readonly ITableMappingService _mappingService;
        private readonly IDataExtractorService _dataExtractor;
        private readonly EncounterTransformer _transformer;

        //private readonly IResourceTransformer<JsonObject> _transformer;
        private readonly IFhirResourceValidator _validator;
        private readonly IFhirSyncRepository<EncounterSyncRecord> _repository;
        private readonly ISyncTrackingRepository _syncTrackingRepository;
        private readonly ILogger<EncounterResourceExtractor> _logger;
        private readonly string _facilityId;
        const string prefix = "Organization/";

        public EncounterResourceExtractor(
            ITableMappingService mappingService,
            IDataExtractorService dataExtractor,
            //IResourceTransformer<JsonObject> transformer,
            EncounterTransformer transformer,
            IFhirResourceValidator validator,
            IFhirSyncRepository<EncounterSyncRecord> repository,
            ISyncTrackingRepository syncTrackingRepository,
            ILogger<EncounterResourceExtractor> logger,
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

            if (string.IsNullOrWhiteSpace(rawReference) || !rawReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
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
                _logger.LogInformation("Started encounter extraction with CorrelationId {CorrelationId}", correlationId);

                var mapping = await _mappingService.GetMappingForResourceAsync("Encounter", cancellationToken);
                var rawRows = await _dataExtractor.ExtractAsync(mapping, cancellationToken);
                _logger.LogInformation("Extracted {Count} encounter rows from {Table}", rawRows.Count(), mapping.TableName);

                foreach (var row in rawRows)
                {
                    var sourceId = row["encounter_id"]?.ToString();
                    var patientId = row["patient_id"]?.ToString();
                    var lastUpdated = row.TryGetValue("created_at", out var u) ? DateTime.Parse(u?.ToString()!) : (DateTime?)null;
                    var rowHash = ComputeRowHash(row);

                    if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(patientId))
                    {
                        _logger.LogWarning("Skipping encounter row with null encounter_id or patient_id");
                        continue;
                    }

                    if (await _syncTrackingRepository.ExistsAsync("Encounter", sourceId, cancellationToken))
                    {
                        _logger.LogInformation("Skipping already tracked encounter {SourceId}", sourceId);
                        continue;
                    }

                    var tracking = new SyncTracking
                    {
                        ResourceType = "Encounter",
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
                            var errMsg = string.Join("; ", errors);
                            _logger.LogWarning("Skipping encounter {SourceId} due to errors: {Errors}", sourceId, errMsg);

                            tracking.ExtractStatus = "Failed";
                            tracking.ErrorMessage = errMsg;
                            tracking.LastAttemptAt = DateTime.UtcNow;
                            await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                            continue;
                        }

                        var record = new EncounterSyncRecord
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
                            var errMsg = string.Join("; ", validation.Errors);
                            _logger.LogWarning("❌ Encounter validation failed {SourceId}: {Errors}", sourceId, errMsg);
                            tracking.ExtractStatus = "Failed";
                            tracking.ErrorMessage = errMsg;
                            await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                            continue;
                        }

                        await _repository.InsertAsync(record, cancellationToken);
                        tracking.ExtractStatus = "Success";
                        await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                        _logger.LogInformation("✅ Persisted encounter {SourceId}", sourceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled error processing encounter {SourceId}", sourceId);
                        tracking.ExtractStatus = "Failed";
                        tracking.ErrorMessage = ex.Message;
                        tracking.LastAttemptAt = DateTime.UtcNow;
                        await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                    }
                }
            }
        }

        private string ComputeRowHash(IDictionary<string, object> row)
        {
            using var sha256 = SHA256.Create();
            var raw = string.Join("|", row.OrderBy(k => k.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
            return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}
