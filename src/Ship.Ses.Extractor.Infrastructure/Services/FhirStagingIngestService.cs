using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Services
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MongoDB.Bson;
    using Ship.Ses.Extractor.Application.Contracts;
    using Ship.Ses.Extractor.Domain.Entities.Extractor;
    using Ship.Ses.Extractor.Domain.Entities.Patients;
    using Ship.Ses.Extractor.Domain.Repositories.Transformer;
    using Ship.Ses.Extractor.Domain.Shared;
    using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;

    public static class StagingStatus
    {
        public const string Pending = "PENDING";
        public const string InProgress = "IN_PROGRESS";
        public const string Exported = "EXPORTED";
        public const string Failed = "FAILED";
    }

    public sealed class FhirStagingIngestService : IFhirStagingIngestService
    {
        private readonly ExtractorDbContext _db;
        private readonly IFhirSyncRepository<PatientSyncRecord> _mongo;
        private readonly ILogger<FhirStagingIngestService> _logger;
        private readonly int _batchSize;
        private readonly string _facilityId;
        const string prefix = "Organization/";

        public FhirStagingIngestService(
            ExtractorDbContext db,
            IFhirSyncRepository<PatientSyncRecord> mongo,
            ILogger<FhirStagingIngestService> logger,
            IOptions<FhirStagingOptions> options,
            IConfiguration configuration)
        {
            _db = db;
            _mongo = mongo;
            _logger = logger;
            _batchSize = Math.Max(1, options.Value.BatchSize);
            var envDefaults = configuration.GetSection("EnvironmentDefaults").Get<EnvironmentDefaults>();

            const string prefix = "Organization/";
            string? rawReference = envDefaults?.ManagingOrganization?.Reference;

            // Check if the reference exists and starts with the expected prefix
            if (string.IsNullOrWhiteSpace(rawReference) || !rawReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = $"Extraction startup failed: 'EnvironmentDefaults:ManagingOrganization.Reference' is missing, empty, or does not start with '{prefix}'. Cannot determine Facility ID.";
                logger.LogError(errorMessage);
                throw new ApplicationException(errorMessage);
            }
            string potentialFacilityId = rawReference.Substring(prefix.Length);
            if (!Guid.TryParse(potentialFacilityId, out _))
            {
                var errorMessage = $"Extraction startup failed: Extracted Facility ID '{potentialFacilityId}' from '{rawReference}' is not a valid GUID format. If a GUID is strictly required, please correct the configuration.";
                logger.LogError(errorMessage);
            }

            _facilityId = potentialFacilityId;
        }

        public async Task<int> IngestPatientsAsync(CancellationToken ct)
        {
            // Fetch a small batch of unprocessed Patient rows
            var rows = await _db.FhirStaging
                .AsTracking()
                .Where(r => r.ResourceType == "Patient"
                            && r.Status == StagingStatus.Pending
                            && r.ShipProcessedAt == null)
                .OrderBy(r => r.CreatedAt)
                .Take(_batchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return 0;

            var inserted = 0;
            foreach (var row in rows)
            {
                try
                {
                    var doc = BsonDocument.Parse(row.FhirBundle); // throws if invalid JSON
                    var resourceId = doc.TryGetValue("id", out var idVal) && idVal.IsString
                                     ? idVal.AsString
                                     : row.ResourceId;

                    var record = new PatientSyncRecord
                    {
                        ResourceId = resourceId,
                        FhirJson = doc,
                        Status = "Pending",     
                        CreatedDate = DateTime.UtcNow,
                        RetryCount = 0,
                        ExtractSource = "extractor",
                        TransactionId = null,
                        ApiResponsePayload = null, 
                        SyncedResourceId = null, 
                        FacilityId = _facilityId,
                    };

                    await _mongo.InsertAsync(record, ct);

                    // mark as exported in staging
                    row.Status = StagingStatus.Exported;
                    row.ShipProcessedAt = DateTime.UtcNow;
                    row.UpdatedAt = DateTime.UtcNow;

                    inserted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert Patient row {RowId} into Landing zone Mongo.", row.Id);
                    row.Status = StagingStatus.Failed;
                    row.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(ct);
            return inserted;
        }
    }
    public sealed class FhirStagingOptions
    {
        public int BatchSize { get; set; } = 200;
    }

}
