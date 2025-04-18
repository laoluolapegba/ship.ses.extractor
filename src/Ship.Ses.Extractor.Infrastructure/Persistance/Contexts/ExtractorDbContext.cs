using Microsoft.EntityFrameworkCore;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Infrastructure.Persistance.Contexts
{
    public class ExtractorDbContext : DbContext
    {
        public DbSet<DataSource> DataSources { get; set; }

        public ExtractorDbContext(DbContextOptions<ExtractorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataSource>().ToTable("data_sources"); // adjust table name
            base.OnModelCreating(modelBuilder);
        }
    }

}
