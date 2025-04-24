using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Application.Services.Transformers
{
    public static class FhirJsonHelper
    {
        public static void ApplyConstants(JsonNode root, Dictionary<string, JsonNode> constants, ILogger logger = null)
        {
            foreach (var kvp in constants)
            {
                SetFhirValue(root, kvp.Key, kvp.Value, logger);
            }
        }
        public static void SetFhirValue(JsonNode root, string fhirPath, JsonNode value, ILogger logger = null)
        {
            var parts = fhirPath
                .Replace("]", "")
                .Split(new[] { '[', '.' }, StringSplitOptions.RemoveEmptyEntries);

            JsonNode current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                var isLast = i == parts.Length - 1;
                var part = parts[i];

                if (int.TryParse(part, out var index))
                {
                    if (current is JsonArray array)
                    {
                        while (array.Count <= index)
                            array.Add(new JsonObject());

                        if (isLast)
                        {
                            array[index] = value;
                            logger?.LogInformation("📌 Applied constant to {FhirPath}", fhirPath);
                        }
                        else
                            current = array[index];
                    }
                    else
                    {
                        throw new InvalidOperationException("Expected array in path.");
                    }
                }
                else
                {
                    if (current[part] == null)
                    {
                        current[part] = isLast ? value : new JsonObject();
                    }
                    else if (isLast)
                    {
                        current[part] = value;
                        logger?.LogInformation("📌 Applied constant to {FhirPath}", fhirPath);
                    }

                    current = current[part];
                }
            }
        }
    }

}
