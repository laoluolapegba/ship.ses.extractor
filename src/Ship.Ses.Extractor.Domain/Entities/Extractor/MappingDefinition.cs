using Ship.Ses.Extractor.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Entities.Extractor
{
    public class MappingDefinition
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int FhirResourceTypeId { get; private set; }
        public FhirResourceType FhirResourceType { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime LastModifiedDate { get; private set; }
        public bool IsActive { get; private set; }

        private readonly List<ColumnMapping> _columnMappings = new();
        public IReadOnlyCollection<ColumnMapping> ColumnMappings => _columnMappings.AsReadOnly();

        private MappingDefinition() { } // For EF Core

        public MappingDefinition(string name, string description, FhirResourceType fhirResourceType)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            FhirResourceType = fhirResourceType ?? throw new ArgumentNullException(nameof(fhirResourceType));
            FhirResourceTypeId = fhirResourceType.Id;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = CreatedDate;
            IsActive = true;
        }

        public void AddMapping(ColumnMapping mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            _columnMappings.Add(mapping);
            LastModifiedDate = DateTime.UtcNow;
        }

        public void RemoveMapping(string emrTable, string emrColumn)
        {
            var mapping = _columnMappings.Find(m => m.EmrTable == emrTable && m.EmrColumn == emrColumn);
            if (mapping != null)
            {
                _columnMappings.Remove(mapping);
                LastModifiedDate = DateTime.UtcNow;
            }
        }

        public void ClearMappings()
        {
            _columnMappings.Clear();
            LastModifiedDate = DateTime.UtcNow;
        }

        public void SetMappings(IEnumerable<ColumnMapping> mappings)
        {
            _columnMappings.Clear();
            _columnMappings.AddRange(mappings);
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Update(string name, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
