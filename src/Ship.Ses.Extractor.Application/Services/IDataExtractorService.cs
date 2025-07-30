using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services
{
    public interface IDataExtractorService
    {
        Task<IEnumerable<IDictionary<string, object>>> ExtractAsync(TableMapping mapping, CancellationToken cancellationToken);
    }
    //public interface IDataExtractorService
    //{
    //    Task<IEnumerable<IDictionary<string, object>>> ExtractAsync<TField>(TableMapping<TField> mapping, CancellationToken cancellationToken) where TField : FieldMapping;
    //}
}
