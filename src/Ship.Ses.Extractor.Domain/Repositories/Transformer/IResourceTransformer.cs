using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Domain.Repositories.Transformer
{
    public interface IResourceTransformer<T> 
    {
        T Transform(IDictionary<string, object> row, TableMapping mapping, List<string> errors);
        JsonObject NormalizeEnumFields(JsonObject json);


    }
    //public interface IResourceTransformer<TField>
    //where TField : FieldMapping
    //{
    //    JsonObject Transform(IDictionary<string, object> row, TableMapping<TField> mapping, List<string> errors);
    //    JsonObject NormalizeEnumFields(JsonObject json);
    //}
}
