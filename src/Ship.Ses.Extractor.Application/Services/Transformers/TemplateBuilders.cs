using Microsoft.Extensions.Logging;
using Ship.Ses.Extractor.Domain.Models.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.Transformers
{
    
    public static class TemplateBuilders
    {
        private static EnvironmentDefaults? _envDefaults;

        public static void ConfigureDefaults(EnvironmentDefaults defaults)
        {
            _envDefaults = defaults;
        }
        public static void ApplyHumanName(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            logger.LogInformation("🔧 Applying HumanName template to {FhirPath}", field.FhirPath);
            var name = new JsonObject();

            if (field.EmrFieldMap != null)
            {
                if (field.EmrFieldMap.TryGetValue("given", out var givenField) &&
                    row.TryGetValue(givenField, out var givenVal) && givenVal != null)
                {
                    name["given"] = new JsonArray { JsonValue.Create(givenVal.ToString()) };
                }

                if (field.EmrFieldMap.TryGetValue("family", out var familyField) &&
                    row.TryGetValue(familyField, out var familyVal) && familyVal != null)
                {
                    name["family"] = JsonValue.Create(familyVal.ToString());
                }

                if (field.EmrFieldMap.TryGetValue("prefix", out var prefixField) &&
                    row.TryGetValue(prefixField, out var prefixVal) && prefixVal != null)
                {
                    name["prefix"] = new JsonArray { JsonValue.Create(prefixVal.ToString()) };
                }
            }

            if (field.Defaults != null && field.Defaults.TryGetValue("use", out var use))
            {
                name["use"] = JsonValue.Create(use.ToString());
            }

            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, name, logger);
            // TODO: Build HumanName structure from emrFieldMap
        }

        public static void ApplyContact(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            var contact = new JsonObject();

            if (field.EmrFieldMap != null)
            {
                foreach (var kvp in field.EmrFieldMap)
                {
                    var key = kvp.Key;
                    var sourceField = kvp.Value;

                    if (!row.TryGetValue(sourceField, out var val) || val == null)
                        continue;

                    if (key.StartsWith("telecom["))
                    {
                        contact["telecom"] ??= new JsonArray();
                        var telecomArray = (JsonArray)contact["telecom"]!;
                        int index = int.Parse(key.Split('[', ']')[1]);
                        while (telecomArray.Count <= index)
                            telecomArray.Add(new JsonObject());
                        var entry = telecomArray[index]!;
                        if (entry is JsonObject obj)
                            obj["value"] = JsonValue.Create(val.ToString());
                    }
                    else if (key.StartsWith("address."))
                    {
                        contact["address"] ??= new JsonObject();
                        var address = (JsonObject)contact["address"]!;
                        var addressField = key["address.".Length..];
                        address[addressField] = JsonValue.Create(val.ToString());
                    }
                    else if (key.StartsWith("name"))
                    {
                        contact["name"] ??= new JsonObject();
                        var name = (JsonObject)contact["name"]!;
                        name["text"] = JsonValue.Create(val.ToString());
                    }
                    else if (key.StartsWith("organization."))
                    {
                        contact["organization"] ??= new JsonObject();
                        var org = (JsonObject)contact["organization"]!;
                        var orgField = key["organization.".Length..];
                        org[orgField] = JsonValue.Create(val.ToString());
                    }
                    else
                    {
                        contact[key] = JsonValue.Create(val.ToString());
                    }
                }
            }

            if (field.Defaults != null)
            {
                foreach (var kvp in field.Defaults)
                {
                    if (kvp.Key != "organization")
                    {
                        contact[kvp.Key] = JsonSerializer.SerializeToNode(kvp.Value);
                    }
                }
            }

            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, contact, logger);
        }

        public static void ApplyAddress(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            logger.LogInformation("🔧 Applying Address template to {FhirPath}", field.FhirPath);
            var address = new JsonObject();

            if (field.EmrFieldMap != null)
            {
                foreach (var kvp in field.EmrFieldMap)
                {
                    if (row.TryGetValue(kvp.Value, out var val) && val != null)
                    {
                        if (kvp.Key.StartsWith("line["))
                        {
                            address["line"] ??= new JsonArray();
                            var lineArray = (JsonArray)address["line"]!;
                            lineArray.Add(JsonValue.Create(val.ToString()));
                        }
                        else
                        {
                            address[kvp.Key] = JsonValue.Create(val.ToString());
                        }
                    }
                }
            }

            if (field.Defaults != null)
            {
                foreach (var kvp in field.Defaults)
                {
                    if (kvp.Key == "line" && kvp.Value is JsonArray defaultLines)
                    {
                        address["line"] ??= new JsonArray();
                        var lineArray = (JsonArray)address["line"]!;
                        foreach (var l in defaultLines)
                        {
                            lineArray.Add(JsonValue.Create(l?.ToString()));
                        }
                    }
                    else
                    {
                        address[kvp.Key] = JsonValue.Create(kvp.Value.ToString());
                    }
                }
            }

            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, address, logger);
            // TODO: Build Address structure
        }

        public static void ApplyCodeableConcept(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(field.EmrField))
            {
                logger.LogWarning("⚠️ Missing emrField for codeableConcept at {FhirPath}", field.FhirPath);
                return;
            }

            if (field.ValueSet == null)
            {
                logger.LogWarning("⚠️ Missing valueSet for codeableConcept at {FhirPath}", field.FhirPath);
                return;
            }

            var code = row.TryGetValue(field.EmrField, out var codeVal) ? codeVal?.ToString() : null;
            logger.LogInformation("🔍 Extracted code '{Code}' from EMR field '{EmrField}'", code, field.EmrField);

            if (string.IsNullOrWhiteSpace(code))
            {
                logger.LogInformation("ℹ️ No code value found for {FhirPath}; skipping.", field.FhirPath);
                return;
            }

            string system = field.ValueSet.TryGetValue("system", out var systemObj) ? systemObj?.ToString() ?? "" : "";
            logger.LogInformation("🔗 Using system: {System}", system);

            string display = code;

            if (field.ValueSet.TryGetValue("displayMap", out var displayMapObj) &&
                displayMapObj is JsonElement elem &&
                elem.ValueKind == JsonValueKind.Object &&
                elem.TryGetProperty(code, out var displayNode))
            {
                display = displayNode.GetString() ?? code;
                logger.LogInformation("✅ Found display '{Display}' for code '{Code}'", display, code);
            }
            else
            {
                logger.LogWarning("❌ No display mapping found for code '{Code}' — using code as fallback.", code);
            }

            var concept = new JsonObject
            {
                ["coding"] = new JsonArray
            {
                new JsonObject
                {
                    ["system"] = system,
                    ["code"] = code,
                    ["display"] = display
                }
            },
                ["text"] = display
            };

            logger.LogInformation("📦 Final CodeableConcept: system={System}, code={Code}, display={Display}", system, code, display);

            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, concept, logger);
        }

        public static void ApplyIdentifier(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            logger.LogInformation("🔧 Applying Identifier template to {FhirPath}", field.FhirPath);
            var identifier = new JsonObject();

            // Extract 'value' from EMR row
            if (field.EmrFieldMap != null &&
                field.EmrFieldMap.TryGetValue("value", out var valueField) &&
                row.TryGetValue(valueField, out var value) && value != null)
            {
                identifier["value"] = JsonValue.Create(value.ToString());
            }
            else
            {
                logger.LogWarning("⚠️ No identifier 'value' found in EMR field map for {FhirPath}", field.FhirPath);
            }

            // Handle defaults
            if (field.Defaults != null)
            {
                foreach (var kvp in field.Defaults)
                {
                    if (kvp.Key == "type")
                    {
                        // Deserialize structured type object
                        identifier["type"] = JsonSerializer.SerializeToNode(kvp.Value);
                    }
                    else
                    {
                        identifier[kvp.Key] = JsonValue.Create(kvp.Value?.ToString());
                    }
                }
            }

            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, identifier, logger);
        }



        public static void ApplyContactPoint(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            var contactPoint = new JsonObject();

            if (field.EmrFieldMap != null && field.EmrFieldMap.TryGetValue("value", out var valueField) &&
                row.TryGetValue(valueField, out var value) && value != null)
            {
                contactPoint["value"] = JsonValue.Create(value.ToString());
            }

            if (field.Defaults != null)
            {
                if (field.Defaults.TryGetValue("system", out var system))
                    contactPoint["system"] = JsonValue.Create(system.ToString());

                if (field.Defaults.TryGetValue("use", out var use))
                    contactPoint["use"] = JsonValue.Create(use.ToString());
            }

            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, contactPoint, logger);
        }
        public static void ApplyReference(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            JsonObject? reference = null;

            if (field.Defaults != null)
            {
                reference = new JsonObject();
                foreach (var kvp in field.Defaults)
                {
                    reference[kvp.Key] = JsonValue.Create(kvp.Value?.ToString());
                }
            }
            else if (field.Template == "reference" && field.FhirPath == "managingOrganization" && _envDefaults?.ManagingOrganization != null)
            {
                logger.LogInformation("ℹ️ Using managingOrganization from environment defaults.");
                reference = JsonSerializer.SerializeToNode(_envDefaults.ManagingOrganization) as JsonObject;
            }

            if (reference != null)
            {
                FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, reference, logger);
            }
            else
            {
                logger.LogWarning("⚠️ No reference value found for {FhirPath}", field.FhirPath);
            }
        }
    }
}
