using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Entities.Extractor
{

    [Table("ship_fhir_resources")]
    public sealed class FhirStagingRecord
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("source_table_token"), MaxLength(255)]
        public string SourceTableToken { get; set; }

        [Column("resource_type"), MaxLength(255)]
        public string ResourceType { get; set; }

        [Column("resource_id"), MaxLength(255)]
        public string ResourceId { get; set; }

        [Column("fhir_bundle")]
        public string FhirBundle { get; set; }

        [Column("ship_id"), MaxLength(255)]
        public string ShipId { get; set; }

        [Column("ship_processed_at")]
        public DateTime? ShipProcessedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("status"), MaxLength(32)]
        public string Status { get; set; }  // PENDING → IN_PROGRESS → EXPORTED → SUBMITTED → ACKED / FAILED
    }

}
