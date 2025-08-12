using Microsoft.EntityFrameworkCore;
using Ship.Ses.Extractor.Application.Contracts;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;

namespace Ship.Ses.Extractor.Infrastructure.Persistance.MySql
{


    //public sealed class FhirStagingRepository : IFhirStagingRepository
    //{
    //    private readonly ExtractorDbContext _db;

    //    public FhirStagingRepository(ExtractorDbContext db) => _db = db;

    //    public async Task<IReadOnlyList<FhirStagingRecord>> DequeueBatchAsync(int batchSize, CancellationToken ct)
    //    {
    //        // Strategy:
    //        // 1) Select unprocessed rows with FOR UPDATE SKIP LOCKED to avoid race under concurrency.
    //        // 2) Mark a transient "in-progress" status (if exists) to be explicit. If no status column, rely on row lock.
    //        //
    //        // Note: Pomelo supports raw SQL; EF Core does not (yet) expose SKIP LOCKED via LINQ. Use a transaction + raw SQL IDs,
    //        // then load the rows normally.

    //        await using var tx = await _db.Database.BeginTransactionAsync(ct);

    //        // Adjust WHERE depending on your schema:
    //        //  - If you have Status: WHERE Status = 'PENDING'
    //        //  - Else: WHERE ship_processed_at IS NULL
    //        var ids = await _db
    //            .FhirStaging
    //            .FromSqlRaw(@"
    //            SELECT *
    //            FROM fhir_staging
    //            WHERE (Status IS NULL OR Status = 'PENDING') 
    //              AND ship_processed_at IS NULL
    //            FOR UPDATE SKIP LOCKED
    //            LIMIT {0}", batchSize)
    //            .Select(r => r.Id)
    //            .ToListAsync(ct);

    //        var rows = await _db.FhirStaging
    //            .Where(r => ids.Contains(r.Id))
    //            .ToListAsync(ct);

    //        // If you have a Status column, set IN_PROGRESS to provide visibility
    //        foreach (var r in rows)
    //        {
    //            r.Status = "IN_PROGRESS";
    //            r.UpdatedAt = DateTime.UtcNow;
    //        }

    //        await _db.SaveChangesAsync(ct);
    //        await tx.CommitAsync(ct);
    //        return rows;
    //    }

    //    public async Task MarkProcessedAsync(long id, CancellationToken ct)
    //    {
    //        var row = await _db.FhirStaging.FirstOrDefaultAsync(x => x.Id == id, ct);
    //        if (row == null) return;
    //        row.ShipProcessedAt = DateTime.UtcNow;
    //        row.Status = "PROCESSED"; // if column exists; safe to set anyway
    //        row.UpdatedAt = DateTime.UtcNow;
    //        await _db.SaveChangesAsync(ct);
    //    }

    //    public async Task MarkFailedAsync(long id, string reason, CancellationToken ct)
    //    {
    //        var row = await _db.FhirStaging.FirstOrDefaultAsync(x => x.Id == id, ct);
    //        if (row == null) return;
    //        row.Status = "FAILED"; // if you have an error column, set it here
    //        row.UpdatedAt = DateTime.UtcNow;
    //        await _db.SaveChangesAsync(ct);
    //    }
    //}

}
