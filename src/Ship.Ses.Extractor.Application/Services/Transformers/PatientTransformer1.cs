using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using System.Text.Json.Nodes;

namespace Ship.Ses.Extractor.Application.Services.Transformers
{    
    public class PatientTransformer1 : IResourceTransformer<JsonObject>
    {
            private readonly ILogger<PatientTransformer1> _logger;

            public PatientTransformer1(ILogger<PatientTransformer1> logger)
            {
                _logger = logger;
            }
            public JsonObject Transform(IDictionary<string, object> row, TableMapping mapping)
            {
                var fhir = new JsonObject
                {
                    ["resourceType"] = mapping.ResourceType
                };               

                    foreach (var field in mapping.Fields)
                    {
                    object? value = null;

                    if (field.EmrField == "__empty__")
                    {
                        value = string.Empty;
                    }
                    else if (!string.IsNullOrWhiteSpace(field.EmrField) && row.TryGetValue(field.EmrField, out var rawVal))
                    {
                        value = rawVal;
                    }

                    if (value == null)
                    {
                        if (field.Required)
                        {
                            _logger?.LogWarning("⚠️ Required field '{FhirPath}' missing from EMR row. Inserting empty string.", field.FhirPath);
                            value = string.Empty;
                        }
                        else
                        {
                            continue; // Optional field missing - skip
                        }
                    }

                    value = ConvertField(value, field.DataType, field.Format);

                    var jsonValue = JsonValue.Create(value);
                    FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, jsonValue, _logger);
                }

                FhirJsonHelper.ApplyConstants(fhir, mapping.Constants, _logger);

                return fhir;
            }

            private object ConvertField(object value, string? type, string? format)
            {
                try
                {
                    return type switch
                    {
                        "date" when value is DateTime dt => dt.ToString(format ?? "yyyy-MM-dd"),
                        "date" => DateTime.Parse(value.ToString()!).ToString(format ?? "yyyy-MM-dd"),
                        _ => value.ToString()
                    };
                }
                catch
                {
                    return value.ToString(); // fallback
                }
            }
            public JsonObject Transform1(IDictionary<string, object> row, TableMapping mapping)
            {
                var fhir = new JsonObject
                {
                    ["resourceType"] = mapping.ResourceType
                };

                foreach (var field in mapping.Fields)
                {
                    var value = row.TryGetValue(field.EmrField, out var rawVal) ? rawVal : null;

                    if (value == null && field.Default != null)
                        value = field.Default;

                    if (value == null) continue;

                    value = ConvertField(value, field.DataType, field.Format);

                    SetFhirValue(fhir, field.FhirPath, value);
                }

                return fhir;
        }

        private object ConvertField1(object value, string? type, string? format)
        {
            try
            {
                return type switch
                {
                    "date" when value is DateTime dt => dt.ToString(format ?? "yyyy-MM-dd"),
                    "date" => DateTime.Parse(value.ToString()!).ToString(format ?? "yyyy-MM-dd"),
                    _ => value.ToString()
                };
            }
            catch
            {
                return value.ToString(); // fallback
            }
        }

        private void SetFhirValue(JsonObject obj, string fhirPath, object value)
        {
            // NOTE: You may want to integrate a FHIRPath-aware library or build a simple dotted-path setter
            // For now, do a minimal dot path handling:
            var parts = fhirPath.Split('.');

            JsonNode? current = obj;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (part.Contains('[')) // array handling, e.g., name[0].given[0]
                {
                    var name = part.Substring(0, part.IndexOf('['));
                    var index = int.Parse(part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1));

                    if (!current.AsObject().TryGetPropertyValue(name, out var arrNode) || arrNode is not JsonArray array)
                    {
                        array = new JsonArray();
                        current[name] = array;
                    }

                    while (array.Count <= index)
                    {
                        array.Add(new JsonObject());
                    }

                    current = array[index];
                }
                else
                {
                    if (i == parts.Length - 1)
                    {
                        current[part] = JsonValue.Create(value);
                    }
                    else
                    {
                        if (!current.AsObject().TryGetPropertyValue(part, out var nextNode) || nextNode is not JsonObject)
                        {
                            nextNode = new JsonObject();
                            current[part] = nextNode;
                        }
                        current = nextNode;
                    }
                }
            }
        }

    }

}
