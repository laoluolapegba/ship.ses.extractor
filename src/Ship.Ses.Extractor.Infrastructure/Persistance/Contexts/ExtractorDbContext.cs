using Microsoft.EntityFrameworkCore;
using Ship.Ses.Extractor.Domain.Entities.Extractor;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Persistance.Contexts
{
    public class ExtractorDbContext : DbContext
    {
        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<MappingDefinition> Mappings { get; set; }
        public DbSet<FhirResourceType> FhirResourceTypes { get; set; }
        public ExtractorDbContext(DbContextOptions<ExtractorDbContext> options)
            : base(options)
        {
        }
        public DbSet<SyncTracking> SyncTracking { get; set; }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<DataSource>().ToTable("data_sources"); // adjust table name
        //    base.OnModelCreating(modelBuilder);
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MappingDefinition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.LastModifiedDate).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();

                // Store the list of column mappings as JSON
                entity.Property<string>("ColumnMappingsJson")
                    .HasColumnName("ColumnMappings")
                    .HasColumnType("nvarchar(max)");

                // Configure relationship to FhirResourceType
                entity.HasOne(e => e.FhirResourceType)
                    .WithMany()
                    .HasForeignKey(e => e.FhirResourceTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FhirResourceType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Structure).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.IsActive).IsRequired();
            });
        }

        public override int SaveChanges()
        {
            ProcessColumnMappings();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ProcessColumnMappings();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ProcessColumnMappings()
        {
            foreach (var entry in ChangeTracker.Entries<MappingDefinition>().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                var entity = entry.Entity;
                if (entity.ColumnMappings != null)
                {
                    entry.Property("ColumnMappingsJson").CurrentValue = JsonSerializer.Serialize(entity.ColumnMappings);
                }
            }
        }
    }
}


