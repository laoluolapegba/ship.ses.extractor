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

                    var stringVal = val.ToString();

                    if (key.StartsWith("telecom["))
                    {
                        contact["telecom"] ??= new JsonArray();
                        var telecomArray = (JsonArray)contact["telecom"]!;
                        int index = int.Parse(key.Split('[', ']')[1]);
                        while (telecomArray.Count <= index)
                            telecomArray.Add(new JsonObject());
                        var entry = telecomArray[index]!;
                        if (entry is JsonObject obj)
                            obj["value"] = JsonValue.Create(stringVal);
                    }
                    else if (key.StartsWith("address."))
                    {
                        contact["address"] ??= new JsonObject();
                        var address = (JsonObject)contact["address"]!;
                        var addressField = key["address.".Length..];
                        address[addressField] = JsonValue.Create(stringVal);
                    }
                    else if (key.StartsWith("name"))
                    {
                        contact["name"] ??= new JsonObject();
                        var name = (JsonObject)contact["name"]!;
                        name["text"] = JsonValue.Create(stringVal);
                    }
                    else if (key.StartsWith("organization."))
                    {
                        contact["organization"] ??= new JsonObject();
                        var org = (JsonObject)contact["organization"]!;
                        var orgField = key["organization.".Length..];
                        org[orgField] = JsonValue.Create(stringVal);
                    }
                    else
                    {
                        // Normalize gender and other enums to lowercase
                        if (key.Equals("gender", StringComparison.OrdinalIgnoreCase))
                            contact[key] = JsonValue.Create(stringVal.ToLowerInvariant());
                        else
                            contact[key] = JsonValue.Create(stringVal);
                    }
                }
            }

            // Apply defaults
            if (field.Defaults != null)
            {
                foreach (var kvp in field.Defaults)
                {
                    var key = kvp.Key;
                    if (key != "organization")
                    {
                        if (key.Equals("gender", StringComparison.OrdinalIgnoreCase) && kvp.Value is string genderStr)
                            contact[key] = JsonValue.Create(genderStr.ToLowerInvariant());
                        else
                            contact[key] = JsonSerializer.SerializeToNode(kvp.Value);
                    }
                }
            }

            // Apply to FHIR object
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
            var identifier = new JsonObject();
            string? selectedField = null;
            object? identifierValue = null;

            // 🔍 Check priority fields in order
            if (field.EmrFieldPriorityMap != null)
            {
                foreach (var (alias, emrField) in field.EmrFieldPriorityMap)
                {
                    if (row.TryGetValue(emrField, out var rawVal) && rawVal != null && !string.IsNullOrWhiteSpace(rawVal.ToString()))
                    {
                        identifierValue = rawVal;
                        selectedField = emrField; // 👈 this should match the key in identifierTypeMap
                        logger.LogInformation("✅ Selected identifier: {Field} = {Value}", emrField, rawVal);
                        break;
                    }
                }

                if (identifierValue == null)
                {
                    logger.LogWarning("❌ No valid identifier found in emrFieldPriorityMap.");
                    return;
                }

                identifier["value"] = JsonValue.Create(identifierValue.ToString());

                // 📦 Add type metadata
                if (field.IdentifierTypeMap != null && field.IdentifierTypeMap.TryGetValue(selectedField!, out var typeMetadata))
                {
                    var coding = new JsonObject();
                    if (typeMetadata.TryGetValue("system", out var system)) coding["system"] = JsonValue.Create(system?.ToString());
                    if (typeMetadata.TryGetValue("code", out var code)) coding["code"] = JsonValue.Create(code?.ToString());
                    if (typeMetadata.TryGetValue("display", out var display)) coding["display"] = JsonValue.Create(display?.ToString());

                    var typeObj = new JsonObject
                    {
                        ["coding"] = new JsonArray { coding }
                    };

                    if (typeMetadata.TryGetValue("text", out var text))
                        typeObj["text"] = JsonValue.Create(text?.ToString());

                    identifier["type"] = typeObj;
                }
                else
                {
                    logger.LogWarning("⚠️ No identifier type metadata found for selected field '{SelectedField}'", selectedField);
                }
            }

            // 🛠 Apply any default values
            if (field.Defaults != null)
            {
                if (field.Defaults.TryGetValue("use", out var use))
                    identifier["use"] = JsonValue.Create(use?.ToString());
                if (field.Defaults.TryGetValue("system", out var system))
                    identifier["system"] = JsonValue.Create(system?.ToString());
            }

            // ✅ Inject into FHIR JSON
            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, identifier, logger);
        }


        public static void ApplyContactPoint(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.FhirPath))
            {
                logger.LogWarning("ApplyContactPoint skipped: field or FhirPath is null.");
                return;
            }

            var contactPoint = new JsonObject();

            // Extract 'value'
            if (field.EmrFieldMap?.TryGetValue("value", out var valueField) == true &&
                row.TryGetValue(valueField, out var value) && value != null)
            {
                contactPoint["value"] = JsonValue.Create(value.ToString());
            }

            // Apply 'system' and 'use' from defaults
            if (field.Defaults != null)
            {
                if (field.Defaults.TryGetValue("system", out var system) && system != null)
                    contactPoint["system"] = JsonValue.Create(system.ToString().ToLowerInvariant());

                if (field.Defaults.TryGetValue("use", out var use) && use != null)
                    contactPoint["use"] = JsonValue.Create(use.ToString().ToLowerInvariant());
            }

            // Set telecom under contact
            FhirJsonHelper.SetFhirValue(fhir, field.FhirPath, contactPoint, logger);

        }



        public static void ApplyContactPoint1(JsonObject fhir, FieldMapping field, IDictionary<string, object> row, ILogger logger)
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
                    var key = kvp.Key.ToLowerInvariant(); // Normalize ALL keys to lowercase
                    reference[key] = JsonValue.Create(kvp.Value?.ToString());
                }
            }
            else if (field.Template == "reference" &&
                     field.FhirPath == "managingOrganization" &&
                     _envDefaults?.ManagingOrganization is not null)
            {
                logger.LogInformation("ℹ️ Using managingOrganization from environment defaults.");
                reference = JsonSerializer.SerializeToNode(_envDefaults.ManagingOrganization) as JsonObject;

                if (reference != null)
                {
                    // Normalize keys: Reference → reference, Display → display
                    NormalizeKey(reference, "Reference", "reference");
                    NormalizeKey(reference, "Display", "display");
                }
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

        private static void NormalizeKey(JsonObject obj, string oldKey, string newKey)
        {
            if (obj.TryGetPropertyValue(oldKey, out var val))
            {
                obj.Remove(oldKey);
                obj[newKey] = val;
            }
        }


    }
}
