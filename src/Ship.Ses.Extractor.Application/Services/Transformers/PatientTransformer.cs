using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.Transformers
{
    public class PatientTransformer : IResourceTransformer<JsonObject>
    {
        private readonly ILogger<PatientTransformer> _logger;

        public PatientTransformer(ILogger<PatientTransformer> logger)
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
                if (!string.IsNullOrWhiteSpace(field.Template))
                {
                    switch (field.Template)
                    {
                        case "humanName":
                            TemplateBuilders.ApplyHumanName(fhir, field, row, _logger);
                            break;
                        case "contactPoint":
                            TemplateBuilders.ApplyContactPoint(fhir, field, row, _logger);
                            break;
                        case "address":
                            TemplateBuilders.ApplyAddress(fhir, field, row, _logger);
                            break;
                        case "codeableConcept":
                            TemplateBuilders.ApplyCodeableConcept(fhir, field, row, _logger);
                            break;
                        case "identifier":
                            TemplateBuilders.ApplyIdentifier(fhir, field, row, _logger);
                            break;
                        case "contact":
                            TemplateBuilders.ApplyContact(fhir, field, row, _logger);
                            break;
                        case "reference":
                            TemplateBuilders.ApplyReference(fhir, field, row, _logger);
                            break;
                        default:
                            _logger.LogWarning("Unknown template type '{Template}' for path {FhirPath}", field.Template, field.FhirPath);
                            break;
                    }

                    continue;
                }

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
                        _logger.LogWarning("⚠️ Required field '{FhirPath}' missing from EMR row. Inserting empty string.", field.FhirPath);
                        value = string.Empty;
                    }
                    else
                    {
                        continue; // Optional field missing - skip
                    }
                }

                value = ConvertField(value, field.DataType, field.Format);
                FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, JsonValue.Create(value), _logger);
            }

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
                    "datetime" => DateTime.Parse(value.ToString()!).ToString(format ?? "yyyy-MM-ddTHH:mm:ss.fffZ"),
                    "bool" => bool.TryParse(value.ToString(), out var b) ? !b : false,
                    _ => value.ToString()
                };
            }
            catch
            {
                return value.ToString(); // fallback
            }
        }
    }
}
