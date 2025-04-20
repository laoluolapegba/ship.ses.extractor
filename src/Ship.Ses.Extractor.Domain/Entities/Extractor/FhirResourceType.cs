using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Entities.Extractor
{
    public class FhirResourceType
    {
        public int Id { get; set; }
        public string Name { get; private set; }
        public string Structure { get; private set; }
        public bool IsActive { get; private set; }

        private FhirResourceType() { } // For EF Core

        public FhirResourceType(string name, string structure, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Resource type name cannot be empty", nameof(name));

            Name = name;
            Structure = structure ?? throw new ArgumentNullException(nameof(structure));
            IsActive = isActive;
        }

        public void UpdateStructure(string structure)
        {
            Structure = structure ?? throw new ArgumentNullException(nameof(structure));
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
