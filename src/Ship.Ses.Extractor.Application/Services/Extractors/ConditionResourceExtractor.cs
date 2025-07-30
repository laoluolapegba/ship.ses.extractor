using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Serilog.Context;
using Ship.Ses.Extractor.Application.Services.Transformers;
using Ship.Ses.Extractor.Domain.Entities.Condition;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Domain.Entities.Patients;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Repositories.Validator;
using Ship.Ses.Extractor.Domain.Shared;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Ship.Ses.Extractor.Application.Services.Extractors
{


    public class ConditionResourceExtractor
    {
        private readonly ITableMappingService _mappingService;
        private readonly IDataExtractorService _dataExtractor;
        private readonly ConditionTransformer _transformer;
        private readonly IFhirResourceValidator _validator;
        private readonly IFhirSyncRepository<ConditionSyncRecord> _repository;
        private readonly ISyncTrackingRepository _syncTrackingRepository;
        private readonly ILogger<ConditionResourceExtractor> _logger;
        private readonly string _facilityId;
        const string prefix = "Organization/";

        public ConditionResourceExtractor(
            ITableMappingService mappingService,
            IDataExtractorService dataExtractor,
            ConditionTransformer transformer,
            IFhirResourceValidator validator,
            IFhirSyncRepository<ConditionSyncRecord> repository,
            ISyncTrackingRepository syncTrackingRepository,
            ILogger<ConditionResourceExtractor> logger,
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
                throw new ApplicationException($"Extraction startup failed: ManagingOrganization reference must begin with '{prefix}'");
            }

            string potentialFacilityId = rawReference.Substring(prefix.Length);
            if (!Guid.TryParse(potentialFacilityId, out _))
            {
                logger.LogWarning("Facility ID '{potentialFacilityId}' is not a valid GUID.");
            }

            _facilityId = potentialFacilityId;
        }

        public async Task ExtractAndPersistAsync(CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogInformation("🩺 Starting Condition extraction... [{CorrelationId}]", correlationId);

                //var mapping = await _mappingService.GetMappingForResourceAsync("Condition", cancellationToken);
                var typedMapping = await _mappingService.GetTypedMappingForResourceAsync<ConditionFieldMapping>("Condition", cancellationToken);
                if (typedMapping == null)
                {
                    _logger.LogError("❌ No mapping found for resource type 'Condition'");
                    return;
                }
                TableMapping mapping = typedMapping;
                var rawRows = await _dataExtractor.ExtractAsync(mapping, cancellationToken);

                _logger.LogInformation("Extracted {Count} condition rows from {Table}", rawRows.Count(), mapping.TableName);

                foreach (var row in rawRows)
                {
                    //var sourceId = row[mapping.IdColumn]?.ToString();
                    var sourceId = row["condition_id"]?.ToString();
                    var lastUpdated = row.TryGetValue("created_at", out var created) ? DateTime.Parse(created.ToString()!) : (DateTime?)null;
                    var rowHash = ComputeRowHash(row);

                    if (string.IsNullOrWhiteSpace(sourceId))
                    {
                        _logger.LogWarning("⚠️ Skipping row with missing condition ID");
                        continue;
                    }

                    if (await _syncTrackingRepository.ExistsAsync("Condition", sourceId, cancellationToken))
                    {
                        _logger.LogInformation("⏭️ Already tracked condition: {SourceId}", sourceId);
                        continue;
                    }

                    var tracking = new SyncTracking
                    {
                        ResourceType = "Condition",
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

                        var record = new ConditionSyncRecord
                        {
                            ResourceId = sourceId,
                            FhirJson = BsonDocument.Parse(normalized.ToJsonString()),
                            CreatedDate = DateTime.UtcNow,
                            Status = "Pending",
                            LastAttemptAt = DateTime.UtcNow,
                            RetryCount = 0,
                            ExtractSource = "extractor",
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
                        _logger.LogError(ex, "❌ Error processing condition record {SourceId}", sourceId);
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
