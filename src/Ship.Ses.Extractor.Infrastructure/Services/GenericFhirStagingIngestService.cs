using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Ship.Ses.Extractor.Application.Contracts;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Domain.Entities.Generic;
using Ship.Ses.Extractor.Domain.Entities.Patients;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Shared;
using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Services
{
    public sealed class GenericFhirStagingIngestService : IGenericFhirStagingIngestService
    {
        private readonly ExtractorDbContext _db;
        private readonly IFhirSyncRepository<PatientSyncRecord> _patientsRepo;
        private readonly IFhirSyncRepository<GenericResourceSyncRecord> _genericRepo;
        private readonly ILogger<GenericFhirStagingIngestService> _logger;
        private readonly int _batchSize;
        private readonly string _facilityId;

        private const string OrgPrefix = "Organization/";

        public GenericFhirStagingIngestService(
            ExtractorDbContext db,
            IFhirSyncRepository<PatientSyncRecord> patientsRepo,
            IFhirSyncRepository<GenericResourceSyncRecord> genericRepo,
            ILogger<GenericFhirStagingIngestService> logger,
            IOptions<FhirStagingOptions> options,
            IConfiguration configuration)
        {
            _db = db;
            _patientsRepo = patientsRepo;
            _genericRepo = genericRepo;
            _logger = logger;

            _batchSize = Math.Max(1, options.Value.BatchSize);

            var envDefaults = configuration.GetSection("EnvironmentDefaults").Get<EnvironmentDefaults>();
            var rawReference = envDefaults?.ManagingOrganization?.Reference;

            if (string.IsNullOrWhiteSpace(rawReference) || !rawReference.StartsWith(OrgPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var msg = $"Extraction startup failed: 'EnvironmentDefaults:ManagingOrganization.Reference' is missing/empty or does not start with '{OrgPrefix}'.";
                _logger.LogError("{ErrorMessage} RawReference={RawReference}", msg, rawReference);
                throw new ApplicationException(msg);
            }

            var potentialFacilityId = rawReference.Substring(OrgPrefix.Length);
            if (!Guid.TryParse(potentialFacilityId, out _))
            {
                _logger.LogWarning(
                    "ManagingOrganization.Reference parsed to non-GUID FacilityId. Parsed='{FacilityId}', Raw='{RawReference}'. Proceeding anyway.",
                    potentialFacilityId, rawReference);
            }

            _facilityId = potentialFacilityId;
            _logger.LogInformation("GenericFhirStagingIngestService initialized. FacilityId={FacilityId} BatchSize={BatchSize}",
                _facilityId, _batchSize);
        }

        public Task<int> IngestPatientsAsync(CancellationToken ct)
            => IngestByFilterAsync(isPatient: true, ct);

        public Task<int> IngestNonPatientsAsync(CancellationToken ct)
            => IngestByFilterAsync(isPatient: false, ct);

        private async Task<int> IngestByFilterAsync(bool isPatient, CancellationToken ct)
        {
            var tag = isPatient ? "Patient" : "NonPatient";
            _logger.LogDebug("IngestByFilterAsync started. Tag={Tag}, FacilityId={FacilityId}, BatchSize={BatchSize}",
                tag, _facilityId, _batchSize);

            var sw = Stopwatch.StartNew();
            List<FhirStagingRecord> rows;

            try
            {
                var q = _db.FhirStaging
                    .AsTracking()
                    .Where(r => r.Status == StagingStatus.Pending && r.ShipProcessedAt == null);

                q = isPatient
                    ? q.Where(r => r.ResourceType == "Patient")
                    : q.Where(r => r.ResourceType != "Patient");

                rows = await q.OrderBy(r => r.CreatedAt)
                              .Take(_batchSize)
                              .ToListAsync(ct);

                _logger.LogDebug("Fetched {Count} pending {Tag} rows in {ElapsedMs} ms.", rows.Count, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed fetching pending {Tag} rows from RDBMS.", tag);
                throw;
            }

            if (rows.Count == 0)
            {
                _logger.LogDebug("No pending {Tag} rows found. Exiting cycle.", tag);
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

                _logger.LogDebug("Processing RowId={RowId}, ResourceType={ResourceType}, ResourceId={ResourceId}, CreatedAt={CreatedAt}",
                    row.Id, row.ResourceType, row.ResourceId, row.CreatedAt);

                try
                {
                    BsonDocument doc;
                    try
                    {
                        doc = BsonDocument.Parse(row.FhirBundle);
                    }
                    catch (FormatException fex)
                    {
                        _logger.LogError(fex, "Invalid FHIR JSON in fhir_bundle for RowId={RowId}. Preview='{Preview}'",
                            row.Id, SafePreview(row.FhirBundle, 200));
                        row.Status = StagingStatus.Failed;
                        row.UpdatedAt = DateTime.UtcNow;
                        continue;
                    }
                    var canonical = doc.ToJson();
                    var resourceId = doc.TryGetValue("id", out var idVal) && idVal.IsString
                                     ? idVal.AsString
                                     : row.ResourceId;


                    if (isPatient)
                    {
                        var record = new PatientSyncRecord
                        {
                            ResourceId = resourceId,
                            FhirJson = doc,
                            ResourceType = "Patient",
                            Status = "Pending",
                            CreatedDate = DateTime.UtcNow,
                            RetryCount = 0,
                            ExtractSource = "extractor",
                            TransactionId = null,
                            ApiResponsePayload = null,
                            SyncedResourceId = null,
                            FacilityId = _facilityId,
                            StagingId = row.Id,
                            ClientEMRCallbackUrl = row.CallbackUrl,
                            ClientId = row.ClientId,
                            CorrelationId = row.CorrelationId,
                            PayloadHash = Sha256(canonical),                            
                            ShipService  = row.ShipService
                        };

                        await _patientsRepo.InsertAsync(record, ct);
                    }
                    else
                    {
                        var record = new GenericResourceSyncRecord
                        {
                            ResourceId = resourceId,
                            FhirJson = doc,
                            ResourceType = row.ResourceType,
                            Status = "Pending",
                            CreatedDate = DateTime.UtcNow,
                            RetryCount = 0,
                            ExtractSource = "extractor",
                            TransactionId = null,
                            ApiResponsePayload = null,
                            SyncedResourceId = null,
                            FacilityId = _facilityId,
                            StagingId = row.Id,
                            ClientEMRCallbackUrl = row.CallbackUrl,
                            ClientId = row.ClientId,
                            CorrelationId = row.CorrelationId,
                            PayloadHash = Sha256(canonical),
                            ShipService = row.ShipService 
                        };

                        await _genericRepo.InsertAsync(record, ct);
                    }

                    row.Status = StagingStatus.Exported;
                    row.ShipProcessedAt = DateTime.UtcNow;
                    row.UpdatedAt = DateTime.UtcNow;

                    inserted++;

                    _logger.LogDebug("Inserted RowId={RowId} → {Pool} and marked EXPORTED. Took {ElapsedMs} ms.",
                        row.Id, isPatient ? "transformed_pool_patients" : "transformed_pool_resources", perRowSw.ElapsedMilliseconds);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation requested during row processing. RowId={RowId}", row.Id);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert row RowId={RowId} into Mongo. Marking FAILED.", row.Id);
                    row.Status = StagingStatus.Failed;
                    row.UpdatedAt = DateTime.UtcNow;
                }
            }

            try
            {
                await _db.SaveChangesAsync(ct);
                sw.Stop();
                _logger.LogInformation(
                    "{Tag} ingest complete. Processed={Processed}, Inserted={Inserted}, Failed={Failed}, Took={ElapsedMs} ms.",
                    tag, processed, inserted, processed - inserted, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB SaveChanges failed after processing batch. Processed={Processed}, Inserted={Inserted}.",
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
        private static string Sha256(string s)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }
    }

}
