
using MongoDB.Bson;
using Ship.Ses.Extractor.Application.Services;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Domain.Entities.Patients;
using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Repositories.Validator;
using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Serilog.Context;
using Microsoft.Extensions.Logging;

namespace Ship.Ses.Extractor.Application.Services.Extractors
{
    public class PatientResourceExtractor
    {
        private readonly ITableMappingService _mappingService;
        private readonly IDataExtractorService _dataExtractor;
        private readonly IResourceTransformer<JsonObject> _transformer;
        private readonly IFhirValidator _validator;
        private readonly IFhirSyncRepository<PatientSyncRecord> _repository;
        private readonly ISyncTrackingRepository _syncTrackingRepository;
        private readonly ILogger<PatientResourceExtractor> _logger;

        public PatientResourceExtractor(
            ITableMappingService mappingService,
            IDataExtractorService dataExtractor,
            IResourceTransformer<JsonObject> transformer,
            IFhirValidator validator,
            IFhirSyncRepository<PatientSyncRecord> repository,
            ISyncTrackingRepository syncTrackingRepository,
            ILogger<PatientResourceExtractor> logger)
        {
            _mappingService = mappingService;
            _dataExtractor = dataExtractor;
            _transformer = transformer;
            _validator = validator;
            _repository = repository;
            _syncTrackingRepository = syncTrackingRepository;
            _logger = logger;
        }

        public async Task ExtractAndPersistAsync(CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation("Started extraction with CorrelationId {CorrelationId}", correlationId);

                var mapping = await _mappingService.GetMappingForResourceAsync("Patient", cancellationToken);
                _logger.LogInformation("Starting extraction for resource {Resource}", mapping.ResourceType);

                var rawRows = await _dataExtractor.ExtractAsync(mapping, cancellationToken);
                _logger.LogInformation("Extracted {Count} rows from source table {Table}", rawRows.Count(), mapping.TableName);

                foreach (var row in rawRows)
                {
                    try
                    {
                        // Extract source keys
                        var sourceId = row["id"]?.ToString();
                        var lastUpdated = row.TryGetValue("updated_at", out var u) ? DateTime.Parse(u?.ToString()!) : (DateTime?)null;
                        var rowHash = ComputeRowHash(row);

                        if (string.IsNullOrWhiteSpace(sourceId))
                        {
                            _logger.LogWarning("Skipping row with null or empty ID");
                            continue;
                        }

                        // Check if already synced
                        if (await _syncTrackingRepository.ExistsAsync("Patient", sourceId, cancellationToken))
                        {
                            _logger.LogInformation("Skipping already tracked record {SourceId}", sourceId);
                            continue;
                        }

                        var json = _transformer.Transform(row, mapping);
                        var record = new PatientSyncRecord
                        {
                            ResourceId = sourceId,
                            FhirJson = BsonDocument.Parse(json.ToJsonString()),
                            CreatedDate = DateTime.UtcNow,
                            Status = "Pending",
                            RetryCount = 0
                        };

                        var tracking = new SyncTracking
                        {
                            ResourceType = "Patient",
                            SourceId = sourceId,
                            SourceHash = rowHash,
                            LastUpdated = lastUpdated,
                            ExtractStatus = "Pending",
                            RetryCount = 0,
                            CreatedAt = DateTime.UtcNow
                        };

                        if (await _validator.IsValidAsync(json, cancellationToken))
                        {
                            await _repository.InsertAsync(record, cancellationToken);
                            tracking.ExtractStatus = "Success";

                            _logger.LogInformation("Successfully persisted record {SourceId}", sourceId);
                        }
                        else
                        {
                            record.Status = "Failed";
                            record.ErrorMessage = "Validation failed";
                            tracking.ExtractStatus = "Failed";
                            tracking.ErrorMessage = record.ErrorMessage;

                            await _repository.InsertAsync(record, cancellationToken);
                            _logger.LogWarning("Validation failed for record {SourceId}", sourceId);
                        }

                        await _syncTrackingRepository.AddOrUpdateAsync(tracking, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        var sourceId = row["id"]?.ToString() ?? "<unknown>";
                        _logger.LogError(ex, "Unhandled error processing record {SourceId}", sourceId);

                        await _syncTrackingRepository.AddOrUpdateAsync(new SyncTracking
                        {
                            ResourceType = "Patient",
                            SourceId = sourceId,
                            SourceHash = null,
                            LastUpdated = null,
                            ExtractStatus = "Failed",
                            RetryCount = 0,
                            ErrorMessage = ex.Message,
                            CreatedAt = DateTime.UtcNow,
                            LastAttemptAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                }

                _logger.LogInformation("Extraction and persistence completed for resource {Resource}", mapping.ResourceType);
            }
        }

        private string ComputeRowHash(IDictionary<string, object> row)
        {
            using var sha256 = SHA256.Create();
            var raw = string.Join("|", row.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash); // .NET 5+
        }
    }



}
