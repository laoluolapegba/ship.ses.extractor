using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services
{
    public interface ITableMappingService
    {
        Task<TableMapping> GetMappingForResourceAsync(string resourceType, CancellationToken cancellationToken = default);
    }
    //public interface ITableMappingService
    //{
    //    object GetRawMappingForResource(string resourceType);

    //    TableMapping<TField> GetTypedMappingForResource<TField>(string resourceType) where TField : FieldMapping;

    //    Task<TableMapping<TField>> GetTypedMappingForResourceAsync<TField>(string resourceType, CancellationToken cancellationToken = default) where TField : FieldMapping;
    //}

}
