using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using System.Text.Json.Nodes;
namespace Ship.Ses.Extractor.Application.Services.Transformers
{


    public class EncounterTransformer : IResourceTransformer<JsonObject>
    {
        private readonly ILogger<EncounterTransformer> _logger;

        public EncounterTransformer(ILogger<EncounterTransformer> logger)
        {
            _logger = logger;
        }

        public JsonObject Transform(IDictionary<string, object> row, TableMapping mapping, List<string> errors)
        {
            var fhir = new JsonObject
            {
                ["resourceType"] = mapping.ResourceType
            };

            foreach (var field in mapping.FieldsUntyped)
            {
                if (!string.IsNullOrWhiteSpace(field.Template))
                {
                    switch (field.Template)
                    {
                        case "codeableConcept":
                            TemplateBuilders.ApplyCodeableConcept(fhir, field, row, _logger);
                            break;
                        case "reference":
                            TemplateBuilders.ApplyReference(fhir, field, row, _logger);
                            break;
                        case "period":
                            TemplateBuilders.ApplyPeriod(fhir, field, row, _logger);
                            break;
                        case "participant":
                            TemplateBuilders.ApplyParticipant(fhir, field, row, _logger);
                            break;
                        case "diagnosis":
                            TemplateBuilders.ApplyDiagnosis(fhir, field, row, _logger);
                            break;
                        default:
                            _logger.LogWarning("Unknown template '{Template}' at {FhirPath}", field.Template, field.FhirPath);
                            break;
                    }
                    continue;
                }

                object? value = null;

                if (field.EmrField == "__empty__")
                {
                    value = string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(field.EmrField) &&
                         row.TryGetValue(field.EmrField, out var rawVal))
                {
                    value = rawVal;
                }

                if (value == null)
                {
                    if (field.Required)
                    {
                        _logger.LogWarning("⚠️ Required field '{FhirPath}' missing from EMR row.", field.FhirPath);
                        errors.Add($"Missing required field: {field.FhirPath} (EMR: {field.EmrField})");
                    }
                    continue;
                }

                value = ConvertField(value, field.DataType, field.Format);
                FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, JsonValue.Create(value), _logger);
            }

            return fhir;
        }

        public JsonObject NormalizeEnumFields(JsonObject resource)
        {
            var enumFields = new[] { "status", "use", "class" };

            foreach (var field in enumFields)
            {
                if (resource.TryGetPropertyValue(field, out var node) &&
                    node is JsonValue value &&
                    value.TryGetValue<string>(out var str))
                {
                    resource[field] = JsonValue.Create(str.ToLowerInvariant());
                }
            }

            return resource;
        }

        private object ConvertField(object value, string? type, string? format)
        {
            try
            {
                return type switch
                {
                    "date" when value is DateTime dt => dt.ToString(format ?? "yyyy-MM-dd"),
                    "date" => DateTime.Parse(value.ToString()!).ToString(format ?? "yyyy-MM-dd"),
                    "datetime" => DateTime.Parse(value.ToString()!).ToString(format ?? "yyyy-MM-ddTHH:mm:ss.fffZ"),
                    _ => value.ToString()
                };
            }
            catch
            {
                return value.ToString();
            }
        }
    }


}
