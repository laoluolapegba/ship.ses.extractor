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
using System.Diagnostics;

namespace Ship.Ses.Extractor.Infrastructure.Services
{


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

        private const string OrgPrefix = "Organization/";

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
            string? rawReference = envDefaults?.ManagingOrganization?.Reference;

            if (string.IsNullOrWhiteSpace(rawReference) || !rawReference.StartsWith(OrgPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage =
                    $"Extraction startup failed: 'EnvironmentDefaults:ManagingOrganization.Reference' is missing/empty or does not start with '{OrgPrefix}'. Cannot determine Facility ID.";
                logger.LogError("{ErrorMessage} RawReference={RawReference}", errorMessage, rawReference);
                throw new ApplicationException(errorMessage);
            }

            var potentialFacilityId = rawReference.Substring(OrgPrefix.Length);
            if (!Guid.TryParse(potentialFacilityId, out _))
            {
                // Not fatal per your current logic—just log clearly.
                logger.LogWarning(
                    "ManagingOrganization.Reference parsed to non-GUID FacilityId. Parsed='{FacilityId}', Raw='{RawReference}'. Proceeding anyway.",
                    potentialFacilityId, rawReference);
            }

            _facilityId = potentialFacilityId;
            _logger.LogInformation("FhirStagingIngestService initialized. FacilityId={FacilityId} BatchSize={BatchSize}",
                _facilityId, _batchSize);
        }

        public async Task<int> IngestPatientsAsync(CancellationToken ct)
        {
            _logger.LogDebug("IngestPatientsAsync started. FacilityId={FacilityId}, BatchSize={BatchSize}",
                _facilityId, _batchSize);

            var sw = Stopwatch.StartNew();
            List<FhirStagingRecord> rows;

            try
            {
                rows = await _db.FhirStaging
                    .AsTracking()
                    .Where(r => r.ResourceType == "Patient"
                                && r.Status == StagingStatus.Pending
                                && r.ShipProcessedAt == null)
                    .OrderBy(r => r.CreatedAt)
                    .Take(_batchSize)
                    .ToListAsync(ct);

                _logger.LogDebug("Fetched {Count} pending Patient rows in {ElapsedMs} ms.",
                    rows.Count, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed fetching pending Patient rows from MySQL.");
                throw;
            }

            if (rows.Count == 0)
            {
                _logger.LogDebug("No pending Patient rows found. Exiting cycle.");
                return 0;
            }

            var inserted = 0;
            var processed = 0;
            var perRowSw = new Stopwatch();

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                processed++;
                perRowSw.Restart();

                _logger.LogDebug("Processing RowId={RowId}, ResourceId={ResourceId}, CreatedAt={CreatedAt}",
                    row.Id, row.ResourceId, row.CreatedAt);

                try
                {
                    BsonDocument doc;
                    try
                    {
                        doc = BsonDocument.Parse(row.FhirBundle);
                    }
                    catch (FormatException fex)
                    {
                        _logger.LogError(fex,
                            "Invalid FHIR JSON in fhir_bundle for RowId={RowId}. Preview='{Preview}'",
                            row.Id, SafePreview(row.FhirBundle, 200));
                        row.Status = StagingStatus.Failed;
                        row.UpdatedAt = DateTime.UtcNow;
                        continue;
                    }

                    var resourceId = doc.TryGetValue("id", out var idVal) && idVal.IsString
                                     ? idVal.AsString
                                     : row.ResourceId;

                    var record = new PatientSyncRecord
                    {
                        ResourceId = resourceId,
                        FhirJson = doc,
                        Status = "Pending",   // transmitter updates later
                        CreatedDate = DateTime.UtcNow,
                        RetryCount = 0,
                        ExtractSource = "extractor",
                        TransactionId = null,
                        ApiResponsePayload = null,
                        SyncedResourceId = null,
                        FacilityId = _facilityId,
                        StagingId = row.Id,
                    };

                    await _mongo.InsertAsync(record, ct);

                    row.Status = StagingStatus.Exported;
                    row.ShipProcessedAt = DateTime.UtcNow;
                    row.UpdatedAt = DateTime.UtcNow;

                    inserted++;

                    _logger.LogDebug("Inserted RowId={RowId} into Mongo and marked EXPORTED. Took {ElapsedMs} ms.",
                        row.Id, perRowSw.ElapsedMilliseconds);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation requested during row processing. RowId={RowId}", row.Id);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to insert Patient row RowId={RowId} into Mongo. Marking FAILED.",
                        row.Id);
                    row.Status = StagingStatus.Failed;
                    row.UpdatedAt = DateTime.UtcNow;
                }
            }

            try
            {
                await _db.SaveChangesAsync(ct);
                sw.Stop();
                _logger.LogInformation(
                    "Ingest cycle complete. Processed={Processed}, Inserted={Inserted}, Failed={Failed}, Took={ElapsedMs} ms.",
                    processed, inserted, processed - inserted, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "MySQL SaveChanges failed after processing batch. Processed={Processed}, Inserted={Inserted}.",
                    processed, inserted);
                throw;
            }

            return inserted;
        }

        private static string SafePreview(string input, int max)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var trimmed = input.Replace("\r", " ").Replace("\n", " ");
            return trimmed.Length <= max ? trimmed : trimmed[..max] + "…";
        }
    }

    public sealed class FhirStagingOptions
    {
        public int BatchSize { get; set; } = 200;
    }

}
